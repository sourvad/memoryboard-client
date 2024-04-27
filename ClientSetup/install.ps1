# Define paths
$appFilesSource = "./" # You should adjust this path
$installPath = "C:\Program Files (x86)\Memoryboard"
$startupShortcutPath = [System.IO.Path]::Combine([Environment]::GetFolderPath("Startup"), "Memoryboard.lnk")

# Create the install directory if it doesn't exist
if (-not (Test-Path -Path $installPath)) {
    New-Item -ItemType Directory -Path $installPath
}

# Copy the application files
Copy-Item -Path "$appFilesSource\*" -Destination $installPath -Exclude "install.ps1" -Force -Recurse

# Create a shortcut in the Startup folder
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($startupShortcutPath)
$Shortcut.TargetPath = "$installPath\Memoryboard.exe"
$Shortcut.IconLocation = "$installPath\Assets\Memoryboard.ico"
$Shortcut.Save()

# Start the Memoryboard application
Start-Process "$installPath\Memoryboard.exe" -WorkingDirectory $installPath