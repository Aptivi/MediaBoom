@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20260118-git-5f6d5b3.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-18-5f6d5b3/mpv-dev-x86_64-20260118-git-5f6d5b3.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20260118-git-5f6d5b3.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20260118-git-5f6d5b3.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2026-01-18-5f6d5b3/mpv-dev-aarch64-20260118-git-5f6d5b3.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20260118-git-5f6d5b3.7z"""

pushd "%ROOTDIR%\tools\"
"%ProgramFiles%\7-Zip\7z.exe" x "%TEMP%\mpv-dev-x86_64-20260118-git-5f6d5b3.7z" libmpv-2.dll
popd
mkdir "%ROOTDIR%\public\MediaBoom.Native\runtimes\win-x64\native\"
move "%ROOTDIR%\tools\libmpv-2.dll" "%ROOTDIR%\public\MediaBoom.Native\runtimes\win-x64\native\"

pushd "%ROOTDIR%\tools\"
"%ProgramFiles%\7-Zip\7z.exe" x "%TEMP%\mpv-dev-aarch64-20260118-git-5f6d5b3.7z" libmpv-2.dll
popd
mkdir "%ROOTDIR%\public\MediaBoom.Native\runtimes\win-arm64\native\"
move "%ROOTDIR%\tools\libmpv-2.dll" "%ROOTDIR%\public\MediaBoom.Native\runtimes\win-arm64\native\"
