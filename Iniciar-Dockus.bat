@echo off
title Dockus - AI Token Glass Monitor
rem Version descargada (ZIP de Releases): Dockus.exe esta junto a este archivo.
if exist "%~dp0Dockus.exe" (
    start "" /D "%~dp0" "%~dp0Dockus.exe"
    exit
)
rem Repositorio clonado: compila la primera vez en la carpeta publish.
if not exist "%~dp0publish\Dockus.exe" (
    echo Compilando Dockus por primera vez...
    dotnet publish "%~dp0src\Dockus.csproj" -c Release -o "%~dp0publish"
)
start "" /D "%~dp0publish" "%~dp0publish\Dockus.exe"
exit
