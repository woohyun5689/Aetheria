using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private readonly Dictionary<string, Sprite> generatedSpriteCache = new Dictionary<string, Sprite>();

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
        }
        else
        {
            sprite = Resources.Load<Sprite>(resourcePath);
        }

        generatedSpriteCache[cacheKey] = sprite;
        return sprite;
    }

    private string GeneratedBackdropPath()
    {
        switch (currentScreen)
        {
            case AetheriaScreen.MainMenu:
            case AetheriaScreen.ClassSelect:
                return "UI/Generated/menu_background";
            case AetheriaScreen.Town:
            case AetheriaScreen.Inventory:
            case AetheriaScreen.Enhancement:
            case AetheriaScreen.SkillTraining:
            case AetheriaScreen.Crafting:
                return "UI/Generated/town_background";
            case AetheriaScreen.Combat:
            case AetheriaScreen.Victory:
            case AetheriaScreen.Defeat:
            case AetheriaScreen.GameClear:
                return "UI/Generated/combat_background";
            default:
                return null;
        }
    }

    private void AddGeneratedScreenBackdrop()
    {
        var resourcePath = GeneratedBackdropPath();
        if (root == null || string.IsNullOrEmpty(resourcePath))
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

        var veilAlpha = currentScreen == AetheriaScreen.MainMenu ? 0.26f : 0.42f;
        var veil = AddFlatPanel("Generated Screen Veil", root, new Color(0.01f, 0.015f, 0.025f, veilAlpha));
        Stretch(veil, 0, 0, 0, 0);
        veil.SetSiblingIndex(1);
        veil.GetComponent<Image>().raycastTarget = false;
    }

    private bool IsGeneratedScreenPage(string name, Transform parent)
    {
        return root != null
            && parent == root
            && name != "Root"
            && name != "Generated Screen Background"
            && name != "Generated Screen Veil";
    }

    private bool ApplyGeneratedPanelSkin(string name, Image image, Color sourceColor)
    {
        if (image == null || sourceColor.a <= 0.05f || string.IsNullOrEmpty(name))
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

        var sprite = LoadGeneratedSprite("UI/Generated/panel_frame", new Vector4(84, 64, 84, 64));
        if (sprite == null)
        {
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, Mathf.Clamp(sourceColor.a, 0.78f, 0.98f));
        return true;
    }

    private bool ApplyGeneratedButtonSkin(Image image, Color accent)
    {
        var sprite = LoadGeneratedSprite("UI/Generated/button_frame", new Vector4(96, 42, 96, 42));
        if (image == null || sprite == null)
        {
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.Lerp(Color.white, accent, 0.16f);
        return true;
    }

    private Sprite LoadCharacterStateSprite(string portraitName, string state)
    {
        var sprite = LoadGeneratedSprite("Characters/" + portraitName + "/" + state, Vector4.zero);
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
        var image = holder.GetComponent<Image>();
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
