; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TRIN001 | Trinity.Mapping | Warning | [RdfProperty] on a property that is not partial, so no mapping is generated
TRIN002 | Trinity.Mapping | Warning | [RdfClass] on a class that is not partial, so no GetTypes() override is generated
TRIN003 | Trinity.Mapping | Warning | Mapped type is nested; mapping is only generated for top-level types
TRIN004 | Trinity.Mapping | Warning | Mapped class does not derive from Semiodesk.Trinity.Resource
TRIN005 | Trinity.Mapping | Warning | Mapped class has no accessible constructor taking a single Uri
TRIN006 | Trinity.Mapping | Warning | URI belongs to a generated vocabulary but is not one of its terms
TRIN007 | Trinity.Mapping | Warning | Mapped property uses System.Uri, whose equality ignores the fragment; use UriRef
