using System;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private const string TextTitlePanelV1Path = "UI/VisualRefresh/TextPanels/text_title_panel_v1";
    private const string TextBodyPanelV1Path = "UI/VisualRefresh/TextPanels/text_body_panel_v1";
    private const string TextCompactPanelV1Path = "UI/VisualRefresh/TextPanels/text_compact_panel_v1";

    private enum OpaqueTextPanelKind
    {
        Title,
        Body,
        Compact
    }

    private bool ApplyOpaqueTextPanel(
        RectTransform target,
        OpaqueTextPanelKind kind,
        string characterKeyOverride = null,
        bool addCharacterFrame = false)
    {
        if (target == null)
        {
            return false;
        }

        var image = target.GetComponent<Image>();
        if (image == null)
        {
            image = target.gameObject.AddComponent<Image>();
        }

        var sprite = LoadGeneratedSprite(TextPanelResourcePath(kind), TextPanelBorder(kind));
        if (sprite == null)
        {
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = TextPanelPixelsPerUnit(kind);
        image.raycastTarget = false;

        var characterKey = string.IsNullOrEmpty(characterKeyOverride)
            ? ScreenCharacterThemeKey()
            : characterKeyOverride;
        var accent = TextPanelCharacterAccent(characterKey, manaColor);
        var tintStrength = kind == OpaqueTextPanelKind.Body ? 0.055f : 0.075f;
        var surface = Color.Lerp(Color.white, new Color(accent.r, accent.g, accent.b, 1f), tintStrength);
        image.color = new Color(surface.r, surface.g, surface.b, 1f);

        var marker = target.GetComponent<UiOpaqueTextPanel>();
        if (marker == null)
        {
            marker = target.gameObject.AddComponent<UiOpaqueTextPanel>();
        }
        marker.kind = kind;

        var oldThemeFrame = target.Find("Character Theme Panel Frame");
        if (oldThemeFrame != null)
        {
            oldThemeFrame.gameObject.SetActive(false);
        }

        var oldTextPanelFrame = target.Find("Character Theme Text Panel Frame");
        if (oldTextPanelFrame != null)
        {
            oldTextPanelFrame.gameObject.SetActive(addCharacterFrame);
        }

        if (addCharacterFrame)
        {
            ApplyCharacterTextPanelFrame(target, kind, characterKey);
        }

        ApplyOpaqueTextPanelTypography(target);
        return true;
    }

    private void ApplyOpaqueTextPanelsForScreen(RectTransform page)
    {
        if (page == null)
        {
            return;
        }

        var targets = page.GetComponentsInChildren<RectTransform>(true);
        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            if (target == null || target.GetComponent<UiOpaqueTextPanel>() != null)
            {
                continue;
            }

            OpaqueTextPanelKind kind;
            if (!TryResolveSceneTextPanelKind(target.gameObject.name, out kind))
            {
                continue;
            }

            ApplyOpaqueTextPanel(target, kind);
        }
    }

    private RectTransform AddOpaqueSceneTopText(
        Transform parent,
        string name,
        string title,
        string detail,
        Color titleColor,
        Color detailColor,
        float titleWidth,
        float detailWidth,
        float preferredHeight = 72f)
    {
        var panel = AddPanel(name, parent, Color.white);
        AddLayoutSize(panel, -1f, preferredHeight);
        ApplyOpaqueTextPanel(panel, OpaqueTextPanelKind.Title);

        var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.padding = new RectOffset(38, 38, 12, 12);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var titleLabel = AddText(panel, title, 38, FontStyle.Bold, titleColor, TextAnchor.MiddleLeft, preferredHeight - 24f);
        AddLayoutSize(titleLabel.rectTransform, titleWidth, preferredHeight - 24f);
        var detailLabel = AddText(panel, detail, 21, FontStyle.Bold, detailColor, TextAnchor.MiddleLeft, preferredHeight - 24f);
        AddLayoutSize(detailLabel.rectTransform, detailWidth, preferredHeight - 24f);
        return panel;
    }

    private RectTransform AddOpaqueSectionHeading(
        Transform parent,
        string name,
        string value,
        Color accent,
        float preferredHeight = 44f)
    {
        var panel = AddPanel(name, parent, Color.white);
        AddLayoutSize(panel, -1f, preferredHeight);
        ApplyOpaqueTextPanel(panel, OpaqueTextPanelKind.Compact);
        var text = AddText(panel, value, 26, FontStyle.Bold, accent, TextAnchor.MiddleLeft, preferredHeight);
        Stretch(text.rectTransform, 24f, 7f, 24f, 7f);
        return panel;
    }

    private bool TryResolveSceneTextPanelKind(string objectName, out OpaqueTextPanelKind kind)
    {
        kind = OpaqueTextPanelKind.Body;
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        if (objectName.StartsWith("Text Readability Plate", StringComparison.Ordinal))
        {
            kind = ReadabilityPanelKindFromName(objectName);
            return true;
        }

        if (objectName.StartsWith("Visual Status Chip", StringComparison.Ordinal)
            || objectName.StartsWith("Growth Badge ", StringComparison.Ordinal)
            || (objectName.StartsWith("Result Chip ", StringComparison.Ordinal)
                && objectName != "Result Chip Emblem"
                && objectName != "Result Chip Copy")
            || objectName.StartsWith("Pin Label ", StringComparison.Ordinal)
            || objectName == "Dungeon State Badge")
        {
            kind = OpaqueTextPanelKind.Body;
            return true;
        }

        if (objectName == "Turn Banner"
            || objectName == "Current Action Ribbon"
            || objectName == "Enemy Intent"
            || objectName == "Dungeon Map Status Overlay")
        {
            kind = OpaqueTextPanelKind.Compact;
            return true;
        }

        if (objectName == "Victory Heading"
            || objectName == "Defeat Heading"
            || objectName == "Game Clear Heading"
            || objectName.StartsWith("Visual Section Title ", StringComparison.Ordinal))
        {
            kind = OpaqueTextPanelKind.Title;
            return true;
        }

        if (objectName.EndsWith(" Backplate", StringComparison.Ordinal)
            || objectName == "Dungeon Map Legend"
            || objectName == "Dungeon Quick Start"
            || objectName == "Action Prompt"
            || objectName == "Player Combat Status HUD"
            || objectName == "Enemy Combat Status HUD")
        {
            kind = OpaqueTextPanelKind.Body;
            return true;
        }

        return false;
    }

    private static OpaqueTextPanelKind ReadabilityPanelKindFromName(string objectName)
    {
        if (objectName.IndexOf("Header", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Heading", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return OpaqueTextPanelKind.Title;
        }

        if (objectName.IndexOf("Caption", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Status", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Message", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Compact", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Style", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return OpaqueTextPanelKind.Compact;
        }

        return OpaqueTextPanelKind.Body;
    }

    private void ApplyCharacterTextPanelFrame(
        RectTransform target,
        OpaqueTextPanelKind kind,
        string characterKey)
    {
        if (target == null)
        {
            return;
        }

        var theme = CharacterThemeForKey(characterKey);
        if (theme == null)
        {
            return;
        }

        var resourceName = kind == OpaqueTextPanelKind.Body ? "panel_frame" : "button_frame";
        var border = kind == OpaqueTextPanelKind.Body
            ? new Vector4(180f, 180f, 180f, 180f)
            : new Vector4(128f, 48f, 128f, 48f);
        var sprite = LoadGeneratedSprite(theme.ResourcePath(resourceName), border);
        if (sprite == null)
        {
            return;
        }

        var frame = target.Find("Character Theme Text Panel Frame") as RectTransform;
        if (frame == null)
        {
            frame = AddFlatPanel("Character Theme Text Panel Frame", target, Color.white);
            frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        var inset = kind == OpaqueTextPanelKind.Body ? 5f : 2f;
        Stretch(frame, inset, inset, inset, inset);
        frame.SetAsFirstSibling();
        var frameImage = frame.GetComponent<Image>();
        frameImage.sprite = sprite;
        frameImage.type = Image.Type.Sliced;
        frameImage.preserveAspect = false;
        frameImage.pixelsPerUnitMultiplier = kind == OpaqueTextPanelKind.Body ? 6f : 4f;
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;
    }

    private void ApplyOpaqueTextPanelTypography(RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        var labels = target.GetComponentsInChildren<Text>(true);
        for (var i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null && IsInsideOpaqueTextPanel(labels[i].transform.parent))
            {
                ApplyOpaqueTextLabelStyle(labels[i]);
            }
        }
    }

    private void ApplyOpaqueTextLabelStyle(Text label)
    {
        if (label == null)
        {
            return;
        }

        label.color = ResolveOpaqueTextPanelColor(label.color, label.fontSize, label.fontStyle);

        var shadows = label.GetComponents<Shadow>();
        Outline outline = null;
        for (var i = 0; i < shadows.Length; i++)
        {
            var candidate = shadows[i];
            if (candidate is Outline)
            {
                if (outline == null)
                {
                    outline = candidate as Outline;
                }
                continue;
            }
            if (candidate != null)
            {
                candidate.enabled = false;
            }
        }

        if (outline == null)
        {
            outline = label.gameObject.AddComponent<Outline>();
        }
        var stroke = label.fontSize >= 32 ? 1.15f : label.fontSize >= 22 ? 0.90f : 0.70f;
        outline.effectColor = new Color(1f, 1f, 1f, 0.80f);
        outline.effectDistance = new Vector2(stroke, -stroke);
        outline.useGraphicAlpha = true;
        outline.enabled = true;
    }

    private Color ResolveOpaqueTextPanelColor(Color source, int size, FontStyle style)
    {
        var accent = TextPanelCharacterAccent(ScreenCharacterThemeKey(), manaColor);
        var saturation = Mathf.Max(source.r, Mathf.Max(source.g, source.b))
            - Mathf.Min(source.r, Mathf.Min(source.g, source.b));
        var bold = style == FontStyle.Bold || style == FontStyle.BoldAndItalic;

        if (SameUiColor(source, dangerColor) || (source.r > source.g * 1.25f && source.r > source.b * 1.12f))
        {
            return Rgb(139, 35, 42);
        }
        if (SameUiColor(source, goodColor))
        {
            return Rgb(26, 104, 70);
        }
        if (SameUiColor(source, goldColor))
        {
            return Rgb(127, 78, 18);
        }
        if (SameUiColor(source, manaColor) || SameUiColor(source, neonPurple))
        {
            return Color.Lerp(Rgb(22, 42, 58), new Color(accent.r, accent.g, accent.b, 1f), 0.44f);
        }
        if (saturation > 0.20f)
        {
            return Color.Lerp(Rgb(24, 39, 54), new Color(source.r, source.g, source.b, 1f), 0.38f);
        }
        if (bold && size >= 24)
        {
            return Color.Lerp(Rgb(20, 38, 54), new Color(accent.r, accent.g, accent.b, 1f), 0.28f);
        }
        if (SameUiColor(source, mutedColor))
        {
            return Rgb(72, 82, 92);
        }
        return Rgb(31, 43, 55);
    }

    private bool IsInsideOpaqueTextPanel(Transform parent)
    {
        var current = parent;
        while (current != null)
        {
            if (current.GetComponent<UiOpaqueTextPanel>() != null)
            {
                return true;
            }
            if (current.GetComponent<UiOpaqueSceneImageButton>() != null)
            {
                return true;
            }

            var image = current.GetComponent<Image>();
            if (image != null && image.color.a > 0.08f)
            {
                var color = image.color;
                var luminance = color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
                if (luminance < 0.68f)
                {
                    return false;
                }
            }
            current = current.parent;
        }
        return false;
    }

    private Color TextPanelCharacterAccent(string characterKey, Color fallback)
    {
        var theme = CharacterThemeForKey(characterKey);
        return theme == null ? fallback : theme.accent;
    }

    private static string TextPanelResourcePath(OpaqueTextPanelKind kind)
    {
        switch (kind)
        {
            case OpaqueTextPanelKind.Title: return TextTitlePanelV1Path;
            case OpaqueTextPanelKind.Compact: return TextCompactPanelV1Path;
            default: return TextBodyPanelV1Path;
        }
    }

    private static Vector4 TextPanelBorder(OpaqueTextPanelKind kind)
    {
        switch (kind)
        {
            case OpaqueTextPanelKind.Title: return new Vector4(220f, 150f, 220f, 150f);
            case OpaqueTextPanelKind.Compact: return new Vector4(250f, 128f, 250f, 128f);
            default: return new Vector4(112f, 112f, 112f, 112f);
        }
    }

    private static float TextPanelPixelsPerUnit(OpaqueTextPanelKind kind)
    {
        switch (kind)
        {
            case OpaqueTextPanelKind.Title: return 6f;
            case OpaqueTextPanelKind.Compact: return 6f;
            default: return 4f;
        }
    }

    private sealed class UiOpaqueTextPanel : MonoBehaviour
    {
        public OpaqueTextPanelKind kind;
    }
}
