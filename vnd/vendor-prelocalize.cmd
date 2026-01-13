@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260113-git-bbcd68f.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-13-bbcd68f/mpv-dev-x86_64-20260113-git-bbcd68f.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260113-git-bbcd68f.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20260113-git-bbcd68f.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-13-bbcd68f/mpv-dev-aarch64-20260113-git-bbcd68f.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20260113-git-bbcd68f.7z"""
