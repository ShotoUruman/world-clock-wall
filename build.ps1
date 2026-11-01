param([string]$OutputPath = "$PSScriptRoot\WorldClockWall.exe")
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /optimize+ /platform:anycpu "/out:$OutputPath" "/win32manifest:$PSScriptRoot\app.manifest" "/win32icon:$PSScriptRoot\clock.ico" "/resource:$PSScriptRoot\assets\land.bin,WorldClock.Land" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll "$PSScriptRoot\WorldClock.cs" "$PSScriptRoot\SettingsForm.cs" "$PSScriptRoot\SolarMap.cs" "$PSScriptRoot\TimeAudit.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output 'Built WorldClockWall.exe using the installed Microsoft .NET Framework compiler.'
