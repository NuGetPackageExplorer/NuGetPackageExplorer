
    $drop = Join-Path (Split-Path -parent $MyInvocation.MyCommand.Definition) "tools"
    $exeName = "NugetPackageExplorer.exe"
    $exe = Join-Path $drop $exeName

    $pp = Get-PackageParameters

    if(-not $pp['NoDesktopShortcut']) {
        $desktop = Join-Path $env:Public -ChildPath 'Desktop'
        $shortcutFile =  Join-Path $desktop -ChildPath "$($exeName.Split('.')[0]).lnk"
        
        $shortcutArgs = @{
            ShortcutFilePath = $shortcutFile
            TargetPath = $exe
            WorkingDirectory = $drop
            Desciption = 'NuGet Package Explorer'
        }

        Install-ChocolateyShortcut @shortcutArgs

    }
    
    New-Item "$exe.gui" -Type File -Force | Out-Null

    # Generate ignore files for all exe files except "NugetPackageExplorer.exe".
    # This prevents chocolatey from generating shims for them.
    $exeFiles = Get-ChildItem $drop -Include *.exe -Recurse -Exclude $exeName

    foreach ($exeFile in $exeFiles) {
        # generate an ignore file
        New-Item "$exeFile.ignore" -Type File -Force | Out-Null
    }    

    $allTypes = (cmd /c assoc)
    $testType1 = $allTypes | ? { $_.StartsWith('.nupkg') }
    if($testType1 -ne $null) {
        $fileType1=$testType1.Split("=")[1]
    } 
    else {
        $fileType1="Nuget.Package"
        Start-ChocolateyProcessAsAdmin "cmd /c assoc .nupkg=$fileType1"
    }
    Start-ChocolateyProcessAsAdmin "cmd /c ftype $fileType1=```"$exe```" ```"%1```""

    $testType2 = $allTypes | ? { $_.StartsWith('.snupkg') }
    if($testType2 -ne $null) {
        $fileType2=$testType2.Split("=")[1]
    } 
    else {
        $fileType2="Nuget.SymbolPackage"
        Start-ChocolateyProcessAsAdmin "cmd /c assoc .snupkg=$fileType2"
    }
    Start-ChocolateyProcessAsAdmin "cmd /c ftype $fileType2=```"$exe```" ```"%1```""
