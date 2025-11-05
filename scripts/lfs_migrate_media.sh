#!/usr/bin/env bash
set -euo pipefail

echo "Checking git-lfs..."
if ! git lfs version >/dev/null 2>&1; then
  echo "git-lfs not found. Install from https://git-lfs.com and rerun." >&2
  exit 1
fi

git lfs install

echo "Tracking common media formats with Git LFS..."
git lfs track "*.mp4" "*.mov" "*.mkv" "*.avi" "*.webm" \
               "*.wav" "*.ogg" "*.mp3" "*.aiff" "*.flac" \
               "*.psd" "*.zip" "*.7z"

echo "Adding .gitattributes..."
git add .gitattributes

echo "Rewriting history to move existing media into LFS (this might take time)..."
git lfs migrate import --everything --include="*.mp4,*.mov,*.mkv,*.avi,*.webm,*.wav,*.ogg,*.mp3,*.aiff,*.flac,*.psd,*.zip,*.7z" || true

echo "Done. Commit and push with force-with-lease if the remote rejected previous large files:"
echo "  git commit -m 'chore: enable Git LFS for media' || true"
echo "  git push origin --force-with-lease"
