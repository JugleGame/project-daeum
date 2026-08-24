from __future__ import annotations

import json
import shutil
import sys
import zipfile
from pathlib import Path

from PIL import Image


PALETTE = tuple(
    bytes.fromhex(color)
    for color in (
        "1a1119",
        "2b1e2a",
        "4a3142",
        "7d4a48",
        "b06b45",
        "e0954a",
        "f2c46b",
        "ffe8b0",
    )
)
TILE_NAMES = (
    "asphalt-base", "asphalt-aggregate", "asphalt-crack-fine", "asphalt-crack-branch",
    "asphalt-repair", "asphalt-repair-worn", "asphalt-line-short", "asphalt-line-long",
    "sidewalk-base", "sidewalk-variant", "sidewalk-joint", "sidewalk-double-joint",
    "sidewalk-crack", "sidewalk-broken", "sidewalk-drain-small", "sidewalk-drain-wide",
    "curb-base", "curb-top-highlight", "curb-front-dark", "curb-broken",
    "curb-left-end", "curb-right-end", "curb-ramp-left", "curb-ramp-right",
    "wall-center", "wall-center-variant", "wall-top", "wall-bottom",
    "wall-left-end", "wall-right-end", "wall-joint", "wall-broken",
)


def is_green(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, _ = pixel
    return g > 80 and g * 5 >= max(r, b) * 7


def nearest_color(pixel: tuple[int, int, int, int]) -> tuple[int, int, int, int]:
    if is_green(pixel):
        return 0, 0, 0, 0
    r, g, b, _ = pixel
    color = min(PALETTE, key=lambda p: (r - p[0]) ** 2 + (g - p[1]) ** 2 + (b - p[2]) ** 2)
    return color[0], color[1], color[2], 255


def runs(values: list[int], minimum: int) -> list[tuple[int, int]]:
    found: list[tuple[int, int]] = []
    start = None
    for index, value in enumerate(values + [0]):
        if value >= minimum and start is None:
            start = index
        elif value < minimum and start is not None:
            found.append((start, index))
            start = None
    return found


def validate_image(path: Path, size: tuple[int, int]) -> dict[str, object]:
    image = Image.open(path).convert("RGBA")
    colors = set(image.getdata())
    alpha = {color[3] for color in colors}
    opaque = {color[:3] for color in colors if color[3]}
    return {
        "path": path.name,
        "size": list(image.size),
        "expected_size": list(size),
        "size_ok": image.size == size,
        "alpha_values": sorted(alpha),
        "binary_alpha_ok": alpha <= {0, 255},
        "palette_ok": opaque <= {tuple(color) for color in PALETTE},
        "green_opaque_pixels": sum(1 for color in image.getdata() if color[3] and is_green(color)),
    }


def main(source: Path, output: Path) -> None:
    output.mkdir(parents=True, exist_ok=True)
    tiles_dir = output / "tiles"
    tiles_dir.mkdir(exist_ok=True)
    raw_path = output / "stage01-source-sheet-raw.png"
    shutil.copyfile(source, raw_path)

    raw = Image.open(source).convert("RGBA")
    processed = Image.new("RGBA", raw.size)
    processed.putdata([nearest_color(pixel) for pixel in raw.getdata()])
    processed_path = output / "stage01-source-sheet-processed.png"
    processed.save(processed_path, optimize=False)

    mask = [1 if pixel[3] else 0 for pixel in processed.getdata()]
    width, height = processed.size
    x_runs = runs([sum(mask[y * width + x] for y in range(height)) for x in range(width)], 8)
    y_runs = runs([sum(mask[y * width + x] for x in range(width)) for y in range(height)], 8)
    if len(x_runs) != 8 or len(y_runs) != 4:
        raise ValueError(f"Expected 8x4 isolated slots, found {len(x_runs)}x{len(y_runs)}")

    tileset = Image.new("RGBA", (128, 64))
    tiles: list[Path] = []
    for row, (top, bottom) in enumerate(y_runs):
        for column, (left, right) in enumerate(x_runs):
            index = row * 8 + column
            crop = processed.crop((left, top, right, bottom))
            bbox = crop.getbbox()
            if bbox is None:
                raise ValueError(f"Tile {index + 1:02d} is empty")
            crop = crop.crop(bbox)
            scale = min(16 / crop.width, 16 / crop.height)
            size = max(1, round(crop.width * scale)), max(1, round(crop.height * scale))
            crop = crop.resize(size, Image.Resampling.NEAREST)
            tile = Image.new("RGBA", (16, 16))
            tile.alpha_composite(crop, ((16 - crop.width) // 2, 16 - crop.height))
            tile.putdata([(r, g, b, 255 if a else 0) if a else (0, 0, 0, 0) for r, g, b, a in tile.getdata()])
            tile_path = tiles_dir / f"{index + 1:02d}-{TILE_NAMES[index]}.png"
            tile.save(tile_path, optimize=False)
            tiles.append(tile_path)
            tileset.alpha_composite(tile, (column * 16, row * 16))

    tileset_path = output / "stage01-tileset-128x64.png"
    tileset.save(tileset_path, optimize=False)

    seam = Image.new("RGBA", (48 * 4, 48))
    for group, tile_index in enumerate((0, 8, 16, 24)):
        tile = Image.open(tiles[tile_index]).convert("RGBA")
        for y in range(3):
            for x in range(3):
                seam.alpha_composite(tile, (group * 48 + x * 16, y * 16))
    seam_path = output / "stage01-seam-check-3x3.png"
    seam.save(seam_path, optimize=False)

    manifest = {
        "layout": {"columns": 8, "rows": 4, "order": "row-major"},
        "tile": {"width": 16, "height": 16, "format": "RGBA"},
        "tileset": {"width": 128, "height": 64},
        "unity": {"pixels_per_unit": 32, "filter_mode": "Point", "compression": "None"},
        "palette": ["#" + bytes(color).hex() for color in PALETTE],
        "tiles": [path.name for path in tiles],
    }
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")

    checks = [validate_image(path, (16, 16)) for path in tiles]
    checks.append(validate_image(tileset_path, (128, 64)))
    report = {
        "source_size": list(raw.size),
        "detected_layout": [len(x_runs), len(y_runs)],
        "tile_count": len(tiles),
        "seam_check": {"method": "3x3 repeat", "base_tile_indices": [1, 9, 17, 25], "image": seam_path.name},
        "checks": checks,
        "all_passed": len(tiles) == 32 and all(
            check["size_ok"] and check["binary_alpha_ok"] and check["palette_ok"] and check["green_opaque_pixels"] == 0
            for check in checks
        ),
    }
    report_path = output / "validation-report.json"
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    if not report["all_passed"]:
        raise ValueError("Validation failed; see validation-report.json")

    zip_path = output / "stage01-tileset-draft-v2.zip"
    files = [raw_path, processed_path, tileset_path, seam_path, output / "manifest.json", report_path, *tiles]
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for path in files:
            info = zipfile.ZipInfo(path.relative_to(output).as_posix(), (2026, 8, 24, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, path.read_bytes())
    print(json.dumps({"output": str(output), "zip": str(zip_path), "all_passed": True}))


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: postprocess.py SOURCE OUTPUT_DIR")
    main(Path(sys.argv[1]), Path(sys.argv[2]))
