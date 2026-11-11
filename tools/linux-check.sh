#!/usr/bin/env bash
# The Linux check of Forge.Next.LocalWebShell. Runs inside mcr.microsoft.com/dotnet/sdk:11.0 with the repository
# mounted read-only at /src and network access (the restore, and the .NET 10 runtime); start it with
# tools/run-linux-check.ps1 (Windows) or:
#   docker run --rm -v "$PWD:/src:ro" mcr.microsoft.com/dotnet/sdk:11.0 bash /src/tools/linux-check.sh
# It prints every check with its expectation and exits non-zero on the first failed one.
set -euo pipefail

fail() { echo "FAIL: $*"; exit 1; }

echo "== 0. The .NET 10 runtime next to the image's .NET 11 (the tests also run on net10.0, the sample is net10.0)"
bash /src/tools/linux-net10-runtime.sh || fail "installing the .NET 10 runtime"

mkdir -p /work/repo
tar -C /src --exclude=bin --exclude=obj --exclude=artifacts --exclude=.git --exclude=.vs -cf - . | tar -C /work/repo -xf -
cd /work/repo

echo "== 1. The test suite on Linux, on net10.0 and net11.0 (as root, no graphical session)"
dotnet test tests/Forge.Next.LocalWebShell.Tests -c Release --nologo > /work/tests.log 2>&1 || { tail -n 60 /work/tests.log; fail "tests"; }
grep -E "Passed!|Failed!" /work/tests.log

echo "== 2. The sample, published"
dotnet publish samples/Forge.Next.LocalWebShell.Sample -c Release -o /work/out --nologo -v q > /work/publish.log 2>&1 || { cat /work/publish.log; fail "publish"; }
chmod -R a+rX /work/out
SAMPLE="/work/out/Forge.Next.LocalWebShell.Sample.dll"

# Runs the sample for a few seconds and prints what it said about the link and the browser, the token hidden.
run_sample() {
    local label="$1"; shift
    local log="/work/sample-$label.log"
    timeout 8 "$@" > "$log" 2>&1 || true
    echo "-- $label:"
    sed -E 's/token=[A-Za-z0-9_-]+/token=<hidden>/' "$log" | grep -E "^Open: |browser was not opened|could not start|xdg-open" || true
}

expect_in_log() {
    grep -q "$2" "/work/sample-$1.log" || fail "$1: expected '$2'"
}

echo "== 3. The browser decision on Linux"
run_sample root dotnet "$SAMPLE"
expect_in_log root "runs as root"

run_sample user-headless runuser -u app -- env HOME=/tmp dotnet "$SAMPLE"
expect_in_log user-headless "no graphical session"

run_sample user-display-without-xdg-open runuser -u app -- env HOME=/tmp DISPLAY=:99 dotnet "$SAMPLE"
expect_in_log user-display-without-xdg-open "could not be started"

if [ "${INSTALL_XDG:-0}" = "1" ]; then
    apt-get update -qq > /dev/null && apt-get install -y -qq xdg-utils > /dev/null 2>&1 || fail "installing xdg-utils"
    run_sample user-display-with-xdg-open runuser -u app -- env HOME=/tmp DISPLAY=:99 dotnet "$SAMPLE"
    if grep -q "browser was not opened" /work/sample-user-display-with-xdg-open.log; then fail "xdg-open was not started"; fi
    echo "   (xdg-open was started; without an X server it cannot show anything, which is expected here)"
fi

echo "== 4. The HTTP flow on Linux (as a normal user, headless)"
runuser -u app -- env HOME=/tmp dotnet "$SAMPLE" > /work/flow.log 2>&1 &
pid=$!
url=""
for _ in $(seq 1 60); do
    url=$(grep -oE "http://127\.0\.0\.1:[0-9]+/\?token=[A-Za-z0-9_-]+" /work/flow.log | head -n 1 || true)
    [ -n "$url" ] && break
    sleep 0.25
done
[ -n "$url" ] || { kill "$pid" 2>/dev/null || true; fail "the sample printed no launch link"; }
base="${url%%\?*}"
without=$(curl -s -o /dev/null -w '%{http_code}' "$base")
admitted=$(curl -s -c /tmp/jar -b /tmp/jar -L "$url" | grep -c "This browser opened" || true)
after=$(curl -s -o /dev/null -w '%{http_code}' -b /tmp/jar "$base")
foreign=$(curl -s -o /dev/null -w '%{http_code}' -H "Host: evil.example" "$url")
kill "$pid" 2>/dev/null || true
echo "without the cookie: $without (expected 401)"
echo "with the launch link: admitted=$admitted (expected 1)"
echo "with the cookie afterwards: $after (expected 200)"
echo "foreign Host header: $foreign (expected 400)"
[ "$without" = "401" ] && [ "$admitted" = "1" ] && [ "$after" = "200" ] && [ "$foreign" = "400" ] || fail "HTTP flow"

echo "PASS"
