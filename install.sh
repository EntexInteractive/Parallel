#!/usr/bin/env bash
set -euo pipefail

REPO="EntexInteractive/Parallel"
ASSET_PATTERN="Parallel-.*-cmd-linux-x64.zip"
INSTALL_ROOT="/usr/local/lib/parallel"
BIN_LINK="/usr/local/bin/parallel"
TMP_DIR="$(mktemp -d)"
ZIP_FILE="$TMP_DIR/parallel.zip"

command -v curl >/dev/null || { echo "curl required"; exit 1; }
command -v unzip >/dev/null || { echo "unzip required"; exit 1; }

echo "[1/5] Fetching latest release info..."
RELEASE_INFO=$(curl -fsSL "https://api.github.com/repos/$REPO/releases/latest")
VERSION=$(echo "$RELEASE_INFO" | jq -r '.tag_name')

DOWNLOAD_URL=$(echo "$RELEASE_INFO" | jq -r --arg pattern "$ASSET_PATTERN" '.assets[] | select(.name | test($pattern)) | .browser_download_url' | head -n 1)
if [[ -z "$DOWNLOAD_URL" || "$DOWNLOAD_URL" == "null" ]]; then
    echo "Could not find a valid release."
    exit 1
fi

echo "[2/5] Downloading Parallel $VERSION..."
curl -fsSL -o "$ZIP_FILE" "$DOWNLOAD_URL"

echo "[3/5] Installing..."
rm -rf "$INSTALL_ROOT"
mkdir -p "$INSTALL_ROOT"

unzip -q "$ZIP_FILE" -d "$INSTALL_ROOT"
BIN_NAME=$(find "$INSTALL_ROOT" -maxdepth 1 -type f -iname "parallel" -printf '%f\n' | head -n 1)

if [[ -z "$BIN_NAME" ]]; then
    echo "Could not find Parallel executable after extraction."
    find "$INSTALL_ROOT" -maxdepth 2 -type f -print
    exit 1
fi

rm -rf "$BIN_LINK"
chmod 755 "$INSTALL_ROOT/$BIN_NAME"
ln -sf "$INSTALL_ROOT/$BIN_NAME" "$BIN_LINK"

echo "[4/5] Cleaning up..."
rm -rf "$TMP_DIR"

echo "[5/5] Installed Parallel $VERSION"
echo "Binary: $BIN_LINK"
