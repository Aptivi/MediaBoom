@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20251229-git-468e2a8.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-12-29-468e2a8/mpv-dev-x86_64-20251229-git-468e2a8.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20251229-git-468e2a8.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20251229-git-468e2a8.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-12-29-468e2a8/mpv-dev-aarch64-20251229-git-468e2a8.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20251229-git-468e2a8.7z"""
