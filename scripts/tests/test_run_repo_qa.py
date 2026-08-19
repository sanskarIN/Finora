from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "run_repo_qa.py"
SPEC = importlib.util.spec_from_file_location("run_repo_qa", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
qa = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = qa
SPEC.loader.exec_module(qa)


class RepositoryQaRunnerTests(unittest.TestCase):
    def test_dependency_free_steps_use_selected_python(self) -> None:
        steps = qa.dependency_free_steps("python-test")

        self.assertEqual(3, len(steps))
        self.assertEqual("python-test", steps[0].command[0])
        self.assertIn("unittest", steps[0].command)
        self.assertEqual(
            ("python-test", "scripts/check_documentation_coverage.py"),
            steps[1].command,
        )
        self.assertEqual(
            ("python-test", "scripts/validate_localization.py"),
            steps[2].command,
        )

    def test_dotnet_step_is_opt_in(self) -> None:
        without_dotnet = qa.planned_steps(
            include_dotnet=False,
            dotnet_configuration="Release",
            python="py",
        )
        with_dotnet = qa.planned_steps(
            include_dotnet=True,
            dotnet_configuration="Debug",
            python="py",
        )

        self.assertEqual(3, len(without_dotnet))
        self.assertEqual(4, len(with_dotnet))
        self.assertEqual(
            ("dotnet", "test", "-c", "Debug", "--nologo"),
            with_dotnet[-1].command,
        )

    def test_run_step_preserves_return_code_and_duration(self) -> None:
        step = qa.QaStep("example", (sys.executable, "-c", "raise SystemExit(3)"))
        with tempfile.TemporaryDirectory() as directory:
            result = qa.run_step(step, cwd=Path(directory))

        self.assertEqual(3, result.return_code)
        self.assertFalse(result.passed)
        self.assertGreaterEqual(result.duration_seconds, 0)

    def test_format_command_quotes_arguments(self) -> None:
        formatted = qa.format_command(("tool", "value with spaces"))

        self.assertIn("tool", formatted)
        self.assertIn("value with spaces", formatted)

    def test_main_list_mode_does_not_execute_steps(self) -> None:
        with mock.patch.object(sys, "argv", ["run_repo_qa.py", "--list"]), mock.patch.object(
            qa, "run_step"
        ) as run_step:
            exit_code = qa.main()

        self.assertEqual(0, exit_code)
        run_step.assert_not_called()


if __name__ == "__main__":
    unittest.main()
