@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-12-08-dbd7a90/ -OutFile ""%TEMP%\"""
if not exist "%TEMP%\" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-12-08-dbd7a90/ -OutFile ""%TEMP%\"""
