<#
.SYNOPSIS
    Runs the Linux check of Forge.Next.LocalWebShell in Docker (tools/linux-check.sh in mcr.microsoft.com/dotnet/sdk:11.0).

.DESCRIPTION
    Needs Docker with a Linux engine (Docker Desktop on Windows) and network access. The .NET 11 SDK image is needed to
    restore the package's net11.0 target; the container adds the .NET 10 runtime (tools/linux-net10-runtime.sh), because
    the tests also run on net10.0 and the sample is a net10.0 application. The repository is mounted read-only; the
    container builds a copy, runs the test suite on both runtimes, and checks the sample: the browser decision as root, as
    a normal user without a graphical session and with DISPLAY but no xdg-open, and the HTTP flow (401 without the
    cookie, admitted with the launch link, 200 with the cookie, 400 for a foreign Host header).

.PARAMETER InstallXdgUtils
    Also installs xdg-utils in the container and checks that xdg-open is started when DISPLAY is set (needs network).

.PARAMETER Desktop
    Runs the desktop check instead (tools/linux-desktop-check.sh, needs network): a virtual X server (Xvfb) and Firefox
    in the container, the sample started as a normal user with DISPLAY set, and the check that Firefox shows the page
    behind the launch cookie.

.EXAMPLE
    pwsh tools/run-linux-check.ps1

.EXAMPLE
    pwsh tools/run-linux-check.ps1 -Desktop
#>
param([switch]$InstallXdgUtils, [switch]$Desktop)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$install = if ($InstallXdgUtils) { '1' } else { '0' }

$script = if ($Desktop) { '/src/tools/linux-desktop-check.sh' } else { '/src/tools/linux-check.sh' }
docker run --rm -e "INSTALL_XDG=$install" -v "${repo}:/src:ro" mcr.microsoft.com/dotnet/sdk:11.0 bash $script
exit $LASTEXITCODE
