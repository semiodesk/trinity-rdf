# 0040. Numeric values are widened, never narrowed, when read into a mapped property

Date: 2026-08-11

## Status
Accepted (2.0).

## Context
A consumer on 1.0.3.77 + Virtuoso reported that a mapped `decimal?` property threw
`InvalidCastException` on **every** read once a whole-number value had been written. The write
succeeded, so a single value made every later lookup of that type fail. It did not reproduce against the
in-memory store, which is why it reached production.

Reproduced in this repository with **no store at all**, by driving `Resource.AddPropertyToMapping`
directly:

```
Invalid cast from 'System.Int32' to 'System.Nullable`1[System.Decimal]'
```

The chain: `XsdTypeMapper` deserializes `xsd:integer` to `Int32`; the mapping gate
(`Resource.GetPropertyMapping` → `IPropertyMapping.IsTypeCompatible`) accepted it, because
`IsPrecisionCompatible` matched none of its guards for `Nullable<decimal>` and fell through to an
unconditional `return true`; and the setter then called
`Convert.ChangeType(value, typeof(Nullable<decimal>))`, which **cannot target `Nullable<T>`** —
`Type.GetTypeCode` returns `Object` — and throws.

**The store is within its rights.** `"400"` is a valid lexical form for `xsd:decimal` (only the
*canonical* form requires `400.0`) and the value space of `xsd:decimal` contains the integers. Non-Trinity
writers exist regardless — bulk TTL load, direct SPARQL update, archive restore — so the read path has to
tolerate this. Forcing a fraction digit on serialization was rejected: it fights a canonicalization the
store may perform, can be re-normalized anyway, and leaves every other writer producing values Trinity
cannot read.

Measurement corrected two initial assumptions worth recording:

- **Nullability was the trigger, not `decimal`.** `Int32` into a non-nullable `decimal` always worked, via
  `Convert.ChangeType` to a non-nullable target.
- **The exact-underlying-type case always worked**, because `typeof(decimal?).IsAssignableFrom(typeof(decimal))`
  is `true`, so it takes the fast path with no conversion. That is precisely why the in-memory store never
  reproduced it: dotNetRDF preserves `xsd:decimal`, so a `decimal` arrives and lands directly. The defect
  needed *a nullable property* **and** *a widening conversion*.

## Decision

`Trinity/NumericConversion.cs` is the single authority for "can this numeric value land in this property".
`IsTypeCompatible` and `SetOrAddMappedValue` both use it, so the gate and the conversion cannot disagree —
a conversion accepted by one and refused by the other would only move the failure. Both unwrap
`Nullable<T>` first, which is what fixes the whole nullable family at once. `Convert.ChangeType` is gone
from both call sites; it survives only *inside* `NumericConversion`, after the allowlist has already
decided the conversion is lossless and the target has been unwrapped.

**Widening only.** Accepted, plus identity:

| From | To |
|---|---|
| `SByte` | `Int16`, `Int32`, `Int64`, `Decimal` |
| `Byte` | `Int16`, `UInt16`, `Int32`, `UInt32`, `Int64`, `UInt64`, `Decimal` |
| `Int16` | `Int32`, `Int64`, `Decimal` |
| `UInt16` | `Int32`, `UInt32`, `Int64`, `UInt64`, `Decimal` |
| `Int32` | `Int64`, `Double`, `Decimal` |
| `UInt32` | `Int64`, `UInt64`, `Double`, `Decimal` |
| `Int64` | `Decimal` |
| `UInt64` | `Decimal` |
| `Single` | `Double` |

**Refused, deliberately:**

- All narrowing — `Decimal`→`Int32`, `Int64`→`Int16`, `Double`→`Single`. The previous behaviour silently
  rounded `3.7m` to `4` for an `int` property, which is a defect in its own right.
- `Double`/`Single`→`Decimal` and `Decimal`→ any floating type: different value spaces, lossy both ways.
- Signed→unsigned (`Int32`→`UInt32`, `Int64`→`UInt64`): the negative range has nowhere to go.
- `Int64`→`Double`, `UInt64`→`Double`, `Int32`→`Single`. **C# permits these implicitly; we do not.** They
  lose precision above 2^53 and 2^24. This is stricter than the language on purpose, because silent
  precision loss on large integers is the failure class this ADR exists to prevent. `Int32`→`Double` and
  `UInt32`→`Double` are exact and stay allowed.
- Anything non-numeric, and any string parsing.

`PropertyMapping<T>.IsPrecisionCompatible` is **removed** — unused after this change, and wrong.
`IsNumericType` is kept: it is correct for what it says and does not unwrap `Nullable<T>`, which callers
may rely on.

## Consequences
- Every nullable numeric mapped property becomes readable when a store returns a different numeric type.
- A refused conversion does **not** throw. It fails the mapping gate, so the value lands among the
  unmapped values and the typed property reads as unset. This is deliberate: resources are open
  ([0017](0017-resources-open-mapped-and-dynamic.md)), so an unmapped value is a normal place for it to live, and
  throwing would make one unexpected literal render the whole resource unreadable — the failure being
  removed here. The value is still reachable via `ListValues(property)`, so nothing is lost; it is simply
  not typed. Consumers who need to know can compare `ListValues` against the typed property.
- The allowlist is a public behavioural contract. Widening it later is safe; narrowing it is breaking.

## Related, not fixed here
- **`xsd:integer` → `Int32`** (`XsdTypeMapper.cs:160`) is lossy: `xsd:integer` is unbounded, so a value
  above `Int32.MaxValue` overflows on read. Left alone deliberately — widening it to `Int64` changes what
  every existing `int`-mapped property receives and deserves its own reproduction.
- **Virtuoso does not preserve `xsd:short`/`xsd:unsignedInt`/`xsd:unsignedShort`.** `Int16Test`,
  `Uint16Test` and `UintTest` in `Trinity.Tests/Store/ResourceTest.cs` fail there and pass in-memory, and
  they are *unmapped*-path tests whose helper casts with `(TValue)`. This ADR does not fix them: the cast
  is in the test, not in `PropertyMapping`. They are evidence of the store-fidelity divergence, which is
  the reason the original defect went unnoticed and is worth addressing separately.

## Related
- [0017](0017-resources-open-mapped-and-dynamic.md), [0018](0018-decorators-are-syntactic-sugar.md),
  [0026](0026-xsd-dotnet-datatype-mapping.md)
