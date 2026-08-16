#!/usr/bin/env bash
set -e

REPO="EntexInteractive/Parallel"
ASSET_PATTERN="Parallel-.*-cmd-linux-x64.zip"
INSTALL_ROOT="/usr/local/lib/parallel"
BIN_LINK="/usr/local/bin/parallel"
TMP_DIR="$(mktemp -d)"
ZIP_FILE="$TMP_DIR/parallel.zip"
BIN_NAME=$(ls "$INSTALL_ROOT" | grep -i parallel | head -n 1)

command -v curl >/dev/null || { echo "curl required"; exit 1; }
command -v unzip >/dev/null || { echo "unzip required"; exit 1; }

echo "[1/5] Fetching latest release info..."
RELEASE_INFO=$(curl -fsSL "https://api.github.com/repos/$REPO/releases/latest")
VERSION=$(echo "$RELEASE_INFO" | jq -r '.tag_name')

DOWNLOAD_URL=$(echo "$RELEASE_INFO" | jq -r --arg pattern "$ASSET_PATTERN" '.assets[] | select(.name | test($pattern)) | .browser_download_url' | head -n 1)
[[ -z "$DOWNLOAD_URL" ]] && { echo "Could not find a valid release."; exit 1; }

echo "[2/5] Downloading $VERSION..."
curl -sSL -o "$ZIP_FILE" "$DOWNLOAD_URL"

echo "[3/5] Installing $VERSION..."
rm -rf "$INSTALL_ROOT"
mkdir -p "$INSTALL_ROOT"
unzip -q "$ZIP_FILE" -d "$INSTALL_ROOT"
chmod 755 "$INSTALL_ROOT/$BIN_NAME"
ln -sf "$INSTALL_ROOT/$BIN_NAME" "$BIN_LINK"

echo "[4/5] Cleaning up..."
rm -rf "$TMP_DIR"

echo "[5/5] Installation complete."
