$ErrorActionPreference = 'Stop'
msbuild '/t:restore;build' DBRestorer.sln /m /p:Configuration=Release
if($LastExitCode -ne 0)
{
    $err = "Failed to build, error " + $LastExitCode.ToString()
    Write-Error $err
    Exit $LastExitCode
}
. $PSScriptRoot/DBRestorer/CreateInstaller.ps1
