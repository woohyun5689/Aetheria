using UnityEngine;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private sealed class CharacterUiTheme
    {
        public readonly string key;
        public readonly Color accent;
        public readonly Color heading;
        public readonly Color muted;

        public CharacterUiTheme(string key, Color accent, Color heading, Color muted)
        {
            this.key = key;
            this.accent = accent;
            this.heading = heading;
            this.muted = muted;
        }

        public string ResourcePath(string assetName)
        {
            return "UI/CharacterThemes/" + key + "/" + assetName;
        }
    }

    private static readonly CharacterUiTheme KnightUiTheme = new CharacterUiTheme(
        "knight", new Color32(214, 164, 71, 255), new Color32(255, 225, 166, 255), new Color32(229, 235, 243, 255));
    private static readonly CharacterUiTheme MageUiTheme = new CharacterUiTheme(
        "mage", new Color32(39, 184, 212, 255), new Color32(157, 224, 255, 255), new Color32(218, 229, 250, 255));
    private static readonly CharacterUiTheme RogueUiTheme = new CharacterUiTheme(
        "rogue", new Color32(148, 200, 61, 255), new Color32(229, 176, 255, 255), new Color32(231, 219, 239, 255));
    private static readonly CharacterUiTheme PriestUiTheme = new CharacterUiTheme(
        "priest", new Color32(217, 169, 40, 255), new Color32(224, 196, 255, 255), new Color32(235, 226, 245, 255));
    private static readonly CharacterUiTheme BomberUiTheme = new CharacterUiTheme(
        "bomber", new Color32(239, 106, 46, 255), new Color32(255, 169, 154, 255), new Color32(244, 222, 216, 255));
    private static readonly CharacterUiTheme SpiritUiTheme = new CharacterUiTheme(
        "spirit", new Color32(226, 165, 59, 255), new Color32(130, 235, 220, 255), new Color32(218, 239, 234, 255));
    private static readonly CharacterUiTheme ArcherUiTheme = new CharacterUiTheme(
        "archer", new Color32(75, 191, 159, 255), new Color32(178, 237, 173, 255), new Color32(222, 240, 222, 255));
    private static readonly CharacterUiTheme MonkUiTheme = new CharacterUiTheme(
        "monk", new Color32(222, 166, 58, 255), new Color32(255, 197, 153, 255), new Color32(241, 226, 213, 255));

    private CharacterUiTheme CharacterThemeForKey(string keyOrClass)
    {
        var key = NormalizeCharacterThemeKey(keyOrClass);
        switch (key)
        {
            case "knight": return KnightUiTheme;
            case "mage": return MageUiTheme;
            case "rogue": return RogueUiTheme;
            case "priest": return PriestUiTheme;
            case "bomber": return BomberUiTheme;
            case "spirit": return SpiritUiTheme;
            case "archer": return ArcherUiTheme;
            case "monk": return MonkUiTheme;
            default: return null;
        }
    }

    private string NormalizeCharacterThemeKey(string keyOrClass)
    {
        if (string.IsNullOrEmpty(keyOrClass))
        {
            return null;
        }

        var key = keyOrClass.Trim().ToLowerInvariant();
        switch (key)
        {
            case "knight":
            case "mage":
            case "rogue":
            case "priest":
            case "bomber":
            case "spirit":
            case "archer":
            case "monk":
                return key;
            default:
                return GearClassId(keyOrClass);
        }
    }

    private string ActiveCharacterThemeKey()
    {
        if (currentScreen == AetheriaScreen.MainMenu
            || currentScreen == AetheriaScreen.ClassSelect
            || currentScreen == AetheriaScreen.Guide
            || player == null)
        {
            return null;
        }

        return NormalizeCharacterThemeKey(
            string.IsNullOrEmpty(player.portraitName) ? player.heroClass : player.portraitName);
    }

    private string ScreenCharacterThemeKey()
    {
        if (currentScreen == AetheriaScreen.ClassSelect)
        {
            return string.IsNullOrEmpty(selectedHeroClassName)
                ? null
                : NormalizeCharacterThemeKey(selectedHeroClassName);
        }

        return ActiveCharacterThemeKey();
    }

    private string CharacterBackdropPath()
    {
        string themeKey;
        switch (currentScreen)
        {
            case AetheriaScreen.ClassSelect:
                themeKey = ScreenCharacterThemeKey();
                break;
            case AetheriaScreen.Town:
            case AetheriaScreen.Inventory:
            case AetheriaScreen.Enhancement:
            case AetheriaScreen.SkillTraining:
            case AetheriaScreen.Crafting:
            case AetheriaScreen.Combat:
            case AetheriaScreen.Victory:
            case AetheriaScreen.Defeat:
            case AetheriaScreen.GameClear:
                themeKey = ActiveCharacterThemeKey();
                break;
            default:
                return null;
        }

        var theme = CharacterThemeForKey(themeKey);
        return theme == null ? null : theme.ResourcePath("background");
    }

    private Color ActiveCharacterAccent(Color fallback)
    {
        var theme = CharacterThemeForKey(ActiveCharacterThemeKey());
        return theme == null ? fallback : theme.accent;
    }

    private Color CharacterThemeHeading(string keyOrClass, Color fallback)
    {
        var theme = CharacterThemeForKey(keyOrClass);
        return theme == null ? fallback : theme.heading;
    }

    private Color CharacterThemeMuted(string keyOrClass, Color fallback)
    {
        var theme = CharacterThemeForKey(keyOrClass);
        return theme == null ? fallback : theme.muted;
    }

    private Color CharacterThemeButtonText(string keyOrClass, Color fallback)
    {
        var theme = CharacterThemeForKey(keyOrClass);
        if (theme == null)
        {
            return fallback;
        }

        var darkInk = Rgb(17, 24, 39);
        var opaqueAccent = new Color(theme.accent.r, theme.accent.g, theme.accent.b, 1f);
        return Color.Lerp(darkInk, opaqueAccent, 0.22f);
    }

    private void ApplyCharacterThemeButtonText(Button button, string keyOrClass)
    {
        var theme = CharacterThemeForKey(keyOrClass);
        var label = button == null ? null : button.GetComponentInChildren<Text>();
        if (theme == null || label == null)
        {
            return;
        }

        var readableButtonColor = CharacterThemeButtonText(keyOrClass, buttonTextColor);
        label.color = readableButtonColor;
        var presenter = button.GetComponent<UiButtonTextPresenter>();
        if (presenter != null)
        {
            presenter.Configure(button, label, readableButtonColor, Rgb(196, 205, 216));
        }
    }

    private Color ResolveCharacterThemeTextColor(Color source, int size, FontStyle style)
    {
        var theme = CharacterThemeForKey(ActiveCharacterThemeKey());
        if (theme == null)
        {
            return source;
        }

        if (SameUiColor(source, manaColor))
        {
            return theme.heading;
        }

        if (SameUiColor(source, neonPurple))
        {
            return theme.heading;
        }

        var bold = style == FontStyle.Bold || style == FontStyle.BoldAndItalic;
        if (bold && size >= 24 && SameUiColor(source, textColor))
        {
            return theme.heading;
        }

        if (SameUiColor(source, mutedColor))
        {
            return Color.Lerp(source, theme.muted, 0.34f);
        }

        return source;
    }

    private static bool SameUiColor(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.002f
            && Mathf.Abs(a.g - b.g) < 0.002f
            && Mathf.Abs(a.b - b.b) < 0.002f
            && Mathf.Abs(a.a - b.a) < 0.002f;
    }

    private void AddCharacterThemeScreenOverlay()
    {
        var theme = CharacterThemeForKey(ScreenCharacterThemeKey());
        if (root == null || theme == null)
        {
            return;
        }

        var sprite = LoadGeneratedSprite(theme.ResourcePath("screen_overlay"), Vector4.zero);
        if (sprite == null)
        {
            return;
        }

        // The ornament is a background motif, not a foreground frame. Keeping it
        // subtle prevents its top crest and rails from running through screen titles.
        var overlayAlpha = currentScreen == AetheriaScreen.ClassSelect ? 0.14f : 0.18f;
        var overlay = AddFlatPanel("Character Theme Screen Overlay", root, new Color(1f, 1f, 1f, overlayAlpha));
        Stretch(overlay, 0, 0, 0, 0);
        overlay.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var image = overlay.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = false;
        image.raycastTarget = false;
        overlay.SetAsLastSibling();
    }

    private bool ShouldApplyCharacterThemePanel(string name, Color sourceColor)
    {
        if (sourceColor.a <= 0.05f || CharacterThemeForKey(ActiveCharacterThemeKey()) == null)
        {
            return false;
        }

        switch (currentScreen)
        {
            case AetheriaScreen.Town:
                return name == "Town HUD"
                    || name == "Hero Showcase"
                    || name == "Adventure Command Board"
                    || name == "Field Journal"
                    || name == "Town Sidebar"
                    || name == "Town Panel Content"
                    || name == "Hero Stats Card"
                    || name == "Equipment Card"
                    || name == "Skill Section"
                    || name == "Town Event Log";
            case AetheriaScreen.Inventory:
                return name == "Equipped Detail"
                    || name.StartsWith("Item Comparison")
                    || name == "Colored Gear Delta";
            case AetheriaScreen.Enhancement:
                return name == "Enhancement Grid";
            case AetheriaScreen.SkillTraining:
                return name == "Skill Training Top" || name.StartsWith("Skill ");
            case AetheriaScreen.Crafting:
                return name == "Crafting Detail";
            case AetheriaScreen.DungeonSelect:
                return name == "Dungeon Map Status Overlay"
                    || name == "Dungeon Quick Start"
                    || name == "Dungeon Info Popup";
            case AetheriaScreen.Combat:
                return name == "Combat Header"
                    || name == "Player Combatant"
                    || name == "Action Deck"
                    || name == "Combat Log";
            case AetheriaScreen.Victory:
                return name == "Victory Panel";
            case AetheriaScreen.Defeat:
                return name == "Defeat Panel";
            case AetheriaScreen.GameClear:
                return name == "Game Clear Panel";
            default:
                return false;
        }
    }

    private bool ApplyCharacterThemePanelFrame(RectTransform target, string keyOverride = null, bool force = false)
    {
        if (target == null || target.Find("Character Theme Panel Frame") != null)
        {
            return false;
        }

        var key = string.IsNullOrEmpty(keyOverride) ? ActiveCharacterThemeKey() : keyOverride;
        var theme = CharacterThemeForKey(key);
        var sourceImage = target.GetComponent<Image>();
        if (theme == null
            || (!force && (sourceImage == null || !ShouldApplyCharacterThemePanel(target.name, sourceImage.color))))
        {
            return false;
        }

        // Keep the complete class ornaments inside the fixed 9-slice corners.
        var sprite = LoadGeneratedSprite(theme.ResourcePath("panel_frame"), new Vector4(180f, 180f, 180f, 180f));
        if (sprite == null)
        {
            return false;
        }

        var frame = AddFlatPanel("Character Theme Panel Frame", target, Color.white);
        Stretch(frame, 0, 0, 0, 0);
        frame.SetAsFirstSibling();
        frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var frameImage = frame.GetComponent<Image>();
        frameImage.sprite = sprite;
        frameImage.type = Image.Type.Sliced;
        frameImage.raycastTarget = false;
        return true;
    }

    private bool ApplyCharacterThemeCardButton(Button button, string keyOrClass, bool selected)
    {
        if (button == null)
        {
            return false;
        }

        var theme = CharacterThemeForKey(keyOrClass);
        var image = button.GetComponent<Image>();
        if (theme == null || image == null)
        {
            return false;
        }

        var sprite = LoadGeneratedSprite(theme.ResourcePath("panel_frame"), Vector4.zero);
        if (sprite == null)
        {
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;
        button.targetGraphic = image;
        DisableGenericButtonChrome(button);

        var normal = selected
            ? Color.Lerp(Color.white, theme.accent, 0.34f)
            : Color.Lerp(Color.white, theme.accent, 0.04f);
        var colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = Color.Lerp(normal, theme.accent, 0.18f);
        colors.pressedColor = Color.Lerp(normal, theme.accent, 0.32f);
        colors.selectedColor = Color.Lerp(Color.white, theme.accent, 0.38f);
        colors.disabledColor = new Color(0.74f, 0.76f, 0.78f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return true;
    }

    private bool ApplyCharacterThemeButtonFrame(Button button, string keyOverride = null, bool force = false)
    {
        if (button == null)
        {
            return false;
        }

        var key = string.IsNullOrEmpty(keyOverride) ? ActiveCharacterThemeKey() : keyOverride;
        var theme = CharacterThemeForKey(key);
        if (theme == null || (!force && currentScreen == AetheriaScreen.ClassSelect))
        {
            return false;
        }

        var sprite = LoadGeneratedSprite(theme.ResourcePath("button_frame"), new Vector4(128f, 48f, 128f, 48f));
        if (sprite == null)
        {
            return false;
        }

        var image = button.GetComponent<Image>();
        if (image == null)
        {
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 4f;
        image.color = Color.white;
        image.raycastTarget = true;
        button.targetGraphic = image;
        DisableGenericButtonChrome(button);
        return true;
    }

    private void DisableGenericButtonChrome(Button button)
    {
        if (button == null)
        {
            return;
        }

        var outlines = button.GetComponents<Outline>();
        for (var i = 0; i < outlines.Length; i++)
        {
            outlines[i].enabled = false;
        }

        var chromeNames = new[]
        {
            "Character Theme Button Frame",
            "Character Theme Panel Frame",
            "Panel Side Accent",
            "Button Shine",
            "Button Shade"
        };
        for (var i = 0; i < chromeNames.Length; i++)
        {
            var chrome = button.transform.Find(chromeNames[i]);
            if (chrome != null)
            {
                chrome.gameObject.SetActive(false);
            }
        }
    }

    private bool AddCharacterThemeHalo(RectTransform parent, string keyOverride, float alpha)
    {
        if (parent == null)
        {
            return false;
        }

        var theme = CharacterThemeForKey(keyOverride);
        if (theme == null)
        {
            return false;
        }

        var sprite = LoadGeneratedSprite(theme.ResourcePath("crest"), Vector4.zero);
        if (sprite == null)
        {
            return false;
        }

        var halo = AddFlatPanel("Character Theme Crest Halo", parent, new Color(1f, 1f, 1f, alpha));
        halo.anchorMin = new Vector2(0.12f, 0.08f);
        halo.anchorMax = new Vector2(0.88f, 0.84f);
        halo.offsetMin = Vector2.zero;
        halo.offsetMax = Vector2.zero;
        halo.SetAsFirstSibling();
        halo.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var image = halo.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return true;
    }
}
