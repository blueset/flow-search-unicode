<#
get-emoji.ps1 - Download and extract fluentui-emoji assets, collect 3D PNGs (including skin tones)
and export them into a single output folder with normalized unicode-hex filenames.

Usage example:
  pwsh .\get-emoji.ps1 -OutDir exported_3d -Overwrite

Behavior:
  - Downloads the default repository ZIP (override with -ZipUrl).
  - Extracts into a temporary folder under $env:TEMP.
  - Finds the first `assets` folder in the extracted repository.
  - For each metadata.json under assets, locates 3D images for Default and skin-tone
    folders (Default, Light, Medium-Light, Medium, Medium-Dark, Dark).
  - Normalizes unicode strings into lowercased, dash-separated filenames
    (e.g. "1F466 1F3FB" -> "1f466-1f3fb.png").
  - Prefers a source PNG whose filename contains matching hex tokens for a target.
  - Copies one selected PNG per target into the destination folder. If multiple
    source PNGs map to the same target name and -Overwrite is not supplied, the
    conflict is logged and the export is skipped.
  - Cleans up temporary files even on error.

Notes:
  - Requires PowerShell 7+ (pwsh).
#>

param (
    [Parameter(Mandatory = $true)]
    [string]$OutDir,

    [switch]$Overwrite,

    [string]$ZipUrl = 'https://github.com/microsoft/fluentui-emoji/archive/refs/heads/main.zip'
)

# SK order mapping
$SK_ORDER = @('Default','Light','Medium-Light','Medium','Medium-Dark','Dark')

# Counters
$metaProcessed = 0
$source3DFound = 0
$exported = 0
$conflictsSkipped = 0
$overwritten = 0

function Normalize-UnicodeString {
    param([string]$s)
    if ([string]::IsNullOrWhiteSpace($s)) { return $null }
    $tokens = $s -split '[\s\-_]+' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
    if (-not $tokens) { return $null }
    $joined = ($tokens | ForEach-Object { $_.ToUpper() }) -join '-'
    return $joined.ToLower()
}

function Extract-HexTokensFromFilename {
    param([string]$filename)
    if ([string]::IsNullOrWhiteSpace($filename)) { return $null }
    # Match sequences like '1F466' or '1F466_1F3FB' or '1F466-1F3FB' or '1f466 1f3fb'
    $m = [regex]::Match($filename, '([0-9A-Fa-f]{4,6}(?:[ _\-][0-9A-Fa-f]{4,6})*)')
    if (-not $m.Success) { return $null }
    $grp = $m.Groups[1].Value
    $tokens = $grp -split '[\s\-_]+' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
    if (-not $tokens) { return $null }
    $norm = ($tokens | ForEach-Object { $_.ToUpper() }) -join '-'
    return $norm.ToLower()
}

# Ensure output dir exists
try {
    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
} catch {
    Write-Error "Failed to create or access OutDir '$OutDir': $_"
    exit 2
}

# Create temp paths
$guid = [GUID]::NewGuid().ToString()
$zipPath = Join-Path $env:TEMP "$guid.zip"
$extractDir = Join-Path $env:TEMP $guid

$zipDownloaded = $false

try {
    Write-Output "Downloading ZIP from $ZipUrl to $zipPath..."
    try {
        Invoke-WebRequest -Uri $ZipUrl -OutFile $zipPath -ErrorAction Stop
        $zipDownloaded = $true
        Write-Output "Downloaded zip to $zipPath"
    } catch {
        Write-Error "Failed to download ZIP from ${ZipUrl}: $_"
        exit 3
    }

    Write-Output "Extracting archive to $extractDir..."
    try {
        Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force
        Write-Output "Extracted to $extractDir"
    } catch {
        Write-Error "Failed to extract archive: $_"
        exit 4
    }

    # Find the assets folder robustly
    Write-Output "Locating 'assets' folder under extracted archive..."
    $assetsFolder = Get-ChildItem -Path $extractDir -Directory -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.Name -ieq 'assets' } | Select-Object -First 1
    if (-not $assetsFolder) {
        Write-Error "Could not find an 'assets' folder under $extractDir"
        exit 5
    }
    $assetsRoot = $assetsFolder.FullName
    Write-Output "Using assets root: $assetsRoot"

    # Find metadata.json files
    Write-Output "Searching for metadata.json files under assets..."
    $metadataFiles = Get-ChildItem -Path $assetsRoot -Recurse -Filter 'metadata.json' -File -ErrorAction SilentlyContinue
    if (-not $metadataFiles -or $metadataFiles.Count -eq 0) {
        Write-Warning "No metadata.json files found under assets"
    }

    # Global mapping of targetNormalizedName -> list of @{Path=..., Preferred=$bool}
    $globalMap = @{}

    foreach ($mdFile in $metadataFiles) {
        $metaProcessed++
        Write-Output "Processing metadata: $($mdFile.FullName)"
        try {
            $md = Get-Content -Raw -Path $mdFile.FullName | ConvertFrom-Json -ErrorAction Stop
        } catch {
            Write-Warning "Failed to parse JSON in $($mdFile.FullName): $_. Skipping."
            continue
        }

        if (-not $md.unicode) {
            Write-Warning "metadata.json $($mdFile.FullName) missing 'unicode' field. Skipping asset."
            continue
        }

        $assetRoot = $mdFile.Directory.FullName

        # Prepare the skintone list (may be null or shorter than expected)
        $skList = @()
        if ($md.unicodeSkintones) { $skList = $md.unicodeSkintones }
        # Determine once whether metadata provides skintone entries
        $hasSkintones = ($md.PSObject.Properties.Name -contains 'unicodeSkintones' -and $md.unicodeSkintones -ne $null -and ($md.unicodeSkintones.Count -gt 0))

        # For each SK order entry, find candidate PNGs
        for ($i = 0; $i -lt $SK_ORDER.Count; $i++) {
            # If no skintones are provided in metadata, skip non-default styles entirely
            if ($i -gt 0 -and -not $hasSkintones) { continue }
            $style = $SK_ORDER[$i]

            # Determine target unicode string (may be null for missing skintone entries)
            if ($skList -and $skList.Count -gt $i) {
                # If the unicodeSkintones array contains an entry for this style index, use it
                $targetUnicodeStr = $skList[$i]
            } else {
                if ($i -eq 0) {
                    # Default fallback to md.unicode when unicodeSkintones[0] not provided
                    $targetUnicodeStr = $md.unicode
                } else {
                    # For non-default styles, warn only when unicodeSkintones property exists and has entries but is too short
                    if ($md.PSObject.Properties.Name -contains 'unicodeSkintones' -and $md.unicodeSkintones -ne $null -and ($md.unicodeSkintones.Count -gt 0)) {
                        Write-Warning "Missing skintone entry for '$style' in $($mdFile.FullName). Will attempt filename-based detection or fallback to base unicode."
                    }
                    $targetUnicodeStr = $null
                }
            }

            # Build search directories for this style
            $searchDirs = @()
            if ($style -ieq 'Default') {
                # Always include top-level 3D folder for default
                $d1 = Join-Path $assetRoot '3D'
                $searchDirs += $d1
                # If metadata provides skintone folders, also check Default\3D
                if ($hasSkintones) {
                    $d2 = Join-Path $assetRoot 'Default\3D'
                    $searchDirs += $d2
                }
            } else {
                # Only traverse skin-tone style directories when unicodeSkintones are defined
                if ($hasSkintones) {
                    $searchDirs += Join-Path $assetRoot (Join-Path $style '3D')
                }
            }

            $foundAny = $false
            foreach ($dir in $searchDirs) {
                if (-not (Test-Path $dir)) { continue }
                $pngs = Get-ChildItem -Path $dir -Recurse -Filter '*.png' -File -ErrorAction SilentlyContinue
                if (-not $pngs -or $pngs.Count -eq 0) { continue }
                $foundAny = $true

                foreach ($png in $pngs) {
                    $source3DFound++
                    $filenameNorm = Extract-HexTokensFromFilename $png.Name

                    if ($targetUnicodeStr) {
                        $targetNorm = Normalize-UnicodeString $targetUnicodeStr
                        if (-not $targetNorm) { continue }
                        # If filename contains matching tokens, mark preferred
                        $preferred = $false
                        if ($filenameNorm -and $filenameNorm -eq $targetNorm) { $preferred = $true }

                        if (-not $globalMap.ContainsKey($targetNorm)) { $globalMap[$targetNorm] = @() }
                        $globalMap[$targetNorm] += @{ Path = $png.FullName; Preferred = $preferred }

                    } else {
                        # No metadata skintone entry: try to infer target from filename
                        if ($filenameNorm) {
                            $targetNorm = $filenameNorm
                            if (-not $globalMap.ContainsKey($targetNorm)) { $globalMap[$targetNorm] = @() }
                            $globalMap[$targetNorm] += @{ Path = $png.FullName; Preferred = $true }
                        } else {
                            # Can't infer target - record under fallback base unicode
                            $fallbackTarget = Normalize-UnicodeString $md.unicode
                            if ($fallbackTarget) {
                                if (-not $globalMap.ContainsKey($fallbackTarget)) { $globalMap[$fallbackTarget] = @() }
                                $globalMap[$fallbackTarget] += @{ Path = $png.FullName; Preferred = $false }
                            }
                        }
                    }
                }
            }

            if (-not $foundAny) {
                Write-Warning "No 3D folder found for style '$style' under asset $assetRoot"
            }
        }
    }

    # Now decide which source to copy for each target
    Write-Output "Selecting source files and exporting to $OutDir..."
    foreach ($targetName in $globalMap.Keys) {
        $sources = $globalMap[$targetName]
        if (-not $sources -or $sources.Count -eq 0) { continue }

        if ($sources.Count -gt 1) {
            # Prefer any preferred sources
            $preferredSources = $sources | Where-Object { $_.Preferred }
            if ($preferredSources.Count -gt 0) {
                $chosen = $preferredSources[0].Path
            } else {
                # Multiple ambiguous sources
                if (-not $Overwrite) {
                    $conflictsSkipped++
                    $srcList = ($sources | ForEach-Object { $_.Path }) -join '; '
                    Write-Warning "Multiple source PNGs found for target '$targetName' and -Overwrite not set. Skipping. Sources: $srcList"
                    continue
                } else {
                    $chosen = $sources[0].Path
                }
            }
        } else {
            $chosen = $sources[0].Path
        }

        $outPath = Join-Path $OutDir ($targetName + '.png')

        if (Test-Path $outPath) {
            if (-not $Overwrite) {
                $conflictsSkipped++
                Write-Warning "Destination already exists and -Overwrite not set: $outPath. Skipping copy from $chosen"
                continue
            } else {
                try {
                    Copy-Item -Path $chosen -Destination $outPath -Force -ErrorAction Stop
                    $overwritten++
                    Write-Output "Overwrote: $outPath <= $chosen"
                    continue
                } catch {
                    Write-Warning "Failed to overwrite $outPath from ${chosen}: $_"
                    continue
                }
            }
        }

        try {
            Copy-Item -Path $chosen -Destination $outPath -ErrorAction Stop
            $exported++
            Write-Output "Exported: $outPath <= $chosen"
        } catch {
            Write-Warning "Failed to copy $chosen to ${outPath}: $_"
        }
    }

    # Summary
    Write-Output "\nSummary:"
    Write-Output "  metadata.json processed: $metaProcessed"
    Write-Output "  3D PNG sources found: $source3DFound"
    Write-Output "  files exported: $exported"
    Write-Output "  conflicts skipped: $conflictsSkipped"
    Write-Output "  files overwritten: $overwritten"

    exit 0

} finally {
    # Cleanup temporary files
    if ($zipDownloaded -and (Test-Path $zipPath)) {
        try { Remove-Item -Path $zipPath -Force -ErrorAction SilentlyContinue } catch { Write-Warning "Failed to remove temp zip ${zipPath}: $_" }
    }
    if (Test-Path $extractDir) {
        try { Remove-Item -Path $extractDir -Recurse -Force -ErrorAction SilentlyContinue } catch { Write-Warning "Failed to remove temp dir ${extractDir}: $_" }
    }
}
