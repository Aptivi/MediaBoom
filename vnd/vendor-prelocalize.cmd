@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20251225-git-c0d989c.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-12-25-c0d989c/mpv-dev-x86_64-20251225-git-c0d989c.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20251225-git-c0d989c.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20251225-git-c0d989c.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-12-25-c0d989c/mpv-dev-aarch64-20251225-git-c0d989c.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20251225-git-c0d989c.7z"""
