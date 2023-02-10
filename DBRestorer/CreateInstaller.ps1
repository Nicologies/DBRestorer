$ErrorActionPreference = 'Stop'
Set-StrictMode -Version latest

if(Test-Path "$PSScriptRoot/Releases")
{
    Remove-Item -Force -Recurse "$PSScriptRoot/Releases"
}

$squirrelInstallFolder = "$PSScriptRoot/temp"
if(Test-Path $squirrelInstallFolder)
{
    Remove-Item -Force -Recurse $squirrelInstallFolder
}
$dbRestorerRelativePath = "$PSScriptRoot/bin/Release/net462/DBRestorer.exe"
$dbRestorerAbsolutePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($dbRestorerRelativePath)
$assembly = [Reflection.Assembly]::Loadfile($dbRestorerAbsolutePath)
[Version]$version = [Version]$assembly.GetName().version
Write-Host "Version from assembly is: $version"
$nugetSemVer = $version.Major.ToString() + "." + $version.Minor + "." + $version.Build

# for squirrel that requires semver
nuget pack "$PSScriptRoot/DBRestorer.csproj" -OutputDirectory $PSScriptRoot `
    -Properties Configuration=Release -Version $nugetSemVer
nuget install squirrel.windows -ExcludeVersion -OutputDirectory $squirrelInstallFolder

function ExecuteProcessAndWait([string]$processFileName, [string]$processParameters, [string]$workingDirectory)
{
	$pinfo = New-Object System.Diagnostics.ProcessStartInfo
	$pinfo.FileName = "$processFileName"
	$pinfo.RedirectStandardError = $true
	$pinfo.RedirectStandardOutput = $true
	$pinfo.UseShellExecute = $false
	$pinfo.WorkingDirectory = $workingDirectory
	$pinfo.Arguments = $processParameters
	$pinfo.Verb = "runas"
	$p = New-Object System.Diagnostics.Process
	$p.StartInfo = $pinfo
	$p.Start() | Out-Null
    $output = $p.StandardOutput.ReadToEnd()

    if (-not ([string]::IsNullOrWhiteSpace($output)))
    {
        Write-Error $output
    }
    $errors = $p.StandardError.ReadToEnd()
    if (-not ([string]::IsNullOrWhiteSpace($errors)))
    {
        Write-Error $errors
    }
	$p.WaitForExit() | Out-Null
	return $p.ExitCode
}

$procPath = "$squirrelInstallFolder/squirrel.windows/tools/Squirrel.exe"
$args = "--releasify $PSScriptRoot/DBRestorer.$nugetSemVer.nupkg -no-msi"
$exitcode = ExecuteProcessAndWait -processFileName $procPath -processParameters $args -workingDirectory $PSScriptRoot
if($exitcode -ne 0)
{
    $err = "Failed to create auto update files, error " + $exitcode.ToString()
    Write-Error $err
    throw $err
}
