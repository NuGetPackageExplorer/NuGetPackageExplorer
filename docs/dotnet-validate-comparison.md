# NuGet Package Validation CLI Tools — Comparison

This document compares three .NET CLI tools for validating NuGet packages:

| Tool | NuGet ID | Repository |
|------|----------|------------|
| **dotnet-validate** (this repo) | [`dotnet-validate`](https://www.nuget.org/packages/dotnet-validate) | [NuGetPackageExplorer/NuGetPackageExplorer — dotnet-validate/](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/tree/main/dotnet-validate) |
| **nupkg-validator** | [`nupkg-validator`](https://www.nuget.org/packages/nupkg-validator) | [nullean/nupkg-validator](https://github.com/nullean/nupkg-validator) |
| **Meziantou.Framework.NuGetPackageValidation.Tool** | [`Meziantou.Framework.NuGetPackageValidation.Tool`](https://www.nuget.org/packages/Meziantou.Framework.NuGetPackageValidation.Tool) | [meziantou/Meziantou.Framework — src/Meziantou.Framework.NuGetPackageValidation.Tool/](https://github.com/meziantou/Meziantou.Framework/tree/main/src/Meziantou.Framework.NuGetPackageValidation.Tool) |

---

## Features

### dotnet-validate (this repo)

**Install / run:**
```bash
dotnet tool install -g dotnet-validate
dotnet validate package local  <file-or-glob>
dotnet validate package remote <packageId> [-v <version>] [-s <feedUrl>] [-d <nugetConfigDir>]
```

**Validation checks performed:**

| Check | Description |
|-------|-------------|
| Source Link | Verifies that Source Link is embedded and valid; distinguishes Valid / ValidExternal / NoSourceLink / InvalidSourceLink / NoSymbols / HasUntrackedSources / NothingToValidate |
| Deterministic build | Checks the `Deterministic` MSBuild flag is set on all managed assemblies |
| Compiler flags | Ensures reproducibility metadata (compiler flags) is embedded in PDB/assemblies |

**Input modes:**
- Local file path or glob pattern
- Remote package on any NuGet V3 feed (downloads to a temp file)

**Output:**
- Human-readable console text with ✅/❌/⚠️ icons
- Non-zero exit code on failure

**Missing / known limitations (open GitHub issues):**
- No machine-readable JSON output ([README To Do](../README.md))
- Exact remote version resolution broken for some feeds (noted in README)
- No validation of package metadata (author, description, icon, license, readme, etc.)
- No assembly-level checks (Release build, strong name, version numbers)
- No glob support for remote packages
- No ability to ignore/select individual checks
- Package signing validation broken on macOS ([#1698](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues/1698))
- CLI does not differentiate failure types in exit code ([#1548](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues/1548))

**Language / framework:** C# · `System.CommandLine` · targets `net8.0` and `net10.0`

---

### nupkg-validator

**Install / run:**
```bash
dotnet tool install -g nupkg-validator
dotnet nupkg-validator <path-to.nupkg> [options]
```

**Validation checks performed:**

| Check | Description |
|-------|-------------|
| Release configuration | Asserts all DLLs were compiled in Release mode (always on; no toggle) |
| Assembly version | Validates `AssemblyVersion`, `AssemblyFileVersion`, and `InformationalVersion` against an expected version string |
| Major-only `AssemblyVersion` | By default asserts `Major.0.0.0`; can be overridden with `--notmajoronly` |
| Strong name / public key token | Verifies a given public key token is present on all assemblies |
| No NuGet dependencies | Optional flag to fail if the `.nuspec` declares any dependencies |

**Input modes:**
- Local `.nupkg` file path only (no remote, no glob)

**Output:**
- Human-readable structured console text (prints full nuspec and DLL metadata)
- Non-zero exit code on failure

**Filtering options:**
- `--assemblynametolookfor` — filter on a specific assembly name
- `--dllstoskip` — comma-separated list of DLL filenames to skip

**Missing / known limitations:**
- No Source Link / deterministic / symbol validation
- No metadata validation (author, description, icon, etc.)
- No remote package support
- No JSON output
- File lock issue when the same DLL name appears in multiple target frameworks ([GitHub issue #14](https://github.com/nullean/nupkg-validator/issues/14))

**Language / framework:** F# · `Argu` CLI parser · `FAKE.IO.Zip` · `FAKE.Core.SemVer` · targets `net8.0`, `net9.0`, `net10.0`

---

### Meziantou.Framework.NuGetPackageValidation.Tool

**Install / run:**
```bash
dotnet tool install -g Meziantou.Framework.NuGetPackageValidation.Tool
meziantou.validate-nuget-package <package-path>... [options]
```

**Validation checks performed (default rules):**

| Rule | Description |
|------|-------------|
| `AssembliesMustBeOptimized` | DLLs compiled in Release mode |
| `AuthorMustBeSet` | Author field is present and not the default value |
| `DescriptionMustBeSet` | Non-empty, non-default description |
| `IconMustBeSet` | Icon file included (not deprecated `iconUrl`) |
| `LicenseMustBeSet` | License expression or file (not deprecated `licenseUrl`) |
| `ProjectUrlMustBeSet` | Project URL is set and accessible |
| `ReadmeMustBeSet` | `readme.md` is included in the package |
| `RepositoryMustBeSet` | Repository type, URL, and commit are set |
| `Symbols` | Comprehensive: PDB format (portable), deterministic build, Source Link, compiler flags, file hash |
| `TagsMustBeSet` | Tags defined, within 4 000-character limit |
| `XmlDocumentationMustBePresent` | XML doc files ship alongside assemblies |

**Additional (opt-in) rules:**

| Rule | Description |
|------|-------------|
| `PackageIdAvailableOnNuGetOrg` | Checks the package ID is not already taken on nuget.org |
| `RepositoryBranchMustBeSet` | Repository branch field is set |

**Input modes:**
- One or more local `.nupkg` file paths (variadic)

**Output:**
- **JSON** to stdout (machine-parsable)
- `--only-report-errors` flag to suppress valid packages
- Non-zero exit code on failure

**Rule customization:**
- `--rules` to select a specific subset
- `--excluded-rules` and `--excluded-rule-ids` to skip individual rules
- `--github-token` for private GitHub Source Link authentication

**Library (`Meziantou.Framework.NuGetPackageValidation`):** all rules are also available as a NuGet library, so the same validation can be embedded in build tasks, custom tools, or tests.

**Missing / known limitations:**
- No remote package download (local only)
- No glob support

**Language / framework:** C# · `System.CommandLine` · `NuGet.Packaging` · targets `net8.0` and `net10.0`

---

## Feature Comparison Matrix

| Feature | dotnet-validate | nupkg-validator | Meziantou Tool |
|---------|:-:|:-:|:-:|
| Local file validation | ✅ | ✅ | ✅ |
| Glob / wildcard input | ✅ | ❌ | ❌ |
| Multiple input files | ✅ (via glob) | ❌ | ✅ |
| Remote NuGet feed | ✅ | ❌ | ❌ |
| Source Link check | ✅ | ❌ | ✅ |
| Deterministic build check | ✅ | ❌ | ✅ |
| Compiler flags check | ✅ | ❌ | ✅ |
| Symbol (PDB) format check | ❌ | ❌ | ✅ |
| Release mode (optimized) | ❌ | ✅ | ✅ |
| Assembly version check | ❌ | ✅ | ❌ |
| Strong name / public key | ❌ | ✅ | ❌ |
| No-dependencies assertion | ❌ | ✅ | ❌ |
| Author metadata check | ❌ | ❌ | ✅ |
| Description check | ❌ | ❌ | ✅ |
| Icon check | ❌ | ❌ | ✅ |
| License check | ❌ | ❌ | ✅ |
| Project URL check | ❌ | ❌ | ✅ |
| Readme check | ❌ | ❌ | ✅ |
| Repository metadata check | ❌ | ❌ | ✅ |
| Tags check | ❌ | ❌ | ✅ |
| XML documentation check | ❌ | ❌ | ✅ |
| JSON output | ❌ | ❌ | ✅ |
| Rule selection / exclusion | ❌ | partial (filters) | ✅ |
| Reusable library | ❌ | ❌ | ✅ |
| Language | C# | F# | C# |

---

## Open Issues

### dotnet-validate (NuGetPackageExplorer)

Issues labelled `area: dotnet-validate` in the [NuGetPackageExplorer repo](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues):

| # | Title |
|---|-------|
| [#1766](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues/1766) | Suggestion: move dotnet-validate to own repo and rename to nupkg-validate |
| [#1698](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues/1698) | Package Signing validation failing on macOS |
| [#1626](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues/1626) | Add support to validate symbols from online feeds other than nuget.org |
| [#1548](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues/1548) | CLI To differentiate between different failures |
| [#1547](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues/1547) | CLI to ignore (or only consider) certain assemblies |

### nupkg-validator

| # | Title |
|---|-------|
| [#14](https://github.com/nullean/nupkg-validator/issues/14) | `Access to the path 'X.dll' is denied` when the same DLL name appears in multiple TFMs |

### Meziantou.Framework.NuGetPackageValidation.Tool

The `meziantou/Meziantou.Framework` repository tracks a single open issue (Renovate dependency dashboard). No user-reported bugs exist against the NuGet validation tool at this time.

---

## Maintenance & Activity

| | dotnet-validate | nupkg-validator | Meziantou Tool |
|-|:-:|:-:|:-:|
| Repository age | 2020 | ~2021 | ~2022 |
| Latest commit (approx.) | Mar 2026 | Nov 2025 | Mar 2026 |
| Commit cadence | Low (part of large mono-repo) | Low (few per year) | High (mono-repo updated weekly) |
| CI | Azure Pipelines | GitHub Actions | GitHub Actions |
| Maintainer | [clairernovotny](https://github.com/clairernovotny) + org contributors | [Mpdreamz (Martijn Laarman)](https://github.com/Mpdreamz) | [meziantou (Gérald Barré)](https://github.com/meziantou) |
| Lives in mono-repo | ✅ (NuGetPackageExplorer) | ❌ (dedicated repo) | ✅ (Meziantou.Framework) |
| Dedicated focus | ❌ (sub-feature of NPE) | ✅ | ❌ (one of 60+ libraries) |
| Open issues | Several (backlog mixed with desktop app issues) | 1 | 0 (tool-specific) |
| Version | Preview (`0.0.1-preview.x`) | `0.2.x` | `1.0.x` (stable) |

---

## Dependencies

### dotnet-validate

| Package | Purpose |
|---------|---------|
| `System.CommandLine` | CLI argument parsing |
| `Microsoft.Extensions.FileSystemGlobbing` | Glob pattern matching for local file input |
| `NuGet.Versioning` (via Core project) | Version string parsing |
| `Core` project (this repo) | `SymbolValidator`, `ZipPackage`, feed download |

### nupkg-validator

| Package | Purpose |
|---------|---------|
| `Argu` | F# argument parsing |
| `Fake.IO.Zip` | Zip extraction for `.nupkg` |
| `Fake.Core.SemVer` | Semantic version parsing |

### Meziantou.Framework.NuGetPackageValidation.Tool

| Package | Purpose |
|---------|---------|
| `System.CommandLine` | CLI argument parsing |
| `System.Text.Json` | JSON output serialization |
| `NuGet.Packaging` (via library) | Package reading |
| `Meziantou.Framework.NuGetPackageValidation` | All validation logic (reusable library) |

---

## Popularity

> Exact download figures are only available in real time from the NuGet.org API. The numbers below are approximate at the time of analysis.

| | dotnet-validate | nupkg-validator | Meziantou Tool |
|-|:-:|:-:|:-:|
| NuGet package status | Preview (0.0.1-preview.x) | Stable (0.2.x) | Stable (1.0.x) |
| GitHub Stars | 3.0k+ (whole NPE repo) | < 100 | 1.5k+ (whole Meziantou.Framework repo) |
| GitHub Watchers | ~100+ (whole NPE repo) | < 20 | ~50+ (whole Meziantou.Framework repo) |
| Ecosystem integration | High — backed by NPE's broad reputation | Low — niche, single-author | Medium-High — Meziantou.Framework is widely used in the .NET community |
| Blog / docs | README only | README only | [Detailed blog post](https://www.meziantou.net/ensuring-best-practices-for-nuget-packages.htm) + README |

---

## Summary & Recommendations

### Strengths of each tool

**dotnet-validate:**
- Only tool with **remote NuGet feed support** (downloads and validates from any V3 feed)
- Deep integration with NuGetPackageExplorer's `SymbolValidator`, which is the most comprehensive open-source symbol validation engine available
- Glob input for batch validation of local files

**nupkg-validator:**
- Only tool that checks **assembly version numbers** and **strong-name signing**
- Unique **no-dependencies assertion** useful for packages that must be self-contained
- Dedicated, focused repository — not buried in a larger project

**Meziantou.Framework.NuGetPackageValidation.Tool:**
- By far the **broadest metadata validation** (13 default rules covering icon, license, readme, author, description, tags, repository, XML docs)
- **JSON output** makes it CI-pipeline friendly (can be parsed and reported as annotations)
- **Reusable library** — the same rules can be embedded in MSBuild tasks or custom tooling
- **Rule selection and exclusion** per invocation
- **Active maintenance** with frequent releases
- Only tool at a **stable (1.0)** version

### Suggested improvements for dotnet-validate

Given the analysis above and [issue #1766](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues/1766), the following improvements would differentiate dotnet-validate and add unique value:

1. **Add JSON output mode** — emit results as structured JSON so CI pipelines can parse and annotate failures
2. **Expose rule selection/exclusion** — allow users to opt in/out of individual checks (e.g. `--exclude SourceLink`)
3. **Add basic metadata checks** — author, description, license, icon, readme (possibly by integrating or referencing `Meziantou.Framework.NuGetPackageValidation` as a library)
4. **Fix remote version resolution** — the exact-version flag for remote packages is noted as broken in the README
5. **Differentiated exit codes** — [#1548](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues/1548) requests distinct exit codes per failure type
6. **Rename to `nupkg-validate`** — [#1766](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues/1766) — the current name is ambiguous
7. **Consider deprecating in favor of Meziantou's tool** — [#1766 comment](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues/1766#issuecomment-4060575996) — given the breadth and quality of `Meziantou.Framework.NuGetPackageValidation.Tool`, it may be worth redirecting users to that tool and focusing dotnet-validate uniquely on what it does best: **remote feed validation** and **symbol/deterministic build checking**
