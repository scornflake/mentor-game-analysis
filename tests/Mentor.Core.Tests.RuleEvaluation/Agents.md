# Agents.md - RuleEvaluation Project Directive

## Purpose

This is an **evaluation and research project**, NOT a standard test project.

The purpose of this project is to objectively measure and compare the quality of LLM analysis with and without game-specific rule augmentation.

## AI Agent Instructions

### DO NOT:
- ❌ Create xUnit tests that run as part of CI/CD
- ❌ Assert on specific values in automated tests
- ❌ Treat this like a normal unit test project

### DO:
- ✅ Create harness classes for manual experimentation
- ✅ Build comparison and metric collection tools
- ✅ Generate reports for human review (but don't create a million little .md files for every task you do)
- ✅ Focus on measurement and data collection

## How to Use This Project

Run comparisons manually by:
1. Instantiating the `RuleComparisonHarness`
2. Providing screenshot paths from `tests/media/`
3. Running dual analysis (with/without rules enabled)
4. Examining generated reports in `reports/` directory

This project produces quantified evidence that humans can use to determine if rule augmentation provides value, rather than automated pass/fail tests.

