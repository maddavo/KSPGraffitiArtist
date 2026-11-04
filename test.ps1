param([string]$KspRoot='C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program')
$ErrorActionPreference='Stop'
$out=Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Force $out | Out-Null
$files=@('Source\MarkingData.cs','Source\SurfaceGeometry.cs','Source\SurfaceModule.cs','Tests\Host.cs','Tests\Tests.cs','Tests\GeometryTests.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /warnaserror+ ('/out:'+(Join-Path $out 'GraffitiArtistTests.exe')) @files
if($LASTEXITCODE -ne 0){throw 'Test compilation failed'}
& (Join-Path $out 'GraffitiArtistTests.exe')
if($LASTEXITCODE -ne 0){throw 'Offline tests failed'}
if(Test-Path -LiteralPath (Join-Path $out 'KSPGraffitiArtist.dll')){
    & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /warnaserror+ ('/out:'+(Join-Path $out 'InstalledApi.exe')) (Join-Path $PSScriptRoot 'Tests\InstalledApi.cs')
    if($LASTEXITCODE -ne 0){throw 'API-check compilation failed'}
    & (Join-Path $out 'InstalledApi.exe') $KspRoot (Join-Path $out 'KSPGraffitiArtist.dll') $PSScriptRoot
    if($LASTEXITCODE -ne 0){throw 'Installed API checks failed'}
}
