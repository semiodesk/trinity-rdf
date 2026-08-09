# 0018. Mapping attributes are syntactic sugar over `PropertyMapping<T>`

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
It is easy to assume the `[RdfClass]`/`[RdfProperty]` attributes
([0002](0002-attribute-based-object-mapping.md)) *are* the mapping. They are not. The
actual, load-bearing mapping mechanism is the `PropertyMapping<T>` backing field plus
accessors that delegate to `Resource.GetValue`/`SetValue`, and a `GetTypes()` override.

## Decision
The decorators are **syntactic sugar**. The canonical form of a mapped property is an
explicit `PropertyMapping<T>` field with getter/setter delegating to `GetValue`/`SetValue`,
and the type's `rdf:type`(s) expressed by overriding `GetTypes()`. Today `cilg` weaves
those members from the attributes ([0003](0003-mapping-via-il-weaving.md)); equivalently they
can be **written by hand with no attributes at all** (as DevHub does). Both forms are
first-class and interoperate — the runtime only ever sees `PropertyMapping<T>` members.

## Consequences
- The mapping does **not** depend on attributes existing at runtime; it depends on the
  `PropertyMapping<T>` members. This is why hand-written mappings work and why a source
  generator ([0013](0013-replace-il-weaving-with-source-generator.md)) is a drop-in path.
- Attributes **alone**, with no weaving or generation, do nothing at runtime — a mapped-looking
  auto-property is just a CLR property the store never sees. Docs and diagnostics should say so
  plainly, since it is a silent-failure trap.

## Related
- [0002](0002-attribute-based-object-mapping.md), [0003](0003-mapping-via-il-weaving.md),
  [0013](0013-replace-il-weaving-with-source-generator.md)
