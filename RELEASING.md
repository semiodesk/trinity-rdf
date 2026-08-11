# Releasing Semiodesk.Trinity

**Releasing is a manual process.** CI (`.github/workflows/ci.yml`) only restores, builds, tests,
and packs — it does **not** publish. A maintainer publishes to nuget.org by hand, so no NuGet API
key and no signing secret lives in the repository or in CI.

Five packages ship together at the **same version** (the single `VersionPrefix` in
`Directory.Build.props`):

| Package | Notes |
|---|---|
| `Semiodesk.Trinity` | Core library **+ the source generator** (shipped as an analyzer) |
| `Semiodesk.Trinity.Virtuoso` | Virtuoso store backend |
| `Semiodesk.Trinity.Fuseki` | Fuseki store backend |
| `Semiodesk.Trinity.GraphDB` | GraphDB store backend |
| `Semiodesk.Trinity.Vocabulary.Tool` | **`dotnet tool`**, command `trinity-vocab` (ADR-0014). Installed with `dotnet tool install -g`, not referenced as a package, but published and pushed the same way |

## Preconditions (gates)

Do **not** publish until every gate holds:

1. **Branch & tree** — the change is merged to the release branch (`develop`/`master`) and the
   working tree is clean.
2. **CI is green** — the GitHub Actions run for the exact commit you're releasing passed.
3. **Version set** — `VersionPrefix` in `Directory.Build.props` is the intended release and follows
   SemVer. A breaking change is a **major** bump (the weaver → source-generator move is why this
   is `2.0.0`). For a prerelease, pass a label instead of editing the prefix:
   `dotnet pack … -p:VersionSuffix=rc.1` → `2.1.0-rc.1`.
   **Release the artifact from a release branch.** CI stamps a `ci.<run>.<sha>` prerelease label on
   every other branch and PR, so a feature-branch build can never be mistaken for the release (and
   would sort below it on nuget.org even if pushed by accident). CI only uploads a package artifact
   from `develop`/`master`.
4. **Builds SDK-only** — `dotnet build Semiodesk.Trinity.sln -c Release` → **0 errors**, using the
   .NET SDK alone (no targeting packs, no Visual Studio).
5. **Tests green** —
   - `dotnet test Trinity.Tests/Trinity.Tests.csproj -c Release` → passing, with only the known
     `[Ignore]`d skips (see `doc/known-test-failures.md`).
   - `dotnet test tests/Trinity.Generator.Tests/Trinity.Generator.Tests.csproj -c Release` → passing.
   - Store integration tests (`tests/Trinity.Tests.{Virtuoso,Fuseki,GraphDB}`) need live servers
     and are **not** a release gate.
6. **nuget.org will accept the push** — packages are pushed **unsigned** (nuget.org applies its own
   repository signature; author/code signing is intentionally not used). Make sure there is **no
   active required-signer enforcement**: on nuget.org, under the owning account's **Certificates**,
   remove any expired/registered certificates so unsigned pushes are accepted. If unsure, dry-run
   with a `-rc`/prerelease id first and unlist it afterward.
7. **Release notes** — CHANGELOG / release notes updated for this version, including the breaking
   changes (see Notes).

## Steps

```bash
# 0. Clean output so only the freshly built packages are pushed
rm -rf ./artifacts

# 1. Pack all five packages (Release) into ./artifacts.
#    `dotnet pack Semiodesk.Trinity.sln -c Release -o ./artifacts` does the same in one command.
dotnet pack Trinity/Trinity.csproj                   -c Release -o ./artifacts
dotnet pack Trinity.Virtuoso/Trinity.Virtuoso.csproj -c Release -o ./artifacts
dotnet pack Trinity.Fuseki/Trinity.Fuseki.csproj     -c Release -o ./artifacts
dotnet pack Trinity.GraphDB/Trinity.GraphDB.csproj   -c Release -o ./artifacts
dotnet pack Trinity.Vocabulary.Cli/Trinity.Vocabulary.Cli.csproj -c Release -o ./artifacts

# 2. Sanity-check the core package: correct version, and it contains
#    lib/netstandard2.0 + analyzers/dotnet/cs — and NO tools/ or build/*.targets
unzip -l ./artifacts/Semiodesk.Trinity.*.nupkg

# 3. Push to nuget.org (unsigned). The API key comes from nuget.org and is NEVER committed;
#    pass it via an environment variable.
dotnet nuget push "./artifacts/*.nupkg" \
  --source https://api.nuget.org/v3/index.json \
  --api-key "$NUGET_API_KEY" \
  --skip-duplicate
```

## After publishing

```bash
git tag v<version>
git push origin v<version>
```

- Create a GitHub Release from the tag and paste the notes.
- Confirm the packages appear on nuget.org and restore cleanly in a throwaway project.

## Notes

- **Signing:** author/code signing is deliberately not used — the old Semiodesk certificate expired
  and nuget.org's repository signature is sufficient for client trust. Do **not** reintroduce a
  signing step unless a valid certificate is registered on the account again.
- **2.0 is a breaking release:** consumers must declare mapped classes and their `[RdfProperty]`
  properties `partial` (C# 13 / .NET 9+); the cilg weaver is gone. Call this out prominently in the
  release notes and migration guidance.
- **Version is single-source:** bump only `VersionPrefix` in `Directory.Build.props`. Per-project
  version overrides were removed so all four packages stay in lockstep. `AssemblyVersion`/`FileVersion`
  stay `2.0.0.0` — assembly versions cannot carry a prerelease label.
