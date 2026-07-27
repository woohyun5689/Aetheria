using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed partial class AetheriaGame
{
    private const string TitleBackgroundV1Path = "UI/VisualRefresh/Title/title_background_v1";
    private const string TitlePortalRingPath = "UI/VisualRefresh/CombatFX/V2/fx_shockwave_ring";
    private const string TitlePortalFlashPath = "UI/VisualRefresh/CombatFX/V2/fx_contact_flash";

    private bool titleScreenActive;
    private bool titleScreenTransitioning;
    private float titleInputUnlockTime;
    private Coroutine titleScreenTransitionCoroutine;
    private UiTitleScreenMotion titleScreenMotion;
    private Font titleDisplayFont;

    private void ShowTitleScreen()
    {
        CancelTitleScreenPresentation();
        titleScreenActive = true;
        currentScreen = AetheriaScreen.MainMenu;
        ResetCombatState(true);
        selectedHeroClassName = "";
        activeSlot = Mathf.Clamp(activeSlot, 0, SaveSlotCount - 1);
        ClearRoot();

        var page = AddFlatPanel("Title Screen V1", root, Color.clear);
        Stretch(page, 0f, 0f, 0f, 0f);
        page.GetComponent<Image>().raycastTarget = false;

        var ambient = AddFlatPanel("Title Ambient Artwork", page, Color.clear);
        Stretch(ambient, 0f, 0f, 0f, 0f);
        ambient.GetComponent<Image>().raycastTarget = false;

        var portalGlow = AddTitleFx(
            ambient,
            "Title Portal Glow",
            null,
            new Vector2(0.505f, 0.475f),
            new Vector2(500f, 500f),
            new Color(1f, 0.78f, 0.30f, 0.12f),
            true);
        var portalRingOuter = AddTitleFx(
            ambient,
            "Title Portal Ring Outer",
            TitlePortalRingPath,
            new Vector2(0.505f, 0.475f),
            new Vector2(426f, 426f),
            new Color(0.68f, 0.91f, 1f, 0.13f),
            false);
        var portalRing = AddTitleFx(
            ambient,
            "Title Portal Ring",
            TitlePortalRingPath,
            new Vector2(0.505f, 0.475f),
            new Vector2(374f, 374f),
            new Color(1f, 0.88f, 0.49f, 0.25f),
            false);
        var portalCore = AddTitleFx(
            ambient,
            "Title Portal Core",
            TitlePortalFlashPath,
            new Vector2(0.505f, 0.475f),
            new Vector2(154f, 154f),
            new Color(1f, 0.94f, 0.72f, 0.16f),
            false);

        var sparkleAnchors = new[]
        {
            new Vector2(0.16f, 0.69f),
            new Vector2(0.24f, 0.46f),
            new Vector2(0.33f, 0.63f),
            new Vector2(0.39f, 0.39f),
            new Vector2(0.44f, 0.56f),
            new Vector2(0.56f, 0.57f),
            new Vector2(0.62f, 0.37f),
            new Vector2(0.68f, 0.65f),
            new Vector2(0.77f, 0.45f),
            new Vector2(0.86f, 0.71f)
        };
        var sparkles = new RectTransform[sparkleAnchors.Length];
        var sparkleImages = new Image[sparkleAnchors.Length];
        for (var i = 0; i < sparkleAnchors.Length; i++)
        {
            sparkles[i] = AddTitleSparkle(
                ambient,
                "Title Light Mote " + (i + 1),
                sparkleAnchors[i],
                8f + (i % 4) * 3f,
                i % 3 == 0 ? new Color(0.50f, 0.88f, 1f, 0.64f) : new Color(1f, 0.86f, 0.46f, 0.56f));
            sparkleImages[i] = sparkles[i].GetComponent<Image>();
        }

        var logo = AddFlatPanel("Title Logo Composition", page, Color.clear);
        SetAnchoredRect(logo, new Vector2(0.5f, 0.805f), new Vector2(1080f, 270f));
        logo.GetComponent<Image>().raycastTarget = false;
        var logoGroup = logo.gameObject.AddComponent<CanvasGroup>();

        var crestGlow = AddTitleFx(
            logo,
            "Title Crest Glow",
            null,
            new Vector2(0.18f, 0.51f),
            new Vector2(270f, 270f),
            new Color(0.46f, 0.84f, 1f, 0.16f),
            true);
        var crest = AddTitleFx(
            logo,
            "Title Crest",
            MainMenuCrestV2Path,
            new Vector2(0.18f, 0.51f),
            new Vector2(190f, 236f),
            Color.white,
            false);

        var title = AddText(logo, "AETHERIA", 96, FontStyle.Bold, Rgb(19, 48, 78), TextAnchor.MiddleCenter, 116f);
        SetAnchoredRect(title.rectTransform, new Vector2(0.62f, 0.65f), new Vector2(760f, 126f));
        title.font = TitleDisplayFont();
        title.resizeTextMinSize = 68;
        title.resizeTextMaxSize = 96;
        ConfigureTitleDisplayText(title, new Color(0.98f, 0.75f, 0.20f, 1f), new Color(0.01f, 0.08f, 0.16f, 0.60f), 2.1f);

        var subtitle = AddText(logo, "빛의 원정", 34, FontStyle.Bold, Rgb(132, 73, 14), TextAnchor.MiddleCenter, 50f);
        SetAnchoredRect(subtitle.rectTransform, new Vector2(0.62f, 0.30f), new Vector2(540f, 56f));
        subtitle.resizeTextMinSize = 27;
        subtitle.resizeTextMaxSize = 34;
        ConfigureTitleDisplayText(subtitle, new Color(1f, 0.98f, 0.88f, 0.96f), new Color(0.18f, 0.07f, 0.01f, 0.42f), 1.15f);

        var tagline = AddText(logo, "아홉 대륙을 잇는 빛의 여정", 20, FontStyle.Bold, Rgb(27, 66, 93), TextAnchor.MiddleCenter, 36f);
        SetAnchoredRect(tagline.rectTransform, new Vector2(0.62f, 0.10f), new Vector2(630f, 38f));
        tagline.resizeTextMinSize = 17;
        tagline.resizeTextMaxSize = 20;
        ConfigureTitleDisplayText(tagline, new Color(1f, 1f, 1f, 0.88f), new Color(0.01f, 0.05f, 0.10f, 0.52f), 0.9f);

        var prompt = AddFlatPanel("Title Start Prompt", page, Color.clear);
        SetAnchoredRect(prompt, new Vector2(0.5f, 0.075f), new Vector2(930f, 82f));
        prompt.GetComponent<Image>().raycastTarget = false;
        var promptGroup = prompt.gameObject.AddComponent<CanvasGroup>();
        AddTitlePromptOrnament(prompt, -1f);
        AddTitlePromptOrnament(prompt, 1f);
        var promptText = AddText(prompt, "아무 키나 눌러 시작", 28, FontStyle.Bold, Rgb(16, 43, 69), TextAnchor.MiddleCenter, 76f);
        Stretch(promptText.rectTransform, 250f, 0f, 250f, 0f);
        promptText.resizeTextMinSize = 23;
        promptText.resizeTextMaxSize = 28;
        ConfigureTitleDisplayText(promptText, new Color(1f, 0.98f, 0.90f, 0.96f), new Color(0.01f, 0.04f, 0.08f, 0.60f), 1.15f);

        var footer = AddText(page, "AETHERIA · 빛의 원정", 16, FontStyle.Bold, Rgba(35, 69, 94, 205), TextAnchor.MiddleRight, 26f);
        footer.rectTransform.anchorMin = new Vector2(0.70f, 0f);
        footer.rectTransform.anchorMax = new Vector2(0.98f, 0.05f);
        footer.rectTransform.offsetMin = Vector2.zero;
        footer.rectTransform.offsetMax = Vector2.zero;

        var transitionFlash = AddFlatPanel("Title Transition Flash", page, Color.clear);
        Stretch(transitionFlash, 0f, 0f, 0f, 0f);
        transitionFlash.GetComponent<Image>().raycastTarget = false;

        var inputSurface = AddFlatPanel("Title Input Surface", page, Color.clear);
        Stretch(inputSurface, 0f, 0f, 0f, 0f);
        var inputImage = inputSurface.GetComponent<Image>();
        inputImage.raycastTarget = true;
        var inputButton = inputSurface.gameObject.AddComponent<Button>();
        inputButton.transition = Selectable.Transition.None;
        inputButton.onClick.AddListener(RequestTitleAdvance);

        titleScreenMotion = page.gameObject.AddComponent<UiTitleScreenMotion>();
        titleScreenMotion.Configure(
            logo,
            logoGroup,
            crest,
            crestGlow,
            prompt,
            promptGroup,
            portalGlow,
            portalRingOuter,
            portalRing,
            portalCore,
            transitionFlash.GetComponent<Image>(),
            sparkles,
            sparkleImages);

        titleInputUnlockTime = Time.unscaledTime + 0.65f;
        ApplyVisualRefreshToScreen(page);
    }

    private RectTransform AddTitleFx(
        Transform parent,
        string objectName,
        string resourcePath,
        Vector2 anchor,
        Vector2 size,
        Color tint,
        bool radialGlow)
    {
        var effect = AddFlatPanel(objectName, parent, tint);
        SetAnchoredRect(effect, anchor, size);
        var image = effect.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = radialGlow ? CombatGlowSprite() : LoadGeneratedSprite(resourcePath, Vector4.zero);
        image.preserveAspect = true;
        return effect;
    }

    private RectTransform AddTitleSparkle(Transform parent, string objectName, Vector2 anchor, float size, Color tint)
    {
        var sparkle = AddFlatPanel(objectName, parent, tint);
        SetAnchoredRect(sparkle, anchor, new Vector2(size, size));
        var image = sparkle.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = MapCircleSprite();

        var vertical = AddFlatPanel("Spark Vertical", sparkle, new Color(tint.r, tint.g, tint.b, tint.a * 0.78f));
        vertical.anchorMin = vertical.anchorMax = new Vector2(0.5f, 0.5f);
        vertical.pivot = new Vector2(0.5f, 0.5f);
        vertical.sizeDelta = new Vector2(Mathf.Max(1.5f, size * 0.16f), size * 2.1f);
        vertical.GetComponent<Image>().raycastTarget = false;

        var horizontal = AddFlatPanel("Spark Horizontal", sparkle, new Color(tint.r, tint.g, tint.b, tint.a * 0.72f));
        horizontal.anchorMin = horizontal.anchorMax = new Vector2(0.5f, 0.5f);
        horizontal.pivot = new Vector2(0.5f, 0.5f);
        horizontal.sizeDelta = new Vector2(size * 2.1f, Mathf.Max(1.5f, size * 0.16f));
        horizontal.GetComponent<Image>().raycastTarget = false;
        return sparkle;
    }

    private void AddTitlePromptOrnament(RectTransform parent, float direction)
    {
        var line = AddFlatPanel(direction < 0f ? "Prompt Line Left" : "Prompt Line Right", parent, Rgba(197, 139, 38, 220));
        line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
        line.pivot = new Vector2(direction < 0f ? 1f : 0f, 0.5f);
        line.anchoredPosition = new Vector2(direction * 184f, 0f);
        line.sizeDelta = new Vector2(212f, 2f);
        line.GetComponent<Image>().raycastTarget = false;

        var gem = AddFlatPanel(direction < 0f ? "Prompt Gem Left" : "Prompt Gem Right", parent, Rgba(77, 184, 222, 245));
        gem.anchorMin = gem.anchorMax = new Vector2(0.5f, 0.5f);
        gem.pivot = new Vector2(0.5f, 0.5f);
        gem.anchoredPosition = new Vector2(direction * 406f, 0f);
        gem.sizeDelta = new Vector2(10f, 10f);
        gem.localEulerAngles = new Vector3(0f, 0f, 45f);
        gem.GetComponent<Image>().raycastTarget = false;
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        var layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = rect.gameObject.AddComponent<LayoutElement>();
        }
        layout.ignoreLayout = true;
    }

    private Font TitleDisplayFont()
    {
        if (titleDisplayFont == null)
        {
            titleDisplayFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Cinzel SemiBold", "Georgia", "Times New Roman", "Malgun Gothic" },
                96);
        }
        return titleDisplayFont != null ? titleDisplayFont : uiFont;
    }

    private static void ConfigureTitleDisplayText(Text text, Color outlineColor, Color shadowColor, float outlineSize)
    {
        if (text == null)
        {
            return;
        }

        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var outline = text.GetComponent<Outline>();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(outlineSize, -outlineSize);
        outline.useGraphicAlpha = true;

        var shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(2.8f, -3.2f);
        shadow.useGraphicAlpha = true;
    }

    private void RequestTitleAdvance()
    {
        if (!titleScreenActive
            || titleScreenTransitioning
            || Time.unscaledTime < titleInputUnlockTime)
        {
            return;
        }

        titleScreenTransitioning = true;
        titleScreenTransitionCoroutine = StartCoroutine(PlayTitleScreenExit());
    }

    private IEnumerator PlayTitleScreenExit()
    {
        const float duration = 0.48f;
        var elapsed = 0f;
        while (elapsed < duration && titleScreenActive)
        {
            elapsed += Mathf.Max(0f, Time.unscaledDeltaTime);
            var progress = Mathf.Clamp01(elapsed / duration);
            if (titleScreenMotion != null)
            {
                titleScreenMotion.SetExitProgress(progress);
            }
            yield return null;
        }

        titleScreenTransitionCoroutine = null;
        titleScreenActive = false;
        titleScreenTransitioning = false;
        ShowMainMenu();
    }

    private void CancelTitleScreenPresentation()
    {
        if (titleScreenTransitionCoroutine != null)
        {
            StopCoroutine(titleScreenTransitionCoroutine);
            titleScreenTransitionCoroutine = null;
        }
        titleScreenActive = false;
        titleScreenTransitioning = false;
        titleScreenMotion = null;
    }

    private bool TitleAdvancePressed()
    {
        if (!titleScreenActive || titleScreenTransitioning)
        {
            return false;
        }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
        if (Gamepad.current != null
            && (Gamepad.current.buttonSouth.wasPressedThisFrame
                || Gamepad.current.startButton.wasPressedThisFrame))
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.anyKeyDown || Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private void ReleaseTitleScreenResources()
    {
        CancelTitleScreenPresentation();
        if (titleDisplayFont != null)
        {
            Destroy(titleDisplayFont);
            titleDisplayFont = null;
        }
    }

    private sealed class UiTitleScreenMotion : MonoBehaviour
    {
        private RectTransform logo;
        private CanvasGroup logoGroup;
        private RectTransform crest;
        private RectTransform crestGlow;
        private RectTransform prompt;
        private CanvasGroup promptGroup;
        private RectTransform portalGlow;
        private RectTransform portalRingOuter;
        private RectTransform portalRing;
        private RectTransform portalCore;
        private Image transitionFlash;
        private RectTransform[] sparkles;
        private Image[] sparkleImages;
        private Vector2 logoRest;
        private Vector2 promptRest;
        private Vector2[] sparkleRest;
        private Color[] sparkleColors;
        private Color portalGlowColor;
        private Color portalRingOuterColor;
        private Color portalRingColor;
        private Color portalCoreColor;
        private float elapsed;
        private float exitProgress;

        public void Configure(
            RectTransform logoValue,
            CanvasGroup logoGroupValue,
            RectTransform crestValue,
            RectTransform crestGlowValue,
            RectTransform promptValue,
            CanvasGroup promptGroupValue,
            RectTransform portalGlowValue,
            RectTransform portalRingOuterValue,
            RectTransform portalRingValue,
            RectTransform portalCoreValue,
            Image transitionFlashValue,
            RectTransform[] sparkleValues,
            Image[] sparkleImageValues)
        {
            logo = logoValue;
            logoGroup = logoGroupValue;
            crest = crestValue;
            crestGlow = crestGlowValue;
            prompt = promptValue;
            promptGroup = promptGroupValue;
            portalGlow = portalGlowValue;
            portalRingOuter = portalRingOuterValue;
            portalRing = portalRingValue;
            portalCore = portalCoreValue;
            transitionFlash = transitionFlashValue;
            sparkles = sparkleValues;
            sparkleImages = sparkleImageValues;
            logoRest = logo != null ? logo.anchoredPosition : Vector2.zero;
            promptRest = prompt != null ? prompt.anchoredPosition : Vector2.zero;

            sparkleRest = new Vector2[sparkles != null ? sparkles.Length : 0];
            sparkleColors = new Color[sparkleImages != null ? sparkleImages.Length : 0];
            for (var i = 0; i < sparkleRest.Length; i++)
            {
                sparkleRest[i] = sparkles[i] != null ? sparkles[i].anchoredPosition : Vector2.zero;
            }
            for (var i = 0; i < sparkleColors.Length; i++)
            {
                sparkleColors[i] = sparkleImages[i] != null ? sparkleImages[i].color : Color.clear;
            }

            portalGlowColor = ColorOf(portalGlow);
            portalRingOuterColor = ColorOf(portalRingOuter);
            portalRingColor = ColorOf(portalRing);
            portalCoreColor = ColorOf(portalCore);
            ApplyStaticState(Application.isBatchMode ? 1f : 0f);
        }

        public void SetExitProgress(float progress)
        {
            exitProgress = Mathf.Clamp01(progress);
        }

        private void Update()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            elapsed += Mathf.Min(Mathf.Max(0f, Time.unscaledDeltaTime), 0.1f);
            var intro = EaseOut(Mathf.Clamp01(elapsed / 0.92f));
            var promptIntro = EaseOut(Mathf.Clamp01((elapsed - 0.56f) / 0.54f));
            var exitFade = 1f - EaseIn(exitProgress);

            if (logoGroup != null)
            {
                logoGroup.alpha = intro * exitFade;
            }
            if (logo != null)
            {
                logo.anchoredPosition = logoRest + new Vector2(0f, Mathf.Lerp(-22f, 0f, intro));
                logo.localScale = Vector3.one * Mathf.Lerp(0.965f, 1f, intro);
            }
            if (crest != null)
            {
                var pulse = 1f + Mathf.Sin(elapsed * 1.35f) * 0.016f;
                crest.localScale = Vector3.one * pulse;
                crest.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(elapsed * 0.52f) * 1.1f);
            }
            if (crestGlow != null)
            {
                crestGlow.localScale = Vector3.one * (1.02f + Mathf.Sin(elapsed * 1.24f + 0.6f) * 0.035f);
                SetAlpha(crestGlow, portalGlowColor.a * (0.76f + Mathf.Sin(elapsed * 1.18f) * 0.22f) * exitFade);
            }

            if (promptGroup != null)
            {
                var pulse = 0.68f + (Mathf.Sin(elapsed * 2.35f) * 0.5f + 0.5f) * 0.32f;
                promptGroup.alpha = promptIntro * pulse * exitFade;
            }
            if (prompt != null)
            {
                prompt.anchoredPosition = promptRest + new Vector2(0f, Mathf.Sin(elapsed * 1.45f) * 2f);
            }

            AnimatePortal(portalGlow, portalGlowColor, 0.92f, 0.055f, 0f, exitFade);
            AnimatePortal(portalRingOuter, portalRingOuterColor, 0.78f, 0.035f, -2.6f, exitFade);
            AnimatePortal(portalRing, portalRingColor, 1.30f, 0.028f, 3.5f, exitFade);
            AnimatePortal(portalCore, portalCoreColor, 1.58f, 0.10f, -1.2f, exitFade);

            for (var i = 0; i < sparkleRest.Length; i++)
            {
                var sparkle = sparkles[i];
                var image = sparkleImages[i];
                if (sparkle == null || image == null)
                {
                    continue;
                }

                var phase = elapsed * (0.58f + (i % 4) * 0.13f) + i * 0.83f;
                sparkle.anchoredPosition = sparkleRest[i] + new Vector2(Mathf.Sin(phase * 0.74f) * 4f, Mathf.Sin(phase) * 7f);
                sparkle.localEulerAngles = new Vector3(0f, 0f, phase * 7f);
                sparkle.localScale = Vector3.one * (0.82f + (Mathf.Sin(phase * 1.47f) * 0.5f + 0.5f) * 0.38f);
                var color = sparkleColors[i];
                color.a *= intro * (0.28f + (Mathf.Sin(phase * 1.73f) * 0.5f + 0.5f) * 0.72f) * exitFade;
                image.color = color;
            }

            if (transitionFlash != null)
            {
                transitionFlash.color = new Color(1f, 0.94f, 0.72f, Mathf.Pow(exitProgress, 2.2f) * 0.72f);
            }
        }

        private void ApplyStaticState(float value)
        {
            if (logoGroup != null)
            {
                logoGroup.alpha = value;
            }
            if (promptGroup != null)
            {
                promptGroup.alpha = value;
            }
            if (transitionFlash != null)
            {
                transitionFlash.color = Color.clear;
            }
            for (var i = 0; i < sparkleImages.Length; i++)
            {
                if (sparkleImages[i] == null)
                {
                    continue;
                }
                var color = sparkleColors[i];
                color.a *= value;
                sparkleImages[i].color = color;
            }
        }

        private void AnimatePortal(RectTransform target, Color baseColor, float speed, float scaleAmount, float degreesPerSecond, float fade)
        {
            if (target == null)
            {
                return;
            }

            var pulse = Mathf.Sin(elapsed * speed) * 0.5f + 0.5f;
            target.localScale = Vector3.one * (1f + pulse * scaleAmount + exitProgress * 0.34f);
            target.localEulerAngles = new Vector3(0f, 0f, elapsed * degreesPerSecond);
            SetAlpha(target, baseColor.a * (0.72f + pulse * 0.28f) * fade);
        }

        private static Color ColorOf(RectTransform target)
        {
            var image = target != null ? target.GetComponent<Image>() : null;
            return image != null ? image.color : Color.clear;
        }

        private static void SetAlpha(RectTransform target, float alpha)
        {
            var image = target != null ? target.GetComponent<Image>() : null;
            if (image == null)
            {
                return;
            }
            var color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        private static float EaseOut(float value)
        {
            value = Mathf.Clamp01(value);
            var inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseIn(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value;
        }
    }
}
