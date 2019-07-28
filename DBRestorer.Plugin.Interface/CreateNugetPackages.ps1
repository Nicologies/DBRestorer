$ErrorActionPreference = 'Stop'
Remove-Item -Force ./dist/*nupkg -ErrorAction Ignore
nuget pack $PSScriptRoot/Nicologies.DBRestorer.Plugin.Interface.csproj `
    -OutputDirectory $PSScriptRoot/dist -Prop Configuration=Release
