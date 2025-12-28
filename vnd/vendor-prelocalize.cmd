@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20251228-git-4ecf729.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-12-28-4ecf729/mpv-dev-x86_64-20251228-git-4ecf729.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20251228-git-4ecf729.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20251228-git-4ecf729.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-12-28-4ecf729/mpv-dev-aarch64-20251228-git-4ecf729.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20251228-git-4ecf729.7z"""
