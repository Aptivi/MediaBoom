@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260121-git-313beaf.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-21-313beaf/mpv-dev-x86_64-20260121-git-313beaf.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260121-git-313beaf.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20260121-git-313beaf.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-21-313beaf/mpv-dev-aarch64-20260121-git-313beaf.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20260121-git-313beaf.7z"""
