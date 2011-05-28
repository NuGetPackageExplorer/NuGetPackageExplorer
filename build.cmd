@echo Off

set password=%1

set deployUrl=%2
if "%deployUrl%" == "" (
   set deployUrl=http://nuget.codeplex.com/releases/clickonce/
)
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild build\build.proj /p:Configuration=release;DeploymentUrl="%deployUrl%";Password="%password%";EnableCodeAnalysis=true