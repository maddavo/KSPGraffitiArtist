param([string]$KspRoot='C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program')
$ErrorActionPreference='Stop'
$managed=Join-Path $KspRoot 'KSP_x64_Data\Managed'
$out=Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Force $out | Out-Null
$refs=@('Assembly-CSharp.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.TextRenderingModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.InputLegacyModule.dll','UnityEngine.PhysicsModule.dll','UnityEngine.ImageConversionModule.dll','UnityEngine.UI.dll','UnityEngine.UIModule.dll') | ForEach-Object { '/reference:'+(Join-Path $managed $_) }
$refs+='/reference:'+(Join-Path $KspRoot 'GameData\KIS\Plugins\KIS.dll')
$refs+='/reference:'+(Join-Path $KspRoot 'GameData\KIS\Plugins\KSPDev_Utils.2.6.dll')
$sources=Get-ChildItem (Join-Path $PSScriptRoot 'Source') -Filter '*.cs' | Select-Object -ExpandProperty FullName
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:library /optimize+ /warnaserror+ ('/out:'+(Join-Path $out 'KSPGraffitiArtist.dll')) @refs @sources
if($LASTEXITCODE -ne 0){throw 'Graffiti Artist compilation failed'}
Get-FileHash -LiteralPath (Join-Path $out 'KSPGraffitiArtist.dll') -Algorithm SHA256
