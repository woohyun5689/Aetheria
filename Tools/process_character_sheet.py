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


def separate_state_layers(
    sheet: Image.Image,
    x_edges: list[int],
    y_edges: list[int],
    threshold: int = 8,
) -> tuple[Image.Image, ...]:
    """Keep connected character parts with their intended state pose.

    Feet, bows, and robe hems can cross a mathematical 2x3 cell boundary in an
    AI-generated sheet.  Instead of cropping a cell and deleting its border,
    label the full sheet first, then assign each connected component to the
    cell containing most of its alpha pixels (with the weighted centre only as
    a deterministic tiebreaker).
    """
    alpha = np.asarray(sheet.getchannel("A"))
    mask = alpha > threshold
    labels, count = ndimage.label(mask, structure=np.ones((3, 3), dtype=np.uint8))
    if count == 0:
        raise ValueError("No visible pixels found in character sheet")

    state_alpha = [np.zeros_like(alpha) for _ in STATES]
    areas = np.bincount(labels.ravel())
    # Preserve detached visual details; discard only chroma-key specks.
    minimum_area = max(8, round(sheet.width * sheet.height * 0.000002))

    for component_id, slices in enumerate(ndimage.find_objects(labels), start=1):
        if slices is None or areas[component_id] < minimum_area:
            continue

        y_slice, x_slice = slices
        component_mask = labels[y_slice, x_slice] == component_id
        component_alpha = alpha[y_slice, x_slice]
        y_points, x_points = np.nonzero(component_mask)
        weights = component_alpha[component_mask].astype(np.float64)
        absolute_y = y_points + y_slice.start
        absolute_x = x_points + x_slice.start
        columns = np.clip(np.searchsorted(x_edges, absolute_x, side="right") - 1, 0, 1)
        rows = np.clip(np.searchsorted(y_edges, absolute_y, side="right") - 1, 0, 2)
        ownership = np.bincount(rows * 2 + columns, weights=weights, minlength=len(STATES))
        candidates = np.flatnonzero(ownership == ownership.max())
        if len(candidates) == 1:
            state_index = int(candidates[0])
        else:
            centre_y = float(np.average(absolute_y, weights=weights))
            centre_x = float(np.average(absolute_x, weights=weights))
            column = min(1, max(0, np.searchsorted(x_edges, centre_x, side="right") - 1))
            row = min(2, max(0, np.searchsorted(y_edges, centre_y, side="right") - 1))
            state_index = row * 2 + column
        destination_alpha = state_alpha[state_index][y_slice, x_slice]
        destination_alpha[component_mask] = component_alpha[component_mask]

    layers: list[Image.Image] = []
    for state_index in range(len(STATES)):
        layer = sheet.copy()
        layer.putalpha(Image.fromarray(state_alpha[state_index], mode="L"))
        layers.append(layer)
    return tuple(layers)


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

    state_layers = separate_state_layers(sheet, x_edges, y_edges)

    for state, layer in zip(STATES, state_layers):
        bbox = alpha_bbox(layer)
        if bbox is None:
            raise ValueError(f"No visible pixels found in {state} cell")

        subject = layer.crop(bbox)
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
