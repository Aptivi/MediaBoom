from typing import Collection
import urllib.request as http_client
import py7zr
from py7zr.exceptions import UnsupportedCompressionMethodError
from subprocess import run
import os


def download_libmpv_win(root_dir, extract: bool = True):
    # Download LibMPV for Windows
    output_dir = root_dir + "/libmpv-win"
    revision = "2026-07-21-94335ab87a"
    filename_amd = "mpv-dev-x86_64-20260721-git-94335ab87a.7z"
    filename_arm = "mpv-dev-aarch64-20260721-git-94335ab87a.7z"
    link_base = "https://github.com/zhongfly/mpv-winbuild/releases/download/"
    path_amd = output_dir + '/' + filename_amd
    path_arm = output_dir + '/' + filename_arm
    if not os.path.isdir(output_dir):
        os.makedirs(output_dir)
    if not os.path.isfile(path_amd):
        http_client.urlretrieve(link_base + revision + '/' + filename_amd,
                                output_dir + '/' + filename_amd)
    if not os.path.isfile(path_arm):
        http_client.urlretrieve(link_base + revision + '/' + filename_arm,
                                output_dir + '/' + filename_arm)

    # Extract if needed
    if extract:
        # Extract the libmpv-2.dll file
        native_amd_dir = root_dir + '/public/MediaBoom.Native/runtimes/' + \
            'win-x64/native/'
        native_arm_dir = root_dir + '/public/MediaBoom.Native/runtimes/' + \
            'win-arm64/native/'

        # Make the directory first
        if not os.path.isdir(native_amd_dir):
            os.makedirs(native_amd_dir)
        if not os.path.isdir(native_arm_dir):
            os.makedirs(native_arm_dir)

        # Install the libmpv-2.dll file to the native directory
        extract_mpv(path_amd, native_amd_dir)
        extract_mpv(path_arm, native_arm_dir)


def bcj2_workaround(path: str, outdir: str) -> None:
    result = run(['7z', 'x', '-y', f'-o{outdir}', f'{path}', 'libmpv-2.dll'])
    if result.returncode != 0:
        raise RuntimeError(f'7z error while decompressing {path}')


def extract_mpv(path: str, outdir: str) -> None:
    try:
        with py7zr.SevenZipFile(path, mode='r') as archive:
            archive.extract(targets=["libmpv-2.dll"], path=outdir)
    except UnsupportedCompressionMethodError as e:
        bcj2_workaround(path, outdir)