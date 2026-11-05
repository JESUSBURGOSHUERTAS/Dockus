---
name: brainstorming
description: Socratic and structured exploration of requirements, technical trade-offs, architecture, and edge cases before writing code. Enforces engineering discipline and prevents premature implementation.
---

# Brainstorming & Architecture Exploration

Use this skill whenever starting a new feature, designing an architecture, refactoring a significant subsystem, or when a task has ambiguity. It prevents premature code generation by ensuring requirements, constraints, and alternatives are systematically explored first.

---

## The Golden Rule of Brainstorming

> **DO NOT start writing implementation code or generating boilerplate until the problem, constraints, and chosen architectural path are agreed upon.**

---

## 5-Step Brainstorming Process

### Step 1: Clarify Intent & Business Context
- What core problem are we solving?
- Who is the end user (developer, customer, manager, admin)?
- What does success look like? What are the key performance or business metrics?
- What are the non-negotiable constraints (e.g., framework versions, latency, backward compatibility, budget, time)?

### Step 2: Surface Hidden Assumptions & Edge Cases
Identify what could go wrong or what assumptions have been made implicitly:
- **Data scale:** What happens with 1 record vs. 1,000,000 records?
- **Concurrency & State:** What happens if two users or processes execute this simultaneously?
- **Failure modes:** What if third-party services fail, network drops, or data is corrupt?
- **Permissions & Security:** Who should NOT be able to do this? What access boundaries exist?

### Step 3: Explore 2–3 Viable Architectural Options
Always contrast alternatives before settling on a solution:
- **Option A (Simple / Minimalist):** Lowest complexity, fastest time-to-deliver, but potential limitations down the line.
- **Option B (Robust / Idiomatic):** Standard best-practice pattern for the given stack, balancing maintenance and flexibility.
- **Option C (High-Scale / Extensible):** Event-driven, decoupled, or plugin-based architecture for long-term scalability.

For each option, provide:
- High-level mechanism.
- Trade-offs (Pros & Cons).
- Estimated complexity and maintenance burden.

### Step 4: Recommend and Justify the Optimal Path
- State clearly which option is recommended and why it best fits the current context and constraints.
- Keep recommendations pragmatic: prefer the simplest solution that reliably solves the problem without over-engineering (YAGNI).

### Step 5: Outline an Incremental Execution Plan
Break the chosen path into bite-sized, verifiable milestones:
1. Milestone 1: Data models / contracts / schema changes.
2. Milestone 2: Core business logic & validations.
3. Milestone 3: UI / API exposure.
4. Milestone 4: Verification, automated tests & edge case validation.

---

## Anti-Patterns to Avoid
- **Coding before thinking:** Generating 200 lines of code immediately upon receiving a vague prompt.
- **Monolithic proposals:** Suggesting a single massive change without breaking it down.
- **Ignoring non-happy paths:** Designing only for valid inputs and fast networks.
- **Over-engineering:** Proposing microservices or complex event buses when a simple database query suffices.
