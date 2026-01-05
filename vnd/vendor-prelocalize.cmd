@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260105-git-0035bb7.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-05-0035bb7/mpv-dev-x86_64-20260105-git-0035bb7.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260105-git-0035bb7.7z"""
if not exist "%TEMP%\" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-05-0035bb7/ -OutFile ""%TEMP%\"""
