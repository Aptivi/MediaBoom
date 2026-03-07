import os
import subprocess
from libmpv_win import download_libmpv_win


def vnd_prebuild():
    solution = os.path.dirname(os.path.abspath(__file__ + '/../'))

    # Download libmpv for Windows
    download_libmpv_win(solution)


def vnd_build(args, extra_args):
    solution = os.path.dirname(os.path.abspath(__file__ + '/../'))
    solution = solution + "/MediaBoom.slnx"
    command = f"dotnet build {solution} {args if args else ''}"
    result = subprocess.run(command, shell=True)
    if result.returncode != 0:
        raise Exception("Build failed with code %i" % (result.returncode))
