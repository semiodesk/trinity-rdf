# 0001. Record architecture decisions

Date: 2026-07-13

## Status
Accepted

## Context
Semiodesk.Trinity was built 2015–2020 and then largely unmaintained for several
years. Much of the design rationale — especially the compile-time code-generation
machinery — lives only in the authors' heads and in scattered build files. The
project is now being revived, with limited time, in collaboration with an AI
assistant. Efficient work requires a shared, durable record of why the system is
the way it is, so decisions are not re-litigated and their consequences are visible.

## Decision
We keep Architecture Decision Records in `doc/adr/`, one Markdown file per decision,
using the lightweight template in `doc/adr/README.md`. We first extract ADRs for the
decisions already embodied in the code (0001–0012), then record new revival decisions
as they are made. Decided ADRs are superseded, not rewritten.

## Consequences
- A newcomer (human or AI) can read `doc/adr/` and understand the system's shape and
  its known pain points without spelunking the whole codebase.
- Small ongoing cost to write an ADR when a significant decision is made.
- The extracted ADRs deliberately capture "what is," including regrettable choices,
  rather than an idealized design.

## Related
- `doc/adr/README.md`
