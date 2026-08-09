# 0030. Deleting a resource removes all triples that reference it (subject and object)

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
In a graph, a resource is referenced both as the **subject** of its own triples and as the
**object** of other resources' triples. Deleting only the subject triples would leave dangling
references to a resource that no longer exists.

## Decision
`StoreBase.DeleteResource(modelUri, resourceUri)` (`Trinity/Stores/StoreBase.cs`) runs two
updates scoped to the target graph:

```sparql
DELETE WHERE { GRAPH @graph { @subject ?p ?o . } };
DELETE WHERE { GRAPH @graph { ?s ?p @object . } }
```

removing every triple where the resource appears **as subject or as object**. Deletion is
therefore referentially clean by default.

## Consequences
- No dangling references after a delete — the resource is fully expunged from the graph.
- Delete is **graph-scoped** (the resource's model) and can be broad: deleting a
  widely-referenced resource silently removes many *incoming* links. There is no "delete only
  my outgoing triples" option.
- Implementation note (in code): dotNetRDF does not support the full SPARQL 1.1 update syntax
  (no `FILTER`/`OPTIONAL` in `MODIFY`), which constrains how deletes can be expressed.

## Related
- [0016](0016-resource-centric-not-triple-centric.md), [0009](0009-supported-store-backends.md)
