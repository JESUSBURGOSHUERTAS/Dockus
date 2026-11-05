---
name: systematic-debugging
description: Methodical root-cause investigation for bugs and unexpected behaviors. Replaces guesswork and shotgun debugging with rigorous hypothesis formulation, isolation, and regression verification.
---

# Systematic Debugging

Use this skill whenever investigating a bug, an exception, unexpected test results, performance regression, or discrepancies in application output. It enforces disciplined root-cause analysis rather than guessing or applying superficial band-aids.

---

## The Core Rule of Systematic Debugging

> **NEVER apply random code patches hoping something works. Every change must test a specific, falsifiable hypothesis grounded in evidence.**

---

## 4-Phase Debugging Protocol

### Phase 1: Reproduce & Isolate
1. **Establish the exact failure condition:**
   - What was the exact input or trigger?
   - What was the expected output or behavior?
   - What was the actual output, exception, or log line?
2. **Minimize the reproduction case:**
   - Strip away unrelated parameters, middleware, or external dependencies.
   - Reproduce via a minimal script, test case, or targeted CLI command.
   - If intermittent, identify timing, race conditions, or environment variables.

### Phase 2: Trace Execution Flow & State
1. **Trace backward from the failure point:**
   - Locate the exact line of code where the error was thrown or where state diverged.
   - Inspect local variables, parameters, and database state at that moment.
2. **Examine system boundaries:**
   - Did invalid data enter through an API, form, or database query earlier in the execution path?
   - Check upstream callers: is the function receiving unexpected `None`, types, or formats?

### Phase 3: Formulate & Test Hypotheses
1. **State the hypothesis explicitly:**
   - *"The bug occurs because `variable_x` is `None` when `condition_y` is triggered."*
2. **Design a targeted experiment:**
   - Add minimal diagnostic logging, an assertion, or run a step-by-step test.
   - Change **ONLY ONE variable** at a time.
3. **Evaluate the result:**
   - If the hypothesis is disproven, record the finding and formulate the next hypothesis based on new evidence.
   - Do not leave temporary debugging statements in production code paths.

### Phase 4: Root-Cause Remediation & Verification
1. **Fix the root cause, not just the symptom:**
   - Avoid `if x is None: return` if `x` should never have been `None` in the first place. Fix the origin of the invalid state.
   - Ensure the fix adheres to the codebase's existing architecture and error handling rules.
2. **Automate regression prevention:**
   - Write a unit test or integration test that reproduces the bug before applying the fix.
   - Verify the test fails without the fix and passes with it.
3. **Check for collateral impact:**
   - Run the existing test suite to ensure the fix did not break downstream features.

---

## Debugging Checklist
- [ ] Error message and stack trace read carefully from top to bottom.
- [ ] Exact line number and failing condition identified.
- [ ] Reproduction script or test case created.
- [ ] Root cause identified (why it happened, not just what failed).
- [ ] Single targeted fix applied.
- [ ] Regression test added.
- [ ] All temporary log statements removed.
