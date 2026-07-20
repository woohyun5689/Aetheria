#!/usr/bin/env python3
"""Split a transparent 2x3 character sheet into Unity-ready portrait sprites."""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage


STATES = ("idle", "combat", "skill", "victory", "defeat", "rest")
POSE_HEIGHT = {
    "idle": 0.90,
    "combat": 0.90,
    "skill": 0.90,
    "victory": 0.90,
    "defeat": 0.80,
    "rest": 0.84,
}


def alpha_bbox(image: Image.Image, threshold: int = 8) -> tuple[int, int, int, int] | None:
    alpha = image.getchannel("A")
    mask = alpha.point(lambda value: 255 if value > threshold else 0)
    return mask.getbbox()


def remove_spill_components(image: Image.Image, threshold: int = 8) -> Image.Image:
    """Remove tiny disconnected fragments, especially along sprite-sheet seams."""
    alpha = np.asarray(image.getchannel("A"))
    mask = alpha > threshold
    labels, count = ndimage.label(mask, structure=np.ones((3, 3), dtype=np.uint8))
    if count == 0:
        return image

    areas = np.bincount(labels.ravel())
    minimum_area = max(96, round(image.width * image.height * 0.0025))
    border_area_limit = round(image.width * image.height * 0.02)
    edge_zone = round(min(image.size) * 0.06)
    keep = np.zeros(count + 1, dtype=bool)

    objects = ndimage.find_objects(labels)
    for component_id, slices in enumerate(objects, start=1):
        if slices is None or areas[component_id] < minimum_area:
            continue
        y_slice, x_slice = slices
        near_edge = (
            x_slice.start < edge_zone
            or y_slice.start < edge_zone
            or x_slice.stop > image.width - edge_zone
            or y_slice.stop > image.height - edge_zone
        )
        if near_edge and areas[component_id] < border_area_limit:
            continue
        keep[component_id] = True

    filtered_alpha = np.where(keep[labels], alpha, 0).astype(np.uint8)
    result = image.copy()
    result.putalpha(Image.fromarray(filtered_alpha, mode="L"))
    return result


def split_sheet(
    source: Path,
    output_dir: Path,
    canvas_width: int,
    canvas_height: int,
) -> None:
    sheet = Image.open(source).convert("RGBA")
    if sheet.width < 2 or sheet.height < 3:
        raise ValueError(f"Sheet is too small: {sheet.size}")

    output_dir.mkdir(parents=True, exist_ok=True)
    x_edges = [round(index * sheet.width / 2) for index in range(3)]
    y_edges = [round(index * sheet.height / 3) for index in range(4)]

    side_margin = max(16, round(canvas_width * 0.055))
    top_margin = max(16, round(canvas_height * 0.035))
    bottom_margin = max(18, round(canvas_height * 0.04))

    for index, state in enumerate(STATES):
        column = index % 2
        row = index // 2
        cell = sheet.crop(
            (
                x_edges[column],
                y_edges[row],
                x_edges[column + 1],
                y_edges[row + 1],
            )
        )
        # Image generators occasionally let a few pixels spill across an exact
        # sheet boundary. The prompt reserves a safe margin, so clearing a thin
        # gutter removes neighboring-pose fragments without touching the sprite.
        gutter = max(6, round(min(cell.size) * 0.02))
        cell.paste((0, 0, 0, 0), (0, 0, cell.width, gutter))
        cell.paste((0, 0, 0, 0), (0, cell.height - gutter, cell.width, cell.height))
        cell.paste((0, 0, 0, 0), (0, 0, gutter, cell.height))
        cell.paste((0, 0, 0, 0), (cell.width - gutter, 0, cell.width, cell.height))
        cell = remove_spill_components(cell)
        bbox = alpha_bbox(cell)
        if bbox is None:
            raise ValueError(f"No visible pixels found in {state} cell")

        subject = cell.crop(bbox)
        max_width = canvas_width - side_margin * 2
        max_height = min(
            canvas_height - top_margin - bottom_margin,
            round(canvas_height * POSE_HEIGHT[state]),
        )
        scale = min(max_width / subject.width, max_height / subject.height)
        target_size = (
            max(1, round(subject.width * scale)),
            max(1, round(subject.height * scale)),
        )
        subject = subject.resize(target_size, Image.Resampling.LANCZOS)

        canvas = Image.new("RGBA", (canvas_width, canvas_height), (0, 0, 0, 0))
        x = (canvas_width - subject.width) // 2
        baseline = canvas_height - bottom_margin
        y = max(top_margin, baseline - subject.height)
        canvas.alpha_composite(subject, (x, y))

        destination = output_dir / f"{state}.png"
        canvas.save(destination, optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--width", type=int, default=512)
    parser.add_argument("--height", type=int, default=768)
    args = parser.parse_args()

    split_sheet(args.input, args.output_dir, args.width, args.height)


if __name__ == "__main__":
    main()
