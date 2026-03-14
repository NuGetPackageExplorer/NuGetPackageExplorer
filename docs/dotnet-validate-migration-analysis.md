# Analysis: Moving `dotnet-validate` to a Separate Repository

## Executive Summary

The `dotnet-validate` CLI tool currently lives inside the NuGet Package Explorer (NPE) monorepo and depends on the `Core` project for package loading (`ZipPackage`) and symbol validation (`SymbolValidator`). Moving it to a separate repository requires publishing the `Core` library (along with its dependency `Types`) as a NuGet package. This document analyses the files involved, the feasibility of splitting the Core project, what should be published to NuGet, and alternative approaches.

---

## 1. Current Architecture

```
NuGetPackageExplorer (monorepo)
│
├── dotnet-validate/          CLI tool (6 files, ~434 LOC)
│   ├── Program.cs            Entry point & validation orchestration
│   ├── PackageDownloader.cs  NuGet feed download logic
│   ├── ConsoleLogger.cs      ILogger implementation for console
│   ├── UnavailableException.cs
│   ├── dotnet-validate.csproj
│   └── version.json
│
├── Core/                     Shared core library (76 .cs files)
│   ├── AssemblyMetadata/     PE/debug metadata parsing (9 files)
│   ├── Async/                Task coordination (1 file)
│   ├── DeepLinking/          WASM route parsing (1 file)
│   ├── Extensions/           Utility extensions (8 files)
│   ├── Packages/             Package model: ZipPackage, etc. (12 files)
│   ├── ProjectSystem/        File system abstraction (2 files)
│   ├── Repositories/         MachineCache (1 file, Windows-specific)
│   ├── SymbolValidation/     SymbolValidator + helpers (7 files)
│   └── Utility/              Telemetry, XML, crypto, etc. (16 files)
│
├── Types/                    Plugin contract interfaces (14 files)
│   ├── IPackageRule.cs, IPackageCommand.cs, IPackageContentViewer.cs, ...
│   └── Packages/             IPackage, IPackageFile, etc.
│
├── NuGetPeGenerators/        Roslyn source generator for .resx (2 files)
│
├── PackageViewModel/         MVVM layer for desktop UI (74 files)
├── PackageExplorer/          WPF desktop application
├── Uno/                      Uno Platform (WASM/WinUI/Skia)
└── tests/                    Test projects
```

### Dependency Graph

```
dotnet-validate ──→ Core ──→ Types
                             ↑
PackageViewModel ──→ Core ──→ Types
       ↑                      ↑
PackageExplorer (WPF) ───────┘
       ↑
Uno/NuGetPackageExplorer.WinUI ──→ Core, PackageViewModel, Types
```

### What the CLI Tool Actually Uses from Core

The CLI tool has a **narrow dependency surface** on Core. It uses exactly:

| Class | Location | Purpose |
|-------|----------|---------|
| `ZipPackage` | `Core/Packages/ZipPackage.cs` | Opens and reads `.nupkg` files |
| `SymbolValidator` | `Core/SymbolValidation/SymbolValidator.cs` | Validates SourceLink, determinism, compiler flags |
| `SymbolValidationResult` | `Core/SymbolValidation/SymbolValidatorResult.cs` | Enum for SourceLink validation outcome |
| `DeterministicResult` | `Core/SymbolValidation/SymbolValidatorResult.cs` | Enum for deterministic build outcome |
| `HasCompilerFlagsResult` | `Core/SymbolValidation/SymbolValidatorResult.cs` | Enum for compiler flags outcome |

However, these classes internally depend on much of Core:
- `SymbolValidator` → `AssemblyMetadata/*`, `Utility/*`, `Extensions/*`, `PathToTreeConverter`
- `ZipPackage` → `Packages/*`, `Utility/*`, `Extensions/*`
- Both → `Types/` project (for `IPackage`, `IPackageFile`, etc.)

---

## 2. Files and Classes to Move to the New Repository

### 2.1 Files That Must Move (the CLI tool itself)

| File | Lines | Description |
|------|-------|-------------|
| `dotnet-validate/Program.cs` | 258 | CLI entry point with `local` and `remote` subcommands |
| `dotnet-validate/PackageDownloader.cs` | 113 | NuGet feed package downloader |
| `dotnet-validate/ConsoleLogger.cs` | 44 | Console `ILogger` implementation |
| `dotnet-validate/UnavailableException.cs` | 19 | Custom exception for unavailable packages |
| `dotnet-validate/dotnet-validate.csproj` | 18 | Project file (would change `ProjectReference` → `PackageReference`) |
| `dotnet-validate/version.json` | 10 | NBGV versioning config |

**Total: 6 files, ~462 lines of code**

### 2.2 Build/Config Files to Replicate in the New Repo

| File | Purpose | Action |
|------|---------|--------|
| `Directory.Build.props` | Common build properties, nullable, lang version | Replicate (simplified subset) |
| `Directory.Build.targets` | Build targets | Replicate if needed |
| `Directory.Packages.props` | Central package management | Replicate (subset of packages) |
| `.editorconfig` | Code style rules | Copy |
| `global.json` | SDK version pinning | Copy |
| `NuGet.config` | Package source configuration | Copy |

### 2.3 What Stays in the NPE Monorepo

Everything except the `dotnet-validate/` directory stays. The Core, Types, PackageViewModel, UI projects, Uno, and tests all remain.

---

## 3. Can We Split the Core Project?

### 3.1 Internal Structure Analysis

The Core project contains **76 source files** across 9 subdirectories. Here is a cohesion analysis:

| Module | Files | Used by CLI? | Used by UI? | Platform-Specific? |
|--------|-------|-------------|-------------|-------------------|
| `SymbolValidation/` | 7 | ✅ Yes | ✅ Yes | No |
| `AssemblyMetadata/` | 9 | ✅ Transitively | ✅ Yes | No |
| `Packages/` | 12 | ✅ Yes | ✅ Yes | No |
| `Extensions/` | 8 | ✅ Transitively | ✅ Yes | No |
| `Utility/` | 16 | ✅ Partially | ✅ Yes | **3 files** (AppInsights, Windows) |
| `ProjectSystem/` | 2 | No | ✅ Yes | No |
| `Repositories/` | 1 | No | ✅ Yes | **Yes** (Windows `MachineCache`) |
| `DeepLinking/` | 1 | No | ✅ Yes (WASM) | No |
| `Async/` | 1 | No | ✅ Yes | No |

### 3.2 Platform-Specific Code in Core

Only **4 files** contain Windows/platform-specific code:

1. **`Utility/DiagnosticsClient.cs`** — Application Insights + WPF `Application.Current` references (`#if WINDOWS`)
2. **`Utility/AppVersionTelemetryInitializer.cs`** — AppInsights `ITelemetryInitializer` with WPF version detection (`#if WINDOWS`)
3. **`Utility/EnvironmentTelemetryInitializer.cs`** — AppInsights initializer with `#if STORE`, `#if NIGHTLY` channel guards
4. **`Repositories/MachineCache.cs`** — Windows `ApplicationData` + `OSVersionHelper` (`#if WINDOWS`)

### 3.3 Splitting Recommendation

**Splitting Core is possible but not recommended for this migration.** Here's why:

- The CLI tool uses `SymbolValidator` and `ZipPackage`, which transitively pull in `AssemblyMetadata/`, `Packages/`, `Extensions/`, and parts of `Utility/` — that's **~52 of 76 files** (~68% of Core).
- The remaining files (`DeepLinking`, `Async`, `ProjectSystem`, `Repositories`) are small and have minimal overhead.
- The platform-specific code is already guarded with `#if WINDOWS` conditionals and compiles cleanly on all targets.
- Splitting would create two tightly-coupled packages that must be versioned together.

**Verdict:** Publish Core as a single NuGet package. The `#if WINDOWS` guards already handle cross-platform concerns.

---

## 4. What Should Be Published to NuGet

To support the separated `dotnet-validate` repo, the following packages should be published from the NPE monorepo:

### 4.1 Recommended NuGet Packages

| Package Name | Source Project(s) | Target Frameworks | Description |
|--------------|-------------------|-------------------|-------------|
| **`NuGetPackageExplorer.Types`** | `Types/` | `net10.0;net8.0` | Plugin contracts: `IPackage`, `IPackageFile`, `IPackageRule`, `PackageIssue`, etc. |
| **`NuGetPackageExplorer.Core`** | `Core/` (depends on Types) | `net10.0;net8.0;net10.0-windows10.0.26100` | Core validation logic: `ZipPackage`, `SymbolValidator`, assembly metadata, utilities |

### 4.2 Package Dependency Chain

```
dotnet-validate (new repo)
  ├── NuGetPackageExplorer.Core (NuGet package)
  │     └── NuGetPackageExplorer.Types (NuGet package)
  ├── Microsoft.Extensions.FileSystemGlobbing
  └── System.CommandLine
```

### 4.3 Changes Required in NPE Monorepo

1. **Add NuGet packaging metadata** to `Core/Core.csproj` and `Types/Types.csproj`:
   - `<PackageId>`, `<PackageVersion>` (via NBGV), `<PackageLicenseExpression>`, `<PackageProjectUrl>`, `<RepositoryUrl>`, `<PackageReadmeFile>`
   - `<IsPackable>true</IsPackable>`

2. **The `NuGetPeGenerators` source generator** is referenced by Core as an analyzer — it does **not** need to be a separate NuGet package. It should be embedded in the Core package using:
   ```xml
   <None Include="$(OutputPath)\NuGetPeGenerators.dll" Pack="true" PackagePath="analyzers/dotnet/cs" />
   ```
   Or kept as a build-time-only dependency.

3. **CI/CD pipeline update**: Add a dedicated publish step for the Core + Types NuGet packages (the existing `Pack Libraries as Package` task may already cover this, but should be explicitly configured for stable releases).

4. **Version strategy**: Use NBGV for both the monorepo packages and the new `dotnet-validate` repo. Pin the `NuGetPackageExplorer.Core` package version in the new repo's `Directory.Packages.props`.

### 4.4 Changes Required in the New dotnet-validate Repository

1. **Replace `ProjectReference` with `PackageReference`** in `dotnet-validate.csproj`:
   ```xml
   <!-- Before (monorepo) -->
   <ProjectReference Include="..\Core\Core.csproj" />

   <!-- After (separate repo) -->
   <PackageReference Include="NuGetPackageExplorer.Core" />
   ```

2. **Replicate build infrastructure** (simplified):
   - `global.json` (SDK version)
   - `Directory.Build.props` (minimal: nullable, lang version, analysis)
   - `Directory.Packages.props` (package versions)
   - `.editorconfig`
   - `version.json` (NBGV, already exists in `dotnet-validate/`)

3. **Set up CI/CD** for the new repo (GitHub Actions or Azure Pipelines) to build, test, and publish the tool to NuGet.org.

---

## 5. Consumers Impact Assessment

| Consumer | Impact | Action Required |
|----------|--------|----------------|
| **PackageViewModel** | None — still uses `ProjectReference` to Core within the monorepo | No change |
| **PackageExplorer (WPF)** | None — still references Core and PackageViewModel via ProjectReference | No change |
| **Uno/WinUI** | None — still references Core, PackageViewModel, Types via ProjectReference | No change |
| **tests/Core.Security.Tests** | None — still tests Core within the monorepo | No change |
| **dotnet-validate** | **Major** — must switch from ProjectReference to PackageReference on published Core | Migrate to new repo |
| **CI/CD Pipeline** | **Minor** — must add NuGet publish step for Core + Types | Update `azure-pipelines.yml` |

---

## 6. Alternative Approaches

### Approach A: Move CLI to Separate Repo + Publish Core as NuGet Package ⭐ (Recommended)

**Description:** Move only `dotnet-validate/` to a new repository. Publish `NuGetPackageExplorer.Core` and `NuGetPackageExplorer.Types` as NuGet packages from the existing monorepo. The new repo references Core via NuGet `PackageReference`.

| Pros | Cons |
|------|------|
| ✅ Clean separation of concerns — CLI has its own release cycle | ❌ Two repos to maintain |
| ✅ Core package usable by other tools and the community | ❌ Core API changes require publish → update cycle |
| ✅ Minimal changes to existing monorepo (just add packaging metadata) | ❌ Must establish and maintain NuGet publishing pipeline |
| ✅ Independent versioning for CLI tool | ❌ Breaking Core changes require coordinated releases |
| ✅ CLI can be maintained/released by different contributors | ❌ Initial setup effort for new repo CI/CD |
| ✅ Only 6 files to move | |

**Effort estimate:** Low-Medium (1–2 days)

---

### Approach B: Move CLI + Core + Types to Separate Repo

**Description:** Move `dotnet-validate/`, `Core/`, `Types/`, and `NuGetPeGenerators/` to a new repository. The NPE monorepo then references Core via NuGet `PackageReference`.

| Pros | Cons |
|------|------|
| ✅ Core library has its own dedicated repo and release cadence | ❌ **Major refactor** — NPE must switch to NuGet PackageReference for Core |
| ✅ Clean library/application boundary | ❌ Breaks existing monorepo build for all UI projects |
| ✅ Core changes automatically available to CLI tool | ❌ PackageViewModel, WPF, Uno all need migration |
| | ❌ Platform-specific `#if WINDOWS` code in Core may cause packaging complexity |
| | ❌ `NuGetPeGenerators` source generator needs separate packaging |
| | ❌ High effort and risk; every NPE contributor affected |

**Effort estimate:** High (1–2 weeks)

---

### Approach C: Keep CLI in Monorepo, Publish Core as NuGet Package Only

**Description:** Keep `dotnet-validate/` in the monorepo but publish `Core` and `Types` as NuGet packages for external consumers. The CLI continues to use `ProjectReference` internally.

| Pros | Cons |
|------|------|
| ✅ Zero file moves — no new repo needed | ❌ Does not achieve goal of separate CLI repo |
| ✅ Core becomes available as a NuGet package for the community | ❌ CLI release is still tied to the monorepo release cycle |
| ✅ No breaking changes to any project | ❌ CLI changes still require full monorepo CI |
| ✅ Simplest approach | |

**Effort estimate:** Very Low (half day)

---

### Approach D: Split Core into Core.Validation + Core.UI, Move CLI + Core.Validation

**Description:** Split `Core` into two packages: `Core.Validation` (SymbolValidator, ZipPackage, AssemblyMetadata, Extensions — the ~52 files needed by CLI) and `Core.UI` (DeepLinking, Repositories, telemetry, etc.). Move CLI + Core.Validation to a new repo.

| Pros | Cons |
|------|------|
| ✅ Smallest possible dependency for CLI consumers | ❌ **Significant refactoring** — must untangle internal dependencies |
| ✅ Clean separation of validation vs. UI-support code | ❌ 52+ files to split, test, and validate |
| ✅ Avoids shipping AppInsights/Windows deps to CLI users | ❌ Two Core packages to version and maintain |
| | ❌ Utility classes are shared — duplication or a third shared package needed |
| | ❌ Risk of breaking the monorepo build |

**Effort estimate:** High (1–2 weeks)

---

### Approach E: Git Subtree / Submodule for Shared Code

**Description:** Use `git subtree` or `git submodule` to share the `Core/` and `Types/` directories between the monorepo and the new CLI repo.

| Pros | Cons |
|------|------|
| ✅ Single source of truth for Core code | ❌ Git submodules are notoriously difficult to manage |
| ✅ No NuGet publishing overhead | ❌ Subtree merges can create confusing git history |
| ✅ Changes propagate via git, not NuGet | ❌ CI/CD complexity increases significantly |
| | ❌ Contributors must understand subtree/submodule workflow |
| | ❌ Not idiomatic for .NET ecosystem |

**Effort estimate:** Medium, with ongoing maintenance burden

---

### Approach F: Monorepo with Separate CI Pipelines (No Split)

**Description:** Keep everything in the monorepo but set up a dedicated CI pipeline for `dotnet-validate` that builds and publishes independently from the UI projects.

| Pros | Cons |
|------|------|
| ✅ Zero structural changes | ❌ Does not achieve repository separation |
| ✅ Shared code stays in sync automatically | ❌ CLI still appears coupled to the UI project |
| ✅ Independent release cadence via pipeline triggers | ❌ Contributors see full monorepo complexity |
| ✅ Can use path-based triggers (`dotnet-validate/**`, `Core/**`) | ❌ PR reviews mix CLI and UI changes |

**Effort estimate:** Very Low (half day)

---

## 7. Recommendation

**Approach A (Move CLI to Separate Repo + Publish Core as NuGet)** is recommended because:

1. **Minimal disruption** — Only 6 files move; the monorepo build is unchanged for all UI projects.
2. **Clean boundary** — The CLI tool has a clear, narrow API surface on Core (2 main classes).
3. **Community benefit** — Publishing `NuGetPackageExplorer.Core` as a NuGet package enables other tools to build on the same validation logic.
4. **Independent release cycles** — CLI tool bugs/features don't require a full NPE release.
5. **Low effort** — Estimated 1–2 days to complete.

### Implementation Checklist for Approach A

#### In the NPE monorepo:

- [ ] Add NuGet package metadata to `Types/Types.csproj` (`PackageId`, license, readme, etc.)
- [ ] Add NuGet package metadata to `Core/Core.csproj` (`PackageId`, license, readme, etc.)
- [ ] Ensure `NuGetPeGenerators` analyzer DLL is properly included in the Core package
- [ ] Add a CI step to publish `NuGetPackageExplorer.Types` and `NuGetPackageExplorer.Core` to NuGet.org
- [ ] Remove or archive `dotnet-validate/` directory after migration is complete
- [ ] Update `NuGetPackageExplorer.slnx` to remove the `dotnet-validate` project
- [ ] Update `README.md` to point to the new repository

#### In the new `dotnet-validate` repository:

- [ ] Initialize repo with `dotnet-validate/` files
- [ ] Set up `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.editorconfig`
- [ ] Replace `ProjectReference` to Core with `PackageReference` to `NuGetPackageExplorer.Core`
- [ ] Set up CI/CD pipeline (build, test, NuGet publish)
- [ ] Configure NBGV versioning (version.json already exists)
- [ ] Add README with installation and usage instructions
- [ ] Publish initial release to NuGet.org

---

## 8. Detailed File Inventory

### Files to Move to New Repository

```
dotnet-validate/
├── Program.cs                  (258 lines) — CLI entry point
├── PackageDownloader.cs        (113 lines) — NuGet feed download
├── ConsoleLogger.cs            ( 44 lines) — Console ILogger
├── UnavailableException.cs     ( 19 lines) — Custom exception
├── dotnet-validate.csproj      ( 18 lines) — Project file (needs edit)
└── version.json                ( 10 lines) — NBGV version config
```

### NuGet Packages to Publish from Monorepo

**Package: `NuGetPackageExplorer.Types`**
```
Types/
├── Packages/
│   ├── IPackage.cs
│   ├── IPackageFile.cs
│   └── ... (package model interfaces)
├── IPackageCommand.cs
├── IPackageContentViewer.cs
├── IPackageRule.cs
├── PackageIssue.cs
├── PackageIssueLevel.cs
├── PackageExtensions.cs
└── Types.csproj
Dependencies: NuGet.Packaging, System.ComponentModel.Composition
```

**Package: `NuGetPackageExplorer.Core`**
```
Core/
├── AssemblyMetadata/           (9 files) — PE metadata parsing
├── Async/                      (1 file)  — Task coordination
├── DeepLinking/                (1 file)  — WASM routing
├── Extensions/                 (8 files) — Utility extensions
├── Packages/                   (12 files) — ZipPackage, SignatureInfo, etc.
├── ProjectSystem/              (2 files) — File system abstraction
├── Repositories/               (1 file)  — MachineCache
├── SymbolValidation/           (7 files) — SymbolValidator
├── Utility/                    (16 files) — Telemetry, XML, crypto
└── Core.csproj
Dependencies: NuGetPackageExplorer.Types, NuGet.Protocol, PeNet,
              Microsoft.SymbolStore, Microsoft.DiaSymReader.Converter,
              System.Reflection.Metadata, System.Formats.Asn1,
              Microsoft.Extensions.DependencyModel,
              AppInsights.WindowsDesktop (Windows only),
              OSVersionHelper (Windows only)
```

### Files That Stay in the Monorepo (No Changes)

- `PackageViewModel/` — 74 files, MVVM layer
- `PackageExplorer/` — WPF desktop application
- `PackageExplorer.Package/` — MSIX packaging
- `Uno/` — Uno Platform projects
- `tests/` — All test projects
- `Build/`, `scripts/`, `images/` — Build and CI infrastructure
- `Common/CommonAssemblyInfo.cs` — Shared assembly info
