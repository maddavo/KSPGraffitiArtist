param([string]$KspRoot='C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program')
$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
    $dirty=git status --porcelain --untracked-files=normal
    if($LASTEXITCODE -ne 0 -or $dirty){throw 'Commit the source changes before packaging the exact revision'}
    $revision=git rev-parse HEAD
    if($LASTEXITCODE -ne 0){throw 'Cannot identify source revision'}
    $buildOutput=& (Join-Path $PSScriptRoot 'build.ps1') -KspRoot $KspRoot | Out-String
    $testOutput=& (Join-Path $PSScriptRoot 'test.ps1') -KspRoot $KspRoot | Out-String
    $candidate=Join-Path $PSScriptRoot ('packages\KSPGraffitiArtist-0.1.4-candidate-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Path $candidate | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'GameData') -Destination $candidate -Recurse
    $addon=Join-Path $candidate 'GameData\KSPGraffitiArtist'
    New-Item -ItemType Directory -Path (Join-Path $addon 'Plugins') | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'build\KSPGraffitiArtist.dll') -Destination (Join-Path $addon 'Plugins')
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $addon 'README.md')
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs') -Destination (Join-Path $addon 'docs') -Recurse
    $sourceArchive=Join-Path $candidate 'source-0.1.4.zip'
    git archive --format=zip ('--output='+$sourceArchive) HEAD
    if($LASTEXITCODE -ne 0){throw 'Source archive failed'}
    $zip=Join-Path $candidate 'KSPGraffitiArtist-0.1.4.zip'
    Compress-Archive -LiteralPath (Join-Path $candidate 'GameData') -DestinationPath $zip
    $files=@(Get-ChildItem -LiteralPath (Join-Path $candidate 'GameData') -Recurse -File | Sort-Object FullName | ForEach-Object {
        [ordered]@{path=$_.FullName.Substring($candidate.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash;bytes=$_.Length}
    })
    $dependencies=@('KSP_x64_Data\Managed\Assembly-CSharp.dll','GameData\KIS\Plugins\KIS.dll','GameData\KIS\Plugins\KSPDev_Utils.2.6.dll') | ForEach-Object {
        [ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath (Join-Path $KspRoot $_) -Algorithm SHA256).Hash}
    }
    $receipt=[ordered]@{project='KSPGraffitiArtist';version='0.1.4';state='prepared_not_installed';source_commit=$revision;source_repository='https://github.com/maddavo/KSPGraffitiArtist';files=$files;dependencies=@($dependencies);zip_sha256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash;source_zip_sha256=(Get-FileHash -LiteralPath $sourceArchive -Algorithm SHA256).Hash;validation=$testOutput.Trim()}
    $utf8=New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText((Join-Path $candidate 'candidate-receipt.json'),($receipt | ConvertTo-Json -Depth 7),$utf8)
    [IO.File]::WriteAllText((Join-Path $candidate 'offline-checks.txt'),($buildOutput+$testOutput),$utf8)
    Write-Output "Prepared candidate (not installed): $candidate"
    Write-Output ($receipt | ConvertTo-Json -Depth 7)
} finally {Pop-Location}
