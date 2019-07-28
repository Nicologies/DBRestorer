Param(
    [Parameter(Mandatory = $True)]
    [string]$version
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version latest

$assemblies = Get-ChildItem -Path $PSScriptRoot -Recurse -Include "AssemblyInfo.cs" `
   | Where-Object {$_.FullName -notmatch ".*ExtendedCL.*"}
foreach($assemblyInfoFile in $assemblies){
    $content = Get-Content $assemblyInfoFile
    $content = $content -replace "AssemblyVersion.*","AssemblyVersion(`"$version`")]"
    $content = $content -replace "AssemblyFileVersion.*","AssemblyFileVersion(`"$version`")]"
    Set-Content -Path $assemblyInfoFile -Value $content -Encoding UTF8
}