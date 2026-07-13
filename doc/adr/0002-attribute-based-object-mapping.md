# 0002. Attribute-based semantic object mapping on a `Resource` base

Date: 2026-07-13 (decision original to 2015–2020 design)

## Status
Accepted

## Context
The goal of Trinity is to let .NET developers work with RDF knowledge graphs using
familiar enterprise patterns (MVC/MVVM, POCOs) instead of manipulating triples
directly through dotNetRDF. dotNetRDF is powerful but low-level; we wanted an
object mapper analogous to an OR-mapper, mapping RDFS/OWL terms to .NET types and
properties.

## Decision
Domain classes derive from `Semiodesk.Trinity.Resource` and are annotated with
ontology attributes:
- `[RdfClass(uri)]` on the type declares its `rdf:type`(s).
- `[RdfProperty(uri, languageInvariant)]` on a property maps it to a predicate.

Each mapped property is backed at runtime by a `PropertyMapping<T>` value holder;
accessors funnel through `Resource.GetValue`/`SetValue`, and the resource also
exposes an untyped triple API (`AddProperty`/`GetValue(Property)`) for unmapped
predicates. Type identity is exposed via a `GetTypes()` override.

See `Trinity/Resource.cs`, `Trinity/PropertyMapping.cs`,
`Trinity/Attributes/RdfClassAttribute.cs`, `Trinity/Attributes/RdfPropertyAttribute.cs`.

## Decision drivers
- First-class .NET ergonomics over raw triples.
- Attributes keep the mapping declarative and close to the property it describes.
- Byte-code manipulation (see [0003](0003-mapping-via-il-weaving.md)) was chosen to
  implement the accessor bodies, so authors write plain auto-properties.

## Consequences
- Very clean authoring model: a mapped class is a POCO plus attributes.
- The model *depends on* a code-generation step to make the accessors actually read
  and write RDF; without it, a mapped auto-property is an ordinary CLR property that
  the store never sees. This coupling is the source of most revival pain.
- Runtime mapping discovery is reflective (`Resource.InitializePropertyMappings`),
  a per-instance cost.
- This attribute surface is part of the public compatibility contract; both external
  consumers rely on it.

## Related
- [0003](0003-mapping-via-il-weaving.md) — how the accessors are implemented
- [0013](0013-replace-il-weaving-with-source-generator.md) — proposed new implementation
