@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260108-git-85bf9f4.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-08-85bf9f4/mpv-dev-x86_64-20260108-git-85bf9f4.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260108-git-85bf9f4.7z"""
if not exist "%TEMP%\" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-08-85bf9f4/ -OutFile ""%TEMP%\"""
