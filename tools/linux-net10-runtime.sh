#!/usr/bin/env bash
# Adds the ASP.NET Core 10 runtime (which brings the .NET 10 runtime) to mcr.microsoft.com/dotnet/sdk:11.0, next to the
# image's own .NET 11 runtime. The Linux checks need the .NET 11 SDK image to restore the package's net11.0 target, but
# the tests also run on net10.0 and the sample is a net10.0 application, and the image carries only the .NET 11 runtime.
# Uses Microsoft's dotnet-install script, so it needs network access. Called by linux-check.sh and
# linux-desktop-check.sh; every download has a deadline, and it exits non-zero when the runtime is not there afterwards.
set -euo pipefail

root="$(dirname "$(readlink -f "$(command -v dotnet)")")"
curl -sSfL --retry 3 --max-time 120 https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
timeout 600 bash /tmp/dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir "$root" --no-path \
    > /tmp/dotnet-install.log 2>&1 || { tail -n 30 /tmp/dotnet-install.log; exit 1; }
runtimes="$(dotnet --list-runtimes)"
grep -qE '^Microsoft\.NETCore\.App 10\.' <<< "$runtimes" || { echo "$runtimes"; echo "no .NET 10 runtime"; exit 1; }
grep -qE '^Microsoft\.AspNetCore\.App 10\.' <<< "$runtimes" || { echo "$runtimes"; echo "no ASP.NET Core 10 runtime"; exit 1; }
grep -E ' 10\.' <<< "$runtimes"
