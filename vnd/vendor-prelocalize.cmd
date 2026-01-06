@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260106-git-c63fe8a.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-06-c63fe8a/mpv-dev-x86_64-20260106-git-c63fe8a.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260106-git-c63fe8a.7z"""
if not exist "%TEMP%\" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-06-c63fe8a/ -OutFile ""%TEMP%\"""
