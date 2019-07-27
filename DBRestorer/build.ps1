$ErrorActionPreference = 'Stop'

if(Test-Path "$PSScriptRoot/Releases")
{
    Remove-Item -Force -Recurse "$PSScriptRoot/Releases"
}

if(Test-Path "$PSScriptRoot/bin/Release")
{
	# SilentlyContinue as no sure why powershell locks the dbrestore.exe
    Remove-Item -Force -Recurse "$PSScriptRoot/bin/Release" -ErrorAction SilentlyContinue
}

$squirrelInstallFolder = "$PSScriptRoot/temp"
if(Test-Path $squirrelInstallFolder)
{
    Remove-Item -Force -Recurse $squirrelInstallFolder
}

# for squirrel that requires semver
nuget pack -OutputDirectory $PSScriptRoot -Properties Configuration=Release -Build
nuget install squirrel.windows -ExcludeVersion -OutputDirectory $squirrelInstallFolder

$dbRestorerRelativePath = "$PSScriptRoot/bin/Release/DBRestorer.exe"
$dbRestorerAbsolutePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($dbRestorerRelativePath)
$assembly = [Reflection.Assembly]::Loadfile($dbRestorerAbsolutePath)
$version = [Version]$assembly.GetName().version
$nugetSemVer = $version.Major.ToString() + "." + $version.Minor + "." + $version.Build

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
    Exit $exitcode
}
