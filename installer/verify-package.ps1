$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$compiler = Join-Path $projectRoot '.tools\InnoSetup\ISCC.exe'
& $compiler /Q /DVerificationBuild (Join-Path $PSScriptRoot 'CharacterLauncher.iss')
if ($LASTEXITCODE -ne 0) { throw 'Verification installer compilation failed.' }
$testRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'verification\artifacts\installed-package'))
$workspacePrefix = [IO.Path]::GetFullPath($projectRoot).TrimEnd('\') + '\'
if (!$testRoot.StartsWith($workspacePrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Test path escapes the workspace.' }
$setup = Join-Path $PSScriptRoot 'output\Z-Orbit-Packaging-Verification.exe'
$installArgs = @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOICONS',('/DIR="' + $testRoot + '"'),('/LOG="' + (Join-Path $projectRoot 'verification\artifacts\install.log') + '"'))
$process = Start-Process -FilePath $setup -ArgumentList $installArgs -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Install failed: $($process.ExitCode)" }
$installedExe = Join-Path $testRoot 'Z-Orbit.exe'
$publishedExe = Join-Path $projectRoot 'publish\Z-Orbit\Z-Orbit.exe'
if ((Get-FileHash -LiteralPath $installedExe).Hash -ne (Get-FileHash -LiteralPath $publishedExe).Hash) { throw 'Installed EXE hash mismatch.' }
$config = Get-Content -LiteralPath (Join-Path $testRoot 'apps.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($config.apps.Count -ne 3) { throw 'Default application count mismatch.' }
if (($config.apps.target -join '|') -ne 'https://grok.com/|https://claude.ai/|https://chatgpt.com/') { throw 'Public website defaults mismatch.' }
if ($config.cleanMode -ne $false) { throw 'First-run help must remain visible.' }
if ($config.language -ne 'zh-CN') { throw 'First-run language must remain Simplified Chinese.' }
if (Get-ChildItem -LiteralPath $testRoot -Recurse -Filter '*.lnk') { throw 'Public installer contains local shortcuts.' }
if (!(Test-Path -LiteralPath (Join-Path $testRoot 'LICENSE'))) { throw 'Missing GPL license.' }
foreach ($entry in $config.apps) {
    if (!(Test-Path -LiteralPath (Join-Path $testRoot $entry.avatar))) { throw "Missing packaged avatar: $($entry.name)" }
    if ($entry.target -notmatch '^https?://' -and !(Test-Path -LiteralPath (Join-Path $testRoot $entry.target))) { throw "Missing packaged shortcut: $($entry.name)" }
}
Write-Output 'PASS: Installer installs matching EXE, 3 web entries and avatars.'
$process = Start-Process -FilePath $setup -ArgumentList $installArgs -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Reinstall failed: $($process.ExitCode)" }
Write-Output 'PASS: Repeat installation succeeds.'
$uninstaller = Join-Path $testRoot 'unins000.exe'
$process = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Uninstall failed: $($process.ExitCode)" }
if (Test-Path -LiteralPath $installedExe) { throw 'Uninstall left the installed application.' }
Write-Output 'PASS: Uninstall removes test application.'
Write-Output 'Packaging test used a separate AppId and no running-app mutex. No target applications were launched.'
