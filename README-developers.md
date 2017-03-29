How to change version number:
===

1. Change version in properties of the PackageExplorer project:
<img src="https://cloud.githubusercontent.com/assets/5808377/13398046/e459d2a4-defc-11e5-837a-b67dbfbcd89e.png" width="600">

2.  Change in "CommonAssemblyInfo.cs" <br>
<img src="https://cloud.githubusercontent.com/assets/5808377/13398070/013b1df6-defd-11e5-9f7c-2135bd298453.png" width="600">


How to publish the ClickOnce (legacy)
====

<img src="https://cloud.githubusercontent.com/assets/5808377/13203106/5f2f18a8-d8af-11e5-98fc-530b9f9d18c3.png" width="400">

<img src="https://cloud.githubusercontent.com/assets/5808377/13203107/62992a60-d8af-11e5-8446-1d178776c0e7.png" width="400">

<img src="https://cloud.githubusercontent.com/assets/5808377/13203109/65303066-d8af-11e5-8483-5f0f2cdeff92.png" width="400">

<img src="https://cloud.githubusercontent.com/assets/5808377/13203110/70861066-d8af-11e5-8969-2341d2557481.png" width="400">

Upload Zip on CodePlex. (Note a clickonce application cannot be released on GitHub)

How to publish on Chocolatey
===

Update: this could be done now from AppVeyor

Old steps:

1. Create a release build
2. Create a zip of the following files: .exe, .dll's, the .config
3. Upload zip to CodePlex to https://npe.codeplex.com/releases/view/68211
4. Copy the download id (last number) from the URL.
5. Download current Chocolatey package on https://chocolatey.org/packages/NugetPackageExplorer
6. Edit the version number
7. Change the `DownloadId` in the querystring to the download in step 4.
8. Upload to [Chocolatey.org](https://chocolatey.org)
