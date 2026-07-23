"""Split the generated chroma-key atlases into Unity-ready RGBA skill VFX.

Run only after remove_chroma_key.py has produced the *_atlas_transparent.png
files. This script owns the knight, bomber, and common V2 outputs only.
"""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[3]
ART = ROOT / "ArtSource" / "Generated" / "CombatFXV2"
SKILLS = (
    ROOT
    / "Assets"
    / "Resources"
    / "UI"
    / "VisualRefresh"
    / "CombatFX"
    / "SkillsV2"
)
COMMON = (
    ROOT
    / "Assets"
    / "Resources"
    / "UI"
    / "VisualRefresh"
    / "CombatFX"
    / "V2"
)


def split_bounds(length: int, count: int) -> list[int]:
    return [round(index * length / count) for index in range(count + 1)]


def content_bbox(image: Image.Image, alpha_floor: int = 5) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    mask = alpha.point(lambda value: 255 if value > alpha_floor else 0)
    return mask.getbbox() or (0, 0, image.width, image.height)


def remove_green_spill(image: Image.Image) -> Image.Image:
    """Warm residual key spill without altering intentional orange/yellow light."""
    pixels = []
    for red, green, blue, alpha in image.getdata():
        value = max(red, green, blue)
        olive_spill = (
            alpha > 0
            and green > blue * 1.12
            and green >= red * 0.78
            and (green >= red - 20 or value < 190)
        )
        if olive_spill and value < 108:
            # Low-value spill belongs to soot/smoke: make it neutral charcoal
            # with a restrained warm bias.
            warmed_red = round(value * 0.54)
            warmed_green = round(value * 0.38)
            warmed_blue = round(value * 0.31)
        elif olive_spill:
            # Brighter spill belongs to pressure rings and sparks: shift it to
            # brass/amber while keeping the original luminance hierarchy.
            warmed_red = min(255, round(value * 1.04))
            warmed_green = round(value * 0.58)
            warmed_blue = round(value * 0.22)
        elif alpha > 0 and green > max(red, blue) + 6:
            warmed_red = max(red, round(green * 0.82))
            warmed_green = min(green, round(max(red, blue) * 1.05 + 2))
            warmed_blue = min(blue, round(warmed_green * 0.65))
        else:
            pixels.append((red, green, blue, alpha))
            continue
        if alpha > 0:
            pixels.append((warmed_red, warmed_green, warmed_blue, alpha))
    warmed = Image.new("RGBA", image.size)
    warmed.putdata(pixels)
    return warmed


def render_cell(
    atlas: Image.Image,
    columns: int,
    rows: int,
    column: int,
    row: int,
    size: tuple[int, int],
    out_path: Path,
    occupancy: tuple[float, float],
    warm_key_spill: bool = False,
    y_bounds: list[int] | None = None,
) -> None:
    xs = split_bounds(atlas.width, columns)
    ys = y_bounds or split_bounds(atlas.height, rows)
    cell = atlas.crop((xs[column], ys[row], xs[column + 1], ys[row + 1]))
    if warm_key_spill:
        cell = remove_green_spill(cell)
    cell = cell.crop(content_bbox(cell))

    max_width = max(1, round(size[0] * occupancy[0]))
    max_height = max(1, round(size[1] * occupancy[1]))
    scale = min(max_width / cell.width, max_height / cell.height)
    resized = cell.resize(
        (max(1, round(cell.width * scale)), max(1, round(cell.height * scale))),
        Image.Resampling.LANCZOS,
    )

    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    x = (size[0] - resized.width) // 2
    y = (size[1] - resized.height) // 2
    canvas.alpha_composite(resized, (x, y))
    if warm_key_spill:
        canvas = remove_green_spill(canvas)

    # Remove mathematically negligible alpha left by resampling while retaining
    # painterly glows and antialiased particles.
    red, green, blue, alpha = canvas.split()
    alpha = alpha.point(lambda value: 0 if value <= 2 else value)
    canvas = Image.merge("RGBA", (red, green, blue, alpha))

    out_path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(out_path, optimize=True)


def process_class(
    atlas_path: Path,
    skills: list[tuple[str, str]],
    warm_key_spill: bool = False,
    row_bounds: list[int] | None = None,
) -> None:
    atlas = Image.open(atlas_path).convert("RGBA")
    for column, (skill_key, mode) in enumerate(skills):
        render_cell(
            atlas,
            3,
            4,
            column,
            0,
            (1254, 1254),
            SKILLS / skill_key / "fx_cast.png",
            (0.86, 0.86),
            warm_key_spill,
            row_bounds,
        )
        if mode == "attack":
            render_cell(
                atlas,
                3,
                4,
                column,
                1,
                (1536, 1024),
                SKILLS / skill_key / "fx_action.png",
                (0.91, 0.88),
                warm_key_spill,
                row_bounds,
            )
            render_cell(
                atlas,
                3,
                4,
                column,
                2,
                (1254, 1254),
                SKILLS / skill_key / "fx_impact.png",
                (0.88, 0.88),
                warm_key_spill,
                row_bounds,
            )
        else:
            render_cell(
                atlas,
                3,
                4,
                column,
                3,
                (1024, 1536),
                SKILLS / skill_key / "fx_support.png",
                (0.88, 0.90),
                warm_key_spill,
                row_bounds,
            )


def process_common(atlas_path: Path) -> None:
    atlas = Image.open(atlas_path).convert("RGBA")
    names = [
        ("fx_contact_flash.png", 0, 0),
        ("fx_shockwave_ring.png", 1, 0),
        ("fx_speed_streaks.png", 0, 1),
        ("fx_spark_cluster.png", 1, 1),
    ]
    for name, column, row in names:
        render_cell(
            atlas,
            2,
            2,
            column,
            row,
            (1254, 1254),
            COMMON / name,
            (0.88, 0.88),
        )


if __name__ == "__main__":
    process_class(
        ART / "knight" / "knight_atlas_transparent.png",
        [
            ("knight_shield_bash", "attack"),
            ("knight_ironwall_guard", "support"),
            ("knight_holy_crush", "attack"),
        ],
    )
    process_class(
        ART / "bomber" / "bomber_atlas_transparent.png",
        [
            ("bomber_blazing_fire", "attack"),
            ("bomber_shatter_bomb", "attack"),
            ("bomber_chain_detonation", "attack"),
        ],
        warm_key_spill=True,
        row_bounds=[0, 365, 690, 1050, 1254],
    )
    process_common(ART / "common" / "common_atlas_transparent.png")
