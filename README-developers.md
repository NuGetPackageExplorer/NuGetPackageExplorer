How to publish
====


![image](https://cloud.githubusercontent.com/assets/5808377/13203106/5f2f18a8-d8af-11e5-98fc-530b9f9d18c3.png)

![image](https://cloud.githubusercontent.com/assets/5808377/13203107/62992a60-d8af-11e5-8446-1d178776c0e7.png)

![image](https://cloud.githubusercontent.com/assets/5808377/13203109/65303066-d8af-11e5-8483-5f0f2cdeff92.png)

![image](https://cloud.githubusercontent.com/assets/5808377/13203110/70861066-d8af-11e5-8969-2341d2557481.png)


Upload Zip on CodePlex. (Note a clickonce application cannot be released on GitHub)


How to publish on Chocolatey
===

1. Create a release build
2. Create a zip of the following files: .exe, .dll's, the .config
3. Upload zip to CodePlex to https://npe.codeplex.com/releases/view/68211
4. Copy the download id (last number) from the URL.
5. Download current Chocolatey package on https://chocolatey.org/packages/NugetPackageExplorer
6. Edit the version number
7. Change the `DownloadId` in the querystring to the download in step 4.
8. Upload to [Chocolatey.org](https://chocolatey.org)
