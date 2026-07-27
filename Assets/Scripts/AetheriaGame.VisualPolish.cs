using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private enum DungeonRouteVisualType
    {
        Combat,
        Elite,
        Shop,
        Rest,
        Boss
    }

    private void EnsureVisualPolishDriver()
    {
        if (root == null)
        {
            return;
        }

        var driver = root.GetComponent<UiVisualPolishDriver>();
        if (driver == null)
        {
            driver = root.gameObject.AddComponent<UiVisualPolishDriver>();
        }
        driver.Configure(this, root);
    }

    private void QueueVisualPolishRefresh()
    {
        if (root == null)
        {
            return;
        }

        var driver = root.GetComponent<UiVisualPolishDriver>();
        if (driver == null)
        {
            EnsureVisualPolishDriver();
            return;
        }
        driver.RequestRapidScan();
    }

    private void ApplyKoreanReadability(Text label)
    {
        if (label == null || label.GetComponent<UiKoreanReadabilityMarker>() != null || !ContainsKorean(label.text))
        {
            return;
        }

        label.gameObject.AddComponent<UiKoreanReadabilityMarker>();
        label.fontSize = Mathf.Max(18, label.fontSize);
        label.resizeTextMaxSize = label.fontSize;
        label.resizeTextMinSize = Mathf.Clamp(Mathf.Max(16, label.resizeTextMinSize), 16, label.resizeTextMaxSize);
        label.lineSpacing = label.text.IndexOf('\n') >= 0
            ? Mathf.Max(1.18f, label.lineSpacing)
            : Mathf.Max(1.05f, label.lineSpacing);

        Outline outline = null;
        Shadow shadow = null;
        var effects = label.GetComponents<Shadow>();
        for (var i = 0; i < effects.Length; i++)
        {
            if (effects[i] is Outline)
            {
                if (outline == null)
                {
                    outline = effects[i] as Outline;
                }
            }
            else if (effects[i] != null && effects[i].GetType() == typeof(Shadow) && shadow == null)
            {
                shadow = effects[i];
            }
        }

        if (outline == null)
        {
            outline = label.gameObject.AddComponent<Outline>();
        }
        if (shadow == null)
        {
            shadow = label.gameObject.AddComponent<Shadow>();
        }

        var luminance = label.color.r * 0.2126f + label.color.g * 0.7152f + label.color.b * 0.0722f;
        var darkGlyph = luminance < 0.54f;
        var stroke = label.fontSize >= 32 ? 1.40f : label.fontSize >= 22 ? 1.15f : 0.90f;
        outline.effectColor = darkGlyph
            ? new Color(1f, 1f, 1f, 0.88f)
            : new Color(0.01f, 0.02f, 0.035f, 0.90f);
        outline.effectDistance = new Vector2(stroke, -stroke);
        outline.useGraphicAlpha = true;
        // Outline and drop shadow together made Korean glyphs look doubled on
        // detailed backgrounds. Keep the thinner outline as the single edge cue.
        shadow.enabled = false;
    }

    private static bool ContainsKorean(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];
            if ((character >= '\u1100' && character <= '\u11ff')
                || (character >= '\u3130' && character <= '\u318f')
                || (character >= '\uac00' && character <= '\ud7a3'))
            {
                return true;
            }
        }
        return false;
    }

    private void EnforceTransparentReadabilityPlate(Image image)
    {
        if (image == null)
        {
            return;
        }

        if (image.GetComponent<UiOpaqueTextPanel>() != null)
        {
            var opaqueColor = image.color;
            opaqueColor.a = 1f;
            image.color = opaqueColor;
            return;
        }

        var imageName = image.gameObject.name ?? "";
        var readabilitySurface = imageName.StartsWith("Text Readability Plate", StringComparison.Ordinal)
            || imageName.StartsWith("Visual Status Chip", StringComparison.Ordinal)
            || imageName.StartsWith("Pin Label ", StringComparison.Ordinal)
            || imageName == "Dungeon Map Top Overlay"
            || imageName == "Dungeon Map Legend"
            || imageName == "Dungeon Map Status Overlay"
            || imageName == "Dungeon Quick Start";
        if (!readabilitySurface)
        {
            return;
        }

        var color = image.color;
        color.a = VisualReadabilityPlateAlpha;
        image.color = color;
    }

    private void ApplyUniversalImageButtonState(Button button)
    {
        if (button == null
            || button.GetComponent<UiMainMenuImageButton>() != null
            || button.GetComponent<UiOpaqueSceneImageButton>() != null
            || button.GetComponent<UiImageButtonStatePresenter>() != null
            || button.GetComponent<UiCombatCardPresenter>() != null
            || !ButtonHasVisibleArtwork(button))
        {
            return;
        }

        var targetImage = button.targetGraphic as Image;
        if (targetImage == null)
        {
            targetImage = button.GetComponent<Image>();
        }

        var marker = button.GetComponent<UiDungeonRouteNodeMarker>();
        var accent = marker != null ? marker.accent : ResolveImageButtonAccent(button, targetImage);
        var stateSprite = targetImage != null && targetImage.sprite != null
            ? targetImage.sprite
            : MapRoundedRectSprite();
        var presenter = button.gameObject.AddComponent<UiImageButtonStatePresenter>();
        presenter.Configure(button, stateSprite, targetImage != null ? targetImage.type : Image.Type.Sliced, accent);
    }

    private bool ButtonHasVisibleArtwork(Button button)
    {
        if (button == null)
        {
            return false;
        }

        var rootImage = button.GetComponent<Image>();
        if (rootImage != null && rootImage.color.a > 0.04f)
        {
            return true;
        }

        var images = button.GetComponentsInChildren<Image>(true);
        for (var i = 0; i < images.Length; i++)
        {
            var candidate = images[i];
            if (candidate == null || candidate == rootImage || candidate.color.a <= 0.04f)
            {
                continue;
            }

            var candidateName = candidate.gameObject.name ?? "";
            if (candidateName.StartsWith("Text Readability Plate", StringComparison.Ordinal)
                || candidateName == "Image Button State Glow")
            {
                continue;
            }
            return true;
        }
        return false;
    }

    private Color ResolveImageButtonAccent(Button button, Image targetImage)
    {
        if (button != null)
        {
            var buttonName = (button.gameObject.name ?? "").ToLowerInvariant();
            if (buttonName.Contains("delete") || buttonName.Contains("cancel") || buttonName.Contains("danger"))
            {
                return dangerColor;
            }
        }

        if (targetImage != null)
        {
            var color = targetImage.color;
            var spread = Mathf.Max(color.r, Mathf.Max(color.g, color.b)) - Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            if (color.a > 0.04f && spread > 0.08f)
            {
                return new Color(color.r, color.g, color.b, 1f);
            }
        }
        return ActiveCharacterAccent(manaColor);
    }

    private void EnsureDefaultUiSelection(Button[] buttons)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null || buttons == null)
        {
            return;
        }

        var current = eventSystem.currentSelectedGameObject;
        var currentButton = current != null ? current.GetComponentInParent<Button>() : null;
        if (currentScreen == AetheriaScreen.Combat
            && combatPresentationPhase == CombatPresentationPhase.PlayerChoice)
        {
            if (currentButton != null
                && currentButton.gameObject.activeInHierarchy
                && currentButton.interactable
                && currentButton.navigation.mode != Navigation.Mode.None)
            {
                return;
            }

            for (var cardIndex = 0; cardIndex < buttons.Length; cardIndex++)
            {
                var card = buttons[cardIndex];
                if (card != null
                    && card.GetComponent<UiCombatCardPresenter>() != null
                    && card.gameObject.activeInHierarchy
                    && card.interactable
                    && card.navigation.mode != Navigation.Mode.None)
                {
                    eventSystem.SetSelectedGameObject(card.gameObject);
                    return;
                }
            }

            // Cards restore navigation as their deal animation completes. Until
            // then, keep selection empty instead of defaulting Submit to retreat.
            eventSystem.SetSelectedGameObject(null);
            return;
        }

        if (currentButton != null
            && currentButton.gameObject.activeInHierarchy
            && currentButton.interactable
            && currentButton.navigation.mode != Navigation.Mode.None)
        {
            return;
        }

        for (var i = 0; i < buttons.Length; i++)
        {
            var candidate = buttons[i];
            if (candidate == null
                || !candidate.gameObject.activeInHierarchy
                || !candidate.interactable
                || candidate.navigation.mode == Navigation.Mode.None)
            {
                continue;
            }

            var candidateName = candidate.gameObject.name ?? "";
            if (candidateName.Contains(" Hit")
                || candidateName == "Combat Input Lock"
                || candidateName == "Dungeon Info Modal Dimmer")
            {
                continue;
            }

            eventSystem.SetSelectedGameObject(candidate.gameObject);
            return;
        }
    }

    private void ApplyDungeonRouteIconIfNeeded(Button button)
    {
        if (button == null || button.GetComponent<UiDungeonRouteNodeMarker>() != null)
        {
            return;
        }

        var buttonName = button.gameObject.name ?? "";
        const string dungeonPinPrefix = "Dungeon Pin ";
        DungeonRouteVisualType type;
        var unlocked = button.interactable;
        if (buttonName.StartsWith(dungeonPinPrefix, StringComparison.Ordinal))
        {
            int dungeonNumber;
            if (!int.TryParse(buttonName.Substring(dungeonPinPrefix.Length), out dungeonNumber))
            {
                return;
            }

            type = DungeonRouteVisualTypeForNumber(dungeonNumber);
            unlocked = player != null && dungeonNumber <= Mathf.Max(1, player.stage);
        }
        else if (!TryNamedDungeonRouteVisualType(buttonName, out type))
        {
            return;
        }

        ApplyDungeonRouteNodeIcon(button, type, unlocked);
    }

    private bool ApplyDungeonRouteNodeIcon(Button button, DungeonRouteVisualType type, bool unlocked)
    {
        if (button == null)
        {
            return false;
        }

        // Route icons already contain real alpha. Loading the texture directly
        // avoids the legacy runtime chroma/magenta mask used by object buttons.
        var sprite = LoadGeneratedSprite("UI/VisualRefresh/Routes/" + DungeonRouteVisualKey(type), Vector4.zero);
        if (sprite == null)
        {
            return false;
        }

        var accent = DungeonRouteVisualAccent(type);
        var marker = button.gameObject.AddComponent<UiDungeonRouteNodeMarker>();
        marker.accent = accent;

        var rootImage = button.GetComponent<Image>();
        if (rootImage != null)
        {
            var surface = Color.Lerp(new Color(0.96f, 0.98f, 1f, 1f), accent, unlocked ? 0.20f : 0.08f);
            rootImage.color = new Color(surface.r, surface.g, surface.b, unlocked ? 0.96f : 0.72f);
        }

        var aura = AddFlatPanel("Dungeon Route Type Aura", button.transform, new Color(accent.r, accent.g, accent.b, unlocked ? 0.26f : 0.10f));
        aura.anchorMin = new Vector2(0.03f, 0.03f);
        aura.anchorMax = new Vector2(0.97f, 0.97f);
        aura.offsetMin = Vector2.zero;
        aura.offsetMax = Vector2.zero;
        aura.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var auraImage = aura.GetComponent<Image>();
        auraImage.sprite = MapCircleSprite();
        auraImage.raycastTarget = false;
        aura.SetAsFirstSibling();

        var icon = AddFlatPanel("Dungeon Route Icon", button.transform, unlocked ? Color.white : new Color(0.58f, 0.61f, 0.65f, 0.64f));
        icon.anchorMin = new Vector2(0.01f, 0.01f);
        icon.anchorMax = new Vector2(0.99f, 0.99f);
        icon.offsetMin = Vector2.zero;
        icon.offsetMax = Vector2.zero;
        icon.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var iconImage = icon.GetComponent<Image>();
        iconImage.sprite = sprite;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        icon.SetSiblingIndex(Mathf.Min(2, button.transform.childCount - 1));

        // The side label already carries the dungeon number and name. Removing the
        // duplicate center numeral gives the generated node art a clean 58-68px read.
        for (var i = 0; i < button.transform.childCount; i++)
        {
            var child = button.transform.GetChild(i);
            var numberLabel = child.GetComponent<Text>();
            int ignored;
            if (numberLabel != null && int.TryParse(numberLabel.text, out ignored))
            {
                numberLabel.gameObject.SetActive(false);
                break;
            }
        }
        return true;
    }

    private static DungeonRouteVisualType DungeonRouteVisualTypeForNumber(int dungeonNumber)
    {
        switch (Mathf.Abs(dungeonNumber) % 5)
        {
            case 0: return DungeonRouteVisualType.Boss;
            case 4: return DungeonRouteVisualType.Elite;
            case 3: return DungeonRouteVisualType.Rest;
            case 2: return DungeonRouteVisualType.Shop;
            default: return DungeonRouteVisualType.Combat;
        }
    }

    private static bool TryNamedDungeonRouteVisualType(string buttonName, out DungeonRouteVisualType type)
    {
        type = DungeonRouteVisualType.Combat;
        if (string.IsNullOrEmpty(buttonName))
        {
            return false;
        }

        var normalized = buttonName.ToLowerInvariant();
        if (!normalized.Contains("route") && !normalized.Contains("node"))
        {
            return false;
        }
        if (normalized.Contains("boss")) type = DungeonRouteVisualType.Boss;
        else if (normalized.Contains("elite")) type = DungeonRouteVisualType.Elite;
        else if (normalized.Contains("shop")) type = DungeonRouteVisualType.Shop;
        else if (normalized.Contains("rest")) type = DungeonRouteVisualType.Rest;
        else if (normalized.Contains("combat")) type = DungeonRouteVisualType.Combat;
        else return false;
        return true;
    }

    private static string DungeonRouteVisualKey(DungeonRouteVisualType type)
    {
        switch (type)
        {
            case DungeonRouteVisualType.Elite: return "elite";
            case DungeonRouteVisualType.Shop: return "shop";
            case DungeonRouteVisualType.Rest: return "rest";
            case DungeonRouteVisualType.Boss: return "boss";
            default: return "combat";
        }
    }

    private static Color DungeonRouteVisualAccent(DungeonRouteVisualType type)
    {
        switch (type)
        {
            case DungeonRouteVisualType.Elite: return new Color32(164, 103, 232, 255);
            case DungeonRouteVisualType.Shop: return new Color32(52, 145, 218, 255);
            case DungeonRouteVisualType.Rest: return new Color32(240, 155, 58, 255);
            case DungeonRouteVisualType.Boss: return new Color32(219, 69, 62, 255);
            default: return new Color32(72, 177, 220, 255);
        }
    }

    private void ApplyCharacterGroundingIfNeeded(Image image)
    {
        if (image == null || image.sprite == null || image.GetComponent<UiCharacterGroundingPresenter>() != null)
        {
            return;
        }

        var imageName = image.gameObject.name ?? "";
        var isCombatCharacter = imageName == "Hero Sprite Visual" || imageName == "Enemy Sprite Visual";
        if (!isCombatCharacter && imageName != "Character Portrait Visual")
        {
            return;
        }

        var host = FindCharacterGroundingHost(image.transform);
        if (host == null)
        {
            return;
        }

        var accent = CharacterPresentationAccent(image.sprite);
        var outline = image.GetComponent<Outline>();
        if (outline == null)
        {
            outline = image.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(accent.r, accent.g, accent.b, isCombatCharacter ? 0.68f : 0.58f);
        outline.effectDistance = imageName == "Enemy Sprite Visual"
            ? new Vector2(-2.6f, -2.6f)
            : new Vector2(2.6f, -2.6f);
        outline.useGraphicAlpha = true;

        if (host.Find("Character Grounding Shadow") == null && host.Find("Combat Ground Shadow") == null)
        {
            var shadow = AddFlatPanel("Character Grounding Shadow", host, new Color(0.025f, 0.045f, 0.065f, 0.28f));
            shadow.anchorMin = new Vector2(0.18f, 0.025f);
            shadow.anchorMax = new Vector2(0.82f, 0.17f);
            shadow.offsetMin = Vector2.zero;
            shadow.offsetMax = Vector2.zero;
            shadow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var shadowImage = shadow.GetComponent<Image>();
            shadowImage.sprite = CombatGlowSprite();
            shadowImage.raycastTarget = false;
            shadow.SetAsFirstSibling();
        }

        Image fogImage = null;
        var existingFog = host.Find("Character Shallow Fog");
        if (existingFog == null)
        {
            var fog = AddFlatPanel("Character Shallow Fog", host, new Color(accent.r, accent.g, accent.b, isCombatCharacter ? 0.13f : 0.11f));
            fog.anchorMin = new Vector2(0.05f, -0.02f);
            fog.anchorMax = new Vector2(0.95f, isCombatCharacter ? 0.30f : 0.25f);
            fog.offsetMin = Vector2.zero;
            fog.offsetMax = Vector2.zero;
            fog.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            fogImage = fog.GetComponent<Image>();
            fogImage.sprite = CombatGlowSprite();
            fogImage.raycastTarget = false;
            fog.SetAsLastSibling();
        }
        else
        {
            fogImage = existingFog.GetComponent<Image>();
        }

        var presenter = image.gameObject.AddComponent<UiCharacterGroundingPresenter>();
        presenter.Configure(this, image, host, fogImage, accent, image.rectTransform.localScale.x);
    }

    private RectTransform FindCharacterGroundingHost(Transform source)
    {
        var current = source == null ? null : source.parent;
        while (current != null && current != root)
        {
            var currentName = current.gameObject.name ?? "";
            if (currentName.StartsWith("Character Portrait ", StringComparison.Ordinal)
                || currentName == "Hero Combat Art Slot"
                || currentName == "Enemy Combat Art Slot")
            {
                return current as RectTransform;
            }
            current = current.parent;
        }
        return null;
    }

    private sealed class UiVisualPolishDriver : MonoBehaviour
    {
        private AetheriaGame owner;
        private Transform scope;
        private float nextScanTime;
        private int rapidScansRemaining;

        public void Configure(AetheriaGame sourceOwner, Transform sourceScope)
        {
            owner = sourceOwner;
            scope = sourceScope;
            // New screens are built over the current frame, so keep two short
            // follow-up passes. Afterwards the hierarchy is mostly static and a
            // Low-frequency maintenance still catches late hierarchy changes
            // without allocating three full component arrays every second.
            rapidScansRemaining = 2;
            Scan();
        }

        public void RequestRapidScan()
        {
            rapidScansRemaining = Mathf.Max(rapidScansRemaining, 2);
            nextScanTime = 0f;
        }

        private void LateUpdate()
        {
            if (owner == null || scope == null || Time.unscaledTime < nextScanTime)
            {
                return;
            }
            Scan();
        }

        private void Scan()
        {
            var interval = rapidScansRemaining > 0 ? 0.18f : 4f;
            nextScanTime = Time.unscaledTime + interval;
            if (rapidScansRemaining > 0)
            {
                rapidScansRemaining--;
            }

            var buttons = scope.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    continue;
                }
                owner.ApplyDungeonRouteIconIfNeeded(buttons[i]);
                owner.ApplyUniversalImageButtonState(buttons[i]);
            }
            owner.EnsureDefaultUiSelection(buttons);

            var labels = scope.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                owner.ApplyKoreanReadability(labels[i]);
            }

            var images = scope.GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                owner.EnforceTransparentReadabilityPlate(images[i]);
                owner.ApplyCharacterGroundingIfNeeded(images[i]);
            }
        }
    }

    private sealed class UiImageButtonStatePresenter : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private Button button;
        private RectTransform rect;
        private Image stateGlow;
        private CanvasGroup canvasGroup;
        private Vector3 baseScale;
        private float baseAlpha;
        private Color accent;
        private bool hovered;
        private bool pressed;
        private bool selected;

        public void Configure(Button sourceButton, Sprite stateSprite, Image.Type imageType, Color sourceAccent)
        {
            button = sourceButton;
            rect = sourceButton == null ? null : sourceButton.GetComponent<RectTransform>();
            accent = new Color(sourceAccent.r, sourceAccent.g, sourceAccent.b, 1f);
            if (button == null || rect == null)
            {
                return;
            }

            baseScale = rect.localScale;
            canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
            }
            baseAlpha = canvasGroup.alpha;

            var glowObject = new GameObject("Image Button State Glow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            glowObject.transform.SetParent(button.transform, false);
            var glowRect = glowObject.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-2f, -2f);
            glowRect.offsetMax = new Vector2(2f, 2f);
            glowObject.GetComponent<LayoutElement>().ignoreLayout = true;
            stateGlow = glowObject.GetComponent<Image>();
            stateGlow.sprite = stateSprite;
            stateGlow.type = stateSprite != null ? imageType : Image.Type.Simple;
            stateGlow.preserveAspect = false;
            stateGlow.raycastTarget = false;
            stateGlow.color = new Color(accent.r, accent.g, accent.b, 0f);
            glowRect.SetAsFirstSibling();
            Refresh(true);
        }

        public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
        public void OnPointerDown(PointerEventData eventData) { pressed = true; }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; }
        public void OnSelect(BaseEventData eventData) { selected = true; }
        public void OnDeselect(BaseEventData eventData) { selected = false; pressed = false; }

        private void LateUpdate()
        {
            Refresh(false);
        }

        private void OnDisable()
        {
            hovered = false;
            pressed = false;
            selected = false;
            if (rect != null)
            {
                rect.localScale = baseScale;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = baseAlpha;
            }
        }

        private void Refresh(bool immediate)
        {
            if (button == null || rect == null || stateGlow == null || canvasGroup == null)
            {
                return;
            }

            var disabled = !button.interactable;
            var active = !disabled && (hovered || selected);
            var targetScale = disabled ? 0.99f : pressed ? 0.965f : active ? 1.028f : 1f;
            var targetAlpha = disabled ? baseAlpha * 0.52f : baseAlpha;
            Color targetGlow;
            if (disabled)
            {
                targetGlow = new Color(0.42f, 0.46f, 0.50f, 0.12f);
            }
            else if (pressed)
            {
                targetGlow = new Color(1f, 1f, 1f, 0.30f);
            }
            else if (active)
            {
                var highlight = Color.Lerp(accent, Color.white, 0.34f);
                targetGlow = new Color(highlight.r, highlight.g, highlight.b, 0.22f);
            }
            else
            {
                targetGlow = new Color(accent.r, accent.g, accent.b, 0f);
            }

            var blend = immediate ? 1f : 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime);
            rect.localScale = Vector3.Lerp(rect.localScale, baseScale * targetScale, blend);
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, blend);
            stateGlow.color = Color.Lerp(stateGlow.color, targetGlow, blend);
        }
    }

    private sealed class UiCharacterGroundingPresenter : MonoBehaviour
    {
        private AetheriaGame owner;
        private Image characterImage;
        private RectTransform host;
        private Image fog;
        private Color fogColor;
        private float fallbackScale;

        public void Configure(
            AetheriaGame sourceOwner,
            Image sourceImage,
            RectTransform sourceHost,
            Image sourceFog,
            Color accent,
            float sourceFallbackScale)
        {
            owner = sourceOwner;
            characterImage = sourceImage;
            host = sourceHost;
            fog = sourceFog;
            fogColor = new Color(accent.r, accent.g, accent.b, sourceFog != null ? sourceFog.color.a : 0.11f);
            fallbackScale = Mathf.Max(0.01f, sourceFallbackScale);
            Refresh(true);
        }

        private void LateUpdate()
        {
            Refresh(false);
        }

        private void Refresh(bool immediate)
        {
            if (owner == null || characterImage == null || host == null)
            {
                return;
            }

            var targetScale = owner.CharacterPresentationScale(characterImage.sprite, fallbackScale);
            var hostWidth = Mathf.Max(1f, host.rect.width);
            var hostHeight = Mathf.Max(1f, host.rect.height);
            var textureAspect = characterImage.sprite != null
                ? characterImage.sprite.rect.height / Mathf.Max(1f, characterImage.sprite.rect.width)
                : 1.5f;
            var canvasHeight = Mathf.Min(hostHeight, hostWidth * textureAspect);
            var targetY = (targetScale - 1f) * canvasHeight * 0.46f - canvasHeight * 0.01f;
            var blend = immediate ? 1f : 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime);
            characterImage.rectTransform.localScale = Vector3.Lerp(
                characterImage.rectTransform.localScale,
                Vector3.one * targetScale,
                blend);
            var position = characterImage.rectTransform.anchoredPosition;
            position.y = Mathf.Lerp(position.y, targetY, blend);
            characterImage.rectTransform.anchoredPosition = position;

            if (fog != null)
            {
                var pulse = 0.88f + Mathf.Sin(Time.unscaledTime * 1.35f) * 0.12f;
                var targetFog = new Color(fogColor.r, fogColor.g, fogColor.b, fogColor.a * pulse);
                fog.color = Color.Lerp(fog.color, targetFog, blend);
            }
        }
    }

    private sealed class UiDungeonRouteNodeMarker : MonoBehaviour
    {
        public Color accent;
    }

    private sealed class UiKoreanReadabilityMarker : MonoBehaviour
    {
    }
}
