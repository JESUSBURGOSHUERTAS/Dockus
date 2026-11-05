# Script para crear acceso directo en el Escritorio de Windows
$WshShell = New-Object -ComObject WScript.Shell
$DesktopPath = [System.Environment]::GetFolderPath('Desktop')
$ShortcutPath = Join-Path -Path $DesktopPath -ChildPath "Dockus - Token Monitor.lnk"
$TargetPath = Join-Path -Path $PSScriptRoot -ChildPath "publish\Dockus.exe"

$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = $TargetPath
$Shortcut.WorkingDirectory = Join-Path -Path $PSScriptRoot -ChildPath "publish"
$Shortcut.Description = "Monitor de Tokens y Contexto para Gemini y otras IAs"
$Shortcut.Save()

Write-Host "Acceso directo creado exitosamente en tu Escritorio: $ShortcutPath" -ForegroundColor Green

