@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260118-git-5f6d5b3.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-18-5f6d5b3/mpv-dev-x86_64-20260118-git-5f6d5b3.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260118-git-5f6d5b3.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20260118-git-5f6d5b3.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-18-5f6d5b3/mpv-dev-aarch64-20260118-git-5f6d5b3.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20260118-git-5f6d5b3.7z"""
