@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20250913-git-d837c43.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-09-13-d837c43/mpv-dev-x86_64-20250913-git-d837c43.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20250913-git-d837c43.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20250913-git-d837c43.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-09-13-d837c43/mpv-dev-aarch64-20250913-git-d837c43.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20250913-git-d837c43.7z"""
