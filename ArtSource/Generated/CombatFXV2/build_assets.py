from __future__ import annotations

from dataclasses import dataclass
from math import hypot
from pathlib import Path

from PIL import Image


SOURCE_ROOT = Path(__file__).resolve().parent
REPOSITORY_ROOT = SOURCE_ROOT.parents[2]
OUTPUT_ROOT = (
    REPOSITORY_ROOT
    / "Assets"
    / "Resources"
    / "UI"
    / "VisualRefresh"
    / "CombatFX"
    / "SkillsV2"
)

PHASE_SIZE = {
    "cast": (1254, 1254),
    "action": (1536, 1024),
    "impact": (1254, 1254),
    "support": (1024, 1536),
}


@dataclass(frozen=True)
class AssetSpec:
    skill_key: str
    phase: str
    region: tuple[float, float, float, float]


SPECS: dict[str, tuple[AssetSpec, ...]] = {
    "mage": (
        AssetSpec("mage_fireball", "cast", (0.0, 0.0, 1 / 3, 1 / 4)),
        AssetSpec("mage_fireball", "action", (0.0, 1 / 4, 1 / 3, 2 / 4)),
        AssetSpec("mage_fireball", "impact", (0.0, 2 / 4, 1 / 3, 3 / 4)),
        AssetSpec("mage_frost_barrier", "cast", (1 / 3, 0.0, 2 / 3, 1 / 4)),
        AssetSpec("mage_frost_barrier", "support", (1 / 3, 3 / 4, 2 / 3, 1.0)),
        AssetSpec("mage_lightning_storm", "cast", (2 / 3, 0.0, 1.0, 1 / 4)),
        AssetSpec("mage_lightning_storm", "action", (2 / 3, 1 / 4, 1.0, 2 / 4)),
        AssetSpec("mage_lightning_storm", "impact", (2 / 3, 2 / 4, 1.0, 3 / 4)),
    ),
    "priest": (
        AssetSpec("priest_radiant_judgment", "cast", (0.0, 0.0, 1 / 3, 1 / 4)),
        AssetSpec("priest_radiant_judgment", "action", (0.0, 1 / 4, 1 / 3, 2 / 4)),
        AssetSpec("priest_radiant_judgment", "impact", (0.0, 2 / 4, 1 / 3, 3 / 4)),
        AssetSpec("priest_sanctuary", "cast", (1 / 3, 0.0, 2 / 3, 1 / 4)),
        AssetSpec("priest_sanctuary", "support", (0.27, 3 / 4, 0.72, 1.0)),
        AssetSpec("priest_salvation_ray", "cast", (2 / 3, 0.0, 1.0, 1 / 4)),
        AssetSpec("priest_salvation_ray", "action", (0.49, 1 / 4, 1.0, 2 / 4)),
        AssetSpec("priest_salvation_ray", "impact", (2 / 3, 2 / 4, 1.0, 3 / 4)),
        AssetSpec("priest_salvation_ray", "support", (0.70, 3 / 4, 1.0, 1.0)),
    ),
    "spirit": (
        AssetSpec("spirit_fire_spirit", "cast", (0.0, 0.0, 1 / 4, 1 / 4)),
        AssetSpec("spirit_fire_spirit", "action", (0.0, 1 / 4, 1 / 4, 2 / 4)),
        AssetSpec("spirit_fire_spirit", "impact", (0.0, 2 / 4, 1 / 4, 3 / 4)),
        AssetSpec("spirit_water_spirit", "cast", (1 / 4, 0.0, 2 / 4, 1 / 4)),
        AssetSpec("spirit_water_spirit", "action", (1 / 4, 1 / 4, 2 / 4, 2 / 4)),
        AssetSpec("spirit_water_spirit", "impact", (1 / 4, 2 / 4, 2 / 4, 3 / 4)),
        AssetSpec("spirit_water_spirit", "support", (1 / 4, 3 / 4, 2 / 4, 1.0)),
        AssetSpec("spirit_wind_spirit", "cast", (2 / 4, 0.0, 3 / 4, 1 / 4)),
        AssetSpec("spirit_wind_spirit", "action", (2 / 4, 1 / 4, 3 / 4, 2 / 4)),
        AssetSpec("spirit_wind_spirit", "impact", (2 / 4, 2 / 4, 3 / 4, 3 / 4)),
        AssetSpec("spirit_earth_spirit", "cast", (3 / 4, 0.0, 1.0, 1 / 4)),
        AssetSpec("spirit_earth_spirit", "action", (3 / 4, 1 / 4, 1.0, 2 / 4)),
        AssetSpec("spirit_earth_spirit", "impact", (3 / 4, 2 / 4, 1.0, 3 / 4)),
    ),
}


def pixel_region(
    image: Image.Image, normalized: tuple[float, float, float, float]
) -> tuple[int, int, int, int]:
    width, height = image.size
    left, top, right, bottom = normalized
    return (
        round(left * width),
        round(top * height),
        round(right * width),
        round(bottom * height),
    )


def trim_effect(cell: Image.Image) -> Image.Image:
    alpha = cell.getchannel("A")
    visible = alpha.point(lambda value: 255 if value >= 8 else 0)
    bounds = visible.getbbox()
    if bounds is None:
        raise ValueError("The requested atlas region contains no visible effect.")

    left, top, right, bottom = bounds
    padding = max(4, round(max(right - left, bottom - top) * 0.035))
    return cell.crop(
        (
            max(0, left - padding),
            max(0, top - padding),
            min(cell.width, right + padding),
            min(cell.height, bottom + padding),
        )
    )


def neutralize_magenta_spill(effect: Image.Image) -> Image.Image:
    """Turn opaque key-light contamination into the mage's violet conduit color."""
    pixels: list[tuple[int, int, int, int]] = []
    for red, green, blue, alpha in effect.getdata():
        key_distance = min(red, blue) - green
        if alpha > 0 and red > 120 and blue > 120 and key_distance > 45:
            strength = min(1.0, max(0.0, (key_distance - 45) / 150))
            value = max(red, blue)
            target = (
                round(value * 0.38),
                round(value * 0.09 + green * 0.25),
                round(value * 0.88),
            )
            red = round(red + (target[0] - red) * strength)
            green = round(green + (target[1] - green) * strength)
            blue = round(blue + (target[2] - blue) * strength)
            if alpha < 48 and strength > 0.55:
                alpha = round(alpha * (1.0 - 0.72 * strength))
        pixels.append((red, green, blue, alpha))

    cleaned = Image.new("RGBA", effect.size)
    cleaned.putdata(pixels)
    return cleaned


def warm_fireball_palette(effect: Image.Image, phase: str) -> Image.Image:
    """Make the fire skill read warm while retaining a small violet conduit edge."""
    width, height = effect.size
    pixels: list[tuple[int, int, int, int]] = []
    for index, (red, green, blue, alpha) in enumerate(effect.getdata()):
        x = (index % width) / max(1, width - 1)
        y = (index // width) / max(1, height - 1)

        if phase == "action":
            warm_zone = min(1.0, max(0.0, (x - 0.10) / 0.42))
        else:
            radius = hypot((x - 0.5) * 2.0, (y - 0.5) * 2.0)
            warm_zone = min(1.0, max(0.0, (0.90 - radius) / 0.25))

        value = max(red, green, blue)
        purple = (
            alpha > 0
            and value > 18
            and blue > green * 1.12
            and red > green * 1.08
            and (red + blue) * 0.5 - green > 18
        )
        warm_smoke = (
            alpha > 0
            and value <= 105
            and red >= green * 1.05
            and blue >= green * 1.05
        )

        if purple or warm_smoke:
            strength = warm_zone * (0.92 if purple else 0.72)
            if value >= 150:
                target = (
                    min(255, round(value * 1.04)),
                    round(value * 0.34),
                    round(value * 0.055),
                )
            else:
                fire_value = min(220, round(value * 1.52 + 24))
                target = (
                    fire_value,
                    round(fire_value * 0.34),
                    round(fire_value * 0.055),
                )
            red = round(red + (target[0] - red) * strength)
            green = round(green + (target[1] - green) * strength)
            blue = round(blue + (target[2] - blue) * strength)

        near_white = alpha > 0 and min(red, green, blue) > 205
        if phase == "action":
            ember_zone = x < 0.76
        else:
            radius = hypot((x - 0.5) * 2.0, (y - 0.5) * 2.0)
            ember_zone = radius > 0.23
        if near_white and ember_zone:
            red, green, blue = (255, 176, 48)

        pixels.append((red, green, blue, alpha))

    warmed = Image.new("RGBA", effect.size)
    warmed.putdata(pixels)
    return warmed


def tune_lightning_palette(effect: Image.Image, phase: str) -> Image.Image:
    """Push the storm toward blue-white electricity with restrained gold accents."""
    width, height = effect.size
    pixels: list[tuple[int, int, int, int]] = []
    for index, (red, green, blue, alpha) in enumerate(effect.getdata()):
        x = (index % width) / max(1, width - 1)
        y = (index // width) / max(1, height - 1)
        value = max(red, green, blue)
        chromatic = (
            alpha > 0
            and 38 < value < 246
            and blue > green * 1.08
            and red > green * 0.72
        )

        gold_conduit = (
            chromatic
            and alpha >= 80
            and 45 < value < 150
            and red < value * 0.55
            and green < value * 0.35
        )

        if gold_conduit:
            gold_value = min(255, max(155, round(value * 1.45 + 34)))
            target = (gold_value, round(gold_value * 0.70), round(gold_value * 0.11))
            strength = 0.86
        elif chromatic:
            target = (
                round(value * 0.18),
                round(value * 0.58),
                value,
            )
            strength = 0.80
        else:
            pixels.append((red, green, blue, alpha))
            continue

        pixels.append(
            (
                round(red + (target[0] - red) * strength),
                round(green + (target[1] - green) * strength),
                round(blue + (target[2] - blue) * strength),
                alpha,
            )
        )

    tuned = Image.new("RGBA", effect.size)
    tuned.putdata(pixels)
    return tuned


def place_on_canvas(effect: Image.Image, phase: str) -> Image.Image:
    canvas_width, canvas_height = PHASE_SIZE[phase]
    fill = 0.91 if phase in {"cast", "impact"} else 0.94
    scale = min(
        canvas_width * fill / effect.width,
        canvas_height * fill / effect.height,
    )
    output_size = (
        max(1, round(effect.width * scale)),
        max(1, round(effect.height * scale)),
    )
    resized = effect.resize(output_size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (canvas_width, canvas_height), (0, 0, 0, 0))
    offset = (
        (canvas_width - resized.width) // 2,
        (canvas_height - resized.height) // 2,
    )
    canvas.alpha_composite(resized, offset)
    return canvas


def main() -> None:
    for class_key, specs in SPECS.items():
        atlas_path = SOURCE_ROOT / class_key / "atlas_rgba.png"
        atlas = Image.open(atlas_path).convert("RGBA")

        for spec in specs:
            cell = atlas.crop(pixel_region(atlas, spec.region))
            effect = trim_effect(cell)
            if class_key == "mage":
                effect = neutralize_magenta_spill(effect)
            if spec.skill_key == "mage_fireball":
                effect = warm_fireball_palette(effect, spec.phase)
            if spec.skill_key == "mage_lightning_storm":
                effect = tune_lightning_palette(effect, spec.phase)
            output = place_on_canvas(effect, spec.phase)
            output_dir = OUTPUT_ROOT / spec.skill_key
            output_dir.mkdir(parents=True, exist_ok=True)
            output.save(output_dir / f"fx_{spec.phase}.png", optimize=True)


if __name__ == "__main__":
    main()
