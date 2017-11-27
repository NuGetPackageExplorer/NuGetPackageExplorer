![Logo](https://cloud.githubusercontent.com/assets/5808377/11324261/06c2ccd8-912d-11e5-87e4-9898b2217baa.png)

[![Build status](https://ci.appveyor.com/api/projects/status/nhowjp0e1w0225v7/branch/master?svg=true)](https://ci.appveyor.com/project/NuGetPackageExplorer/nugetpackageexplorer/branch/master)
[![Twitter Follow](https://img.shields.io/twitter/follow/NuGetPE.svg?style=social?maxAge=2592000)](https://twitter.com/NuGetPE)
[![Join the chat at https://gitter.im/NuGetPackageExplorer/NuGetPackageExplorer](https://badges.gitter.im/NuGetPackageExplorer/NuGetPackageExplorer.svg)](https://gitter.im/NuGetPackageExplorer/NuGetPackageExplorer?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## How to install
You could install with the [Windows 10 Store](https://www.microsoft.com/store/apps/9wzdncrdmdm3) or [Chocolatey](https://chocolatey.org/packages/NugetPackageExplorer). The Microsoft Store is the preferred version for Windows 10 Anniversary Update and later.

There also a nightly build available for direct install on Windows 10 Anniversary Update and higher. The nightly build installs alongside
the release version with no interference.

| Build Number | Link |
| ------------ | ---- |
| ![Nightly build number](https://nugetpackageexplorer.blob.core.windows.net/nightly/version_badge.svg?v=1)| [Download](https://nugetpackageexplorer.blob.core.windows.net/nightly/PackageExplorer.Package.Nightly.appxbundle)
| ![Stable build number](https://nugetpackageexplorer.blob.core.windows.net/stable/version_badge.svg) | [Microsoft Store](https://www.microsoft.com/store/apps/9wzdncrdmdm3) |
| ![Chocolatey build number](https://img.shields.io/chocolatey/v/NugetPackageExplorer.svg) | [Chocolatey](https://chocolatey.org/packages/NugetPackageExplorer) |


### Microsoft Store (recommended, Windows 10 Anniversary Update needed)
<a href="https://www.microsoft.com/store/apps/9wzdncrdmdm3?ocid=badge"><img height="104" width="288" src="https://assets.windowsphone.com/f2f77ec7-9ba9-4850-9ebe-77e366d08adc/English_Get_it_Win_10_InvariantCulture_Default.png" alt="Get it on Windows 10"></a>

### Chocolatey
Chocolatey is another great way to install and update your application. 

1. Run PowerShell (as Admin)
2. Install Chocolatey: `iwr https://chocolatey.org/install.ps1 -UseBasicParsing | iex`
3. Install NuGet Package Explorer: `choco install nugetpackageexplorer`


## What is NuGet Package Explorer?

NuGet Package Explorer (NPE) is an application that makes it easy to create and explore NuGet packages. You can load a .nupkg file from disk or directly from a feed such as [nuget.org](https://www.nuget.org/).

To build packages from the command line, use NuGet command-line tools, as documented on the [official NuGet site](https://docs.nuget.org/ndocs/create-packages/creating-a-package).

![image](https://cloud.githubusercontent.com/assets/5808377/13399085/cefc7a10-df01-11e5-88b9-423a90107dce.png)

## Current development state / looking for developers

Currently NPE isn't actively developed, but we do accept (not too large) pull requests (PR).

If you'd like to help, please check the GitHub [issues](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/issues). If you'd like to contribute more structurally, we would be happy to add you to our team! 

## Issues

Please check the [FAQ](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer/wiki) first and search for duplicate issues before reporting them. 

## Creating a Package

1. Launch NPE and select **File > New** (Ctrl-N), or select **Create a new package** from the **Common tasks** dialog when Package Explorer starts:

	![Package Explorer's common tasks dialog](https://cloud.githubusercontent.com/assets/1339874/19167418/7bca3b18-8bc0-11e6-8ecf-de5b05ed8923.png)

2. Select **Edit > Edit Package Metadata** (Ctrl-K) to open the editor for the underlying .nuspec file. Details for the metadata can be found in the [nuspec reference](https://docs.nuget.org/ndocs/schema/nuspec).

	![Editing package metadata with the Package Explorer](https://cloud.githubusercontent.com/assets/1339874/19167426/8399b85a-8bc0-11e6-8516-6f0b53ddc595.png)

3. Open the files you want to include in the package in Windows explorer, then drag them into the **Package contents** pane of Package Explorer. Package Explorer will attempt to infer where the content belongs and prompt you to place it in the correct directory within the package. (You can also explicitly add specific folders using the **Content** menu.)

	For example, if you drag an assembly into the Package contents window, it will prompt you to place the assembly in the **lib** folder:

	![Package Explorer infers content location and prompts for confirmations](https://cloud.githubusercontent.com/assets/1339874/19167427/88c80fc0-8bc0-11e6-8d39-cc6e04024013.png)

	![The package's lib folder with added content](https://cloud.githubusercontent.com/assets/1339874/19167432/8e675a3a-8bc0-11e6-9848-0dd8cf73b4f9.png)


4. Save your package with **File > Save** (Ctrl-S).

## Publishing a Package

1. Create a free account on [nuget.org](http://nuget.org/), or log in if you already have one. When creating a new account, you'll receive a confirmation email. You must confirm the account before you can upload a package.

2. Once logged in, click your username (on the upper right) to navigate to your account settings.

3. Under **API Key**, click **copy to clipboard** to retrieve the API key you'll need in the next step.

      ![Copying the API key from the nuget.org profile](https://cloud.githubusercontent.com/assets/1339874/19167409/6fd8d238-8bc0-11e6-86b4-49af64483d78.png)

4. Assuming your package is loaded in Package Explorer, select **File > Publish** (Ctrl-P) to bring up the **Publish Package** dialog.

	![Publish Package Dialog](https://cloud.githubusercontent.com/assets/1339874/19167436/90ebbbc0-8bc0-11e6-8cb1-68717ec811e7.png)

5. Paste your API key into **Publish key** and click **Publish** to push the package to nuget.org.

6. In your profile on nuget.org, click **Manage my Packages** to see the one that you just published; you'll also receive a confirmation email. Note that it might take a while for your package to be indexed and appear in search results, during which time you'll see a message that the package hasn't yet been indexed.

## Build

Requirements to build the project:

- VS2017 15.4 or later
- [Windows 10 SDK](https://developer.microsoft.com/en-US/windows/downloads/windows-10-sdk)
