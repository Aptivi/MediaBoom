@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20250926-git-74b47cf.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-09-26-74b47cf/mpv-dev-x86_64-20250926-git-74b47cf.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20250926-git-74b47cf.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20250926-git-74b47cf.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-09-26-74b47cf/mpv-dev-aarch64-20250926-git-74b47cf.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20250926-git-74b47cf.7z"""
