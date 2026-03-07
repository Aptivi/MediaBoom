import urllib.request as http_client
import py7zr


def download_libmpv_win(root_dir, extract: bool = True):
    # Download LibMPV for Windows
    output_dir = root_dir + "/artifacts"
    revision = "2026-03-06-3b55bc9"
    filename_amd = "mpv-dev-x86_64-20260306-git-3b55bc9.7z"
    filename_arm = "mpv-dev-aarch64-20260306-git-3b55bc9.7z"
    link_base = "https://github.com/zhongfly/mpv-winbuild/releases/download/"
    path_amd = output_dir + '/' + filename_amd
    path_arm = output_dir + '/' + filename_arm
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
            os.path.makedirs(native_amd_dir)
        if not os.path.isdir(native_arm_dir):
            os.path.makedirs(native_arm_dir)

        # Install the libmpv-2.dll file to the native directory
        with py7zr.SevenZipFie(path_amd, mode='r') as archive:
            archive.extract(targets=[filename_amd], path=native_amd_dir)
        with py7zr.SevenZipFie(path_arm, mode='r') as archive:
            archive.extract(targets=[filename_arm], path=native_arm_dir)
