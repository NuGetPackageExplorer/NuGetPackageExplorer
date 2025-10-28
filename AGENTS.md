# Repository Automation Documentation

This document provides a comprehensive overview of all automated agents, bots, and workflows used in the NuGetPackageExplorer/NuGetPackageExplorer repository.

## Table of Contents

- [Overview](#overview)
- [Agents and Workflows](#agents-and-workflows)
  - [Azure Pipelines CI/CD](#azure-pipelines-cicd)
  - [GitHub Actions - Azure Static Web App PR Validation](#github-actions---azure-static-web-app-pr-validation)
  - [Dependabot](#dependabot)
  - [Release Drafter](#release-drafter)
- [How to Add a New Agent](#how-to-add-a-new-agent)
- [Best Practices](#best-practices)
- [Change History](#change-history)

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

**Auto-Discovery Notes:**
- Scanned `.github/workflows/` - Found 1 GitHub Action workflow
- Scanned `.github/` - Found Dependabot and Release Drafter configurations
- Scanned root directory - Found Azure Pipelines configuration
- No Renovate, CircleCI, AppVeyor, or custom bot scripts detected
- No CODEOWNERS file detected

**Items Requiring Maintainer Verification:**
- [ ] Confirm ownership and contact information for each agent
- [ ] Verify Azure DevOps permissions and secret scope
- [ ] Check if Release Drafter GitHub Action workflow exists
- [ ] Review and update security considerations as needed
- [ ] Add any additional agents not automatically detected
