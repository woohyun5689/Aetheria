using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    // Text may sit on a backplate only when the background art makes it necessary.
    // Keeping this value in one place prevents screen-by-screen opacity drift.
    private const float VisualReadabilityPlateAlpha = 0.30f;

    private enum VisualActionRole
    {
        Portal,
        Satchel,
        Anvil,
        Tome,
        Alchemy,
        Inn,
        Journal,
        Guide,
        Menu,
        Attack,
        Skill,
        Reward,
        Confirm,
        Cancel,
        Back,
        Neutral
    }

    private enum VisualStatusTone
    {
        Neutral,
        Information,
        Good,
        Warning,
        Danger,
        Mana,
        Locked
    }

    private Sprite LoadFirstVisualSprite(params string[] resourcePaths)
    {
        if (resourcePaths == null)
        {
            return null;
        }

        for (var i = 0; i < resourcePaths.Length; i++)
        {
            if (string.IsNullOrEmpty(resourcePaths[i]))
            {
                continue;
            }

            var resourcePath = resourcePaths[i];
            // VisualRefresh object art is imported with real alpha. Only the old
            // Generated/Objects set still needs the expensive runtime key mask.
            var sprite = resourcePath.StartsWith("UI/Generated/Objects/", StringComparison.Ordinal)
                ? LoadRuntimeObjectSprite(resourcePath)
                : LoadGeneratedSprite(resourcePath, Vector4.zero);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private RectTransform AddSceneBackdropLayer(Transform parent, string resourcePath, float alpha = 1f, bool sendBehindContent = true)
    {
        if (parent == null || string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        var sprite = LoadGeneratedSprite(resourcePath, Vector4.zero);
        if (sprite == null)
        {
            return null;
        }

        var layer = AddFlatPanel("Scene Backdrop Layer " + resourcePath, parent, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
        Stretch(layer, 0f, 0f, 0f, 0f);
        layer.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var image = layer.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = false;
        image.raycastTarget = false;
        var fitter = layer.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        if (sendBehindContent)
        {
            layer.SetAsFirstSibling();
        }
        return layer;
    }

    private RectTransform AddReadabilityPlate(Transform parent, string name, Color tint)
    {
        if (parent == null)
        {
            return null;
        }

        var plateColor = new Color(tint.r, tint.g, tint.b, VisualReadabilityPlateAlpha);
        var plate = AddFlatPanel("Text Readability Plate " + (name ?? ""), parent, plateColor);
        var image = plate.GetComponent<Image>();
        image.sprite = MapRoundedRectSprite();
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        return plate;
    }

    private bool ApplyReadabilityPlate(RectTransform target, Color tint)
    {
        if (target == null)
        {
            return false;
        }

        var image = target.GetComponent<Image>();
        if (image == null)
        {
            var nestedPlate = AddReadabilityPlate(target, target.name, tint);
            if (nestedPlate == null)
            {
                return false;
            }
            Stretch(nestedPlate, 0f, 0f, 0f, 0f);
            nestedPlate.SetAsFirstSibling();
            nestedPlate.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return true;
        }

        target.name = target.name.StartsWith("Text Readability Plate", StringComparison.Ordinal)
            ? target.name
            : "Text Readability Plate " + target.name;
        image.sprite = MapRoundedRectSprite();
        image.type = Image.Type.Sliced;
        image.color = new Color(tint.r, tint.g, tint.b, VisualReadabilityPlateAlpha);
        image.raycastTarget = false;
        return true;
    }

    private RectTransform AddReadabilityTextBlock(
        Transform parent,
        string name,
        string value,
        int size,
        FontStyle style,
        Color textTint,
        TextAnchor alignment,
        float preferredHeight,
        Color plateTint)
    {
        var plate = AddReadabilityPlate(parent, name, plateTint);
        if (plate == null)
        {
            return null;
        }

        AddLayoutSize(plate, -1f, preferredHeight);
        var label = AddText(plate, value, size, style, textTint, alignment, preferredHeight);
        Stretch(label.GetComponent<RectTransform>(), 14f, 6f, 14f, 6f);
        return plate;
    }

    private Button AddObjectActionButton(
        Transform parent,
        string label,
        Action onClick,
        Color accent,
        VisualActionRole role,
        string iconKey = null,
        float preferredWidth = 280f,
        float preferredHeight = 94f)
    {
        var button = AddButton(parent, label, onClick, accent);
        ConstrainLayoutSize(button.GetComponent<RectTransform>(), preferredWidth, preferredHeight);
        ApplyObjectActionButtonStyle(button, role, iconKey, accent);
        return button;
    }

    private bool ApplyObjectActionButtonStyle(Button button, VisualActionRole role, string iconKey, Color accent)
    {
        if (button == null)
        {
            return false;
        }

        var rect = button.GetComponent<RectTransform>();
        var hitImage = button.GetComponent<Image>();
        if (rect == null || hitImage == null)
        {
            return false;
        }

        // The real button label is always a direct child. Fallback visuals can
        // contain their own Text glyph, so a recursive lookup may style the
        // icon instead of the label when this method is applied again.
        Text label = null;
        for (var childIndex = 0; childIndex < rect.childCount; childIndex++)
        {
            var directLabel = rect.GetChild(childIndex).GetComponent<Text>();
            if (directLabel != null)
            {
                label = directLabel;
                break;
            }
        }

        DisableGenericButtonChrome(button);
        hitImage.sprite = null;
        hitImage.color = Color.clear;
        hitImage.raycastTarget = true;
        button.targetGraphic = hitImage;

        var oldVisual = rect.Find("Object Action Visual");
        RectTransform visual;
        Image visualImage;
        if (oldVisual == null)
        {
            visual = AddFlatPanel("Object Action Visual", rect, Color.clear);
            visual.anchorMin = new Vector2(0.025f, 0.08f);
            visual.anchorMax = new Vector2(0.29f, 0.92f);
            visual.offsetMin = Vector2.zero;
            visual.offsetMax = Vector2.zero;
            visual.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            visual.SetAsFirstSibling();
            visualImage = visual.GetComponent<Image>();
            visualImage.raycastTarget = false;

            var roleKey = role.ToString().ToLowerInvariant();
            var sprite = LoadFirstVisualSprite(
                string.IsNullOrEmpty(iconKey) ? null : "UI/VisualRefresh/Objects/" + iconKey,
                "UI/VisualRefresh/Objects/" + roleKey,
                string.IsNullOrEmpty(iconKey) ? null : "UI/Generated/Objects/" + iconKey,
                "UI/Generated/Objects/" + roleKey);
            if (sprite == null)
            {
                var fallbackPath = ObjectActionFallbackVisualPath(role);
                sprite = string.IsNullOrEmpty(fallbackPath)
                    ? null
                    : LoadGeneratedSprite(fallbackPath, Vector4.zero);
            }
            if (sprite != null)
            {
                visualImage.sprite = sprite;
                visualImage.preserveAspect = true;
                visualImage.color = Color.white;
                if (role == VisualActionRole.Cancel)
                {
                    AddDeleteJournalSlash(visual);
                }
            }
            else
            {
                BuildFallbackObjectGlyph(visual, role, accent);
            }
        }
        else
        {
            visual = oldVisual as RectTransform;
            visualImage = oldVisual.GetComponent<Image>();
        }

        if (label != null)
        {
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.27f, 0.12f);
            labelRect.anchorMax = new Vector2(0.98f, 0.88f);
            labelRect.offsetMin = new Vector2(14f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.resizeTextMinSize = 17;
            label.resizeTextMaxSize = Mathf.Max(20, label.fontSize);

            var labelPlate = rect.Find("Text Readability Plate Object Button");
            if (labelPlate == null)
            {
                var plate = AddReadabilityPlate(rect, "Object Button", Rgb(10, 18, 29));
                // One continuous 30% surface makes the art and copy read as a
                // single image button instead of a detached badge plus text box.
                plate.anchorMin = new Vector2(0.01f, 0.08f);
                plate.anchorMax = new Vector2(0.99f, 0.92f);
                plate.offsetMin = Vector2.zero;
                plate.offsetMax = Vector2.zero;
                plate.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                plate.SetAsFirstSibling();
                visual.SetSiblingIndex(Mathf.Min(1, rect.childCount - 1));
                label.transform.SetAsLastSibling();
            }

            var textPresenter = button.GetComponent<UiButtonTextPresenter>();
            if (textPresenter != null)
            {
                textPresenter.Configure(button, label, Color.white, Rgb(180, 190, 200));
            }
        }

        var presenter = button.GetComponent<UiObjectActionPresenter>();
        if (presenter == null)
        {
            presenter = button.gameObject.AddComponent<UiObjectActionPresenter>();
        }
        presenter.Configure(button, visual, visualImage, accent);
        return true;
    }

    private static string ObjectActionFallbackVisualPath(VisualActionRole role)
    {
        switch (role)
        {
            case VisualActionRole.Confirm:
            case VisualActionRole.Portal:
                return "UI/VisualRefresh/Objects/portal";
            case VisualActionRole.Cancel:
            case VisualActionRole.Journal:
                return "UI/VisualRefresh/Objects/journal";
            case VisualActionRole.Back:
            case VisualActionRole.Menu:
                return "UI/VisualRefresh/Objects/menu";
            default:
                return null;
        }
    }

    private RectTransform AddDeleteRecordVisual(Transform parent, float size)
    {
        var visual = AddFlatPanel("Delete Record Visual", parent, Color.clear);
        AddLayoutSize(visual, size, size);
        var image = visual.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.sprite = LoadGeneratedSprite("UI/VisualRefresh/Objects/journal", Vector4.zero);
        image.color = image.sprite != null ? Color.white : new Color(0.92f, 0.14f, 0.22f, 0.96f);
        AddDeleteJournalSlash(visual);
        return visual;
    }

    private void AddDeleteJournalSlash(RectTransform parent)
    {
        if (parent == null)
        {
            return;
        }

        AddVisualPrimitive(
            parent,
            "Delete Journal Slash",
            new Vector2(0.14f, 0.485f),
            new Vector2(0.86f, 0.535f),
            new Color(0.92f, 0.14f, 0.22f, 0.96f),
            false,
            -34f);
    }

    private void BuildFallbackObjectGlyph(RectTransform parent, VisualActionRole role, Color accent)
    {
        var opaqueAccent = new Color(accent.r, accent.g, accent.b, 1f);
        var body = AddVisualPrimitive(parent, "Object Silhouette", new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.90f), opaqueAccent, true, 0f);
        var light = Color.Lerp(opaqueAccent, Color.white, 0.66f);
        var compactSeal = role == VisualActionRole.Confirm
            || role == VisualActionRole.Cancel
            || role == VisualActionRole.Back
            || role == VisualActionRole.Menu
            || role == VisualActionRole.Neutral;

        switch (role)
        {
            case VisualActionRole.Portal:
                body.GetComponent<Image>().sprite = MapCircleSprite();
                AddVisualPrimitive(parent, "Portal Inner Light", new Vector2(0.29f, 0.27f), new Vector2(0.71f, 0.73f), new Color(light.r, light.g, light.b, 0.92f), true, 0f);
                break;
            case VisualActionRole.Satchel:
                body.anchorMin = new Vector2(0.13f, 0.22f);
                body.anchorMax = new Vector2(0.87f, 0.84f);
                AddVisualPrimitive(parent, "Satchel Handle", new Vector2(0.31f, 0.73f), new Vector2(0.69f, 0.94f), light, true, 0f);
                break;
            case VisualActionRole.Anvil:
                body.anchorMin = new Vector2(0.12f, 0.54f);
                body.anchorMax = new Vector2(0.90f, 0.76f);
                AddVisualPrimitive(parent, "Anvil Neck", new Vector2(0.39f, 0.28f), new Vector2(0.66f, 0.61f), opaqueAccent, true, 0f);
                AddVisualPrimitive(parent, "Anvil Foot", new Vector2(0.22f, 0.17f), new Vector2(0.80f, 0.34f), light, true, 0f);
                break;
            case VisualActionRole.Tome:
            case VisualActionRole.Journal:
            case VisualActionRole.Guide:
                body.anchorMin = new Vector2(0.18f, 0.14f);
                body.anchorMax = new Vector2(0.82f, 0.88f);
                AddVisualPrimitive(parent, "Book Spine", new Vector2(0.48f, 0.18f), new Vector2(0.53f, 0.84f), light, true, 0f);
                break;
            case VisualActionRole.Alchemy:
                body.GetComponent<Image>().sprite = MapCircleSprite();
                body.anchorMin = new Vector2(0.19f, 0.10f);
                body.anchorMax = new Vector2(0.81f, 0.67f);
                AddVisualPrimitive(parent, "Flask Neck", new Vector2(0.40f, 0.58f), new Vector2(0.60f, 0.94f), opaqueAccent, true, 0f);
                break;
            case VisualActionRole.Inn:
                body.anchorMin = new Vector2(0.18f, 0.12f);
                body.anchorMax = new Vector2(0.82f, 0.66f);
                AddVisualPrimitive(parent, "Inn Roof", new Vector2(0.23f, 0.53f), new Vector2(0.77f, 0.91f), light, false, 45f);
                break;
            case VisualActionRole.Attack:
                body.anchorMin = new Vector2(0.43f, 0.05f);
                body.anchorMax = new Vector2(0.59f, 0.94f);
                body.localRotation = Quaternion.Euler(0f, 0f, -38f);
                AddVisualPrimitive(parent, "Attack Guard", new Vector2(0.25f, 0.43f), new Vector2(0.75f, 0.56f), light, true, -38f);
                break;
            case VisualActionRole.Skill:
            case VisualActionRole.Reward:
                body.anchorMin = new Vector2(0.20f, 0.18f);
                body.anchorMax = new Vector2(0.80f, 0.82f);
                body.localRotation = Quaternion.Euler(0f, 0f, 45f);
                AddVisualPrimitive(parent, "Object Core", new Vector2(0.38f, 0.37f), new Vector2(0.62f, 0.63f), light, true, 45f);
                break;
        }

        if (compactSeal)
        {
            body.GetComponent<Image>().sprite = MapCircleSprite();
            body.GetComponent<Image>().type = Image.Type.Simple;
            body.anchorMin = new Vector2(0.5f, 0.5f);
            body.anchorMax = new Vector2(0.5f, 0.5f);
            body.pivot = new Vector2(0.5f, 0.5f);
            body.anchoredPosition = Vector2.zero;
            body.sizeDelta = new Vector2(62f, 62f);
        }

        var glyph = AddText(parent, VisualActionFallbackGlyph(role), 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 36f);
        var glyphRect = glyph.GetComponent<RectTransform>();
        glyphRect.anchorMin = compactSeal ? new Vector2(0.5f, 0.5f) : new Vector2(0.18f, 0.24f);
        glyphRect.anchorMax = compactSeal ? new Vector2(0.5f, 0.5f) : new Vector2(0.82f, 0.76f);
        glyphRect.offsetMin = Vector2.zero;
        glyphRect.offsetMax = Vector2.zero;
        if (compactSeal)
        {
            glyphRect.anchoredPosition = Vector2.zero;
            glyphRect.sizeDelta = new Vector2(56f, 56f);
        }
        glyphRect.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;
    }

    private RectTransform AddVisualPrimitive(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color,
        bool rounded,
        float rotation)
    {
        var primitive = AddFlatPanel(name, parent, color);
        primitive.anchorMin = anchorMin;
        primitive.anchorMax = anchorMax;
        primitive.offsetMin = Vector2.zero;
        primitive.offsetMax = Vector2.zero;
        primitive.localRotation = Quaternion.Euler(0f, 0f, rotation);
        primitive.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var image = primitive.GetComponent<Image>();
        image.raycastTarget = false;
        if (rounded)
        {
            image.sprite = MapRoundedRectSprite();
            image.type = Image.Type.Sliced;
        }
        return primitive;
    }

    private static string VisualActionFallbackGlyph(VisualActionRole role)
    {
        switch (role)
        {
            case VisualActionRole.Portal: return "문";
            case VisualActionRole.Satchel: return "짐";
            case VisualActionRole.Anvil: return "강";
            case VisualActionRole.Tome: return "술";
            case VisualActionRole.Alchemy: return "합";
            case VisualActionRole.Inn: return "쉼";
            case VisualActionRole.Journal: return "록";
            case VisualActionRole.Guide: return "?";
            case VisualActionRole.Menu: return "≡";
            case VisualActionRole.Attack: return "공";
            case VisualActionRole.Skill: return "기";
            case VisualActionRole.Reward: return "보";
            case VisualActionRole.Confirm: return "✓";
            case VisualActionRole.Cancel: return "×";
            case VisualActionRole.Back: return "←";
            default: return "·";
        }
    }

    private RectTransform AddIconBadge(
        Transform parent,
        string iconKey,
        string fallbackGlyph,
        Color accent,
        float size = 48f)
    {
        if (parent == null)
        {
            return null;
        }

        var badge = AddFlatPanel("Visual Icon Badge " + (iconKey ?? ""), parent, new Color(accent.r, accent.g, accent.b, 0.94f));
        AddLayoutSize(badge, size, size);
        var badgeImage = badge.GetComponent<Image>();
        badgeImage.sprite = LoadFirstVisualSprite(
            string.IsNullOrEmpty(iconKey) ? null : "UI/VisualRefresh/Icons/" + iconKey,
            string.IsNullOrEmpty(iconKey) ? null : "UI/Generated/Icons/" + iconKey);
        if (badgeImage.sprite == null)
        {
            badgeImage.sprite = MapCircleSprite();
        }
        else
        {
            badgeImage.color = Color.white;
        }
        badgeImage.preserveAspect = true;
        badgeImage.raycastTarget = false;

        if (badgeImage.sprite != null && !string.IsNullOrEmpty(fallbackGlyph)
            && (string.IsNullOrEmpty(iconKey) || badgeImage.sprite == MapCircleSprite()))
        {
            var glyph = AddText(badge, fallbackGlyph, Mathf.RoundToInt(size * 0.42f), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, size);
            Stretch(glyph.GetComponent<RectTransform>(), 4f, 4f, 4f, 4f);
        }

        return badge;
    }

    private RectTransform AddGroundingShadow(
        RectTransform parent,
        float width,
        float height,
        Vector2 anchoredPosition,
        float alpha = 0.18f)
    {
        if (parent == null)
        {
            return null;
        }

        var shadow = AddFlatPanel("Character Grounding Shadow", parent, new Color(0.04f, 0.06f, 0.08f, Mathf.Clamp(alpha, 0f, 0.32f)));
        shadow.anchorMin = new Vector2(0.5f, 0f);
        shadow.anchorMax = new Vector2(0.5f, 0f);
        shadow.pivot = new Vector2(0.5f, 0.5f);
        shadow.anchoredPosition = anchoredPosition;
        shadow.sizeDelta = new Vector2(width, height);
        shadow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var image = shadow.GetComponent<Image>();
        image.sprite = MapRoundedRectSprite();
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        shadow.SetAsFirstSibling();
        return shadow;
    }

    private RectTransform AddSectionTitle(
        Transform parent,
        string title,
        string subtitle,
        string iconKey,
        Color accent,
        float preferredHeight = 72f)
    {
        if (parent == null)
        {
            return null;
        }

        var section = AddPanel("Visual Section Title " + (title ?? ""), parent, Color.clear);
        AddLayoutSize(section, -1f, preferredHeight);
        var row = section.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 12f;
        row.padding = new RectOffset(2, 2, 4, 4);
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        AddIconBadge(section, iconKey, string.IsNullOrEmpty(title) ? "·" : title.Substring(0, 1), accent, preferredHeight - 12f);
        var textStack = AddPanel("Section Title Text", section, Color.clear);
        AddLayoutSize(textStack, -1f, preferredHeight - 8f);
        AddVertical(textStack, 0, TextAnchor.MiddleLeft, new RectOffset(0, 0, 0, 0));
        AddText(textStack, title, 26, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, string.IsNullOrEmpty(subtitle) ? preferredHeight - 10f : 36f);
        if (!string.IsNullOrEmpty(subtitle))
        {
            AddText(textStack, subtitle, 18, FontStyle.Bold, CharacterThemeMuted(ActiveCharacterThemeKey(), mutedColor), TextAnchor.MiddleLeft, 26f);
        }

        var underline = AddFlatPanel("Section Title Accent", section, new Color(accent.r, accent.g, accent.b, 0.84f));
        underline.anchorMin = new Vector2(0.18f, 0f);
        underline.anchorMax = new Vector2(1f, 0f);
        underline.pivot = new Vector2(0.5f, 0f);
        underline.anchoredPosition = Vector2.zero;
        underline.sizeDelta = new Vector2(0f, 3f);
        underline.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        underline.GetComponent<Image>().raycastTarget = false;
        return section;
    }

    private RectTransform AddStatusChip(
        Transform parent,
        string label,
        VisualStatusTone tone,
        string iconKey = null,
        float preferredWidth = 150f,
        float preferredHeight = 38f)
    {
        if (parent == null)
        {
            return null;
        }

        var accent = VisualStatusColor(tone);
        var chip = AddFlatPanel("Visual Status Chip " + (label ?? ""), parent, new Color(accent.r, accent.g, accent.b, VisualReadabilityPlateAlpha));
        AddLayoutSize(chip, preferredWidth, preferredHeight);
        var image = chip.GetComponent<Image>();
        image.sprite = MapRoundedRectSprite();
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;

        var row = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 6f;
        row.padding = new RectOffset(8, 10, 4, 4);
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;
        if (!string.IsNullOrEmpty(iconKey))
        {
            AddIconBadge(chip, iconKey, "·", accent, preferredHeight - 10f);
        }
        var text = AddText(chip, label, 18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, preferredHeight - 8f);
        AddLayoutSize(text.GetComponent<RectTransform>(), -1f, preferredHeight - 8f);
        return chip;
    }

    private RectTransform AddStatComparisonArrow(
        Transform parent,
        string statLabel,
        int currentValue,
        int nextValue,
        Color accent,
        float preferredHeight = 48f)
    {
        if (parent == null)
        {
            return null;
        }

        var comparison = nextValue.CompareTo(currentValue);
        var row = AddPanel("Stat Comparison Arrow " + (statLabel ?? ""), parent, Color.clear);
        AddLayoutSize(row, -1f, preferredHeight);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(6, 6, 2, 2);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var label = AddText(row, statLabel, 18, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, preferredHeight - 4f);
        AddLayoutSize(label.GetComponent<RectTransform>(), 104f, preferredHeight - 4f);
        AddStatusChip(row, currentValue.ToString(), VisualStatusTone.Neutral, null, 72f, preferredHeight - 8f);
        var arrow = AddText(row, "→", 26, FontStyle.Bold, accent, TextAnchor.MiddleCenter, preferredHeight - 4f);
        AddLayoutSize(arrow.GetComponent<RectTransform>(), 36f, preferredHeight - 4f);
        AddStatusChip(
            row,
            nextValue.ToString(),
            comparison > 0 ? VisualStatusTone.Good : comparison < 0 ? VisualStatusTone.Danger : VisualStatusTone.Neutral,
            null,
            72f,
            preferredHeight - 8f);

        var delta = nextValue - currentValue;
        AddText(
            row,
            delta > 0 ? "+" + delta : delta.ToString(),
            18,
            FontStyle.Bold,
            comparison > 0 ? goodColor : comparison < 0 ? dangerColor : mutedColor,
            TextAnchor.MiddleLeft,
            preferredHeight - 4f);
        return row;
    }

    private static Color VisualStatusColor(VisualStatusTone tone)
    {
        switch (tone)
        {
            case VisualStatusTone.Information: return Rgb(14, 116, 144);
            case VisualStatusTone.Good: return Rgb(5, 122, 85);
            case VisualStatusTone.Warning: return Rgb(180, 83, 9);
            case VisualStatusTone.Danger: return Rgb(185, 28, 28);
            case VisualStatusTone.Mana: return Rgb(67, 56, 202);
            case VisualStatusTone.Locked: return Rgb(75, 85, 99);
            default: return Rgb(31, 41, 55);
        }
    }

    private void ApplyVisualRefreshToScreen(RectTransform page)
    {
        if (page == null)
        {
            return;
        }

        EnsureVisualPolishDriver();
        HideGenericUxBoxes(page);
    }

    private void HideGenericUxBoxes(Transform scope)
    {
        if (scope == null)
        {
            return;
        }

        var images = scope.GetComponentsInChildren<Image>(true);
        for (var i = 0; i < images.Length; i++)
        {
            var image = images[i];
            if (image == null)
            {
                continue;
            }

            var imageName = image.gameObject.name ?? "";
            if (imageName.StartsWith("Text Readability Plate", StringComparison.Ordinal)
                || imageName.StartsWith("Visual Status Chip", StringComparison.Ordinal))
            {
                var fixedColor = image.color;
                fixedColor.a = VisualReadabilityPlateAlpha;
                image.color = fixedColor;
                continue;
            }

            if (image.GetComponent<Button>() != null || ShouldPreserveVisualGraphic(imageName))
            {
                continue;
            }

            if (LooksLikeGenericUxBox(imageName))
            {
                var transparent = image.color;
                transparent.a = 0f;
                image.color = transparent;
                image.raycastTarget = false;
            }
        }
    }

    private static bool LooksLikeGenericUxBox(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return name.Contains("Panel")
            || name.Contains("Card")
            || name.Contains("Grid")
            || name.Contains("HUD")
            || name.Contains("Header")
            || name.Contains("Board")
            || name.Contains("Detail")
            || name.Contains("Viewport")
            || name.Contains("Content")
            || name.Contains("Sidebar")
            || name.Contains("Popup")
            || name.Contains("Actions")
            || name.Contains("Buttons")
            || name.Contains("Section")
            || name.Contains("Log");
    }

    private static bool ShouldPreserveVisualGraphic(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return name.Contains("Background")
            || name.Contains("Backdrop")
            || name.Contains("Portrait")
            || name.Contains("Artwork")
            || name.Contains("World Map")
            || name.Contains("Opening")
            || name.Contains("Scroll Leaf")
            || name.Contains("Image")
            || name.Contains("Overlay")
            || name.Contains("Veil")
            || name.Contains("Shadow")
            || name.Contains("Fill")
            || name.EndsWith(" Bar", StringComparison.Ordinal)
            || name.Contains("Divider")
            || name.Contains("Accent")
            || name.Contains("Crest")
            || name.Contains("Character Theme")
            || name.Contains("Glow")
            || name.Contains("Pin")
            || name.Contains("Badge")
            || name.Contains("Object Action")
            || name.Contains("Object Silhouette")
            || name.Contains("Object Core")
            || name.Contains("Portal Inner")
            || name.Contains("Satchel")
            || name.Contains("Anvil")
            || name.Contains("Book Spine")
            || name.Contains("Flask")
            || name.Contains("Inn Roof")
            || name.Contains("Attack Guard")
            || name.Contains("Scrollbar");
    }

    private sealed class UiObjectActionPresenter : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private Button button;
        private RectTransform visual;
        private Image visualImage;
        private Color enabledColor;
        private bool hasDirectArtwork;
        private bool hovered;
        private bool pressed;
        private bool selected;

        public void Configure(Button sourceButton, RectTransform sourceVisual, Image sourceImage, Color accent)
        {
            button = sourceButton;
            visual = sourceVisual;
            visualImage = sourceImage;
            hasDirectArtwork = sourceImage != null && sourceImage.sprite != null;
            enabledColor = hasDirectArtwork ? sourceImage.color : Color.clear;
            if (!hasDirectArtwork && visualImage != null)
            {
                visualImage.color = Color.clear;
            }
        }

        public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
        public void OnPointerDown(PointerEventData eventData) { pressed = true; }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; }
        public void OnSelect(BaseEventData eventData) { selected = true; }
        public void OnDeselect(BaseEventData eventData) { selected = false; }

        private void Update()
        {
            if (visual == null || button == null)
            {
                return;
            }

            var active = button.interactable && (hovered || selected);
            var targetScale = pressed ? 0.94f : active ? 1.07f : 1f;
            var targetLift = pressed ? -2f : active ? 5f : 0f;
            var targetRotation = active && !pressed ? -2.5f : 0f;
            var speed = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
            visual.localScale = Vector3.Lerp(visual.localScale, Vector3.one * targetScale, speed);
            var position = visual.anchoredPosition;
            position.y = Mathf.Lerp(position.y, targetLift, speed);
            visual.anchoredPosition = position;
            visual.localRotation = Quaternion.Lerp(visual.localRotation, Quaternion.Euler(0f, 0f, targetRotation), speed);

            if (visualImage != null && hasDirectArtwork)
            {
                visualImage.color = button.interactable
                    ? enabledColor
                    : new Color(0.55f, 0.58f, 0.61f, Mathf.Min(0.66f, enabledColor.a));
            }
        }
    }
}
