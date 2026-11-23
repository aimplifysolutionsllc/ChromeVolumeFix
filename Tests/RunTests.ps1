$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testDir = Join-Path $repoRoot 'artifacts\tests'
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
$testExe = Join-Path $testDir 'RepairTests.exe'
& $compiler /nologo /target:exe /main:RepairTests /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll ("/out:" + $testExe) (Join-Path $repoRoot 'Source\Program.cs') (Join-Path $PSScriptRoot 'RepairTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
