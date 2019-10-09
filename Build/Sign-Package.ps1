$currentDirectory = split-path $MyInvocation.MyCommand.Definition

# See if we have the ClientSecret available
if([string]::IsNullOrEmpty($Env:SignClientSecret)){
	Write-Host "Client Secret not found, not signing packages"
	return;
}

# Setup Variables we need to pass into the sign client tool
$appSettings = "$currentDirectory\appsettings.json"
$fileList = "$currentDirectory\filelist.txt"

& $currentDirectory\SignClient "sign" -c "$appSettings" -b "$Env:ArtifactDirectory" -i "**/*.{appxbundle,appinstaller,zip,nupkg}" -f "$fileList" -r $Env:SignClientUser -s $Env:SignClientSecret -n "NuGet Package Explorer" -d "NuGet Package Explorer" -u "https://github.com/NuGetPackageExplorer/NuGetPackageExplorer"

if ($LASTEXITCODE -ne 0) {
  exit 1
}