"""Tests for commit_command.py, run by `python3 -m unittest discover -s .github/scripts`.

The first table is review item A2 on #58: the commands the old text search got wrong. One row is
corrected. The review listed `gh pr create --body "runs before every \`git commit\`"` as not a
commit, but inside double quotes bash *executes* backticks -- that command runs `git commit` --
so it is one; the single-quoted spelling, which bash leaves literal, is the non-commit.
"""
import json
import os
import subprocess
import sys
import tempfile
import unittest

from commit_command import commits

HERE = "/work/repo"


def committed_in(command, cwd=HERE):
    return commits(command, cwd)


class DecidesFromTokens(unittest.TestCase):
    def test_the_review_table(self):
        cases = [
            ("gh pr create --body 'runs before every `git commit`'", []),
            ('gh pr create --body "runs before every `git commit`"', [HERE]),  # bash runs it
            ("echo 'then git commit'", []),
            ("cat > notes.md <<EOF\nsome text\ngit commit\nEOF", []),
            ("/usr/bin/git commit -m x", [HERE]),
            ("git --git-dir /r1/.git commit -m x", [HERE]),
            ('git -c user.name="A B" commit -m x', [HERE]),
        ]
        for command, expected in cases:
            with self.subTest(command=command):
                self.assertEqual(committed_in(command), expected)

    def test_commits(self):
        cases = [
            "git commit -m x",
            "git add a b && git commit -q -F - <<'EOF'\nsubject\n\ngit commit is mentioned here\nEOF",
            "git status; git commit -am x",
            "git diff | cat\ngit commit",
            "(git commit -m x)",
            "echo $(git commit -m x)",
            "VAR=1 env -i git commit",
            "bash -c 'git commit -m x'",
            "git --no-pager commit -v",
            "git merge --continue",
            "git -c core.editor=true rebase --continue",
            "git cherry-pick --continue",
            # Wrappers that run another command (review leftovers on #58).
            "timeout 60 git commit -m x",
            "timeout -k 5 --signal=TERM 60 git commit",
            "gtimeout 60 git commit",
            "sudo git commit -m x",
            "sudo -u me -- git commit",
            "printf 'a\\n' | xargs git commit -m x",
            "xargs -n 1 -P 2 git commit",
            "stdbuf -oL nice -n 5 git commit",
            "env -S 'git commit -m x'",
            "flock /tmp/l git commit",
            "flock -w 5 /tmp/l -c 'git commit -m x'",
        ]
        for command in cases:
            with self.subTest(command=command):
                self.assertEqual(committed_in(command), [HERE])

    def test_not_commits(self):
        cases = [
            "git status",
            "git commit-tree abc",
            "git log --oneline -- .github/commit.txt",
            "grep -n commit ci.yml",
            "git merge develop",
            "git rebase develop",
            "git show HEAD:commit.md",
            "legit commit",
            "echo commit > x 2>&1",
            "gh pr create --title t --body \"$(cat body.md)\"",
            "cat <<-EOF\n\tgit commit\n\tEOF",
            "python3 -c 'import os; os.system(\"git commit\")'",
            "git help commit",
            "timeout 60 git status",
            "sudo git log commit",
            "xargs -I {} echo git commit {}",
        ]
        for command in cases:
            with self.subTest(command=command):
                self.assertEqual(committed_in(command), [])

    def test_which_tree(self):
        self.assertEqual(committed_in("git -C /other commit -m x"), ["/other"])
        self.assertEqual(committed_in("git -C /other -C sub commit"), ["/other/sub"])
        self.assertEqual(committed_in("git -C rel commit"), ["/work/repo/rel"])
        self.assertEqual(committed_in("cd /elsewhere && git commit"), ["/elsewhere"])
        self.assertEqual(committed_in("cd sub; git -C .. commit"), ["/work/repo"])
        self.assertEqual(committed_in("git --work-tree=/wt commit"), ["/wt"])
        self.assertEqual(committed_in("git --work-tree /wt commit"), ["/wt"])
        self.assertEqual(committed_in("env -C /other git commit"), ["/other"])
        self.assertEqual(committed_in("sudo -D /other git commit"), ["/other"])
        self.assertEqual(committed_in("sudo -C 3 git commit"), [HERE])  # -C is --close-from for sudo

    def test_unbalanced_quotes_cannot_be_parsed(self):
        from commit_command import Undecidable
        with self.assertRaises(Undecidable):
            committed_in("git commit -m 'unterminated")


class HookInterface(unittest.TestCase):
    """The exit codes the hook relies on, through the real entry point."""

    def run_main(self, payload):
        script = os.path.join(os.path.dirname(os.path.abspath(__file__)), "commit_command.py")
        data = payload if isinstance(payload, str) else json.dumps(payload)
        return subprocess.run([sys.executable, script], input=data, capture_output=True, text=True)

    def test_exit_codes(self):
        with tempfile.TemporaryDirectory() as repo:
            subprocess.run(["git", "init", "-q", repo], check=True)
            top = subprocess.run(["git", "-C", repo, "rev-parse", "--show-toplevel"],
                                 capture_output=True, text=True, check=True).stdout.strip()

            commit = self.run_main({"cwd": repo, "tool_input": {"command": "git commit -m x"}})
            self.assertEqual((commit.returncode, commit.stdout.strip()), (0, top))

            other = self.run_main({"cwd": "/", "tool_input": {"command": f"git -C {repo} commit"}})
            self.assertEqual((other.returncode, other.stdout.strip()), (0, top))

            self.assertEqual(self.run_main({"cwd": repo, "tool_input": {"command": "git status"}}).returncode, 1)
            self.assertEqual(self.run_main({"cwd": repo, "tool_input": {"command": "echo 'x"}}).returncode, 1)
            self.assertEqual(self.run_main({"cwd": repo, "tool_input": {"command": "git commit -m 'x"}}).returncode, 2)
            self.assertEqual(self.run_main({"cwd": repo, "tool_input": {"command": 5}}).returncode, 2)
            self.assertEqual(self.run_main("not json").returncode, 2)
            outside = self.run_main({"cwd": "/", "tool_input": {"command": "git -C /nonexistent commit"}})
            self.assertEqual(outside.returncode, 2)


if __name__ == "__main__":
    unittest.main()
