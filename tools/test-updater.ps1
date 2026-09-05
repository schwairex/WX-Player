param([Parameter(Mandatory=$true)][string]$AppExe,[string]$OutputPath=(Join-Path $PSScriptRoot '..\artifacts\updater-test'))
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$root=[IO.Path]::GetFullPath($OutputPath)
if(Test-Path -LiteralPath $root){throw 'Use a fresh output directory for the isolated updater test.'}
New-Item -ItemType Directory -Path $root | Out-Null
$payload=Join-Path $root 'fixture-payload'
& dotnet publish (Join-Path $repo 'tests\WXPlayer.UpdateFixture') -c Release -r win-x64 --self-contained false -m:1 -p:UseSharedCompilation=false -o $payload
if($LASTEXITCODE -ne 0){throw 'Fixture build failed'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=Join-Path $root 'fixture.zip'
[IO.Compression.ZipFile]::CreateFromDirectory($payload,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
$hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
$info=Join-Path $root 'PayloadInfo.cs'
[IO.File]::WriteAllText($info,('internal static class PayloadInfo { public const string Sha256="'+$hash+'"; public const string Version="1.4.0"; }'))
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$fixture=Join-Path $root 'future-WXPlayer.exe'
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:x64 /optimize+ "/out:$fixture" /r:System.Windows.Forms.dll /r:System.Drawing.dll "/r:$(Join-Path $framework 'System.IO.Compression.dll')" "/r:$(Join-Path $framework 'System.IO.Compression.FileSystem.dll')" "/resource:$zip,WXPlayer.Payload.zip" (Join-Path $repo 'tools\Launcher.cs') $info
if($LASTEXITCODE -ne 0){throw 'Fixture launcher build failed'}
$data=Join-Path $root 'isolated data'
$oldRoot=$env:WXPLAYER_APP_ROOT
try {
    $env:WXPLAYER_APP_ROOT=Join-Path $root 'runtime'
    $app=[IO.Path]::GetFullPath($AppExe)
    $process=Start-Process -FilePath $app -ArgumentList @('--smoke','--data-dir',('"'+$data+'"'),'--restart-fixture',('"'+$fixture+'"')) -WindowStyle Hidden -PassThru
    if(-not $process.WaitForExit(120000)){throw 'WX Player smoke test timed out'}
    $result=Get-Content -Raw -LiteralPath (Join-Path $data 'smoke-results.json') | ConvertFrom-Json
    if(-not $result.success -or -not $result.updaterRestartDispatched){throw 'Updater dispatch failed'}
    $marker=Join-Path $data 'fixture-activated.json'
    $deadline=(Get-Date).AddSeconds(40)
    while(-not(Test-Path -LiteralPath $marker)){if((Get-Date) -gt $deadline){throw 'Future version did not activate'};Start-Sleep -Milliseconds 200}
    $activated=Get-Content -Raw -LiteralPath $marker | ConvertFrom-Json
    if(-not $activated.previousExited -or -not $activated.pointer -or -not $activated.dataPreserved){throw 'Restart activation assertions failed'}
    $process=Start-Process -FilePath $app -ArgumentList @('--data-dir',('"'+$data+'"')) -WindowStyle Hidden -PassThru
    $process.WaitForExit(30000) | Out-Null
    $counter=Join-Path $data 'fixture-launch-count.txt'
    $deadline=(Get-Date).AddSeconds(30)
    while([int](Get-Content -Raw -LiteralPath $counter) -lt 2){if((Get-Date) -gt $deadline){throw 'Old shortcut did not forward'};Start-Sleep -Milliseconds 200}
    [pscustomobject]@{success=$true;verifiedDownload=$true;waitedForOldProcess=$activated.previousExited;activatedNewVersion=$activated.version;oldShortcutForwarded=$true;libraryPreserved=$activated.dataPreserved;releaseWasLocalFixture=$true} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'updater-integration-results.json') -Encoding utf8
    Get-Content -LiteralPath (Join-Path $root 'updater-integration-results.json')
} finally {$env:WXPLAYER_APP_ROOT=$oldRoot}

