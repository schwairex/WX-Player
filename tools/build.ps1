param([string]$OutputPath = (Join-Path $PSScriptRoot '..\artifacts'), [switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$outDir = [IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Force $outDir | Out-Null
if (-not $SkipTests) {
    & dotnet run --project (Join-Path $repo 'tests\WXPlayer.Tests') -c Release -p:UseSharedCompilation=false -- (Join-Path $outDir 'test-results.json')
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
}
$portable = Join-Path $outDir 'WXPlayer-win-x64'
if (Test-Path $portable) { throw "Output already exists: $portable. Select a new output directory to avoid stale files." }
& dotnet publish (Join-Path $repo 'src\WXPlayer.App\WXPlayer.App.csproj') -c Release -m:1 -p:UseSharedCompilation=false -r win-x64 --self-contained true -p:VlcWindowsX86Enabled=false -p:VlcWindowsArm64Enabled=false -p:DebugType=None -o $portable
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
Copy-Item -LiteralPath (Join-Path $repo 'README.md'),(Join-Path $repo 'LICENSE'),(Join-Path $repo 'THIRD-PARTY-NOTICES.md') -Destination $portable
Copy-Item -LiteralPath (Join-Path $repo 'licenses') -Destination $portable -Recurse
Copy-Item -LiteralPath (Join-Path $repo 'docs') -Destination $portable -Recurse
Add-Type -AssemblyName System.IO.Compression.FileSystem
$payload = Join-Path $outDir 'WXPlayer-win-x64.zip'
[IO.Compression.ZipFile]::CreateFromDirectory($portable,$payload,[IO.Compression.CompressionLevel]::Optimal,$false)
$hash = (Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash.ToLowerInvariant()
$version = ([xml](Get-Content -Raw (Join-Path $repo 'Directory.Build.props'))).Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must be major.minor.patch' }
$hashSource = Join-Path $outDir 'PayloadInfo.cs'
[IO.File]::WriteAllText($hashSource,('internal static class PayloadInfo { public const string Sha256 = "' + $hash + '"; public const string Version = "' + $version + '"; }'))
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $framework 'csc.exe'
if (-not (Test-Path $csc)) { throw '.NET Framework 4 compiler is required for the single EXE wrapper.' }
$launcher = Join-Path $outDir 'WXPlayer.exe'
& $csc /nologo /target:winexe /platform:x64 /optimize+ "/out:$launcher" "/win32icon:$(Join-Path $repo 'src\WXPlayer.App\Assets\wx.ico')" "/win32manifest:$(Join-Path $repo 'src\WXPlayer.App\app.manifest')" /r:System.Windows.Forms.dll /r:System.Drawing.dll "/r:$(Join-Path $framework 'System.IO.Compression.dll')" "/r:$(Join-Path $framework 'System.IO.Compression.FileSystem.dll')" "/resource:$payload,WXPlayer.Payload.zip" (Join-Path $repo 'tools\Launcher.cs') $hashSource
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed' }
Get-FileHash -LiteralPath $launcher,$payload -Algorithm SHA256 | ForEach-Object { $_.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($_.Path) } | Set-Content -LiteralPath (Join-Path $outDir 'SHA256SUMS.txt')
Write-Host "Created: $launcher and $payload"

