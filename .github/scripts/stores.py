#!/usr/bin/env python3
"""The store suites, and which of them a change affects.

Usage: stores.py list
       stores.py affected <revision>     suites whose inputs differ from <revision> (working tree)
       stores.py images <store>          the container images that suite pins

This is the one list of store suites. The `stores` matrix in .github/workflows/ci.yml must name the
same four -- a matrix has to be literal YAML, so it cannot read this -- and tests/Trinity.Tests.Stardog
is deliberately absent: it targets net472, is not in the solution, and has no provider behind it.

Which suites a change affects is a path table rather than a walk over the csproj ProjectReferences.
The graph is seven projects, and the references are not a faithful picture of it: the Fuseki and
GraphDB test projects still reference other adapters that none of their code uses, so a walk would
run three suites for a Virtuoso-only change. Anything a store suite is compiled from runs it:

- its adapter or its own test project runs that suite;
- core, the generator, the shared fixtures in Trinity.Tests, or a root build input runs all four --
  store-specific defects in core are the kind the in-memory suite cannot see.

Changed paths come from `git diff --name-only`, not from diff hunks: the vendored
Trinity.Virtuoso/Dependencies/OpenLink.Data.Virtuoso.dll is binary and has no hunks.
"""
import glob
import os
import re
import sys

from changed_lines import git, repo_root, untracked_files

STORES = ("Fuseki", "GraphDB", "Oxigraph", "Virtuoso")

SHARED_DIRS = ("Trinity/", "Trinity.Generator/", "Trinity.Tests/")
SHARED_FILES = ("Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.config")


def adapter_dir(store):
    return f"Trinity.{store}/"


def adapter_of(path):
    """The store whose adapter `path` belongs to, or None for shared code."""
    return next((s for s in STORES if path.startswith(adapter_dir(s))), None)


def changed_paths(revision):
    diff = git("diff", "--name-only", "--no-renames", revision, text=True).splitlines()
    return {p for p in diff + untracked_files() if p}


def affected(revision):
    hit = set()
    for path in changed_paths(revision):
        if path.startswith(SHARED_DIRS) or path in SHARED_FILES:
            return list(STORES)
        for store in STORES:
            if path.startswith((adapter_dir(store), f"tests/Trinity.Tests.{store}/")):
                hit.add(store)
    return [s for s in STORES if s in hit]


def images(store):
    found = []
    for source in sorted(glob.glob(os.path.join(repo_root(), "tests", f"Trinity.Tests.{store}", "*.cs"))):
        with open(source, encoding="utf-8-sig") as handle:
            found += re.findall(r'WithImage\("([^"]+)"\)', handle.read())
    return found


if __name__ == "__main__":
    command = sys.argv[1] if len(sys.argv) > 1 else ""
    if command == "list" and len(sys.argv) == 2:
        print("\n".join(STORES))
    elif command == "affected" and len(sys.argv) == 3:
        print("\n".join(affected(sys.argv[2])))
    elif command == "images" and len(sys.argv) == 3 and sys.argv[2] in STORES:
        print("\n".join(images(sys.argv[2])))
    else:
        print(__doc__, file=sys.stderr)
        sys.exit(2)
