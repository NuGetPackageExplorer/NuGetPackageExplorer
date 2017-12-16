@echo Off
setlocal enableextensions enabledelayedexpansion

set password=%1

set deployUrl=%2
if "%deployUrl%" == "" (
   set deployUrl=https://npe.codeplex.com/releases/clickonce/
)

if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" (
	FOR /F "delims=" %%E in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\installer\vswhere.exe" -latest -property installationPath') DO (
		set "MSBUILD_EXE=%%E\MSBuild\15.0\Bin\MSBuild.exe"
		if exist "!MSBUILD_EXE!" goto :build
	)
)

FOR %%E in (Enterprise, Professional, Community) DO (
	set "MSBUILD_EXE=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\%%E\MSBuild\15.0\Bin\MSBuild.exe"
	if exist "!MSBUILD_EXE!" goto :build
)

REM Couldn't be located in the standard locations, expand search
FOR /F "delims=" %%E IN ('dir /b /ad "%ProgramFiles(x86)%\Microsoft Visual Studio\"') DO (
	set "MSBUILD_EXE=%ProgramFiles(x86)%\Microsoft Visual Studio\%%E\MSBuild\15.0\Bin\MSBuild.exe"
	if exist "!MSBUILD_EXE!" goto :build

	FOR /F "delims=" %%F IN ('dir /b /ad "%ProgramFiles(x86)%\Microsoft Visual Studio\%%E"') DO (
		set "MSBUILD_EXE=%ProgramFiles(x86)%\Microsoft Visual Studio\%%E\%%F\MSBuild\15.0\Bin\MSBuild.exe"
		if exist "!MSBUILD_EXE!" goto :build
	)
)

echo Could not find MSBuild 15
set "MSBUILD_EXE=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"

:build
"%MSBUILD_EXE%" NuGetPackageExplorer.sln /t:Restore
"%MSBUILD_EXE%" NuGetPackageExplorer.sln /bl /verbosity:minimal /p:Configuration=Release;DeploymentUrl="%deployUrl%";Password="%password%";EnableCodeAnalysis=true
