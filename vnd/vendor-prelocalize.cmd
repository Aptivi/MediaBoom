@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20251226-git-c0d989c.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-12-26-c0d989c/mpv-dev-x86_64-20251226-git-c0d989c.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20251226-git-c0d989c.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20251226-git-c0d989c.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-12-26-c0d989c/mpv-dev-aarch64-20251226-git-c0d989c.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20251226-git-c0d989c.7z"""
