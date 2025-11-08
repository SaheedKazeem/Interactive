#!/usr/bin/env python3
"""Generate scene transition hints by probing video metadata via ffprobe.

Reads Assets/StreamingAssets/videos.json, extracts every scene's
`windowsLocalPath`, inspects the clip with ffprobe, and emits
Assets/StreamingAssets/sceneTransitions.json containing fade durations,
easing suggestions, and bookkeeping for Unity's SceneTransitionLibrary.
"""

from __future__ import annotations

import argparse
import json
import math
import subprocess
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional

REPO_ROOT = Path(__file__).resolve().parents[1]
VIDEOS_CONFIG = REPO_ROOT / "Assets" / "StreamingAssets" / "videos.json"
OUTPUT_FILE = REPO_ROOT / "Assets" / "StreamingAssets" / "sceneTransitions.json"


@dataclass
class VideoStats:
    scene: str
    path: Path
    duration: float = 0.0
    fps: float = 0.0
    bitrate: float = 0.0
    width: int = 0
    height: int = 0


def windows_to_wsl(path_str: str) -> Path:
    if not path_str:
        return Path()
    normalized = path_str.replace("\\", "/")
    if len(normalized) > 1 and normalized[1] == ":":
        drive = normalized[0].lower()
        rest = normalized[2:].lstrip("/")
        return Path("/mnt") / drive / rest
    return Path(normalized)


def parse_fraction(raw: Optional[str]) -> float:
    if not raw:
        return 0.0
    if "/" in raw:
        num, den = raw.split("/", 1)
        try:
            n = float(num)
            d = float(den)
            return n / d if d else 0.0
        except ValueError:
            return 0.0
    try:
        return float(raw)
    except ValueError:
        return 0.0


def safe_float(value: Optional[str]) -> float:
    try:
        return float(value)
    except (TypeError, ValueError):
        return 0.0


def probe_video(path: Path) -> VideoStats:
    cmd = [
        "ffprobe",
        "-v",
        "quiet",
        "-print_format",
        "json",
        "-show_streams",
        "-show_format",
        str(path),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True, check=True)
    data = json.loads(result.stdout)
    stats = VideoStats(scene="", path=path)
    fmt = data.get("format", {})
    stats.duration = safe_float(fmt.get("duration"))
    stats.bitrate = safe_float(fmt.get("bit_rate"))
    for stream in data.get("streams", []):
        if stream.get("codec_type") != "video":
            continue
        stats.width = int(stream.get("width") or 0)
        stats.height = int(stream.get("height") or 0)
        stats.fps = parse_fraction(stream.get("avg_frame_rate") or stream.get("r_frame_rate"))
        break
    return stats


def score_style(stats: VideoStats) -> str:
    pace = 0
    if stats.fps >= 48:
        pace += 1
    if stats.duration <= 60:
        pace += 1
    if stats.bitrate >= 8_000_000:
        pace += 1
    if stats.duration >= 150:
        pace -= 1
    if stats.fps <= 24:
        pace -= 1

    if pace >= 2:
        return "snap"
    if pace <= -1:
        return "linger"
    return "steady"


STYLE_PRESETS: Dict[str, Dict[str, float]] = {
    "snap": {"fadeOut": 0.18, "fadeIn": 0.22, "holdAfterLoad": 0.05},
    "steady": {"fadeOut": 0.32, "fadeIn": 0.38, "holdAfterLoad": 0.12},
    "linger": {"fadeOut": 0.55, "fadeIn": 0.7, "holdAfterLoad": 0.2},
}

STYLE_EASE = {
    "snap": ("Linear", "Linear"),
    "steady": ("OutQuad", "InQuad"),
    "linger": ("InOutSine", "InOutSine"),
}


def build_entry(scene: str, stats: VideoStats) -> Dict:
    style = score_style(stats)
    preset = STYLE_PRESETS.get(style, STYLE_PRESETS["steady"])
    ease_out, ease_in = STYLE_EASE.get(style, ("Linear", "Linear"))
    entry = {
        "name": scene,
        "style": style,
        "fadeOut": round(preset["fadeOut"], 3),
        "fadeIn": round(preset["fadeIn"], 3),
        "color": "#000000",
        "easeOut": ease_out,
        "easeIn": ease_in,
        "holdAfterLoad": round(preset["holdAfterLoad"], 3),
        "sourceVideo": str(stats.path).replace(str(REPO_ROOT), ""),
        "durationSeconds": round(stats.duration, 3),
        "avgFps": round(stats.fps, 3),
        "bitrate": stats.bitrate,
    }
    return entry


def load_videos_config(path: Path) -> List[Dict]:
    data = json.loads(path.read_text())
    return data.get("scenes", [])


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--videos", type=Path, default=VIDEOS_CONFIG, help="Path to videos.json")
    parser.add_argument("--output", type=Path, default=OUTPUT_FILE, help="Where to write sceneTransitions.json")
    args = parser.parse_args()

    scenes = load_videos_config(args.videos)
    entries: List[Dict] = []
    for scene in scenes:
        name = scene.get("name")
        src = scene.get("windowsLocalPath")
        if not name or not src:
            continue
        clip_path = windows_to_wsl(src)
        if not clip_path.exists():
            print(f"[warn] Skipping {name}: missing file {clip_path}")
            continue
        stats = probe_video(clip_path)
        stats.scene = name
        entries.append(build_entry(name, stats))
        print(f"Probed {name}: duration={stats.duration:.1f}s fps={stats.fps:.2f} style={entries[-1]['style']}")

    payload = {
        "generatedAt": datetime.utcnow().isoformat() + "Z",
        "defaults": {
            "fadeOut": 0.35,
            "fadeIn": 0.4,
            "color": "#000000",
            "easeOut": "Linear",
            "easeIn": "Linear",
            "holdAfterLoad": 0.1,
        },
        "scenes": entries,
    }

    args.output.write_text(json.dumps(payload, indent=4))
    print(f"Wrote {args.output} ({len(entries)} entries)")


if __name__ == "__main__":
    main()
