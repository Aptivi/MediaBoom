@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260106-git-c63fe8a.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-06-c63fe8a/mpv-dev-x86_64-20260106-git-c63fe8a.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260106-git-c63fe8a.7z"""
if not exist "%TEMP%\" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-06-c63fe8a/ -OutFile ""%TEMP%\"""

pushd "%ROOTDIR%\tools\"
"%ProgramFiles%\7-Zip\7z.exe" x "%TEMP%\mpv-dev-x86_64-20260106-git-c63fe8a.7z" libmpv-2.dll
popd
mkdir "%ROOTDIR%\public\MediaBoom.Native\runtimes\win-x64\native\"
move "%ROOTDIR%\tools\libmpv-2.dll" "%ROOTDIR%\public\MediaBoom.Native\runtimes\win-x64\native\"

pushd "%ROOTDIR%\tools\"
"%ProgramFiles%\7-Zip\7z.exe" x "%TEMP%\" libmpv-2.dll
popd
mkdir "%ROOTDIR%\public\MediaBoom.Native\runtimes\win-arm64\native\"
move "%ROOTDIR%\tools\libmpv-2.dll" "%ROOTDIR%\public\MediaBoom.Native\runtimes\win-arm64\native\"
