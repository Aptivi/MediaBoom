@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260123-git-dd50a47.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-23-dd50a47/mpv-dev-x86_64-20260123-git-dd50a47.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260123-git-dd50a47.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20260123-git-dd50a47.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-23-dd50a47/mpv-dev-aarch64-20260123-git-dd50a47.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20260123-git-dd50a47.7z"""
