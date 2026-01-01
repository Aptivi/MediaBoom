@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260101-git-f57c5ca.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-01-f57c5ca/mpv-dev-x86_64-20260101-git-f57c5ca.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260101-git-f57c5ca.7z"""
if not exist "%TEMP%\" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-01-f57c5ca/ -OutFile ""%TEMP%\"""
