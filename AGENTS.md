## Architecture
- `PackageExplorer` is the WPF shell; `App.xaml.cs` composes MEF exports from itself and `PackageViewModel` so new UI services or commands must be `[Export]`ed to join the composition.
- `Core` holds platform-agnostic package inspection (loading `.nupkg`, `SymbolValidation`, telemetry helpers) and feeds both desktop and CLI; favor adding heavy logic here.
- `PackageViewModel` implements the MVVM surface and MEF extensibility (`Commands/`, `PackageAnalyzer/`, `PluginManagerViewModel`); desktop views bind directly to these models.
- `Types` defines plugin contracts (`IPackageCommand`, `IPackageContentViewer`, `IPackageRule`) consumed through MEF; shared XAML in `PackageExplorer/NugetPackageExplorer.Views.Shared.projitems` is reused by UNO targets.
- `dotnet-validate` (CLI) and `Uno/NuGetPackageExplorer.WinUI.csproj` reference the same `Core` + `PackageViewModel` assemblies, so cross-cutting changes must build across WPF, WinUI, and browser WASM.

## Build & Tooling
- The repo pins SDK `10.0.100-rc.2` in `global.json`; keep preview features enabled (`LangVersion preview`, `Nullable enable`, `AllowUnsafeBlocks true`).
- Standard desktop build: `dotnet build NuGetPackageExplorer.sln -c Release` (outputs under `artifacts/bin` due to `UseArtifactsOutput=true`).
- UNO/Web builds require workloads: `dotnet workload install wasm-tools wasm-tools-net9` before publishing Uno targets.
- Uno desktop publish: `dotnet publish Uno/NuGetPackageExplorer/NuGetPackageExplorer.WinUI.csproj -f net10.0-desktop -c Release`.
- Uno WebAssembly publish: `dotnet publish Uno/NuGetPackageExplorer/NuGetPackageExplorer.WinUI.csproj -f net10.0-browserwasm -c Release`.
- All builds must complete without warnings or errors; treat warning-free output as the acceptance bar.
- Azure pipelines drive packaging via `ReleaseChannel`; local publishes mimic this (`dotnet publish PackageExplorer/NuGetPackageExplorer.csproj /p:PublishProfile=...`).
- Versions flow from `version.json` + Nerdbank.GitVersioning (`nbgv`) and land in manifests (`PackageExplorer.Package/*.appxmanifest`); adjust there instead of hardcoding.

## Developer Workflows
- Run `nbgv get-version` before packaging to confirm semantic versioning; pipeline calls `nbgv cloud -c -a` so keep `version.json` in sync.
- Desktop package publishes use `Properties/PublishProfiles/WinX64.pubxml`; WAP builds live in `PackageExplorer.Package/*.wapproj` and require the Release channel to populate manifests before MSBuild.
- `dotnet run --project dotnet-validate/dotnet-validate.csproj package local <path>` exercises the CLI validator against local nupkg files; `remote` subcommand pulls through NuGet feeds for regression checks.
- Build artifacts collect under `artifacts/<channel>`; binlogs land in `artifacts/logs` (match pipeline layout when reproducing MSBuild failures locally).
- When testing feed interactions, seed credentials through `App.xaml.cs::InitCredentialService` providers; many flows depend on `machine.config` and the MRU caches persisted in `%APPDATA%`.

## Uno Platform
- `Uno/NuGetPackageExplorer/NuGetPackageExplorer.WinUI.csproj` is a single project targeting WASM, WinUI, and Skia; it reuses WPF XAML via `NugetPackageExplorer.Views.Shared.projitems` plus `NugetPackageExplorer.Shared.Legacy.projitems` for legacy shims.
- WASM builds rely on `Platforms/WebAssembly` assets and set env keys (`MSDL_PROXY_LOCATION`, `NPE_AI_INSTRUMENTATIONKEY`, `MONO_GC_PARAMS`); keep these when adding scripts or telemetry.
- WebAssembly debugging depends on the `Uno` CORS Azure Function (`Uno/Api`); ensure new HTTP calls go through that proxy or are CORS-safe.
- Platform feature flags come from MSBuild constants: `USE_MONACO_EDITOR` lights up Monaco in WASM, while Windows targets define `USE_WINUI`; guard new code with the same constants.
- Uno `MefServices` in `Uno/NuGetPackageExplorer/MefServices` replace WPF services; flesh out `NotImplementedException` placeholders or add platform conditionals before calling them from shared code.

## Coding Conventions
- `.editorconfig` enforces 4-space indentation for C# and UTF-8 BOM; keep private fields `_camelCase`, public APIs PascalCase, and interfaces prefixed with `I`.
- Prefer `var` for locals, expression-bodied properties, and modern C# features (pattern matching, `??=`) while keeping methods/constructors block-bodied unless clarity suffers.
- Nullable is enabled solution-wide; WPF constructors often wrap specific sections in warning pragmas—follow that pattern instead of disabling nullable for whole files.
- Analyzer noise is tuned: `CA2007`, `CA1031`, and high-DPI warnings are suppressed; keep any new warning suppressions scoped tightly to match existing pragmas.
- Resource strings flow through the `ResXCompositeFormatGenerator`; request interpolated text from generated `CompositeFormat` properties instead of `string.Format` literals.

## Patterns & Conventions
- MVVM is handwritten; view models inherit `ViewModelBase`, call `OnPropertyChanged`, and marshal back to the UI thread with the WPF dispatcher or Uno equivalents.
- MEF drives extensibility: export new services/commands with `[Export]`, supply metadata attributes when needed, and prefer `ImportMany` collections to stay recomposable.
- Package validation rules live in `PackageViewModel/PackageAnalyzer/*Rule.cs`—return `PackageIssue` records and keep rule logic pure so the CLI and desktop share identical diagnostics.
- `PackageViewModelFactory` materializes dependencies lazily; follow that pattern when introducing expensive services to avoid startup regressions.
- Shared services (e.g. `MefServices/*.cs`) adapt UI abstractions; keep platform-specific behavior behind these facades so WPF and Uno stay aligned.

## External Integrations
- NuGet feed access uses `NuGet.Protocol` with defaults from `Core/NuGetConstants.cs`; leverage `PackageChooser` + `INuGetPackageDownloader` when touching remote feeds.
- Credential flow is orchestrated in `App.xaml.cs::InitCredentialService`; new credential providers should export `ICredentialProvider` and respect the manager cache.
- Symbol checks rely on `Core/SymbolValidation/SymbolValidator`; keep deterministic build requirements and Source Link evaluations consistent when editing packaging logic.
- Application Insights keys are injected at build time (`Build/` scripts, `ApplicationInsights.config`); avoid checking secrets into source.

## Gotchas
- `UseArtifactsOutput` means tests or tooling looking for binaries should read from `artifacts/` instead of `bin/` directly.
- Nightly/Store builds inject `ReleaseChannel` constants (`Directory.Build.props`); guard channel-specific code with the existing `NIGHTLY` and `STORE` preprocessor symbols.
- There is minimal automated testing—manual validation paths (`PackageExplorer/MainWindow.xaml.cs` workflows, `dotnet-validate`) are the de facto regression checks.
- UNO `MefServices` currently throw `NotImplementedException` for some contracts; desktop features that assume complete implementations need conditional guards.
- `Common/CommonAssemblyInfo.cs` is linked into each project; update assembly metadata here rather than per-project attributes.
