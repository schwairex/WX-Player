param([string]$OutputPath = (Join-Path $PSScriptRoot '..\WXPlayer-source.zip'))

$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath($OutputPath)
if ([IO.File]::Exists($output)) { throw "Output already exists: $output" }
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($output)) | Out-Null

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::Open($output, [IO.Compression.ZipArchiveMode]::Create)
try {
    $roots = @('.github', 'docs', 'licenses', 'samples', 'src', 'tests', 'tools')
    $files = foreach ($name in $roots) {
        Get-ChildItem -LiteralPath (Join-Path $repo $name) -Recurse -Force -File |
            Where-Object { $_.FullName.Substring($repo.Length + 1) -notmatch '(^|[\\/])(bin|obj|\.vs)([\\/]|$)' }
    }
    $files += @('.gitignore', 'Directory.Build.props', 'LICENSE', 'README.md', 'THIRD-PARTY-NOTICES.md', 'WXPlayer.sln') |
        ForEach-Object { Get-Item -LiteralPath (Join-Path $repo $_) }
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($repo.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, "WXPlayer/$relative", [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally { $zip.Dispose() }
Write-Host "Created: $output"
