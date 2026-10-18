#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
STAGE="$ROOT/packaging/stage"
DIST="$ROOT/dist"
VERSION="$(grep -E '^\s*<Version>' "$ROOT/Qingtu.csproj" | sed -E 's/.*<Version>([^<]+).*/\1/')"
NAME="轻图-avalonia-${VERSION}-win64-绿色版"

cd "$ROOT"
dotnet publish -c Release -r win-x64 \
  -p:SelfContained=false \
  -p:PublishReadyToRun=false \
  -p:DebugType=none \
  -p:DebugSymbols=false \
  -o "$ROOT/publish-out"

PUBLISH="$ROOT/publish-out"
[[ -f "$PUBLISH/qingtu-avalonia.exe" ]] || { echo "未找到 exe"; exit 1; }

PORTABLE_DIR="$DIST/$NAME"
rm -rf "$DIST"
mkdir -p "$PORTABLE_DIR"
cp -a "$PUBLISH/." "$PORTABLE_DIR/"
cp -f "$STAGE/启动.bat" "$STAGE/使用说明.txt" "$PORTABLE_DIR/"

(cd "$DIST" && tar -a -cf "${NAME}.zip" "$NAME")
echo "完成: $DIST/${NAME}.zip ($(du -sh "$PORTABLE_DIR" | cut -f1))"
