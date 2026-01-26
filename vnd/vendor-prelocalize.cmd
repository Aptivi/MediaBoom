@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260126-git-d1743b6.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-26-d1743b6/mpv-dev-x86_64-20260126-git-d1743b6.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260126-git-d1743b6.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20260126-git-d1743b6.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-26-d1743b6/mpv-dev-aarch64-20260126-git-d1743b6.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20260126-git-d1743b6.7z"""
