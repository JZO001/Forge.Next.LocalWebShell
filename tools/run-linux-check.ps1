<#
.SYNOPSIS
    Runs the Linux check of Forge.Next.LocalWebShell in Docker (tools/linux-check.sh in mcr.microsoft.com/dotnet/sdk:10.0).

.DESCRIPTION
    Needs Docker with a Linux engine (Docker Desktop on Windows). The repository is mounted read-only; the container
    builds a copy, runs the test suite, and checks the sample: the browser decision as root, as a normal user without a
    graphical session and with DISPLAY but no xdg-open, and the HTTP flow (401 without the cookie, admitted with the
    launch link, 200 with the cookie, 400 for a foreign Host header).

.PARAMETER InstallXdgUtils
    Also installs xdg-utils in the container and checks that xdg-open is started when DISPLAY is set (needs network).

.EXAMPLE
    pwsh tools/run-linux-check.ps1
#>
param([switch]$InstallXdgUtils)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$install = if ($InstallXdgUtils) { '1' } else { '0' }

docker run --rm -e "INSTALL_XDG=$install" -v "${repo}:/src:ro" mcr.microsoft.com/dotnet/sdk:10.0 bash /src/tools/linux-check.sh
exit $LASTEXITCODE
