$currentDirectory = split-path $MyInvocation.MyCommand.Definition

# See if we have the ClientSecret available
if([string]::IsNullOrEmpty($Env:SignClientSecret)){
	Write-Host "Client Secret not found, not signing packages"
	return;
}

# Setup Variables we need to pass into the sign client tool
$appSettings = "$currentDirectory\appsettings.json"
$fileList = "$currentDirectory\filelist.txt"

$zips = gci $Env:ArtifactDirectory\*.zip | Select -ExpandProperty FullName

foreach ($zip in $zips){
	Write-Host "Submitting $zip for signing"

	& $currentDirectory\SignClient 'sign' -c $appSettings -i $zip -f $fileList -r $Env:SignClientUser -s $Env:SignClientSecret -n 'NuGet Package Explorer' -d 'NuGet Package Explorer' -u 'https://github.com/NuGetPackageExplorer/NuGetPackageExplorer' 

	Write-Host "Finished signing $zip"
}

Write-Host "Sign-Zip complete"
