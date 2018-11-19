
    $drop = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
    $exe = "$drop\NugetPackageExplorer.exe"
    Install-ChocolateyDesktopLink $exe
    $allTypes = (cmd /c assoc)
    $testType1 = $allTypes | ? { $_.StartsWith('.nupkg') }
    if($testType1 -ne $null) {
        $fileType1=$testType1.Split("=")[1]
    } 
    else {
        $fileType1="Nuget.Package"
        Start-ChocolateyProcessAsAdmin "cmd /c assoc .nupkg=$fileType1"
    }
    Start-ChocolateyProcessAsAdmin "cmd /c ftype $fileType1=`"$exe`" %1"

    $testType2 = $allTypes | ? { $_.StartsWith('.snupkg') }
    if($testType2 -ne $null) {
        $fileType2=$testType2.Split("=")[1]
    } 
    else {
        $fileType2="Nuget.SymbolPackage"
        Start-ChocolateyProcessAsAdmin "cmd /c assoc .snupkg=$fileType2"
    }
    Start-ChocolateyProcessAsAdmin "cmd /c ftype $fileType2=`"$exe`" %1"
