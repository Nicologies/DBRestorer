$ErrorActionPreference = 'Stop'
msbuild '/t:restore;build' DBRestorer.sln /m /p:Configuration=Release
if($LastExitCode -ne 0)
{
    $err = "Failed to build, error " + $LastExitCode.ToString()
    Write-Error $err
    Exit $LastExitCode
}
Write-Host "Build finished, now creating the installer"
. $PSScriptRoot/DBRestorer/CreateInstaller.ps1
