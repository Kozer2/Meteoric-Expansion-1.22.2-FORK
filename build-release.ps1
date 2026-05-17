$ErrorActionPreference = "Stop"

$Repo = $PSScriptRoot
$Stage = Join-Path $Repo "release"
$ModInfo = Get-Content (Join-Path $Repo "modinfo.json") | ConvertFrom-Json

$Version = $ModInfo.version
$ModId = $ModInfo.modid

$Zip = Join-Path $Repo "$ModId`_$Version-test.zip"

$DllOut = "C:\Projects\VintageStory\Mods\Debug\meteoricexpansion"

Remove-Item $Stage -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $Zip -Force -ErrorAction SilentlyContinue

dotnet clean $Repo
dotnet build $Repo

New-Item -ItemType Directory -Path $Stage | Out-Null

Copy-Item (Join-Path $Repo "assets") (Join-Path $Stage "assets") -Recurse -Force
Copy-Item (Join-Path $Repo "modinfo.json") $Stage -Force
Copy-Item (Join-Path $Repo "modicon.png") $Stage -Force
Copy-Item (Join-Path $DllOut "meteoricexpansion.dll") $Stage -Force
Copy-Item (Join-Path $DllOut "meteoricexpansion.deps.json") $Stage -Force
Copy-Item (Join-Path $DllOut "meteoricexpansion.pdb") $Stage -Force

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zipArchive = [System.IO.Compression.ZipFile]::Open($Zip, "Create")

Get-ChildItem $Stage -Recurse -File | ForEach-Object {
    $relativePath = $_.FullName.Substring($Stage.Length + 1)
    $zipPath = $relativePath.Replace("\", "/")

    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $zipArchive,
        $_.FullName,
        $zipPath,
        [System.IO.Compression.CompressionLevel]::Optimal
    ) | Out-Null
}

$zipArchive.Dispose()

Write-Host "Built: $Zip"