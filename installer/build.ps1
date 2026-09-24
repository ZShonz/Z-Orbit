param([string]$Compiler = (Join-Path $PSScriptRoot '..\.tools\InnoSetup\ISCC.exe'))
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.dotnet-home'
$dotnet = Join-Path $projectRoot '.dotnet\dotnet.exe'
if (!(Test-Path -LiteralPath $dotnet)) { $dotnet = 'dotnet' }
if (!(Test-Path -LiteralPath $Compiler)) { throw 'Inno Setup compiler was not found. Pass -Compiler with the path to ISCC.exe.' }
& $dotnet publish (Join-Path $projectRoot 'CharacterLauncher.csproj') -c Release -r win-x64 --self-contained true --no-restore -o (Join-Path $projectRoot 'publish\Z-Orbit')
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
& $Compiler (Join-Path $PSScriptRoot 'CharacterLauncher.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
