@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260114-git-2007f55.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-14-2007f55/mpv-dev-x86_64-20260114-git-2007f55.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260114-git-2007f55.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20260114-git-2007f55.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-14-2007f55/mpv-dev-aarch64-20260114-git-2007f55.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20260114-git-2007f55.7z"""
