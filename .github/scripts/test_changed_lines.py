"""Tests for changed_lines.py, run by `python3 -m unittest discover -s .github/scripts`.

Each test builds a scratch repository, so the real checkout's state never matters.
"""
import os
import subprocess
import tempfile
import unittest

import changed_lines


class ScratchRepo(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.TemporaryDirectory()
        self.root = self.dir.name
        self.run_git("init", "-q")
        self.run_git("config", "user.email", "test@example.org")
        self.run_git("config", "user.name", "test")
        self.saved_root = changed_lines._root
        changed_lines._root = self.root

    def tearDown(self):
        changed_lines._root = self.saved_root
        self.dir.cleanup()

    def run_git(self, *args):
        subprocess.run(["git", *args], cwd=self.root, check=True, capture_output=True)

    def write(self, path, data):
        with open(os.path.join(self.root, path), "wb") as handle:
            handle.write(data if isinstance(data, bytes) else data.encode("utf-8"))

    def commit(self, path, data):
        self.write(path, data)
        self.run_git("add", path)
        self.run_git("commit", "-q", "-m", path)


class Hunks(ScratchRepo):
    def test_a_changed_non_utf8_line_does_not_crash(self):
        # Review item B1: Documentation/api/index.md holds a cp1252 en-dash (0x96).
        self.commit("doc.md", b"a\nen \x96 dash\nc\n")
        self.write("doc.md", b"a\nen \x96 dash, edited\nc\n")
        self.assertEqual([h.new_path for h in changed_lines.hunks("HEAD")], ["doc.md"])

    def test_content_that_looks_like_a_header_is_content(self):
        # Review item B2: removing `-- dashed` puts `--- dashed` in the diff.
        self.commit("t.md", "a\n-- dashed\nb\nc\nd\ne\nf\n")
        self.write("t.md", "a\nb\nc\nd\ne\nF\n")
        found = list(changed_lines.hunks("HEAD"))
        self.assertEqual([(h.old_path, h.new_path) for h in found], [("t.md", "t.md")] * 2)
        self.assertEqual([(h.old_start, h.old_count) for h in found], [(2, 1), (7, 1)])

    def test_an_added_line_that_looks_like_a_header_is_content(self):
        self.commit("t.md", "a\nb\nc\nd\n")
        self.write("t.md", "a\n++ plus\nb\nc\nD\n")
        self.assertEqual([h.new_path for h in changed_lines.hunks("HEAD")], ["t.md", "t.md"])

    def test_no_newline_marker_is_not_counted(self):
        self.commit("t.md", "a\nb")
        self.write("t.md", "a\nB")
        self.commit("u.md", "x\n")
        self.write("u.md", "y\n")
        self.assertEqual([h.new_path for h in changed_lines.hunks("HEAD")], ["t.md", "u.md"])

    def test_untracked_files_are_one_all_new_hunk(self):
        self.commit("t.md", "a\n")
        self.write("new.cs", "one\ntwo\n")
        self.assertIn(changed_lines.Hunk(None, 0, 0, "new.cs", 1, 2), list(changed_lines.hunks("HEAD")))


if __name__ == "__main__":
    unittest.main()
