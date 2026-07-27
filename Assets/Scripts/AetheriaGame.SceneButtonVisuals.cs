using System;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private const string SceneActionButtonV2Path = "UI/VisualRefresh/SceneButtons/scene_action_v2";
    private const string SceneDangerButtonV2Path = "UI/VisualRefresh/SceneButtons/scene_danger_v2";
    private const string CombatCardSurfaceV2Path = "UI/VisualRefresh/SceneButtons/combat_card_surface_v2";

    private bool ApplyOpaqueSceneButtonSkin(Image image, Color accent, bool danger = false)
    {
        if (image == null)
        {
            return false;
        }

        var sprite = LoadGeneratedSprite(
            danger ? SceneDangerButtonV2Path : SceneActionButtonV2Path,
            new Vector4(320f, 142f, 320f, 142f));
        if (sprite == null)
        {
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = 8f;
        var tint = Color.Lerp(
            Color.white,
            new Color(accent.r, accent.g, accent.b, 1f),
            danger ? 0.025f : 0.035f);
        image.color = new Color(tint.r, tint.g, tint.b, 1f);
        image.raycastTarget = true;

        var button = image.GetComponent<Button>();
        if (button != null)
        {
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            if (button.GetComponent<UiOpaqueSceneImageButton>() == null)
            {
                button.gameObject.AddComponent<UiOpaqueSceneImageButton>();
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, new Color(accent.r, accent.g, accent.b, 1f), 0.10f);
            colors.pressedColor = Color.Lerp(Color.white, new Color(accent.r, accent.g, accent.b, 1f), 0.22f);
            colors.selectedColor = Color.Lerp(Color.white, new Color(accent.r, accent.g, accent.b, 1f), 0.14f);
            colors.disabledColor = Rgb(190, 196, 202);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        return true;
    }

    private bool ApplyOpaqueSceneButtonSkin(Button button, Color accent, bool danger = false)
    {
        return button != null && ApplyOpaqueSceneButtonSkin(button.GetComponent<Image>(), accent, danger);
    }

    private bool ApplyOpaqueCombatCardSkin(Button button, Color accent, string characterKeyOverride = null)
    {
        if (button == null)
        {
            return false;
        }

        var image = button.GetComponent<Image>();
        if (image == null)
        {
            return false;
        }

        var theme = CharacterThemeForKey(characterKeyOverride);
        var themedSprite = theme == null
            ? null
            : LoadGeneratedSprite(theme.ResourcePath("combat_card_v1"), Vector4.zero);
        var sprite = themedSprite != null
            ? themedSprite
            : LoadGeneratedSprite(
                CombatCardSurfaceV2Path,
                new Vector4(184f, 184f, 184f, 184f));
        if (sprite == null)
        {
            return false;
        }

        var usesCharacterCard = themedSprite != null;
        image.sprite = sprite;
        image.type = usesCharacterCard ? Image.Type.Simple : Image.Type.Sliced;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = usesCharacterCard ? 1f : 4f;
        if (usesCharacterCard)
        {
            image.color = Color.white;
        }
        else
        {
            var tint = Color.Lerp(Color.white, new Color(accent.r, accent.g, accent.b, 1f), 0.025f);
            image.color = new Color(tint.r, tint.g, tint.b, 1f);
        }
        image.raycastTarget = true;
        button.targetGraphic = image;
        if (button.GetComponent<UiOpaqueSceneImageButton>() == null)
        {
            button.gameObject.AddComponent<UiOpaqueSceneImageButton>();
        }

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.Lerp(Color.white, new Color(accent.r, accent.g, accent.b, 1f), 0.08f);
        colors.pressedColor = Color.Lerp(Color.white, new Color(accent.r, accent.g, accent.b, 1f), 0.18f);
        colors.selectedColor = Color.Lerp(Color.white, new Color(accent.r, accent.g, accent.b, 1f), 0.12f);
        colors.disabledColor = Rgb(196, 199, 201);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return true;
    }

    private bool IsDangerSceneButton(Image image, Color accent)
    {
        var name = image == null || image.gameObject == null
            ? ""
            : (image.gameObject.name ?? "").ToLowerInvariant();
        if (name.Contains("delete")
            || name.Contains("cancel")
            || name.Contains("danger")
            || name.Contains("retreat"))
        {
            return true;
        }

        return accent.r > 0.55f
            && accent.r > accent.g * 1.28f
            && accent.r > accent.b * 1.12f;
    }

    private sealed class UiOpaqueSceneImageButton : MonoBehaviour
    {
    }
}
