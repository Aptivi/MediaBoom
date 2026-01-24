@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260124-git-9c6a3c0.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-24-9c6a3c0/mpv-dev-x86_64-20260124-git-9c6a3c0.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260124-git-9c6a3c0.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20260124-git-9c6a3c0.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-24-9c6a3c0/mpv-dev-aarch64-20260124-git-9c6a3c0.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20260124-git-9c6a3c0.7z"""
