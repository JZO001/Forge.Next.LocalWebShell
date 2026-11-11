#!/usr/bin/env bash
# The Linux desktop check of Forge.Next.LocalWebShell: the xdg-open route for real, with a browser on a display. Runs
# inside mcr.microsoft.com/dotnet/sdk:11.0 (Ubuntu 26.04) with the repository mounted read-only at /src and network
# access (it installs packages, and the .NET 10 runtime for the net10.0 sample); start it with
# `pwsh tools/run-linux-check.ps1 -Desktop` (Windows) or:
#   docker run --rm -v "$PWD:/src:ro" mcr.microsoft.com/dotnet/sdk:11.0 bash /src/tools/linux-desktop-check.sh
#
# A virtual X server (Xvfb) stands in for the desktop, Firefox from Mozilla's own APT repository is the browser (Ubuntu's
# own firefox package is only a stub for the snap, which does not run in a container), and xdg-open picks it as a
# desktop session without a desktop environment does (x-www-browser). The sample runs as the image's normal user with
# DISPLAY set; the check passes when Firefox's window shows the sample page's title, which only the page behind the
# launch cookie has: the launch link was opened, the cookie set and kept over the redirect, and the page served.
# Every wait has a deadline; the script exits non-zero on the first failed check.
set -euo pipefail

fail() { echo "FAIL: $*"; exit 1; }

echo "== 1. Packages: Xvfb, xdg-utils, the X11 tools, Firefox from Mozilla's repository"
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq > /dev/null || fail "apt-get update"
apt-get install -y -qq xvfb xdg-utils x11-utils wget gnupg > /dev/null 2>&1 || fail "installing Xvfb and the tools"
install -d -m 0755 /etc/apt/keyrings
wget -q https://packages.mozilla.org/apt/repo-signing-key.gpg -O /etc/apt/keyrings/packages.mozilla.org.asc || fail "Mozilla's key"
echo "deb [signed-by=/etc/apt/keyrings/packages.mozilla.org.asc] https://packages.mozilla.org/apt mozilla main" > /etc/apt/sources.list.d/mozilla.list
printf 'Package: *\nPin: origin packages.mozilla.org\nPin-Priority: 1000\n' > /etc/apt/preferences.d/mozilla
apt-get update -qq > /dev/null || fail "apt-get update with Mozilla's repository"
apt-get install -y -qq firefox > /dev/null 2>&1 || fail "installing Firefox"
firefox --version
echo "x-www-browser: $(readlink -f "$(command -v x-www-browser || true)" || echo none)"

echo "== 2. The sample, published (with the .NET 10 runtime next to the image's .NET 11; the sample is net10.0)"
bash /src/tools/linux-net10-runtime.sh || fail "installing the .NET 10 runtime"
mkdir -p /work/repo
tar -C /src --exclude=bin --exclude=obj --exclude=artifacts --exclude=.git --exclude=.vs -cf - . | tar -C /work/repo -xf -
cd /work/repo
dotnet publish samples/Forge.Next.LocalWebShell.Sample -c Release -o /work/out --nologo -v q > /work/publish.log 2>&1 || { cat /work/publish.log; fail "publish"; }
chmod -R a+rX /work/out
SAMPLE="/work/out/Forge.Next.LocalWebShell.Sample.dll"

echo "== 3. The display, as the normal user"
install -d -o app -g app /tmp/apphome
runuser -u app -- env HOME=/tmp/apphome Xvfb :99 -screen 0 1280x800x24 -nolisten tcp > /work/xvfb.log 2>&1 &
xvfb=$!
for _ in $(seq 1 40); do
    runuser -u app -- env DISPLAY=:99 xwininfo -root > /dev/null 2>&1 && break
    sleep 0.25
done
runuser -u app -- env DISPLAY=:99 xwininfo -root > /dev/null 2>&1 || { cat /work/xvfb.log; fail "the virtual display did not start"; }
echo "the virtual display :99 answers"

cleanup() {
    kill "${sample:-0}" 2>/dev/null || true
    pkill -u app firefox 2>/dev/null || true
    kill "$xvfb" 2>/dev/null || true
}
trap cleanup EXIT

echo "== 4. The sample with DISPLAY: xdg-open starts Firefox with the launch link"
runuser -u app -- env HOME=/tmp/apphome DISPLAY=:99 dotnet "$SAMPLE" > /work/sample.log 2>&1 &
sample=$!
link=""
for _ in $(seq 1 80); do
    link=$(grep -oE "http://127\.0\.0\.1:[0-9]+/\?token=" /work/sample.log | head -n 1 || true)
    [ -n "$link" ] && break
    sleep 0.25
done
[ -n "$link" ] || { cat /work/sample.log; fail "the sample printed no launch link"; }
echo "launch link printed: ${link}<hidden>"
if grep -qE "browser was not opened|could not be started" /work/sample.log; then
    sed -E 's/token=[A-Za-z0-9_-]+/token=<hidden>/' /work/sample.log
    fail "the browser was not opened"
fi

echo "== 5. Firefox shows the page behind the cookie (its window title is the sample page's title)"
# A UTF-8 locale for xwininfo: Firefox's title holds a dash outside ASCII, which the C locale cannot print.
title=""
for _ in $(seq 1 120); do
    title=$(runuser -u app -- env LC_ALL=C.UTF-8 DISPLAY=:99 xwininfo -root -tree 2>/dev/null | grep -oE '"Forge\.Next\.LocalWebShell sample[^"]*"' | head -n 1 || true)
    [ -n "$title" ] && break
    sleep 0.5
done
if [ -z "$title" ]; then
    echo "windows on the display:"
    runuser -u app -- env LC_ALL=C.UTF-8 DISPLAY=:99 xwininfo -root -tree 2>/dev/null | grep -E '^\s+0x[0-9a-f]+ "' | head -n 20 || true
    pgrep -a -u app firefox | sed -E 's/token=[A-Za-z0-9_-]+/token=<hidden>/' || echo "no firefox process"
    fail "Firefox did not show the sample page within 60 s"
fi
echo "Firefox window: $title"
echo "Firefox process: $(pgrep -a -u app firefox | head -n 1 | sed -E 's/token=[A-Za-z0-9_-]+/token=<hidden>/' | cut -c1-160)"

echo "PASS"
