---
name: changelog-generator
description: Automated generation and maintenance of structured, human-readable release notes and changelogs adhering to Semantic Versioning and Keep a Changelog standards.
---

# Changelog Generator & Release Documentation

Use this skill when preparing releases, tagging new versions, generating release notes, or documenting changes across commits, pull requests, and git diffs.

---

## 1. Principles of Effective Changelogs

- **For Humans, Not Machines:** A changelog is not a raw `git log` dump. It translates technical commits into clear, user-centric and stakeholder-readable improvements.
- **Single Source of Truth:** Keep a central `CHANGELOG.md` at the repository root.
- **Chronological & Grouped:** Order versions with newest on top. Every version has an ISO release date (`YYYY-MM-DD`).

---

## 2. Standard Change Categories (Keep a Changelog)

Group all entries within a version into these standardized sections:

- **`Added`**: New features or capabilities exposed to users or developers.
- **`Changed`**: Changes in existing functionality or interface workflows.
- **`Deprecated`**: Soon-to-be removed features with migration instructions.
- **`Removed`**: Features that have been completely removed.
- **`Fixed`**: Bug fixes, performance remediations, and edge case patches.
- **`Security`**: Vulnerability mitigations and security enhancements.

---

## 3. Semantic Versioning (SemVer) Guidance

Choose the version number increment based on impact:
- **`MAJOR` (X.0.0):** Incompatible API changes, breaking data schema migrations, or removed features.
- **`MINOR` (0.X.0):** Backwards-compatible new features, added endpoints, or significant functional improvements.
- **`PATCH` (0.0.X):** Backwards-compatible bug fixes, minor performance tweaks, or documentation updates.

---

## 4. Standard Format Example

```markdown
# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Forecast horizon simulation supporting whole-box replenishment.

## [1.2.0] - 2026-11-02
### Added
- Dynamic sales record grouping by invoice number (`invoice_name`).
- Ultra-compact mobile Kanban view for executives.

### Fixed
- QWeb template compile error in sales record header styling.
- Fraction-box repeat calculation in order suggestion projections.

### Changed
- Refined customer type and channel extraction to read directly from branch/subsede.

## [1.1.0] - 2026-10-15
...
```

---

## 5. Workflow to Generate a Changelog

1. **Inspect Commit History:**
   Run `git log <last_tag>..HEAD --oneline` or analyze the git diff.
2. **Filter & Consolidate:**
   Combine micro-commits, WIP commits, and typo fixes into single cohesive bullet points.
3. **Categorize by Impact:**
   Classify each item into `Added`, `Changed`, `Fixed`, etc.
4. **Draft Executive Summary:**
   Provide a 1–2 sentence high-level summary at the top of the release notes outlining the core value delivered.
