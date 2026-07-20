#!/usr/bin/env python3
"""Normalize transparent enemy illustrations into Unity-ready combat sprites."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def alpha_bbox(image: Image.Image, threshold: int = 8) -> tuple[int, int, int, int] | None:
    alpha = image.getchannel("A")
    return alpha.point(lambda value: 255 if value > threshold else 0).getbbox()


def normalize(source: Path, destination: Path, width: int, height: int) -> None:
    image = Image.open(source).convert("RGBA")
    bbox = alpha_bbox(image)
    if bbox is None:
        raise ValueError(f"No visible sprite pixels in {source}")

    subject = image.crop(bbox)
    side_margin = round(width * 0.055)
    top_margin = round(height * 0.035)
    bottom_margin = round(height * 0.04)
    scale = min(
        (width - side_margin * 2) / subject.width,
        (height - top_margin - bottom_margin) / subject.height,
    )
    subject = subject.resize(
        (max(1, round(subject.width * scale)), max(1, round(subject.height * scale))),
        Image.Resampling.LANCZOS,
    )

    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    x = (width - subject.width) // 2
    y = max(top_margin, height - bottom_margin - subject.height)
    canvas.alpha_composite(subject, (x, y))
    destination.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(destination, optimize=True)


def trim_to_subject(
    source: Path,
    destination: Path,
    max_edge: int,
    padding_ratio: float,
) -> None:
    """Keep a tight, padded canvas so wide monsters use the full combat slot."""
    image = Image.open(source).convert("RGBA")
    bbox = alpha_bbox(image)
    if bbox is None:
        raise ValueError(f"No visible sprite pixels in {source}")

    subject = image.crop(bbox)
    padding = max(2, round(max(subject.size) * padding_ratio))
    canvas = Image.new(
        "RGBA",
        (subject.width + padding * 2, subject.height + padding * 2),
        (0, 0, 0, 0),
    )
    canvas.alpha_composite(subject, (padding, padding))

    scale = min(1.0, max_edge / max(canvas.size))
    if scale < 1.0:
        canvas = canvas.resize(
            (max(1, round(canvas.width * scale)), max(1, round(canvas.height * scale))),
            Image.Resampling.LANCZOS,
        )

    destination.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(destination, optimize=True)


def contact_sheet(sprites: list[Path], destination: Path) -> None:
    cell_width, cell_height = 300, 390
    columns = 3
    rows = (len(sprites) + columns - 1) // columns
    sheet = Image.new("RGB", (cell_width * columns, cell_height * rows), "#edf5fa")
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, path in enumerate(sprites):
        sprite = Image.open(path).convert("RGBA")
        preview = sprite.copy()
        preview.thumbnail((cell_width - 24, cell_height - 50), Image.Resampling.LANCZOS)
        x = (index % columns) * cell_width + (cell_width - preview.width) // 2
        y = (index // columns) * cell_height + 10
        sheet.paste(preview, (x, y), preview)
        draw.text(
            ((index % columns) * cell_width + 12, (index // columns + 1) * cell_height - 30),
            path.stem,
            fill="#152b40",
            font=font,
        )
    destination.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(destination, optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input-dir", required=True, type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--width", type=int, default=512)
    parser.add_argument("--height", type=int, default=768)
    parser.add_argument("--tight", action="store_true")
    parser.add_argument("--max-edge", type=int, default=768)
    parser.add_argument("--padding", type=float, default=0.06)
    parser.add_argument("--contact-sheet", type=Path)
    args = parser.parse_args()

    sources = sorted(args.input_dir.glob("*.png"))
    if not sources:
        raise ValueError(f"No PNG files found in {args.input_dir}")
    outputs: list[Path] = []
    for source in sources:
        destination = args.output_dir / source.name
        if args.tight:
            trim_to_subject(source, destination, args.max_edge, args.padding)
        else:
            normalize(source, destination, args.width, args.height)
        outputs.append(destination)
    if args.contact_sheet:
        contact_sheet(outputs, args.contact_sheet)


if __name__ == "__main__":
    main()
