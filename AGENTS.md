# Repository Automation Documentation

This document provides a comprehensive overview of all automated agents, bots, and workflows used in the NuGetPackageExplorer/NuGetPackageExplorer repository, as well as essential development information for contributors and AI agents working on this codebase.

## Table of Contents

- [Development Environment Setup](#development-environment-setup)
  - [Prerequisites](#prerequisites)
  - [Building the Project](#building-the-project)
  - [Project Structure](#project-structure)
  - [Testing](#testing)
- [Coding Standards](#coding-standards)
  - [Language Features](#language-features)
  - [Code Style](#code-style)
  - [Naming Conventions](#naming-conventions)
- [Agents and Workflows](#agents-and-workflows)
  - [Azure Pipelines CI/CD](#azure-pipelines-cicd)
  - [GitHub Actions - Azure Static Web App PR Validation](#github-actions---azure-static-web-app-pr-validation)
  - [Dependabot](#dependabot)
  - [Release Drafter](#release-drafter)
- [How to Add a New Agent](#how-to-add-a-new-agent)
- [Best Practices](#best-practices)
- [Change History](#change-history)

## Development Environment Setup

### Prerequisites

**Required:**
- **Visual Studio 2022** or later with support for Preview .NET Core SDKs
- **.NET SDK 10.0.100-preview.2** (as specified in `global.json`)
  - The project uses `allowPrerelease: true` and `rollForward: latestMajor`
  - Download from [.NET Preview Downloads](https://dotnet.microsoft.com/download/dotnet)
- **Uno Platform SDK 6.0.146** (configured via MSBuild SDK)

**Optional but Recommended:**
- **Git** for version control
- **Azure CLI** for working with Azure resources (if deploying)

### Building the Project

**Command Line Build:**
```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build NuGetPackageExplorer.sln

# Build specific configurations
dotnet build -c Release
dotnet build -c Debug
```

**Visual Studio:**
1. Open `NuGetPackageExplorer.sln`
2. Enable "Use previews of the .NET SDK" in Tools > Options > Environment > Preview Features
3. Build > Build Solution (Ctrl+Shift+B)

**Release Channels:**
The project supports multiple build channels:
- **Zip**: Standard desktop application
- **Store**: Microsoft Store package
- **Nightly**: Nightly builds with auto-update
- **Choco**: Chocolatey package
- **WebAssembly**: Uno Platform WebAssembly build
- **UnoSkia**: Uno Platform Skia Desktop

### Project Structure

```
NuGetPackageExplorer/
├── Core/                    # Core NuGet package manipulation logic
├── Types/                   # Shared types and interfaces
├── PackageViewModel/        # View models and business logic
├── PackageExplorer/         # Main WPF application (Windows Desktop)
├── PackageExplorer.Package/ # MSIX packaging project
├── Uno/                     # Uno Platform projects
│   ├── NuGetPackageExplorer/ # Uno WebAssembly and Skia apps
│   └── Api/                 # Azure Functions API for CORS
├── dotnet-validate/         # CLI tool for package validation
└── NuGetPeGenerators/       # Source generators
```

**Key Files:**
- `Directory.Build.props` - Common MSBuild properties
- `Directory.Build.targets` - Common MSBuild targets
- `Directory.Packages.props` - Central package management (CPM)
- `global.json` - SDK version pinning
- `version.json` - Version management via Nerdbank.GitVersioning
- `.editorconfig` - Code style and formatting rules
- `NuGetPackageExplorer.ruleset` - Code analysis rules

### Testing

**Running Tests:**
```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests for specific project
dotnet test PackageViewModel/PackageViewModel.csproj
```

**Test Projects:**
- Tests should be added alongside the code they test
- Follow existing test patterns using xUnit or MSTest
- Ensure tests pass locally before submitting PRs

## Coding Standards

### Language Features

**C# Language Version:** `preview`
- Use latest C# language features
- Nullable reference types are **enabled** by default
- Implicit usings are **enabled**
- Unsafe blocks are **allowed** where necessary

**Target Frameworks:**
- Desktop (WPF): `net9.0-windows10.0.19041.0`
- Uno WebAssembly: `net9.0-browserwasm`
- Uno Desktop: `net9.0-desktop`
- CLI Tool: `net8.0` and `net9.0`

**Key PropertyGroup Settings:**
```xml
<LangVersion>preview</LangVersion>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
<AnalysisMode>AllEnabledByDefault</AnalysisMode>
<EnforceCodeStyleInBuild>True</EnforceCodeStyleInBuild>
```

### Code Style

**Formatting (from `.editorconfig`):**

**Indentation:**
- Use **spaces**, not tabs
- C# files: 4 spaces
- XML/config files: 2 spaces
- JSON/YAML: 2 spaces

**Braces:**
- New line before opening brace (`csharp_new_line_before_open_brace = all`)
- New line before `else`, `catch`, `finally`

**var Usage:**
- Prefer `var` for built-in types
- Prefer `var` when type is apparent
- Prefer `var` elsewhere (suggestion level)

**Expression Bodies:**
- Methods: Do **not** use expression bodies
- Properties/indexers/accessors: **Use** expression bodies
- Lambdas: **Use** expression bodies

**Null Checking:**
- Use throw expressions where appropriate
- Use null-conditional operators (`?.`)
- Use null-coalescing operators (`??`)

**Modern Features:**
- Use object initializers
- Use collection initializers
- Use pattern matching over `is` with cast
- Use pattern matching over `as` with null check

### Naming Conventions

**Interfaces:** `IPascalCase` (prefix with `I`)

**Public Members:** `PascalCase`

**Parameters:** `camelCase`

**Constants:** `PascalCase`

**Private Fields:** `_camelCase` (prefix with underscore)
```csharp
private int _myField;
```

**Static Readonly Fields:** `PascalCase`
```csharp
private static readonly string MyStaticField = "value";
```

**Modifier Order:**
```
public, private, protected, internal, static, extern, new, virtual, abstract, sealed, override, readonly, unsafe, volatile, async
```

**Suppressed Diagnostics:**
- CA1303 (literals as localized parameters)
- CA1051 (visible instance fields)
- CA1031 (general exception types)
- CA1515 (public types internal)
- CA1054/CA1056 (Uri parameters)
- CA2007 (ConfigureAwait)

## Overview

This repository uses several automated agents to handle continuous integration, dependency management, deployment, and release management. Each agent has specific triggers, permissions, and responsibilities to ensure smooth development and deployment workflows.

## Agents and Workflows

### Azure Pipelines CI/CD

**Name:** Azure Pipelines CI/CD Pipeline

**Purpose:** Main continuous integration and deployment pipeline that builds, tests, signs, and publishes NuGet Package Explorer across multiple release channels (Zip, Store, Nightly, Choco, WebAssembly, UnoSkia).

**Configuration File:** [`azure-pipelines.yml`](./azure-pipelines.yml)

**Triggers:**
- Push to `main` branch
- Push to `rel/*` branches
- Pull requests to `main` branch
- Pull requests to `rel/*` branches

**What It Does:**
- Builds the application for multiple platforms and release channels:
  - Windows Desktop (Zip)
  - Microsoft Store (Store)
  - Nightly builds (Nightly)
  - Chocolatey package (Choco)
  - WebAssembly (WebAssembly)
  - Uno Skia Desktop (UnoSkia)
- Updates version numbers in manifests and badges
- Packs NuGet libraries
- Creates signed packages (Authenticode signing via Azure Key Vault)
- Publishes build artifacts
- Generates build logs

**Permissions / Secrets Required:**
- `AppInsightsKey` - Application Insights connection string
- `AppInsightsKeyWebAssembly` - Application Insights key for WebAssembly builds
- Azure RM subscription access for code signing
- `SignKeyVaultCertificate` - Azure Key Vault certificate for code signing
- `SignKeyVaultUrl` - Azure Key Vault URL
- Access to Azure DevOps variable group: "SignCLI Config"

**Ownership:** Repository maintainers (configured in Azure DevOps)

**How to Disable:**
- Temporarily: Add `[skip ci]` or `[skip azurepipelines]` to commit message
- Permanently: Delete or rename `azure-pipelines.yml` (not recommended)
- Per-branch: Configure branch policies in Azure DevOps

**Security Considerations:**
- Uses Azure Key Vault for secure code signing
- Secrets are stored in Azure DevOps variable groups
- Code signing only runs for non-PR builds
- Least privilege principle: signing happens in a separate stage after build

**Example Snippet:**
```yaml
trigger:
- main
- rel/*

stages:
- stage: Build
  variables:
    BuildConfiguration: Release
  jobs:
  - job: Build
    pool:
      vmImage: windows-latest
    strategy:
      matrix:
        Channel_Zip:
          ReleaseChannel: Zip
        # ... other channels
```

**Links:**
- [Build Status Badge](https://dev.azure.com/clairernovotny/GitBuilds/_apis/build/status/NuGet%20Package%20Explorer/NuGet%20Package%20Explorer%20CI?branchName=master)
- [Azure DevOps Project](https://dev.azure.com/clairernovotny/GitBuilds/_build/latest?definitionId=16)

---

### GitHub Actions - Azure Static Web App PR Validation

**Name:** Azure Static Web Apps PR Validation

**Purpose:** Validates pull requests by building the Uno WebAssembly application and deploying preview environments to Azure Static Web Apps.

**Configuration File:** [`.github/workflows/azure-static-web-app-pr-validation.yml`](./.github/workflows/azure-static-web-app-pr-validation.yml)

**Triggers:**
- Pull request opened, synchronized, reopened, or closed on `main` branch
- Only runs for `NuGetPackageExplorer/NuGetPackageExplorer` repository (not forks)

**What It Does:**
- Builds the Uno WebAssembly application (net9.0-browserwasm)
- Builds the CORS Azure Function API
- Deploys preview environment to Azure Static Web Apps for PR review
- Cleans up the preview environment when PR is closed
- Uploads artifacts for the WASM site and API

**Permissions / Secrets Required:**
- `AZURE_STATIC_WEB_APPS_API_TOKEN_CI` - Azure Static Web Apps deployment token
- `GITHUB_TOKEN` - Automatically provided by GitHub Actions for PR comments

**Ownership:** Repository maintainers

**How to Disable:**
- Temporarily: Add `[skip ci]` to commit message
- Permanently: Delete or rename the workflow file
- Per-PR: Close the PR or remove the workflow file before opening the PR

**Security Considerations:**
- Uses `skip_deploy_on_missing_secrets: true` to gracefully handle missing secrets
- Only deploys for the main repository, not forks
- Deployment token is scoped to specific Azure Static Web App
- Uses read-only `GITHUB_TOKEN` for PR comments

**Example Snippet:**
```yaml
on:
  pull_request:
    types: [opened, synchronize, reopened, closed]
    branches:
      - main

jobs:
  build_and_deploy_job:
    if: github.event_name == 'pull_request' && github.event.action != 'closed' && github.repository == 'NuGetPackageExplorer/NuGetPackageExplorer'
    runs-on: ubuntu-latest
    container: 'unoplatform/wasm-build:3.0'
```

---

### Dependabot

**Name:** Dependabot Dependency Updates

**Purpose:** Automatically checks for and creates pull requests to update NuGet package dependencies.

**Configuration File:** [`.github/dependabot.yml`](./.github/dependabot.yml)

**Triggers:**
- Scheduled: Daily
- Manual: Can be triggered from GitHub UI

**What It Does:**
- Scans NuGet package dependencies in the repository root
- Creates pull requests to update outdated dependencies
- Limits to 10 open pull requests at a time
- Respects ignore rules (e.g., Microsoft.Web.Xdt > 2.1.2)

**Permissions / Secrets Required:**
- GitHub's built-in Dependabot service (no additional secrets required)
- Uses `GITHUB_TOKEN` automatically

**Ownership:** GitHub Dependabot service (configured by repository maintainers)

**How to Disable:**
- Temporarily: Close pending PRs and add dependencies to ignore list
- Permanently: Delete `.github/dependabot.yml`
- Per-dependency: Add to `ignore` section in config file

**Security Considerations:**
- Dependabot PRs should be reviewed before merging
- Automated updates can introduce breaking changes
- Consider enabling Dependabot security updates for faster security patches
- Limit open PRs to avoid overwhelming maintainers (currently set to 10)

**Example Snippet:**
```yaml
version: 2
updates:
- package-ecosystem: nuget
  directory: "/"
  schedule:
    interval: daily
  open-pull-requests-limit: 10
  ignore:
  - dependency-name: Microsoft.Web.Xdt
    versions:
    - "> 2.1.2"
```

**TODO for maintainers:** Verify if security-only updates should be enabled separately.

---

### Release Drafter

**Name:** Release Drafter

**Purpose:** Automatically drafts release notes based on pull requests and labels.

**Configuration File:** [`.github/release-drafter.yml`](./.github/release-drafter.yml)

**Triggers:**
- Automatically runs when pull requests are merged to default branch
- Requires GitHub Action workflow to be set up (not currently visible in `.github/workflows/`)

**What It Does:**
- Drafts release notes by collecting merged pull requests
- Organizes changes by category (if labels are used)
- Creates a draft release that maintainers can review and publish

**Permissions / Secrets Required:**
- `GITHUB_TOKEN` - Provided automatically by GitHub Actions
- Requires write access to releases

**Ownership:** Repository maintainers

**How to Disable:**
- Delete `.github/release-drafter.yml`
- Remove the associated GitHub Action workflow (if exists)

**Security Considerations:**
- Only drafts releases, does not publish automatically
- Uses minimal permissions (read PRs, write draft releases)
- Review draft releases before publishing

**Example Snippet:**
```yaml
template: |
  ## What's Changed

  $CHANGES
```

**TODO for maintainers:** 
- Verify if the corresponding GitHub Action workflow exists for Release Drafter
- Consider expanding the template to include categories (features, bug fixes, etc.)
- Add labels to PRs to enable automatic categorization

---

## How to Add a New Agent

When adding a new automated agent or workflow to this repository:

1. **Create the agent configuration:**
   - For GitHub Actions: Add a `.yml` file to `.github/workflows/`
   - For other services: Add appropriate configuration files

2. **Document the agent in this file:**
   ```markdown
   ### [Agent Name]

   **Name:** [Full descriptive name]

   **Purpose:** [Brief description of what it does]

   **Configuration File:** [Link to config file]

   **Triggers:** [When it runs]

   **What It Does:** [Detailed actions]

   **Permissions / Secrets Required:** [List secrets/permissions]

   **Ownership:** [Team or individual responsible]

   **How to Disable:** [Instructions]

   **Security Considerations:** [Security notes]

   **Example Snippet:** [Small config excerpt]
   ```

3. **Test the agent:**
   - Test in a feature branch first
   - Verify permissions are minimal and appropriate
   - Ensure secrets are properly secured

4. **Update this document:**
   - Add entry to Table of Contents
   - Add detailed section
   - Update Change History

## Best Practices

### Security

- **Least Privilege:** Grant agents only the minimum permissions required
- **Secret Management:** 
  - Store secrets in GitHub Secrets or Azure Key Vault
  - Never commit secrets to repository
  - Rotate secrets regularly
  - Use environment-specific secrets when possible
- **Code Review:** Always review changes made by automated agents
- **Scope Limitation:** Limit agent actions to specific branches or conditions when possible

### Testing

- **Test Locally:** When possible, test workflows locally using tools like:
  - [act](https://github.com/nektos/act) for GitHub Actions
  - Azure CLI for Azure Pipelines
- **Feature Branches:** Test new agents in feature branches before enabling on main
- **Monitoring:** Monitor agent execution and review logs regularly
- **Fail-Safe:** Configure agents to fail gracefully and not block development

### Maintenance

- **Documentation:** Keep this document up to date when adding/modifying agents
- **Regular Review:** Periodically review agent configurations and permissions
- **Dependency Updates:** Keep agent dependencies and actions up to date
- **Cleanup:** Remove unused agents and workflows

## Change History

| Date | Change | Author |
|------|--------|--------|
| 2025-10-28 | Initial creation - Auto-discovered 4 agents (Azure Pipelines, GitHub Actions PR Validation, Dependabot, Release Drafter) | Automated Agent |
| 2025-10-28 | Expanded with comprehensive development environment setup, coding standards, language features, and project structure information | Automated Agent |

**Auto-Discovery Notes:**
- Scanned `.github/workflows/` - Found 1 GitHub Action workflow
- Scanned `.github/` - Found Dependabot and Release Drafter configurations
- Scanned root directory - Found Azure Pipelines configuration
- Extracted coding standards from `.editorconfig`
- Extracted build configuration from `Directory.Build.props`, `global.json`
- Documented project structure and testing approach
- No Renovate, CircleCI, AppVeyor, or custom bot scripts detected
- No CODEOWNERS file detected

**Items Requiring Maintainer Verification:**
- [ ] Confirm ownership and contact information for each agent
- [ ] Verify Azure DevOps permissions and secret scope
- [ ] Check if Release Drafter GitHub Action workflow exists
- [ ] Review and update security considerations as needed
- [ ] Verify .NET SDK version requirements are current
- [ ] Add any additional agents not automatically detected
