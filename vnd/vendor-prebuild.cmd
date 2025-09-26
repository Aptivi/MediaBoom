@echo off

set ROOTDIR=%~dp0\..

REM Download libmpv for Windows and build
if not exist "%TEMP%\mpv-dev-x86_64-20250926-git-74b47cf.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-09-26-74b47cf/mpv-dev-x86_64-20250926-git-74b47cf.7z -OutFile ""%TEMP%\mpv-dev-x86_64-20250926-git-74b47cf.7z"""
if not exist "%TEMP%\mpv-dev-aarch64-20250926-git-74b47cf.7z" powershell -Command "Invoke-WebRequest https://github.com/zhongfly/mpv-winbuild/releases/download/2025-09-26-74b47cf/mpv-dev-aarch64-20250926-git-74b47cf.7z -OutFile ""%TEMP%\mpv-dev-aarch64-20250926-git-74b47cf.7z"""

pushd "%ROOTDIR%\tools\"
"%ProgramFiles%\7-Zip\7z.exe" x "%TEMP%\mpv-dev-x86_64-20250926-git-74b47cf.7z" libmpv-2.dll
popd
mkdir "%ROOTDIR%\public\BassBoom.Native\runtimes\win-x64\native\"
move "%ROOTDIR%\tools\libmpv-2.dll" "%ROOTDIR%\public\BassBoom.Native\runtimes\win-x64\native\"

pushd "%ROOTDIR%\tools\"
"%ProgramFiles%\7-Zip\7z.exe" x "%TEMP%\mpv-dev-aarch64-20250926-git-74b47cf.7z" libmpv-2.dll
popd
mkdir "%ROOTDIR%\public\BassBoom.Native\runtimes\win-arm64\native\"
move "%ROOTDIR%\tools\libmpv-2.dll" "%ROOTDIR%\public\BassBoom.Native\runtimes\win-arm64\native\"
