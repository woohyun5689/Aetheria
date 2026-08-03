using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private const string MainMenuCrestV2Path = "UI/VisualRefresh/MainMenu/main_crest_v2";
    private const string MainMenuSaveSlotV2Path = "UI/VisualRefresh/MainMenu/save_slot_v2";
    private const string MainMenuActionButtonV2Path = "UI/VisualRefresh/MainMenu/action_button_v2";
    private const string MainMenuSectionBannerV2Path = "UI/VisualRefresh/MainMenu/section_banner_v2";

    private void AddMainMenuAmbientEffects(RectTransform frame)
    {
        if (frame == null)
        {
            return;
        }

        var layer = AddFlatPanel("Main Menu Ambient Artwork", frame, Color.clear);
        Stretch(layer, 0f, 0f, 0f, 0f);
        layer.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        layer.GetComponent<Image>().raycastTarget = false;
        layer.SetAsFirstSibling();

        var anchors = new[]
        {
            new Vector2(0.08f, 0.30f),
            new Vector2(0.15f, 0.74f),
            new Vector2(0.26f, 0.53f),
            new Vector2(0.74f, 0.56f),
            new Vector2(0.84f, 0.76f),
            new Vector2(0.92f, 0.32f)
        };
        var effects = new RectTransform[anchors.Length];
        var images = new Image[anchors.Length];
        for (var i = 0; i < anchors.Length; i++)
        {
            var blue = i % 2 == 0;
            var effect = AddFlatPanel(
                "Main Menu Crystal Glow " + (i + 1),
                layer,
                blue
                    ? new Color(0.34f, 0.81f, 1f, 0.075f)
                    : new Color(1f, 0.82f, 0.38f, 0.065f));
            SetAnchoredRect(effect, anchors[i], new Vector2(150f + (i % 3) * 34f, 150f + (i % 3) * 34f));
            var image = effect.GetComponent<Image>();
            image.sprite = CombatGlowSprite();
            image.raycastTarget = false;
            effects[i] = effect;
            images[i] = image;
        }

        var presenter = layer.gameObject.AddComponent<UiMainMenuAmbientMotion>();
        presenter.Configure(effects, images);
        AttachScreenParallax(layer, 8f, -1f);
    }

    private void AddMainMenuTitleStage(Transform parent)
    {
        var stage = AddFlatPanel("Main Menu Title Stage", parent, Color.clear);
        ConstrainLayoutSize(stage, 1060f, 190f);
        stage.GetComponent<Image>().raycastTarget = false;

        var stageLayout = stage.gameObject.AddComponent<HorizontalLayoutGroup>();
        stageLayout.spacing = 18f;
        stageLayout.padding = new RectOffset(24, 24, 0, 0);
        stageLayout.childAlignment = TextAnchor.MiddleCenter;
        stageLayout.childControlWidth = true;
        stageLayout.childControlHeight = true;
        stageLayout.childForceExpandWidth = false;
        stageLayout.childForceExpandHeight = false;

        var crest = AddFlatPanel("Main Menu Crest V2", stage, Color.white);
        AddLayoutSize(crest, 146f, 184f);
        var crestImage = crest.GetComponent<Image>();
        crestImage.sprite = LoadGeneratedSprite(MainMenuCrestV2Path, Vector4.zero);
        crestImage.preserveAspect = true;
        crestImage.raycastTarget = false;
        crest.gameObject.AddComponent<UiMainMenuCrestMotion>();

        var copy = AddFlatPanel("Main Menu Title Copy", stage, Color.clear);
        AddLayoutSize(copy, 760f, 184f);
        copy.GetComponent<Image>().raycastTarget = false;
        AddVertical(copy, 0, TextAnchor.MiddleCenter, new RectOffset(0, 0, 4, 4));

        var title = AddText(copy, "AETHERIA", 72, FontStyle.Bold, Rgb(22, 48, 74), TextAnchor.MiddleCenter, 86f);
        title.font = TitleDisplayFont();
        ConfigureMainMenuText(title, Rgb(22, 48, 74), new Color(1f, 0.91f, 0.56f, 0.96f), 1.35f);

        var subtitle = AddText(copy, "빛의 원정", 31, FontStyle.Bold, Rgb(121, 72, 19), TextAnchor.MiddleCenter, 42f);
        ConfigureMainMenuText(subtitle, Rgb(121, 72, 19), new Color(1f, 0.98f, 0.88f, 0.96f), 1.05f);

        var tagline = AddText(copy, "영웅을 선택하고 아에테리아의 원정을 이어가세요", 19, FontStyle.Bold, Rgb(24, 55, 78), TextAnchor.MiddleCenter, 32f);
        ConfigureMainMenuText(tagline, Rgb(24, 55, 78), new Color(1f, 1f, 1f, 0.92f), 0.9f);
    }

    private RectTransform AddMainMenuSectionBanner(Transform parent, string label)
    {
        var banner = AddFlatPanel("Main Menu Section Artwork Image", parent, Color.white);
        ConstrainLayoutSize(banner, 680f, 84f);
        var image = banner.GetComponent<Image>();
        image.sprite = LoadGeneratedSprite(MainMenuSectionBannerV2Path, Vector4.zero);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;

        var text = AddText(banner, label, 30, FontStyle.Bold, Rgb(26, 51, 73), TextAnchor.MiddleCenter, 84f);
        Stretch(text.GetComponent<RectTransform>(), 82f, 8f, 82f, 8f);
        ConfigureMainMenuText(text, Rgb(26, 51, 73), new Color(1f, 1f, 1f, 0.88f), 0.95f);
        return banner;
    }

    private Button AddMainMenuSaveSlotButton(
        Transform parent,
        string label,
        Action onClick,
        bool selected,
        float preferredWidth,
        float preferredHeight)
    {
        return AddMainMenuImageButton(
            parent,
            selected ? "Selected Adventure Journal" : "Adventure Journal",
            label,
            onClick,
            selected ? goldColor : manaColor,
            MainMenuSaveSlotV2Path,
            preferredWidth,
            preferredHeight,
            true,
            selected);
    }

    private Button AddMainMenuActionButton(
        Transform parent,
        string objectName,
        string label,
        Action onClick,
        Color accent,
        float preferredWidth,
        float preferredHeight)
    {
        return AddMainMenuImageButton(
            parent,
            objectName,
            label,
            onClick,
            accent,
            MainMenuActionButtonV2Path,
            preferredWidth,
            preferredHeight,
            false,
            false);
    }

    private Button AddMainMenuImageButton(
        Transform parent,
        string objectName,
        string label,
        Action onClick,
        Color accent,
        string resourcePath,
        float preferredWidth,
        float preferredHeight,
        bool saveSlot,
        bool selected)
    {
        var button = AddButton(parent, label, onClick, accent);
        button.gameObject.name = objectName;
        var motionPresenter = button.gameObject.AddComponent<UiMainMenuImageButton>();

        var rect = button.GetComponent<RectTransform>();
        ConstrainLayoutSize(rect, preferredWidth, preferredHeight);

        var image = button.GetComponent<Image>();
        var sprite = LoadGeneratedSprite(resourcePath, Vector4.zero);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }
        image.color = selected
            ? Color.Lerp(Color.white, new Color(goldColor.r, goldColor.g, goldColor.b, 1f), 0.10f)
            : Color.white;
        image.raycastTarget = true;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.Lerp(Color.white, new Color(accent.r, accent.g, accent.b, 1f), 0.08f);
        colors.pressedColor = Color.Lerp(Color.white, new Color(accent.r, accent.g, accent.b, 1f), 0.18f);
        colors.selectedColor = Color.Lerp(Color.white, new Color(goldColor.r, goldColor.g, goldColor.b, 1f), 0.12f);
        colors.disabledColor = Rgb(185, 192, 198);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.10f;
        button.colors = colors;

        var rootOutline = button.GetComponent<Outline>();
        if (rootOutline != null)
        {
            rootOutline.enabled = false;
        }

        var labelText = DirectButtonLabel(rect);
        if (labelText != null)
        {
            var textColor = Color.Lerp(Rgb(27, 49, 67), new Color(accent.r, accent.g, accent.b, 1f), saveSlot ? 0.08f : 0.18f);
            var disabledTextColor = Rgb(77, 84, 91);
            var labelRect = labelText.GetComponent<RectTransform>();
            if (saveSlot)
            {
                var lineCount = string.IsNullOrEmpty(labelText.text) ? 1 : labelText.text.Split('\n').Length;
                var saveFontSize = lineCount >= 4 ? 17 : 18;
                labelRect.anchorMin = new Vector2(0.31f, 0.12f);
                labelRect.anchorMax = new Vector2(0.93f, 0.88f);
                labelRect.offsetMin = new Vector2(4f, 1f);
                labelRect.offsetMax = new Vector2(-5f, -1f);
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.fontSize = saveFontSize;
                labelText.resizeTextMinSize = 14;
                labelText.resizeTextMaxSize = saveFontSize;
                labelText.lineSpacing = lineCount >= 4 ? 1.02f : 1.08f;
            }
            else
            {
                labelRect.anchorMin = new Vector2(0.12f, 0.12f);
                labelRect.anchorMax = new Vector2(0.88f, 0.88f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.fontSize = 21;
                labelText.resizeTextMinSize = 17;
                labelText.resizeTextMaxSize = 21;
            }

            ConfigureMainMenuText(labelText, textColor, new Color(1f, 1f, 1f, 0.84f), 0.75f);
            var textPresenter = button.GetComponent<UiButtonTextPresenter>();
            if (textPresenter != null)
            {
                textPresenter.Configure(button, labelText, textColor, disabledTextColor);
            }
        }

        motionPresenter.Configure(button, sprite, image.type, accent, saveSlot, selected);
        return button;
    }

    private Text AddMainMenuHint(Transform parent, string value)
    {
        var hint = AddText(parent, value, 20, FontStyle.Bold, Rgb(25, 52, 73), TextAnchor.MiddleCenter, 38f);
        ConstrainLayoutSize(hint.GetComponent<RectTransform>(), 1080f, 38f);
        ConfigureMainMenuText(hint, Rgb(25, 52, 73), new Color(1f, 1f, 1f, 0.92f), 0.9f);
        return hint;
    }

    private static Text DirectButtonLabel(RectTransform rect)
    {
        if (rect == null)
        {
            return null;
        }

        for (var i = 0; i < rect.childCount; i++)
        {
            var label = rect.GetChild(i).GetComponent<Text>();
            if (label != null)
            {
                return label;
            }
        }
        return null;
    }

    private static void ConfigureMainMenuText(Text label, Color textColor, Color edgeColor, float edgeSize)
    {
        if (label == null)
        {
            return;
        }

        label.color = new Color(textColor.r, textColor.g, textColor.b, 1f);
        var outline = label.GetComponent<Outline>();
        if (outline == null)
        {
            outline = label.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = edgeColor;
        outline.effectDistance = new Vector2(edgeSize, -edgeSize);
        outline.useGraphicAlpha = true;

        var effects = label.GetComponents<Shadow>();
        for (var i = 0; i < effects.Length; i++)
        {
            if (!(effects[i] is Outline))
            {
                effects[i].enabled = false;
            }
        }
    }

    private sealed class UiMainMenuImageButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private Button button;
        private RectTransform rect;
        private Image glow;
        private Vector3 baseScale = Vector3.one;
        private Color accent;
        private bool saveSlot;
        private bool selectedSlot;
        private bool hovered;
        private bool pressed;
        private bool keyboardSelected;

        public void Configure(
            Button sourceButton,
            Sprite stateSprite,
            Image.Type imageType,
            Color sourceAccent,
            bool isSaveSlot,
            bool isSelectedSlot)
        {
            button = sourceButton;
            rect = sourceButton != null ? sourceButton.GetComponent<RectTransform>() : null;
            accent = new Color(sourceAccent.r, sourceAccent.g, sourceAccent.b, 1f);
            saveSlot = isSaveSlot;
            selectedSlot = isSelectedSlot;
            if (button == null || rect == null)
            {
                return;
            }

            baseScale = rect.localScale;
            var glowObject = new GameObject("Main Menu Button Glow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            glowObject.transform.SetParent(button.transform, false);
            var glowRect = glowObject.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-4f, -4f);
            glowRect.offsetMax = new Vector2(4f, 4f);
            glowObject.GetComponent<LayoutElement>().ignoreLayout = true;
            glow = glowObject.GetComponent<Image>();
            glow.sprite = stateSprite;
            glow.type = stateSprite != null ? imageType : Image.Type.Simple;
            glow.preserveAspect = false;
            glow.raycastTarget = false;
            glow.color = new Color(accent.r, accent.g, accent.b, selectedSlot ? 0.08f : 0f);
            glowRect.SetAsFirstSibling();
            Refresh(true);
        }

        public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
        public void OnPointerDown(PointerEventData eventData) { pressed = true; }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; }
        public void OnSelect(BaseEventData eventData) { keyboardSelected = true; }
        public void OnDeselect(BaseEventData eventData) { keyboardSelected = false; pressed = false; }

        private void LateUpdate()
        {
            Refresh(false);
        }

        private void OnDisable()
        {
            hovered = false;
            pressed = false;
            keyboardSelected = false;
            if (rect != null)
            {
                rect.localScale = baseScale;
            }
        }

        private void Refresh(bool immediate)
        {
            if (button == null || rect == null || glow == null)
            {
                return;
            }

            var disabled = !button.interactable;
            var active = !disabled && (hovered || keyboardSelected);
            var idleSelectedPulse = selectedSlot
                ? Mathf.Sin(Time.unscaledTime * 1.55f) * 0.5f + 0.5f
                : 0f;
            var hoverScale = saveSlot ? 1.012f : 1.020f;
            var targetScale = disabled ? 0.992f : pressed ? 0.975f : active ? hoverScale : 1f;
            var targetGlowAlpha = disabled
                ? 0.025f
                : pressed
                    ? 0.18f
                    : active
                        ? (saveSlot ? 0.13f : 0.17f)
                        : selectedSlot
                            ? Mathf.Lerp(0.075f, 0.125f, idleSelectedPulse)
                            : 0f;
            var highlight = Color.Lerp(accent, Color.white, active ? 0.34f : 0.12f);
            var targetGlow = new Color(highlight.r, highlight.g, highlight.b, targetGlowAlpha);
            var blend = immediate ? 1f : 1f - Mathf.Exp(-19f * Mathf.Min(Time.unscaledDeltaTime, 0.1f));
            rect.localScale = Vector3.Lerp(rect.localScale, baseScale * targetScale, blend);
            glow.color = Color.Lerp(glow.color, targetGlow, blend);
        }
    }

    private sealed class UiMainMenuCrestMotion : MonoBehaviour
    {
        private RectTransform rect;
        private Vector3 baseScale;

        private void Awake()
        {
            rect = transform as RectTransform;
            baseScale = rect != null ? rect.localScale : Vector3.one;
        }

        private void LateUpdate()
        {
            if (Application.isBatchMode || rect == null)
            {
                return;
            }
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 1.32f) * 0.018f;
            rect.localScale = baseScale * pulse;
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(Time.unscaledTime * 0.58f) * 0.75f);
        }

        private void OnDisable()
        {
            if (rect != null)
            {
                rect.localScale = baseScale;
                rect.localEulerAngles = Vector3.zero;
            }
        }
    }

    private sealed class UiMainMenuAmbientMotion : MonoBehaviour
    {
        private RectTransform[] effects;
        private Image[] images;
        private Vector2[] restPositions;
        private Color[] baseColors;

        public void Configure(RectTransform[] effectValues, Image[] imageValues)
        {
            effects = effectValues;
            images = imageValues;
            restPositions = new Vector2[effects != null ? effects.Length : 0];
            baseColors = new Color[images != null ? images.Length : 0];
            for (var i = 0; i < restPositions.Length; i++)
            {
                restPositions[i] = effects[i] != null ? effects[i].anchoredPosition : Vector2.zero;
            }
            for (var i = 0; i < baseColors.Length; i++)
            {
                baseColors[i] = images[i] != null ? images[i].color : Color.clear;
            }
        }

        private void LateUpdate()
        {
            if (Application.isBatchMode || effects == null || images == null)
            {
                return;
            }

            for (var i = 0; i < effects.Length; i++)
            {
                if (effects[i] == null || images[i] == null)
                {
                    continue;
                }

                var phase = Time.unscaledTime * (0.52f + i * 0.055f) + i * 0.86f;
                effects[i].anchoredPosition = restPositions[i] + new Vector2(Mathf.Sin(phase) * 4f, Mathf.Sin(phase * 0.73f) * 7f);
                effects[i].localScale = Vector3.one * (0.96f + (Mathf.Sin(phase * 1.31f) * 0.5f + 0.5f) * 0.08f);
                var color = baseColors[i];
                color.a *= 0.74f + (Mathf.Sin(phase * 1.47f) * 0.5f + 0.5f) * 0.26f;
                images[i].color = color;
            }
        }
    }
}
