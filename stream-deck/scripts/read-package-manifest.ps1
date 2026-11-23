param([Parameter(Mandatory=$true)][string]$Package)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($Package)
try {
    $entries = @($archive.Entries | Where-Object { $_.FullName -eq 'manifest.json' -or $_.FullName.EndsWith('/manifest.json') })
    if ($entries.Count -ne 1 -or $entries[0].Length -gt 65536) { throw 'Invalid packaged manifest.' }
    $reader = [System.IO.StreamReader]::new($entries[0].Open())
    try { $reader.ReadToEnd() } finally { $reader.Dispose() }
} finally { $archive.Dispose() }
