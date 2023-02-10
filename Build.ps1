param(
    [string]$version
)
$ErrorActionPreference = 'Stop'
Write-Host "version is ${version}"
msbuild '/t:restore;build' DBRestorer.sln /m /p:Configuration=Release /p:Version=$version
if($LastExitCode -ne 0)
{
    $err = "Failed to build, error " + $LastExitCode.ToString()
    Write-Error $err
    Exit $LastExitCode
}
Write-Host "Build finished, now creating the installer"
. $PSScriptRoot/DBRestorer/CreateInstaller.ps1
