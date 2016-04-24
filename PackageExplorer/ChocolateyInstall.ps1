
    $drop = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
    $exe = "$drop\NugetPackageExplorer.exe"
    Install-ChocolateyDesktopLink $exe
    $allTypes = (cmd /c assoc)
    $testType = $allTypes | ? { $_.StartsWith('.nupkg') }
    if($testType -ne $null) {
        $fileType=$testType.Split("=")[1]
    } 
    else {
        $fileType="Nuget.Package"
        Start-ChocolateyProcessAsAdmin "cmd /c assoc .nupkg=$fileType"
    }
    Start-ChocolateyProcessAsAdmin "cmd /c ftype $fileType=`"$exe`" %1"
