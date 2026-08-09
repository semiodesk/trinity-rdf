# 0024. SPARQL queries reuse registered ontology prefixes

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
Writing SPARQL normally means repeating `PREFIX` declarations for every vocabulary used.
Since Trinity already knows the registered ontologies ([0020](0020-runtime-metadata-discovery.md)),
query authors should not have to redeclare them.

## Decision
`SparqlQuery(string queryString, bool declarePrefixes = true)` runs a preprocessor
(`SparqlPreprocessor.Process`, `Trinity/Query/SparqlPreprocessor.cs`) that scans the query for
used-but-undeclared prefixes and, for each one present in `OntologyDiscovery.Namespaces`,
**injects the matching `PREFIX` declaration automatically**. A query can therefore use
`foaf:name`, `rdfs:label`, etc. with no manual `PREFIX` lines, as long as the ontology is
registered. Pass `declarePrefixes: false` to opt out; `GetDeclaredPrefixes()` reports what was
added.

## Consequences
- Terser, less error-prone queries; prefixes stay consistent with the registered vocabularies.
- Depends on `OntologyDiscovery` being populated ([0020](0020-runtime-metadata-discovery.md)) —
  an unregistered prefix is left undeclared and the query fails at the store.
- Auto-injection is a lightweight textual transform of the query string; unusual query text
  could in principle interact with the preprocessor.

## Related
- [0020](0020-runtime-metadata-discovery.md), [0019](0019-models-are-named-graphs-modelgroups.md)
