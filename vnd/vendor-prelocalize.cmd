@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260119-git-b7e8fe9.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-19-b7e8fe9/mpv-dev-x86_64-20260119-git-b7e8fe9.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260119-git-b7e8fe9.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20260119-git-b7e8fe9.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-19-b7e8fe9/mpv-dev-aarch64-20260119-git-b7e8fe9.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20260119-git-b7e8fe9.7z"""
