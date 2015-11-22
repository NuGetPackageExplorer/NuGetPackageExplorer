param($installPath, $toolpath, $package, $project) 

$project.Object.References.Item("NuGetPackageExplorer.Types").CopyLocal = $false