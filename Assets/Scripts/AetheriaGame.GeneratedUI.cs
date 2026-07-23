using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private readonly Dictionary<string, Sprite> generatedSpriteCache = new Dictionary<string, Sprite>();
    private readonly HashSet<Sprite> ownedGeneratedSprites = new HashSet<Sprite>();

    private Sprite LoadGeneratedSprite(string resourcePath, Vector4 border)
    {
        var cacheKey = resourcePath + "|" + border;
        if (generatedSpriteCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var texture = Resources.Load<Texture2D>(resourcePath);
        Sprite sprite = null;
        if (texture != null)
        {
            var safeBorder = new Vector4(
                Mathf.Min(border.x, texture.width * 0.32f),
                Mathf.Min(border.y, texture.height * 0.32f),
                Mathf.Min(border.z, texture.width * 0.32f),
                Mathf.Min(border.w, texture.height * 0.32f));
            sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                safeBorder);
            sprite.name = "Generated " + resourcePath;
            sprite.hideFlags = HideFlags.DontSave;
            ownedGeneratedSprites.Add(sprite);
        }
        else
        {
            sprite = Resources.Load<Sprite>(resourcePath);
        }

        if (sprite != null)
        {
            generatedSpriteCache[cacheKey] = sprite;
        }
        return sprite;
    }

    private void ReleaseGeneratedSprites()
    {
        foreach (var sprite in ownedGeneratedSprites)
        {
            if (sprite != null)
            {
                Destroy(sprite);
            }
        }
        ownedGeneratedSprites.Clear();
        generatedSpriteCache.Clear();
    }

    private string GeneratedBackdropPath()
    {
        var candidates = GeneratedBackdropCandidates();
        for (var i = 0; i < candidates.Length; i++)
        {
            if (Resources.Load<Texture2D>(candidates[i]) != null || Resources.Load<Sprite>(candidates[i]) != null)
            {
                return candidates[i];
            }
        }

        return null;
    }

    private string[] GeneratedBackdropCandidates()
    {
        switch (currentScreen)
        {
            case AetheriaScreen.MainMenu:
            case AetheriaScreen.ClassSelect:
            case AetheriaScreen.Guide:
                return new[]
                {
                    "UI/FacilityBackdrops/menu_hall",
                    "UI/VisualRefresh/Backgrounds/menu",
                    "UI/Generated/menu_background_v2",
                    "UI/Generated/menu_background_bright"
                };
            case AetheriaScreen.Town:
                return new[]
                {
                    "UI/FacilityBackdrops/town_hub",
                    "UI/VisualRefresh/Backgrounds/town",
                    "UI/Generated/town_background_v2",
                    "UI/Generated/town_background_bright"
                };
            case AetheriaScreen.Inventory:
                return new[]
                {
                    "UI/FacilityBackdrops/inventory_armory",
                    "UI/VisualRefresh/Backgrounds/inventory",
                    "UI/Generated/inventory_background_bright",
                    "UI/Generated/town_background_v2"
                };
            case AetheriaScreen.Enhancement:
                return new[]
                {
                    "UI/FacilityBackdrops/forge",
                    "UI/VisualRefresh/Backgrounds/forge",
                    "UI/Generated/forge_background_bright",
                    "UI/Generated/town_background_v2"
                };
            case AetheriaScreen.SkillTraining:
                return new[]
                {
                    "UI/FacilityBackdrops/skill_shrine",
                    "UI/VisualRefresh/Backgrounds/skill_shrine",
                    "UI/Generated/skill_background_bright",
                    "UI/Generated/town_background_v2"
                };
            case AetheriaScreen.Crafting:
                return new[]
                {
                    "UI/FacilityBackdrops/crafting_workshop",
                    "UI/VisualRefresh/Backgrounds/crafting",
                    "UI/Generated/crafting_background_bright",
                    "UI/Generated/town_background_v2"
                };
            case AetheriaScreen.DungeonSelect:
                return new[]
                {
                    "UI/VisualRefresh/Backgrounds/world_map",
                    "UI/Generated/menu_background_v2",
                    "UI/Generated/menu_background_bright"
                };
            case AetheriaScreen.Combat:
            case AetheriaScreen.Victory:
            case AetheriaScreen.Defeat:
            case AetheriaScreen.GameClear:
                var regionKey = CurrentVisualCombatRegionKey();
                return new[]
                {
                    CurrentVisualCombatBackdropPath(regionKey),
                    "UI/VisualRefresh/Backgrounds/Regions/" + regionKey,
                    "UI/VisualRefresh/Backgrounds/combat_" + regionKey,
                    "UI/Generated/combat_" + regionKey + "_bright",
                    "UI/Generated/combat_background_v2",
                    "UI/Generated/combat_background_bright"
                };
            default:
                return new string[0];
        }
    }

    private string CurrentVisualCombatRegionKey()
    {
        if (currentDungeon != null)
        {
            return DungeonRegionKey(currentDungeon);
        }

        return string.IsNullOrEmpty(selectedDungeonRegionKey) ? "green" : selectedDungeonRegionKey;
    }

    private static string CurrentVisualCombatBackdropPath(string regionKey)
    {
        switch (regionKey)
        {
            case "frost": return "UI/SceneBackdrops/region_frostlands";
            case "green": return "UI/SceneBackdrops/region_greenwood";
            case "lava": return "UI/SceneBackdrops/region_volcanic";
            case "elesia": return "UI/SceneBackdrops/region_elysia";
            case "arcadia": return "UI/SceneBackdrops/region_arcadia";
            case "desert": return "UI/SceneBackdrops/region_golden_desert";
            case "shadow": return "UI/SceneBackdrops/region_shadowlands";
            case "isles": return "UI/SceneBackdrops/region_blue_archipelago";
            case "wind": return "UI/SceneBackdrops/region_windlands";
            default: return "UI/SceneBackdrops/region_greenwood";
        }
    }

    private void AddGeneratedScreenBackdrop()
    {
        if (root == null)
        {
            return;
        }

        EnsureGrowthResultVisualPresenter();

        var resourcePath = GeneratedBackdropPath();
        if (string.IsNullOrEmpty(resourcePath))
        {
            return;
        }

        var sprite = LoadGeneratedSprite(resourcePath, Vector4.zero);
        if (sprite == null)
        {
            return;
        }

        var background = AddFlatPanel("Generated Screen Background", root, Color.white);
        Stretch(background, 0, 0, 0, 0);
        background.SetAsFirstSibling();
        var backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = sprite;
        backgroundImage.preserveAspect = false;
        backgroundImage.raycastTarget = false;
        var backgroundFitter = background.gameObject.AddComponent<AspectRatioFitter>();
        backgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        backgroundFitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        AttachScreenParallax(background, 6f, -1f);

        var characterPath = CharacterBackdropPath();
        var characterSprite = string.IsNullOrEmpty(characterPath)
            ? null
            : LoadGeneratedSprite(characterPath, Vector4.zero);
        if (characterSprite != null)
        {
            var characterLayer = AddFlatPanel("Character Theme Backdrop Layer", root, new Color(1f, 1f, 1f, CharacterBackdropLayerAlpha()));
            Stretch(characterLayer, 0, 0, 0, 0);
            characterLayer.SetSiblingIndex(1);
            characterLayer.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var characterImage = characterLayer.GetComponent<Image>();
            characterImage.sprite = characterSprite;
            characterImage.preserveAspect = false;
            characterImage.raycastTarget = false;
            var characterFitter = characterLayer.gameObject.AddComponent<AspectRatioFitter>();
            characterFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            characterFitter.aspectRatio = characterSprite.rect.width / Mathf.Max(1f, characterSprite.rect.height);
            AttachScreenParallax(characterLayer, 10f, -1f);
        }

        var veilAlpha = currentScreen == AetheriaScreen.MainMenu ? 0.025f : 0.055f;
        var veil = AddFlatPanel("Generated Screen Veil", root, new Color(0.92f, 0.97f, 1f, veilAlpha));
        Stretch(veil, 0, 0, 0, 0);
        veil.SetSiblingIndex(characterSprite == null ? 1 : 2);
        veil.GetComponent<Image>().raycastTarget = false;

        // This ornament is deliberately added while only background layers exist.
        // Every screen page is created afterwards, so the class motif remains
        // visible without sitting over text or intercepting input.
        AddCharacterThemeScreenOverlay();
    }

    private bool IsGeneratedScreenPage(string name, Transform parent)
    {
        return root != null
            && parent == root
            && name != "Root"
            && name != "Generated Screen Background"
            && name != "Generated Screen Veil"
            && name != "Character Theme Backdrop Layer"
            && name != "Character Theme Screen Overlay";
    }

    private bool ApplyGeneratedPanelSkin(string name, Image image, Color sourceColor)
    {
        if (image == null || sourceColor.a <= 0.05f || string.IsNullOrEmpty(name))
        {
            return false;
        }

        // Content panels prioritize uninterrupted text space. The illustrated frame
        // is reserved for the full-screen border created with the backdrop above.
        if (!name.Contains("Artwork Panel"))
        {
            return false;
        }

        if (name == "Root"
            || name == "Fill"
            || name == "Divider"
            || name.Contains(" Bar")
            || name.Contains("Portrait")
            || name.Contains("Artwork")
            || name.Contains("World Map")
            || name.Contains("Dungeon Map")
            || name.Contains("Opening")
            || name.Contains("Closed Scroll")
            || name.Contains("Scroll Leaf")
            || name.Contains("Pin ")
            || name.Contains("Map Shadow")
            || name.Contains("Map Gold")
            || name.Contains("Overlay")
            || name.Contains("Depth")
            || name.Contains("Shine")
            || name.Contains("Shade")
            || name.Contains("Accent")
            || name.Contains("Viewport"))
        {
            return false;
        }

        var sprite = LoadGeneratedSprite("UI/Generated/panel_frame_bright", new Vector4(96, 76, 96, 76));
        if (sprite == null)
        {
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        var tint = Color.Lerp(Color.white, new Color(sourceColor.r, sourceColor.g, sourceColor.b, 1f), 0.08f);
        image.color = new Color(tint.r, tint.g, tint.b, Mathf.Clamp(sourceColor.a, 0.84f, 0.98f));
        return true;
    }

    private bool ApplyGeneratedButtonSkin(Image image, Color accent)
    {
        var sprite = LoadGeneratedSprite("UI/Generated/button_frame_bright", new Vector4(272f, 136f, 272f, 136f));
        if (image == null || sprite == null)
        {
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 8f;
        image.color = Color.Lerp(Color.white, accent, 0.18f);
        return true;
    }

    private Sprite LoadCharacterStateSprite(string portraitName, string state)
    {
        state = string.IsNullOrEmpty(state) ? "idle" : state;

        // The v2 set uses one consistent transparent, portrait-format canvas for
        // every state so the character no longer changes scale or gains a square
        // concept-art background when the UI switches pose.
        var sprite = LoadGeneratedSprite("CharactersV2/" + portraitName + "/" + state, Vector4.zero);
        if (sprite == null && state != "idle")
        {
            sprite = LoadGeneratedSprite("CharactersV2/" + portraitName + "/idle", Vector4.zero);
        }

        if (sprite != null)
        {
            return sprite;
        }

        sprite = LoadGeneratedSprite("Characters/" + portraitName + "/" + state, Vector4.zero);
        if (sprite == null && state != "idle")
        {
            sprite = LoadGeneratedSprite("Characters/" + portraitName + "/idle", Vector4.zero);
        }

        if (sprite != null)
        {
            return sprite;
        }

        var resourcePath = "Portraits/" + portraitName;
        sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        var texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        sprite.name = "Portrait " + portraitName;
        return sprite;
    }

    private RectTransform AddCharacterArt(Transform parent, string portraitName, string state, float width, float height)
    {
        var holder = AddFlatPanel("Character Portrait " + state, parent, new Color(0, 0, 0, 0));
        AddLayoutSize(holder, width, height);
        holder.GetComponent<Image>().raycastTarget = false;
        AddCharacterThemeHalo(holder, portraitName, 0.18f);

        var visual = AddFlatPanel("Character Portrait Visual", holder, Color.clear);
        Stretch(visual, 0, 0, 0, 0);
        visual.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var image = visual.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = LoadCharacterStateSprite(portraitName, state);
        image.preserveAspect = true;
        image.color = image.sprite == null ? new Color(0, 0, 0, 0) : Color.white;

        if (image.sprite == null)
        {
            var fallback = AddText(holder, "이미지 없음", 18, FontStyle.Normal, mutedColor, TextAnchor.MiddleCenter, height);
            Stretch(fallback.GetComponent<RectTransform>(), 0, 0, 0, 0);
        }

        return holder;
    }
}
