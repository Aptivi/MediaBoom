@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260112-git-363341a.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-12-363341a/mpv-dev-x86_64-20260112-git-363341a.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260112-git-363341a.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20260112-git-363341a.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-12-363341a/mpv-dev-aarch64-20260112-git-363341a.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20260112-git-363341a.7z"""
