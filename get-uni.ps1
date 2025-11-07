# Load JSON from the latest release API
$releaseInfo = Invoke-RestMethod -Uri "https://api.github.com/repos/blueset/uni/releases/latest"

# Find the first asset that ends with '-windows-amd64.exe.gz'
$asset = $releaseInfo.assets | Where-Object { $_.name -like "*-windows-amd64.exe.gz" } | Select-Object -First 1

# Get the download URL
$downloadUrl = $asset.browser_download_url

# Download the .gz file
$gzFilePath = "uni-windows-amd64.exe.gz"
Invoke-WebRequest -Uri $downloadUrl -OutFile $gzFilePath

# Extract the .gz file to get the .exe file
$exeFilePath = "uni.exe"
$gzipStream = [System.IO.Compression.GzipStream]::new([System.IO.File]::OpenRead($gzFilePath), [System.IO.Compression.CompressionMode]::Decompress)
$fileStream = [System.IO.File]::Create($exeFilePath)
$gzipStream.CopyTo($fileStream)
$gzipStream.Close()
$fileStream.Close()

# Clean up the .gz file
Remove-Item -Path $gzFilePath
