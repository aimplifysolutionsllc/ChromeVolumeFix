$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path (Split-Path $PSScriptRoot) 'ChromeVolumeRepair.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll ("/win32icon:" + (Join-Path $PSScriptRoot 'volume-repair.ico')) ("/win32manifest:" + (Join-Path $PSScriptRoot 'app.manifest')) ("/out:" + $output) (Join-Path $PSScriptRoot 'Program.cs') (Join-Path $PSScriptRoot 'ChromeAudio.cs')
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Host "Built $output"
