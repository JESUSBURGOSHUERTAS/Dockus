@echo off
title Dockus - AI Token Glass Monitor
if not exist "%~dp0publish\Dockus.exe" (
    echo Compilando Dockus por primera vez...
    dotnet publish "%~dp0src\Dockus.csproj" -c Release -o "%~dp0publish"
)
start "" /D "%~dp0publish" "%~dp0publish\Dockus.exe"
exit
