@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20250914-git-b9b3106.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-09-14-b9b3106/mpv-dev-x86_64-20250914-git-b9b3106.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20250914-git-b9b3106.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20250914-git-b9b3106.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-09-14-b9b3106/mpv-dev-aarch64-20250914-git-b9b3106.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20250914-git-b9b3106.7z"""
