# Combat FX V2 generated sources

These assets were generated with the built-in image-generation tool as three
original chroma-key atlases. Each class folder preserves the exact prompt,
the untouched magenta-key output, and the locally keyed RGBA atlas.

The transparent atlases were produced with the installed `imagegen` skill
helper using border auto-detection, soft matte, thresholds 12/220, and despill.
The mage atlas also uses a one-pixel edge contraction, and the build script
shifts any remaining opaque magenta spill into its intended black-violet
conduit color. Fireball derivations additionally warm the image-generated
purple smoke and inner energy into orange-red flame while retaining the
outer/far-tail violet conduit accent.
Lightning derivations tune the generated violet body toward electric blue
while preserving the white core and adding restrained gold conduit accents.
Run `build_assets.py` from any working directory to rebuild the phase PNGs.

Output phase dimensions:

- `fx_cast.png`: 1254 x 1254
- `fx_action.png`: 1536 x 1024
- `fx_impact.png`: 1254 x 1254
- `fx_support.png`: 1024 x 1536
