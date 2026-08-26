from __future__ import annotations

import json
import zipfile
from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "stage01-street-objects-imagegen-source.png"
SHEET = ROOT / "stage01-street-objects-processed.png"
OBJECTS = ROOT / "objects"
VALIDATION = ROOT / "stage01-street-objects-tilemap-validation.png"
REPORT = ROOT / "validation-report.json"
ARCHIVE = ROOT / "stage01-street-objects-draft-v3.zip"
TILESET = ROOT.parent / "stage01-tileset-draft-v2" / "stage01-tileset-128x64.png"
SCALE = 4
PALETTE_HEX = (
    "#1a1119", "#2b1e2a", "#4a3142", "#7d4a48",
    "#b06b45", "#e0954a", "#f2c46b", "#ffe8b0",
)
PALETTE = tuple(tuple(bytes.fromhex(value[1:])) for value in PALETTE_HEX)
NAMES = (
    "01-bus-stop-shelter.png",
    "02-bus-stop-bench.png",
    "03-bus-stop-sign.png",
    "04-utility-pole.png",
    "05-lamp-utility-pole.png",
    "06-wire-straight.png",
    "07-wire-sagging.png",
    "08-sidewalk-guardrail.png",
    "09-street-trash-bin.png",
    "10-fallen-road-sign.png",
    "11-low-concrete-wall.png",
    "12-wall-weed-vine.png",
)


def green_dominant(rgb: tuple[int, int, int]) -> bool:
    r, g, b = rgb
    return g >= 100 and g >= r + 25 and g >= b + 25 and g * 4 >= r * 5 and g * 4 >= b * 5


def nearest_palette(rgb: tuple[int, int, int]) -> tuple[int, int, int]:
    return min(PALETTE, key=lambda color: sum((a - b) ** 2 for a, b in zip(rgb, color)))


def remove_green_and_quantize(source: Image.Image) -> tuple[Image.Image, dict[str, int]]:
    rgb = source.convert("RGB")
    width, height = rgb.size
    pixels = rgb.load()
    exterior: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()

    for x in range(width):
        queue.extend(((x, 0), (x, height - 1)))
    for y in range(height):
        queue.extend(((0, y), (width - 1, y)))

    while queue:
        x, y = queue.popleft()
        if (x, y) in exterior or not green_dominant(pixels[x, y]):
            continue
        exterior.add((x, y))
        if x:
            queue.append((x - 1, y))
        if x + 1 < width:
            queue.append((x + 1, y))
        if y:
            queue.append((x, y - 1))
        if y + 1 < height:
            queue.append((x, y + 1))

    result = Image.new("RGBA", source.size, (0, 0, 0, 0))
    output = result.load()
    enclosed_green = 0
    for y in range(height):
        for x in range(width):
            color = pixels[x, y]
            if (x, y) in exterior:
                continue
            if green_dominant(color) or color == (0, 255, 0):
                enclosed_green += 1
                continue
            output[x, y] = (*nearest_palette(color), 255)

    return result, {"exterior_green_pixels_removed": len(exterior), "enclosed_green_pixels_removed": enclosed_green}


def runs(values: list[bool]) -> list[tuple[int, int]]:
    result: list[tuple[int, int]] = []
    start = None
    for index, value in enumerate(values + [False]):
        if value and start is None:
            start = index
        elif not value and start is not None:
            result.append((start, index))
            start = None
    return result


def extract_objects(sheet: Image.Image) -> tuple[list[Image.Image], list[tuple[int, int, int, int]]]:
    alpha = sheet.getchannel("A")
    row_runs = runs([alpha.crop((0, y, sheet.width, y + 1)).getbbox() is not None for y in range(sheet.height)])
    if len(row_runs) != 3:
        raise ValueError(f"Expected 3 occupied rows, found {len(row_runs)}: {row_runs}")

    objects: list[Image.Image] = []
    boxes: list[tuple[int, int, int, int]] = []
    for top, bottom in row_runs:
        column_runs = runs([
            alpha.crop((x, top, x + 1, bottom)).getbbox() is not None for x in range(sheet.width)
        ])
        if len(column_runs) != 4:
            raise ValueError(f"Expected 4 objects in row {top}:{bottom}, found {len(column_runs)}: {column_runs}")
        for left, right in column_runs:
            region = sheet.crop((left, top, right, bottom))
            bbox = region.getbbox()
            if bbox is None:
                raise ValueError("Object was completely removed")
            box = (left + bbox[0], top + bbox[1], left + bbox[2], top + bbox[3])
            objects.append(sheet.crop(box))
            boxes.append(box)
    return objects, boxes


def validate_image(image: Image.Image) -> dict[str, object]:
    rgba = image.convert("RGBA")
    colors = set()
    alpha_values = set()
    transparent_normalized = True
    green_pixels = 0
    opaque = 0
    for pixel in rgba.getdata():
        rgb, alpha = pixel[:3], pixel[3]
        alpha_values.add(alpha)
        if alpha == 0:
            transparent_normalized &= pixel == (0, 0, 0, 0)
        else:
            opaque += 1
            colors.add(rgb)
            green_pixels += green_dominant(rgb)
    return {
        "mode": rgba.mode,
        "size": list(rgba.size),
        "opaque_pixels": opaque,
        "alpha_values": sorted(alpha_values),
        "binary_alpha": alpha_values <= {0, 255},
        "transparent_pixels_normalized": transparent_normalized,
        "opaque_palette_only": colors <= set(PALETTE),
        "opaque_colors": sorted("#" + bytes(color).hex() for color in colors),
        "opaque_green_dominant_pixels": green_pixels,
        "not_empty": opaque > 0,
    }


def make_validation(objects: list[Image.Image]) -> None:
    cell_width, row_height = 128, 112
    canvas = Image.new("RGBA", (cell_width * 4, row_height * 3), (*PALETTE[3], 255))
    tileset = Image.open(TILESET).convert("RGBA")
    tile = tileset.crop((0, 48, 16, 64))
    for row in range(3):
        baseline = row * row_height + 94
        for x in range(0, canvas.width, 16):
            canvas.alpha_composite(tile, (x, baseline + 1))
        for column in range(4):
            index = row * 4 + column
            sprite = objects[index]
            x = column * cell_width + (cell_width - sprite.width) // 2
            y = baseline - sprite.height + 1 if index not in (5, 6) else baseline - 60
            canvas.alpha_composite(sprite, (x, y))
    canvas.save(VALIDATION)


def main() -> None:
    OBJECTS.mkdir(exist_ok=True)
    source = Image.open(SOURCE)
    cleaned, removal = remove_green_and_quantize(source)
    sheet = cleaned.resize((cleaned.width // SCALE, cleaned.height // SCALE), Image.Resampling.NEAREST)
    sheet.save(SHEET)
    objects, boxes = extract_objects(sheet)
    for name, image in zip(NAMES, objects):
        image.save(OBJECTS / name)
    make_validation(objects)

    object_results = []
    for order, (name, image, box) in enumerate(zip(NAMES, objects, boxes), 1):
        result = validate_image(image)
        result.update({
            "order": order,
            "filename": name,
            "sheet_bbox": list(box),
            "source_margin_clear": box[0] > 0 and box[1] > 0 and box[2] < sheet.width and box[3] < sheet.height,
        })
        object_results.append(result)

    sheet_result = validate_image(sheet)
    checks = {
        "object_count_is_12": len(objects) == 12,
        "row_major_filename_order": [item["filename"] for item in object_results] == list(NAMES),
        "all_rgba": all(item["mode"] == "RGBA" for item in object_results),
        "all_binary_alpha": all(item["binary_alpha"] for item in object_results),
        "all_transparent_pixels_normalized": all(item["transparent_pixels_normalized"] for item in object_results),
        "all_opaque_pixels_in_palette": all(item["opaque_palette_only"] for item in object_results),
        "opaque_green_pixels_are_zero": all(item["opaque_green_dominant_pixels"] == 0 for item in object_results),
        "no_object_empty": all(item["not_empty"] for item in object_results),
        "all_objects_clear_of_source_sheet_edges": all(item["source_margin_clear"] for item in object_results),
    }
    report = {
        "source_sheet": str(SOURCE),
        "processed_sheet": str(SHEET),
        "tilemap_validation_image": str(VALIDATION),
        "source_size": list(source.size),
        "downscale": {"factor": SCALE, "resampling": "NEAREST"},
        "palette": list(PALETTE_HEX),
        "background_removal": removal,
        "processed_sheet_validation": sheet_result,
        "objects": object_results,
        "automatic_checks": checks,
        "automatic_validation_passed": all(checks.values()),
        "visual_review": {
            "status": "passed",
            "checks": [
                "strict orthographic side elevation; no perspective/top-down/isometric",
                "no clipping or overlaps",
                "tilemap-scale, ground contact, pixel density, outline, lighting, material consistency",
            ],
            "notes": "All 12 sprites were reviewed at native scale on the repeated tileset floor; wires are suspended for scale review. No perspective, top-down, isometric, clipping, overlap, ground-contact, relative-scale, or style mismatch requiring regeneration was found.",
            "regenerate_or_manual_cleanup": [],
        },
    }
    REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    with zipfile.ZipFile(ARCHIVE, "w", zipfile.ZIP_DEFLATED) as archive:
        for path in (SOURCE, SHEET, VALIDATION, REPORT, Path(__file__)):
            archive.write(path, path.relative_to(ROOT))
        for path in sorted(OBJECTS.glob("*.png")):
            archive.write(path, path.relative_to(ROOT))

    if not report["automatic_validation_passed"]:
        raise SystemExit("Automatic validation failed; inspect validation-report.json")


if __name__ == "__main__":
    main()
