using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private UiCombatCardPresenter activeCombatCardTarget;
    private UiCombatCardPresenter hoveredCombatCardTarget;
    private UiCombatCardPresenter focusedCombatCardTarget;
    private RectTransform combatCardTargetRoot;
    private RectTransform combatCardTargetGlow;
    private RectTransform combatCardTargetCore;
    private RectTransform combatCardTargetRing;
    private Image combatCardTargetGlowImage;
    private Image combatCardTargetCoreImage;
    private Image combatCardTargetRingImage;
    private Texture2D combatCardTargetRingTexture;
    private Sprite combatCardTargetRingSprite;

    private void ConfigureCombatActionCardMotion(Button card, int cardIndex, int cardCount, string shortcut, Color accent)
    {
        if (card == null)
        {
            return;
        }

        var cardRect = card.GetComponent<RectTransform>();
        var shortcutPlate = AddFlatPanel(
            "Text Readability Plate Card Shortcut",
            cardRect,
            new Color(accent.r, accent.g, accent.b, VisualReadabilityPlateAlpha));
        shortcutPlate.anchorMin = new Vector2(1f, 1f);
        shortcutPlate.anchorMax = new Vector2(1f, 1f);
        shortcutPlate.pivot = new Vector2(1f, 1f);
        shortcutPlate.anchoredPosition = new Vector2(-9f, -9f);
        shortcutPlate.sizeDelta = new Vector2(34f, 27f);
        shortcutPlate.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        shortcutPlate.GetComponent<Image>().raycastTarget = false;

        var shortcutLabel = AddText(
            shortcutPlate,
            shortcut,
            14,
            FontStyle.Bold,
            textColor,
            TextAnchor.MiddleCenter,
            27f);
        Stretch(shortcutLabel.GetComponent<RectTransform>(), 2f, 0f, 2f, 0f);
        shortcutLabel.raycastTarget = false;

        var presenter = card.gameObject.AddComponent<UiCombatCardPresenter>();
        presenter.Configure(this, card, Mathf.Clamp(cardIndex, 0, 4), Mathf.Clamp(cardCount, 1, 5), accent);
    }

    private bool AddBasicAttackIconToActionCard(RectTransform card, bool enabled, Color accent)
    {
        if (card == null)
        {
            return false;
        }

        var iconObject = new GameObject(
            "Basic Attack Icon",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement));
        iconObject.transform.SetParent(card, false);
        var icon = iconObject.GetComponent<RectTransform>();
        var layout = iconObject.GetComponent<LayoutElement>();
        layout.ignoreLayout = true;
        icon.anchorMin = new Vector2(0f, 0.5f);
        icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0f, 0.5f);
        icon.anchoredPosition = new Vector2(16f, 0f);
        icon.sizeDelta = new Vector2(112f, 112f);

        var image = iconObject.GetComponent<Image>();
        var generatedSprite = LoadGeneratedSprite("UI/VisualRefresh/Combat/intent_attack", Vector4.zero);
        image.sprite = generatedSprite != null ? generatedSprite : CombatGlowSprite();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = generatedSprite != null
            ? (enabled ? Color.white : new Color(0.64f, 0.66f, 0.69f, 0.64f))
            : new Color(accent.r, accent.g, accent.b, enabled ? 0.34f : 0.18f);

        if (generatedSprite == null)
        {
            // A lightweight procedural slash keeps the card readable if the optional
            // attack-intent resource has not been imported yet.
            for (var index = 0; index < 3; index++)
            {
                var slash = AddFlatPanel(
                    "Basic Attack Fallback Slash " + index,
                    icon,
                    new Color(1f, 1f, 1f, enabled ? 0.88f : 0.42f));
                slash.anchorMin = new Vector2(0.5f, 0.5f);
                slash.anchorMax = new Vector2(0.5f, 0.5f);
                slash.pivot = new Vector2(0.5f, 0.5f);
                slash.sizeDelta = new Vector2(64f - index * 8f, 8f);
                slash.anchoredPosition = new Vector2(-10f + index * 10f, 11f - index * 11f);
                slash.localRotation = Quaternion.Euler(0f, 0f, 42f);
                slash.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                slash.GetComponent<Image>().raycastTarget = false;
            }
        }

        icon.SetAsLastSibling();
        return true;
    }

    private void SelectCombatCardWhenReady(Button card)
    {
        if (card == null
            || currentScreen != AetheriaScreen.Combat
            || combatPresentationPhase != CombatPresentationPhase.PlayerChoice
            || !card.gameObject.activeInHierarchy
            || !card.interactable
            || card.navigation.mode == Navigation.Mode.None
            || EventSystem.current == null)
        {
            return;
        }

        var selected = EventSystem.current.currentSelectedGameObject;
        var selectedButton = selected != null ? selected.GetComponentInParent<Button>() : null;
        if (selectedButton == null
            || selectedButton.GetComponent<UiCombatCardPresenter>() == null)
        {
            EventSystem.current.SetSelectedGameObject(card.gameObject);
        }
    }

    private bool CanShowCombatCardTarget(Button button)
    {
        return button != null
            && button.interactable
            && combatPresentationPhase == CombatPresentationPhase.PlayerChoice
            && !actionLocked
            && currentEnemy != null
            && combatEnemyPanel != null
            && combatFxLayer != null;
    }

    private void ShowCombatCardTarget(
        UiCombatCardPresenter presenter,
        Button button,
        RectTransform card,
        Color accent)
    {
        if (presenter == null || card == null || !CanShowCombatCardTarget(button))
        {
            HideCombatCardTarget(presenter);
            return;
        }

        EnsureCombatCardTargetVisuals();
        if (combatCardTargetRoot == null)
        {
            return;
        }

        activeCombatCardTarget = presenter;
        combatCardTargetGlowImage.color = new Color(accent.r, accent.g, accent.b, 0.18f);
        combatCardTargetCoreImage.color = new Color(
            Mathf.Lerp(accent.r, 1f, 0.46f),
            Mathf.Lerp(accent.g, 1f, 0.46f),
            Mathf.Lerp(accent.b, 1f, 0.46f),
            0.82f);
        combatCardTargetRingImage.color = new Color(accent.r, accent.g, accent.b, 0.78f);
        combatCardTargetRoot.gameObject.SetActive(true);
        RefreshCombatCardTarget(presenter, card);
    }

    private void SetCombatCardHovered(UiCombatCardPresenter presenter, bool hovered)
    {
        if (hovered)
        {
            hoveredCombatCardTarget = presenter;
        }
        else if (hoveredCombatCardTarget == presenter)
        {
            hoveredCombatCardTarget = null;
        }
        RefreshPreferredCombatCardTarget();
    }

    private void SetCombatCardFocused(UiCombatCardPresenter presenter, bool focused)
    {
        if (focused)
        {
            focusedCombatCardTarget = presenter;
        }
        else if (focusedCombatCardTarget == presenter)
        {
            focusedCombatCardTarget = null;
        }
        RefreshPreferredCombatCardTarget();
    }

    private void RemoveCombatCardTarget(UiCombatCardPresenter presenter)
    {
        if (hoveredCombatCardTarget == presenter)
        {
            hoveredCombatCardTarget = null;
        }
        if (focusedCombatCardTarget == presenter)
        {
            focusedCombatCardTarget = null;
        }
        RefreshPreferredCombatCardTarget();
    }

    private bool IsPreferredCombatCardTarget(UiCombatCardPresenter presenter)
    {
        return presenter != null
            && (hoveredCombatCardTarget != null
                ? hoveredCombatCardTarget == presenter
                : focusedCombatCardTarget == presenter);
    }

    private void RefreshPreferredCombatCardTarget()
    {
        var preferred = hoveredCombatCardTarget != null
            ? hoveredCombatCardTarget
            : focusedCombatCardTarget;
        if (preferred == null)
        {
            HideCombatCardTarget(null);
            return;
        }
        preferred.PresentTarget();
    }

    private void RefreshCombatCardTarget(UiCombatCardPresenter presenter, RectTransform card)
    {
        if (presenter == null
            || presenter != activeCombatCardTarget
            || card == null
            || combatCardTargetRoot == null
            || !combatCardTargetRoot.gameObject.activeSelf)
        {
            return;
        }

        var target = combatEnemyArt != null ? combatEnemyArt : combatEnemyPanel;
        if (target == null)
        {
            HideCombatCardTarget(presenter);
            return;
        }

        var startWorld = card.TransformPoint(new Vector3(card.rect.center.x, card.rect.yMax - 6f, 0f));
        var targetWorld = target.TransformPoint(new Vector3(target.rect.center.x, target.rect.center.y, 0f));
        var start = (Vector2)combatCardTargetRoot.InverseTransformPoint(startWorld);
        var end = (Vector2)combatCardTargetRoot.InverseTransformPoint(targetWorld);
        var direction = end - start;
        var distance = direction.magnitude;
        if (distance <= 1f)
        {
            return;
        }

        PositionCombatCardTargetLine(combatCardTargetGlow, start, end, 12f);
        PositionCombatCardTargetLine(combatCardTargetCore, start, end, 2.6f);
        combatCardTargetRing.anchoredPosition = end;
        var pulse = 1f + Mathf.Sin(Time.unscaledTime * 5.2f) * 0.045f;
        combatCardTargetRing.localScale = Vector3.one * pulse;

        var coreColor = combatCardTargetCoreImage.color;
        coreColor.a = 0.70f + Mathf.Sin(Time.unscaledTime * 6.4f) * 0.12f;
        combatCardTargetCoreImage.color = coreColor;
        var ringColor = combatCardTargetRingImage.color;
        ringColor.a = 0.66f + Mathf.Sin(Time.unscaledTime * 5.2f) * 0.12f;
        combatCardTargetRingImage.color = ringColor;
    }

    private static void PositionCombatCardTargetLine(
        RectTransform line,
        Vector2 start,
        Vector2 end,
        float thickness)
    {
        if (line == null)
        {
            return;
        }

        var direction = end - start;
        line.anchoredPosition = (start + end) * 0.5f;
        line.sizeDelta = new Vector2(direction.magnitude, thickness);
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private void HideCombatCardTarget(UiCombatCardPresenter presenter)
    {
        if (presenter != null && activeCombatCardTarget != presenter)
        {
            return;
        }

        activeCombatCardTarget = null;
        if (combatCardTargetRoot != null)
        {
            combatCardTargetRoot.gameObject.SetActive(false);
        }
    }

    private void EnsureCombatCardTargetVisuals()
    {
        if (combatCardTargetRoot != null || combatFxLayer == null)
        {
            return;
        }

        var rootObject = new GameObject("Combat Card Targeting", typeof(RectTransform));
        rootObject.transform.SetParent(combatFxLayer, false);
        combatCardTargetRoot = rootObject.GetComponent<RectTransform>();
        Stretch(combatCardTargetRoot, 0f, 0f, 0f, 0f);

        combatCardTargetGlow = AddCombatCardTargetLine(
            combatCardTargetRoot,
            "Combat Card Target Line Glow",
            out combatCardTargetGlowImage);
        combatCardTargetCore = AddCombatCardTargetLine(
            combatCardTargetRoot,
            "Combat Card Target Line Core",
            out combatCardTargetCoreImage);

        var ringObject = new GameObject("Combat Card Enemy Target Ring", typeof(RectTransform), typeof(Image));
        ringObject.transform.SetParent(combatCardTargetRoot, false);
        combatCardTargetRing = ringObject.GetComponent<RectTransform>();
        combatCardTargetRing.anchorMin = new Vector2(0.5f, 0.5f);
        combatCardTargetRing.anchorMax = new Vector2(0.5f, 0.5f);
        combatCardTargetRing.pivot = new Vector2(0.5f, 0.5f);
        combatCardTargetRing.sizeDelta = new Vector2(238f, 238f);
        combatCardTargetRingImage = ringObject.GetComponent<Image>();
        combatCardTargetRingImage.sprite = CombatCardTargetRingSprite();
        combatCardTargetRingImage.preserveAspect = true;
        combatCardTargetRingImage.raycastTarget = false;
        combatCardTargetRoot.SetAsLastSibling();
        combatCardTargetRoot.gameObject.SetActive(false);
    }

    private static RectTransform AddCombatCardTargetLine(
        Transform parent,
        string objectName,
        out Image image)
    {
        var lineObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(parent, false);
        var rect = lineObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        image = lineObject.GetComponent<Image>();
        image.raycastTarget = false;
        return rect;
    }

    private Sprite CombatCardTargetRingSprite()
    {
        if (combatCardTargetRingSprite != null)
        {
            return combatCardTargetRingSprite;
        }

        const int size = 128;
        combatCardTargetRingTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        combatCardTargetRingTexture.name = "Combat Card Target Ring";
        var pixels = new Color32[size * size];
        var center = (size - 1) * 0.5f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x - center) / center;
                var dy = (y - center) / center;
                var radius = Mathf.Sqrt(dx * dx + dy * dy);
                var inner = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 0.79f, radius));
                var outer = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.91f, 0.98f, radius));
                var alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(inner * outer) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        combatCardTargetRingTexture.SetPixels32(pixels);
        combatCardTargetRingTexture.Apply(false, true);
        combatCardTargetRingSprite = Sprite.Create(
            combatCardTargetRingTexture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
        combatCardTargetRingSprite.name = "Combat Card Target Ring Sprite";
        return combatCardTargetRingSprite;
    }

    private void ReleaseCombatCardMotionResources()
    {
        InvalidateCombatCardMotionView();

        if (combatCardTargetRingSprite != null)
        {
            Destroy(combatCardTargetRingSprite);
            combatCardTargetRingSprite = null;
        }
        if (combatCardTargetRingTexture != null)
        {
            Destroy(combatCardTargetRingTexture);
            combatCardTargetRingTexture = null;
        }
    }

    private void InvalidateCombatCardMotionView()
    {
        activeCombatCardTarget = null;
        hoveredCombatCardTarget = null;
        focusedCombatCardTarget = null;
        combatCardTargetRoot = null;
        combatCardTargetGlow = null;
        combatCardTargetCore = null;
        combatCardTargetRing = null;
        combatCardTargetGlowImage = null;
        combatCardTargetCoreImage = null;
        combatCardTargetRingImage = null;
    }

    private sealed class UiCombatCardPresenter : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private AetheriaGame owner;
        private Button button;
        private RectTransform rect;
        private RectTransform parentRect;
        private CanvasGroup canvasGroup;
        private Image disabledVeil;
        private Color accent;
        private Vector2 basePosition;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private float baseAlpha;
        private float fanAngle;
        private float fanYOffset;
        private int cardIndex;
        private bool ready;
        private bool dealt;
        private bool hovered;
        private bool focused;
        private bool pressed;
        private bool skipDealAnimation;
        private Navigation baseNavigation;
        private bool inputGateReleased;

        public void Configure(
            AetheriaGame sourceOwner,
            Button sourceButton,
            int sourceCardIndex,
            int sourceCardCount,
            Color sourceAccent)
        {
            owner = sourceOwner;
            button = sourceButton;
            rect = sourceButton == null ? null : sourceButton.GetComponent<RectTransform>();
            parentRect = rect == null ? null : rect.parent as RectTransform;
            cardIndex = Mathf.Clamp(sourceCardIndex, 0, 4);
            accent = new Color(sourceAccent.r, sourceAccent.g, sourceAccent.b, 1f);
            var fanCenter = (Mathf.Clamp(sourceCardCount, 1, 5) - 1f) * 0.5f;
            var distanceFromCenter = Mathf.Abs(cardIndex - fanCenter);
            fanAngle = (cardIndex - fanCenter) * 0.55f;
            fanYOffset = Mathf.Max(0f, 10f - distanceFromCenter * 5f);
            if (button == null || rect == null)
            {
                enabled = false;
                return;
            }

            baseScale = rect.localScale;
            baseRotation = rect.localRotation;
            canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
            }
            baseAlpha = canvasGroup.alpha;
            baseNavigation = button.navigation;
            skipDealAnimation = Application.isBatchMode;
            if (skipDealAnimation)
            {
                inputGateReleased = true;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = button.interactable ? baseAlpha : baseAlpha * 0.64f;
                rect.localScale = baseScale;
                rect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, fanAngle);
            }
            else
            {
                inputGateReleased = false;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                var hiddenNavigation = baseNavigation;
                hiddenNavigation.mode = Navigation.Mode.None;
                button.navigation = hiddenNavigation;
                canvasGroup.alpha = 0f;
                rect.localScale = baseScale * 0.84f;
                rect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, fanAngle * 1.55f);
            }

            var veilObject = new GameObject("Combat Card Disabled Veil", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            veilObject.transform.SetParent(button.transform, false);
            var veilRect = veilObject.GetComponent<RectTransform>();
            veilRect.anchorMin = Vector2.zero;
            veilRect.anchorMax = Vector2.one;
            veilRect.offsetMin = Vector2.zero;
            veilRect.offsetMax = Vector2.zero;
            veilObject.GetComponent<LayoutElement>().ignoreLayout = true;
            disabledVeil = veilObject.GetComponent<Image>();
            disabledVeil.color = new Color(
                0.10f,
                0.15f,
                0.20f,
                skipDealAnimation && !button.interactable ? 0.18f : 0f);
            disabledVeil.raycastTarget = false;
            veilRect.SetAsFirstSibling();
        }

        private IEnumerator Start()
        {
            yield return null;
            if (rect == null)
            {
                yield break;
            }

            Canvas.ForceUpdateCanvases();
            if (parentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
            basePosition = rect.anchoredPosition;
            ready = true;

            if (skipDealAnimation)
            {
                rect.anchoredPosition = basePosition + new Vector2(0f, fanYOffset);
                rect.localScale = baseScale;
                rect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, fanAngle);
                canvasGroup.alpha = button.interactable ? baseAlpha : baseAlpha * 0.64f;
                dealt = true;
                yield break;
            }

            var elapsed = -cardIndex * 0.065f;
            const float duration = 0.34f;
            while (elapsed < duration && rect != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - progress, 3f);
                var startPosition = basePosition + new Vector2(0f, -142f - cardIndex * 8f);
                var finalPosition = basePosition + new Vector2(0f, fanYOffset);
                rect.anchoredPosition = Vector2.LerpUnclamped(startPosition, finalPosition, eased);
                rect.localScale = baseScale * Mathf.Lerp(0.84f, 1f, eased);
                rect.localRotation = Quaternion.Lerp(
                    baseRotation * Quaternion.Euler(0f, 0f, fanAngle * 1.55f),
                    baseRotation * Quaternion.Euler(0f, 0f, fanAngle),
                    eased);
                canvasGroup.alpha = Mathf.Lerp(0f, button.interactable ? baseAlpha : baseAlpha * 0.64f, eased);
                yield return null;
            }
            ReleaseInputGate();
            dealt = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            if (owner != null && button != null && button.interactable)
            {
                owner.SetCombatCardHovered(this, true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
            if (owner != null)
            {
                owner.SetCombatCardHovered(this, false);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = button != null && button.interactable;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            focused = true;
            if (owner != null && button != null && button.interactable)
            {
                owner.SetCombatCardFocused(this, true);
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            focused = false;
            pressed = false;
            if (owner != null)
            {
                owner.SetCombatCardFocused(this, false);
            }
        }

        private void LateUpdate()
        {
            if (!ready || !dealt || rect == null || button == null || canvasGroup == null)
            {
                return;
            }

            var interactable = button.interactable;
            var active = interactable && owner != null && owner.IsPreferredCombatCardTarget(this);
            if (!interactable && owner != null)
            {
                owner.RemoveCombatCardTarget(this);
            }

            var targetLift = active ? 31f : 0f;
            if (pressed)
            {
                targetLift -= 7f;
            }
            var targetPosition = basePosition + new Vector2(0f, fanYOffset + targetLift);
            var targetScale = !interactable
                ? 0.975f
                : (pressed ? (active ? 1.025f : 0.965f) : (active ? 1.07f : 1f));
            var targetRotation = baseRotation * Quaternion.Euler(0f, 0f, active ? 0f : fanAngle);
            var targetAlpha = interactable ? baseAlpha : baseAlpha * 0.64f;
            var blend = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);

            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, blend);
            rect.localScale = Vector3.Lerp(rect.localScale, baseScale * targetScale, blend);
            rect.localRotation = Quaternion.Slerp(rect.localRotation, targetRotation, blend);
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, blend);
            if (disabledVeil != null)
            {
                var veilColor = disabledVeil.color;
                veilColor.a = Mathf.Lerp(veilColor.a, interactable ? 0f : 0.18f, blend);
                disabledVeil.color = veilColor;
            }

            if (owner != null && active)
            {
                if (owner.activeCombatCardTarget == this)
                {
                    owner.RefreshCombatCardTarget(this, rect);
                }
                else
                {
                    PresentTarget();
                }
            }
        }

        private void OnDisable()
        {
            hovered = false;
            focused = false;
            pressed = false;
            if (owner != null)
            {
                owner.RemoveCombatCardTarget(this);
            }
            ReleaseInputGate();
            if (ready && rect != null)
            {
                rect.anchoredPosition = basePosition + new Vector2(0f, fanYOffset);
                rect.localScale = baseScale;
                rect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, fanAngle);
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = baseAlpha;
            }
        }

        private void OnDestroy()
        {
            if (owner != null)
            {
                owner.RemoveCombatCardTarget(this);
            }
        }

        public void PresentTarget()
        {
            if (owner != null && button != null && rect != null && button.interactable)
            {
                owner.ShowCombatCardTarget(this, button, rect, accent);
            }
        }

        private void ReleaseInputGate()
        {
            if (inputGateReleased)
            {
                return;
            }
            inputGateReleased = true;
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            if (button != null)
            {
                button.navigation = baseNavigation;
                if (cardIndex == 0 && owner != null)
                {
                    owner.SelectCombatCardWhenReady(button);
                }
            }
        }
    }
}
