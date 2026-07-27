using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private const int RuntimeSkillIconSize = 384;

    private static readonly Dictionary<string, string> SkillIconResourceByName = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "화염구", "mage_fireball" },
        { "냉기 장벽", "mage_frost_barrier" },
        { "번개 폭풍", "mage_lightning_storm" },
        { "그림자 습격", "rogue_shadow_strike" },
        { "독 묻은 단검", "rogue_poison_dagger" },
        { "황혼 난무", "rogue_twilight_flurry" },
        { "찬란한 징벌", "priest_radiant_judgment" },
        { "성역", "priest_sanctuary" },
        { "구원의 광선", "priest_salvation_ray" },
        { "폭염 불꽃", "bomber_blazing_fire" },
        { "파쇄 폭탄", "bomber_shatter_bomb" },
        { "연쇄 기폭", "bomber_chain_detonation" },
        { "화염 정령", "spirit_fire_spirit" },
        { "물결 정령", "spirit_water_spirit" },
        { "질풍 정령", "spirit_wind_spirit" },
        { "대지 정령", "spirit_earth_spirit" },
        { "정조준 사격", "archer_aimed_shot" },
        { "속박 화살", "archer_binding_arrow" },
        { "관통 연사", "archer_piercing_volley" },
        { "바람걸음 사격", "archer_windstep_shot" },
        { "철권 연타", "monk_iron_fist_combo" },
        { "급소 봉쇄", "monk_pressure_lock" },
        { "내공 폭발", "monk_inner_burst" },
        { "방패 후려치기", "knight_shield_bash" },
        { "철벽 방어", "knight_ironwall_guard" },
        { "성광 분쇄", "knight_holy_crush" }
    };

    private readonly Dictionary<string, Texture2D> runtimeSkillIconCache = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
    private readonly Dictionary<string, Texture2D> runtimeObjectTextureCache = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
    private readonly Dictionary<string, Sprite> runtimeObjectSpriteCache = new Dictionary<string, Sprite>(StringComparer.Ordinal);

    private RectTransform AddSkillIconVisual(Transform parent, string skillName, float width, float height, float opacity = 1f)
    {
        if (!SkillIconResourceByName.ContainsKey(skillName))
        {
            return null;
        }

        var go = new GameObject("Skill Icon " + skillName, typeof(RectTransform), typeof(RawImage), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        AddLayoutSize(rect, width, height);

        var image = go.GetComponent<RawImage>();
        image.raycastTarget = false;
        image.texture = RuntimeSkillIcon(skillName);
        image.color = image.texture != null ? new Color(1f, 1f, 1f, Mathf.Clamp01(opacity)) : Color.clear;

        if (image.texture == null)
        {
            var fallback = AddFlatPanel("Missing Skill Icon", rect, new Color(0.24f, 0.36f, 0.48f, 0.30f));
            Stretch(fallback, 0, 0, 0, 0);
            fallback.GetComponent<Image>().raycastTarget = false;
            fallback.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var fallbackText = AddText(fallback, "SKILL", 17, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, height);
            Stretch(fallbackText.GetComponent<RectTransform>(), 4, 4, 4, 4);
        }

        return rect;
    }

    private bool AddSkillIconToActionCard(RectTransform card, string skillName, bool enabled)
    {
        if (card == null || !SkillIconResourceByName.ContainsKey(skillName))
        {
            return false;
        }

        var icon = AddSkillIconVisual(
            card,
            skillName,
            CombatActionCardIconSize,
            CombatActionCardIconSize,
            1f);
        if (icon == null)
        {
            return false;
        }

        var layout = icon.GetComponent<LayoutElement>();
        layout.ignoreLayout = true;
        icon.anchorMin = new Vector2(0f, 0.5f);
        icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0f, 0.5f);
        icon.anchoredPosition = new Vector2(16f, -12f);
        icon.sizeDelta = new Vector2(CombatActionCardIconSize, CombatActionCardIconSize);
        icon.SetAsLastSibling();
        return true;
    }

    private Texture2D RuntimeSkillIcon(string skillName)
    {
        if (!SkillIconResourceByName.TryGetValue(skillName, out var resourceName))
        {
            return null;
        }

        if (runtimeSkillIconCache.TryGetValue(resourceName, out var cached) && cached != null)
        {
            return cached;
        }

        var source = Resources.Load<Texture2D>("UI/SkillIcons/" + resourceName);
        if (source == null)
        {
            return null;
        }

        // The current SkillIcons set is already imported with real alpha. Using
        // the source texture avoids a synchronous GPU ReadPixels copy the first
        // time every skill appears.
        runtimeSkillIconCache[resourceName] = source;
        return source;
    }

    private void ReleaseRuntimeSkillIconResources()
    {
        // Skill icon entries are Resources assets, not runtime-owned copies.
        runtimeSkillIconCache.Clear();

        foreach (var entry in runtimeObjectSpriteCache)
        {
            if (entry.Value != null)
            {
                Destroy(entry.Value);
            }
        }
        runtimeObjectSpriteCache.Clear();

        foreach (var entry in runtimeObjectTextureCache)
        {
            if (entry.Value != null)
            {
                Destroy(entry.Value);
            }
        }
        runtimeObjectTextureCache.Clear();
    }

    private Texture2D CreateRuntimeMaskedSkillIcon(Texture2D source, bool greenMask)
    {
        if (source == null)
        {
            return null;
        }

        RenderTexture temporary = null;
        var previous = RenderTexture.active;
        try
        {
            temporary = RenderTexture.GetTemporary(
                RuntimeSkillIconSize,
                RuntimeSkillIconSize,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            temporary.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;

            var readable = new Texture2D(RuntimeSkillIconSize, RuntimeSkillIconSize, TextureFormat.RGBA32, false);
            readable.name = source.name + " Runtime Mask";
            readable.hideFlags = HideFlags.DontSave;
            readable.wrapMode = TextureWrapMode.Clamp;
            readable.filterMode = FilterMode.Bilinear;
            readable.ReadPixels(new Rect(0f, 0f, RuntimeSkillIconSize, RuntimeSkillIconSize), 0, 0, false);
            readable.Apply(false, false);

            var pixels = readable.GetPixels32();
            // Newly generated icons are imported with a real alpha matte. Preserve that
            // matte instead of running the legacy checker/green-screen detector over
            // saturated cyan, green, or gold artwork.
            if (HasMeaningfulTransparency(pixels))
            {
                readable.Apply(false, true);
                return readable;
            }

            var greenKey = greenMask ? EstimateGreenScreenKey(pixels, RuntimeSkillIconSize, RuntimeSkillIconSize) : Color.black;
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                var mask = greenMask ? GreenScreenMask(pixel, greenKey) : CheckerboardMask(pixel);
                if (mask <= 0f)
                {
                    continue;
                }

                if (mask >= 0.82f)
                {
                    mask = 1f;
                }

                if (greenMask)
                {
                    var spillTarget = Mathf.Max(pixel.r, pixel.b);
                    pixel.g = (byte)Mathf.RoundToInt(Mathf.Lerp(pixel.g, spillTarget, Mathf.Clamp01(mask * 0.92f)));
                }
                pixel.a = (byte)Mathf.RoundToInt(pixel.a * (1f - Mathf.Clamp01(mask)));
                pixels[i] = pixel;
            }

            readable.SetPixels32(pixels);
            readable.Apply(false, true);
            return readable;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Aetheria] 스킬 아이콘 런타임 마스크 실패: " + source.name + " / " + exception.Message);
            return null;
        }
        finally
        {
            RenderTexture.active = previous;
            if (temporary != null)
            {
                RenderTexture.ReleaseTemporary(temporary);
            }
        }
    }

    private static bool HasMeaningfulTransparency(Color32[] pixels)
    {
        if (pixels == null || pixels.Length == 0)
        {
            return false;
        }

        var transparentSamples = 0;
        var step = Mathf.Max(1, pixels.Length / 2048);
        for (var i = 0; i < pixels.Length; i += step)
        {
            if (pixels[i].a <= 16)
            {
                transparentSamples++;
                if (transparentSamples >= 8)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static float CheckerboardMask(Color32 pixel)
    {
        var r = pixel.r / 255f;
        var g = pixel.g / 255f;
        var b = pixel.b / 255f;
        var maximum = Mathf.Max(r, Mathf.Max(g, b));
        var minimum = Mathf.Min(r, Mathf.Min(g, b));
        var saturation = maximum - minimum;
        var brightness = (r + g + b) / 3f;
        var neutral = 1f - Mathf.SmoothStep(0.025f, 0.15f, saturation);
        var bright = Mathf.SmoothStep(0.70f, 0.93f, brightness);
        return Mathf.Clamp01(neutral * bright);
    }

    private static float GreenScreenMask(Color32 pixel, Color key)
    {
        var r = pixel.r / 255f;
        var g = pixel.g / 255f;
        var b = pixel.b / 255f;
        var distance = Mathf.Sqrt(
            (r - key.r) * (r - key.r)
            + (g - key.g) * (g - key.g)
            + (b - key.b) * (b - key.b));
        var keySimilarity = 1f - Mathf.SmoothStep(0.07f, 0.34f, distance);
        var greenDominance = g - Mathf.Max(r, b);
        var chroma = Mathf.SmoothStep(0.055f, 0.28f, greenDominance) * Mathf.SmoothStep(0.28f, 0.72f, g);
        var keyProximity = 1f - Mathf.SmoothStep(0.18f, 0.50f, distance);
        return Mathf.Clamp01(Mathf.Max(keySimilarity, chroma * keyProximity * 0.92f));
    }

    private static Color EstimateGreenScreenKey(Color32[] pixels, int width, int height)
    {
        if (pixels == null || pixels.Length == 0 || width <= 0 || height <= 0)
        {
            return new Color(0f, 1f, 0f, 1f);
        }

        var sum = Vector3.zero;
        var count = 0;
        var step = Mathf.Max(1, Mathf.Min(width, height) / 24);
        for (var x = 0; x < width; x += step)
        {
            AccumulateGreenKeyPixel(pixels[x], ref sum, ref count);
            AccumulateGreenKeyPixel(pixels[(height - 1) * width + x], ref sum, ref count);
        }
        for (var y = 0; y < height; y += step)
        {
            AccumulateGreenKeyPixel(pixels[y * width], ref sum, ref count);
            AccumulateGreenKeyPixel(pixels[y * width + width - 1], ref sum, ref count);
        }

        if (count <= 0)
        {
            return new Color(0f, 1f, 0f, 1f);
        }
        var average = sum / count;
        return new Color(average.x, average.y, average.z, 1f);
    }

    private static void AccumulateGreenKeyPixel(Color32 pixel, ref Vector3 sum, ref int count)
    {
        var r = pixel.r / 255f;
        var g = pixel.g / 255f;
        var b = pixel.b / 255f;
        if (g <= Mathf.Max(r, b) + 0.04f)
        {
            return;
        }

        sum += new Vector3(r, g, b);
        count++;
    }

    private static bool SkillIconUsesGreenMask(string resourceName)
    {
        return resourceName.StartsWith("archer_", StringComparison.Ordinal)
            || resourceName.StartsWith("monk_", StringComparison.Ordinal)
            || resourceName.StartsWith("knight_", StringComparison.Ordinal);
    }

    private Sprite LoadRuntimeObjectSprite(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        if (runtimeObjectSpriteCache.TryGetValue(resourcePath, out var cached) && cached != null)
        {
            return cached;
        }

        var source = Resources.Load<Texture2D>(resourcePath);
        if (source == null)
        {
            return null;
        }

        var texture = CreateRuntimeMaskedObjectTexture(source);
        if (texture == null)
        {
            return null;
        }

        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        sprite.name = source.name + " Runtime Object Sprite";
        sprite.hideFlags = HideFlags.DontSave;
        runtimeObjectTextureCache[resourcePath] = texture;
        runtimeObjectSpriteCache[resourcePath] = sprite;
        return sprite;
    }

    private Texture2D CreateRuntimeMaskedObjectTexture(Texture2D source)
    {
        RenderTexture temporary = null;
        var previous = RenderTexture.active;
        try
        {
            temporary = RenderTexture.GetTemporary(
                RuntimeSkillIconSize,
                RuntimeSkillIconSize,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            temporary.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;

            var readable = new Texture2D(RuntimeSkillIconSize, RuntimeSkillIconSize, TextureFormat.RGBA32, false);
            readable.name = source.name + " Runtime Object Mask";
            readable.hideFlags = HideFlags.DontSave;
            readable.wrapMode = TextureWrapMode.Clamp;
            readable.filterMode = FilterMode.Bilinear;
            readable.ReadPixels(new Rect(0f, 0f, RuntimeSkillIconSize, RuntimeSkillIconSize), 0, 0, false);
            readable.Apply(false, false);

            var pixels = readable.GetPixels32();
            if (HasMeaningfulTransparency(pixels))
            {
                readable.Apply(false, true);
                return readable;
            }

            var key = EstimateMagentaScreenKey(pixels, RuntimeSkillIconSize, RuntimeSkillIconSize);
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                var mask = MagentaScreenMask(pixel, key);
                if (mask <= 0f)
                {
                    continue;
                }

                if (mask >= 0.84f)
                {
                    mask = 1f;
                }

                var neutralEdge = Mathf.Max(pixel.g, Mathf.Min(pixel.r, pixel.b));
                pixel.r = (byte)Mathf.RoundToInt(Mathf.Lerp(pixel.r, neutralEdge, Mathf.Clamp01(mask * 0.92f)));
                pixel.b = (byte)Mathf.RoundToInt(Mathf.Lerp(pixel.b, neutralEdge, Mathf.Clamp01(mask * 0.92f)));
                pixel.a = (byte)Mathf.RoundToInt(pixel.a * (1f - Mathf.Clamp01(mask)));
                pixels[i] = pixel;
            }

            readable.SetPixels32(pixels);
            readable.Apply(false, true);
            return readable;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Aetheria] 오브젝트 버튼 런타임 마스크 실패: " + source.name + " / " + exception.Message);
            return null;
        }
        finally
        {
            RenderTexture.active = previous;
            if (temporary != null)
            {
                RenderTexture.ReleaseTemporary(temporary);
            }
        }
    }

    private static float MagentaScreenMask(Color32 pixel, Color key)
    {
        var r = pixel.r / 255f;
        var g = pixel.g / 255f;
        var b = pixel.b / 255f;
        var distance = Mathf.Sqrt(
            (r - key.r) * (r - key.r)
            + (g - key.g) * (g - key.g)
            + (b - key.b) * (b - key.b));
        var keySimilarity = 1f - Mathf.SmoothStep(0.055f, 0.30f, distance);
        var magentaDominance = Mathf.Min(r, b) - g;
        var chroma = Mathf.SmoothStep(0.18f, 0.52f, magentaDominance)
            * Mathf.SmoothStep(0.55f, 0.88f, Mathf.Min(r, b));
        var keyProximity = 1f - Mathf.SmoothStep(0.14f, 0.38f, distance);
        return Mathf.Clamp01(Mathf.Max(keySimilarity, chroma * keyProximity * 0.95f));
    }

    private static Color EstimateMagentaScreenKey(Color32[] pixels, int width, int height)
    {
        if (pixels == null || pixels.Length == 0 || width <= 0 || height <= 0)
        {
            return new Color(1f, 0f, 1f, 1f);
        }

        var sum = Vector3.zero;
        var count = 0;
        var step = Mathf.Max(1, Mathf.Min(width, height) / 24);
        for (var x = 0; x < width; x += step)
        {
            AccumulateMagentaKeyPixel(pixels[x], ref sum, ref count);
            AccumulateMagentaKeyPixel(pixels[(height - 1) * width + x], ref sum, ref count);
        }
        for (var y = 0; y < height; y += step)
        {
            AccumulateMagentaKeyPixel(pixels[y * width], ref sum, ref count);
            AccumulateMagentaKeyPixel(pixels[y * width + width - 1], ref sum, ref count);
        }

        if (count <= 0)
        {
            return new Color(1f, 0f, 1f, 1f);
        }

        var average = sum / count;
        return new Color(average.x, average.y, average.z, 1f);
    }

    private static void AccumulateMagentaKeyPixel(Color32 pixel, ref Vector3 sum, ref int count)
    {
        var r = pixel.r / 255f;
        var g = pixel.g / 255f;
        var b = pixel.b / 255f;
        if (Mathf.Min(r, b) <= g + 0.18f)
        {
            return;
        }

        sum += new Vector3(r, g, b);
        count++;
    }
}
