@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260125-git-e99a916.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-25-e99a916/mpv-dev-x86_64-20260125-git-e99a916.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260125-git-e99a916.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20260125-git-e99a916.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-25-e99a916/mpv-dev-aarch64-20260125-git-e99a916.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20260125-git-e99a916.7z"""
