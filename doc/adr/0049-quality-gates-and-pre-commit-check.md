# 0049. Quality gates: duplication, changed-line reports, store coverage and a pre-commit check

Date: 2026-09-30

## Status
Accepted (2.0). Amends [0044](0044-store-suites-green-and-in-ci.md): the `stores` legs also
collect coverage.

## Context

CI gated line coverage (a 78% floor over the in-memory suites) and nothing else. Three gaps showed:

- **A fix landing in one copy of duplicated code is this codebase's recurring defect.** For example,
  `Read(update: true)` lost data in Fuseki and GraphDB; GraphDB's copy of `TryParse` lacked the TriG
  case; and the bulk-reader fix reached two of the three `IModel` implementations (0046). A scan
  showed the store adapters are largely copies of each other: 67% of Fuseki's lines sit in a clone,
  mostly with Oxigraph. The open `AbsoluteUri` quarantine is the same defect again: Oxigraph passes
  `OriginalString` to `HasGraph`, while the otherwise identical lines in Fuseki's copy do not.
- **Coverage never measured the store adapters.** The `stores` legs (0044) run each adapter against a
  real server, but collected nothing.
- **Most commits are now made by Claude Code sessions**, and a gate that runs only after a push
  reports a problem one round-trip too late.

## Decision

1. **Duplication is gated.** jscpd, pinned to an exact version and run by `duplication.py`, scans
   product C# with comments ignored. Its scope lives in `.jscpd.json`, which is also the single
   definition of "product code" the coverage scripts use.
   - **The measure counts both sides of every clone**: each duplicated line has a twin that a fix
     must also reach. jscpd's own figure counts one side, 5.4% where this reads 9.6%.
   - **The ceiling is 10.5%**, a little above the current figure. It sits above for the coverage
     floor's reason inverted: deleting unduplicated code raises the share without adding a clone.

2. **Pull requests get changed-line reports, which are never gated.** They are annotated on the diff
   and cover three things:
   - changed product lines no test runs;
   - edits that landed on one side of a clone only;
   - new code that repeats existing code.

   One-sided edits are judged against clones scanned **at the base revision**, not in the result.
   Editing one copy is exactly what stops the two matching, so a scan of the result no longer contains
   the clone the edit landed in.

3. **The store legs collect coverage, which is reported, not gated.** A `coverage` job merges it with
   the in-memory reports. The rules:
   - **One row per adapter, and no merged grand total.** Adding the adapters to the denominator would
     pull the headline figure below the gated one for no reason.
   - **An adapter counts only from its own suite.**
   - **A leg whose outcome is not an explicit `success` is left out.** A partial report makes covered
     code read as uncovered.
   - **Only source files that belong to this repo count.** The store suites also instrument
     Testcontainers, whose package ships symbols. So a file counts only if git knows it and
     `.jscpd.json` does not exclude it.

   A floor on adapter coverage waits until `VirtuosoManager.cs` is trimmed (#59). Until then its
   unreachable API is most of what such a floor would measure.

4. **CI runs on pull requests, and on pushes to `develop` and `master` only.**
   - Also triggering on `feature/**` pushes ran every job twice per PR push, once per event.
   - A newer commit on a PR cancels the superseded run.
   - Only PR runs share a concurrency group. GitHub cancels a pending run in a shared group even
     without `cancel-in-progress`, and every `develop` build produces a package.

5. **A local check runs before every commit a Claude Code session makes.**
   - `.github/scripts/check.sh` mirrors the `build`, `duplication` and `scripts` jobs. It also runs the
     store suites the change affects, as decided by `stores.py`, the one store list.
   - A checked-in `PreToolUse` hook (`.claude/hooks/pre-commit.sh`) runs it. It follows these rules:
     - **One decision, by parsing.** `commit_command.py` tokenizes the command; no text search is
       made. It counts `git commit` and the `--continue` forms, because the plain merge, cherry-pick,
       revert and rebase build their tree only when they run.
     - **The tree being committed is the one checked**, not the main checkout.
     - **A commit is never let through silently.** A missing hook prerequisite (python3, GNU
       `timeout`) or an unparseable command blocks, naming the cause.
     - **A command that is not a commit is never blocked.**
     - **A missing optional tool** (Docker, npx) is reported *not run* and left to CI.
     - **The check is bounded at 840 s**, under the hook's 900 s limit. A hook that outlives its
       limit is killed and the command proceeds silently, so exceeding the bound blocks instead.

## Consequences

- **A commit costs about 45 s.** A change to core or the shared fixtures also runs all four store
  suites, which brings it to about 110 s with Docker. About 82% of recent commits were of that kind.
- **People are not gated.** The hook runs only for Claude Code sessions. A person runs `check.sh`
  by hand, or relies on CI.
- **The hook sees the tree before the command runs.** An edit made by the same command that commits
  (`sed -i … && git commit`) is not checked. CLAUDE.md says to edit and commit in separate commands.
- **The hook no longer uses an `if` filter.** That filter's prefix matching never matched
  `git -C <dir> commit`. Instead the hook runs for every Bash command, and anything that mentions
  neither `commit` nor `--continue` returns before Python starts.
- **The scripts are unit-tested** (a `scripts` job, and a step in `check.sh`), because a mistake in
  them fails open or closed without a sign.
- **Known limits:** a subcommand spelled through a variable (`git $sub`), aliases and `eval` are not
  followed.

## Related
- [0044](0044-store-suites-green-and-in-ci.md) — the store suites in CI, amended here
- [0036](0036-integration-tests-testcontainers.md) — Testcontainers
- [0046](0046-bulk-subject-binding-with-values.md) — a fix that reached two of three implementations
- `.github/scripts/` (`coverage.py`, `duplication.py`, `changed_lines.py`, `stores.py`,
  `commit_command.py`, `check.sh`), `.jscpd.json`, `.claude/settings.json`, `.claude/hooks/pre-commit.sh`
- Issues #59–#62 — the follow-ups
