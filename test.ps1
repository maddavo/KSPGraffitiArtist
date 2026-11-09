param([string]$KspRoot='C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program')
$ErrorActionPreference='Stop'
$out=Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Force $out | Out-Null
$files=@('Source\MarkingData.cs','Source\WorldSurfaceData.cs','Source\WorldPaintStore.cs','Source\FontCoverage.cs','Source\PawIntegration.cs','Source\SurfaceGeometry.cs','Source\SurfaceModule.cs','Tests\Host.cs','Tests\Tests.cs','Tests\GeometryTests.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /warnaserror+ ('/out:'+(Join-Path $out 'GraffitiArtistTests.exe')) @files
if($LASTEXITCODE -ne 0){throw 'Test compilation failed'}
& (Join-Path $out 'GraffitiArtistTests.exe')
if($LASTEXITCODE -ne 0){throw 'Offline tests failed'}
$atlasFiles=@('Source\MarkingData.cs','Source\UserFonts.cs','Source\PaintFonts.cs','Source\PaintMaterial.cs','Source\FontCoverage.cs','Source\FontCoverageAtlas.cs','Tests\FontAtlasHost.cs','Tests\FontAtlasTests.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /warnaserror+ ('/out:'+(Join-Path $out 'FontAtlasTests.exe')) @atlasFiles
if($LASTEXITCODE -ne 0){throw 'Atlas-host test compilation failed'}
& (Join-Path $out 'FontAtlasTests.exe')
if($LASTEXITCODE -ne 0){throw 'Font atlas capture regression failed'}
if(Test-Path -LiteralPath (Join-Path $out 'KSPGraffitiArtist.dll')){
    & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /warnaserror+ ('/out:'+(Join-Path $out 'InstalledApi.exe')) (Join-Path $PSScriptRoot 'Tests\InstalledApi.cs')
    if($LASTEXITCODE -ne 0){throw 'API-check compilation failed'}
    & (Join-Path $out 'InstalledApi.exe') $KspRoot (Join-Path $out 'KSPGraffitiArtist.dll') $PSScriptRoot
    if($LASTEXITCODE -ne 0){throw 'Installed API checks failed'}
}

# Optional read-only compiled-variant audit when the local UnityPy helper is present.
$unityReader=Join-Path $out 'unity-reader'
if(Test-Path -LiteralPath (Join-Path $unityReader 'UnityPy')){
    python (Join-Path $PSScriptRoot 'Tools/audit_paint_shader.py') $unityReader $KspRoot (Join-Path $out 'paint-shader-audit.json')
    if($LASTEXITCODE -ne 0){throw 'Installed shader variant audit failed'}
}

$terrainFiles=@('Source/TerrainSurfaces.cs','Tests/TerrainHost.cs','Tests/TerrainTests.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
$terrainCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $terrainCompiler /nologo /warnaserror+ ('/out:'+(Join-Path $out 'TerrainTests.exe')) @terrainFiles
if($LASTEXITCODE -ne 0){throw 'Terrain-host compilation failed'}
& (Join-Path $out 'TerrainTests.exe')
if($LASTEXITCODE -ne 0){throw 'Terrain resolver regression failed'}
$pickingFiles=@('Source/PaintPicking.cs','Tests/PickingTests.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
& $terrainCompiler /nologo /warnaserror+ ('/out:'+(Join-Path $out 'PickingTests.exe')) @pickingFiles
if($LASTEXITCODE -ne 0){throw 'Picking-host compilation failed'}
& (Join-Path $out 'PickingTests.exe')
if($LASTEXITCODE -ne 0){throw 'Local picking regression failed'}
$windowsFontFiles=@('Source/MarkingData.cs','Source/UserFonts.cs','Source/WindowsTextRasterizer.cs','Tests/WindowsFontTests.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
& $terrainCompiler /nologo /warnaserror+ ('/out:'+(Join-Path $out 'WindowsFontTests.exe')) @windowsFontFiles
if($LASTEXITCODE -ne 0){throw 'Windows font test compilation failed'}
& (Join-Path $out 'WindowsFontTests.exe') (Join-Path $out 'countdown-native-proof.pgm')
if($LASTEXITCODE -ne 0){throw 'Windows font raster regression failed'}
