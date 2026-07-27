using UnityEngine;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private const string AetheriaCrestPath = "UI/VisualRefresh/Brand/aetheria_crest";
    private const string CombatGroundRunePath = "UI/VisualRefresh/Combat/ground_rune";

    private void ConstrainLayoutSize(RectTransform rect, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        AddLayoutSize(rect, width, height);
        var layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
        {
            return;
        }

        if (width > 0f)
        {
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.flexibleWidth = 0f;
        }
        if (height > 0f)
        {
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }
    }

    private static void ConfigureNonExpandingRow(RectTransform row)
    {
        if (row == null)
        {
            return;
        }

        var layout = row.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            return;
        }

        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;
    }

    private Image AddAetheriaCrestIcon(Transform parent, string name, float width, float height, float alpha)
    {
        var sprite = LoadGeneratedSprite(AetheriaCrestPath, Vector4.zero);
        if (parent == null || sprite == null)
        {
            return null;
        }

        var crest = AddFlatPanel(name, parent, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
        ConstrainLayoutSize(crest, width, height);
        var image = crest.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private void AddAetheriaCrestWatermark(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float alpha)
    {
        var sprite = LoadGeneratedSprite(AetheriaCrestPath, Vector4.zero);
        if (parent == null || sprite == null || parent.Find(name) != null)
        {
            return;
        }

        var crest = AddFlatPanel(name, parent, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
        crest.anchorMin = anchorMin;
        crest.anchorMax = anchorMax;
        crest.offsetMin = Vector2.zero;
        crest.offsetMax = Vector2.zero;
        crest.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        crest.SetAsFirstSibling();
        var image = crest.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private void AddGuideFlowRoute(RectTransform flow)
    {
        if (flow == null || flow.Find("Guide Flow Route Artwork") != null)
        {
            return;
        }

        var artwork = AddFlatPanel("Guide Flow Route Artwork", flow, Color.clear);
        Stretch(artwork, 0f, 0f, 0f, 0f);
        artwork.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        artwork.SetAsFirstSibling();
        artwork.GetComponent<Image>().raycastTarget = false;

        var route = AddFlatPanel("Guide Flow Luminous Path", artwork, new Color(goldColor.r, goldColor.g, goldColor.b, 0.42f));
        route.anchorMin = new Vector2(0.11f, 0.78f);
        route.anchorMax = new Vector2(0.89f, 0.78f);
        route.pivot = new Vector2(0.5f, 0.5f);
        route.sizeDelta = new Vector2(0f, 4f);
        route.anchoredPosition = Vector2.zero;
        route.GetComponent<Image>().raycastTarget = false;

        var centers = new[] { 0.125f, 0.375f, 0.625f, 0.875f };
        for (var index = 0; index < centers.Length; index++)
        {
            var node = AddFlatPanel("Guide Flow Route Node " + (index + 1), artwork, new Color(1f, 1f, 1f, 0.38f));
            node.anchorMin = new Vector2(centers[index], 0.78f);
            node.anchorMax = node.anchorMin;
            node.pivot = new Vector2(0.5f, 0.5f);
            node.anchoredPosition = Vector2.zero;
            node.sizeDelta = new Vector2(26f, 26f);
            var nodeImage = node.GetComponent<Image>();
            nodeImage.sprite = MapCircleSprite();
            nodeImage.raycastTarget = false;
        }

        for (var index = 0; index < 3; index++)
        {
            var x = 0.25f + index * 0.25f;
            AddGuideFlowChevronStroke(artwork, x, 0.79f, -34f);
            AddGuideFlowChevronStroke(artwork, x, 0.77f, 34f);
        }
    }

    private void AddGuideFlowChevronStroke(RectTransform parent, float x, float y, float rotation)
    {
        var stroke = AddFlatPanel("Guide Flow Chevron", parent, new Color(goldColor.r, goldColor.g, goldColor.b, 0.88f));
        stroke.anchorMin = new Vector2(x, y);
        stroke.anchorMax = stroke.anchorMin;
        stroke.pivot = new Vector2(0.5f, 0.5f);
        stroke.anchoredPosition = Vector2.zero;
        stroke.sizeDelta = new Vector2(22f, 4f);
        stroke.localRotation = Quaternion.Euler(0f, 0f, rotation);
        stroke.GetComponent<Image>().raycastTarget = false;
    }

    private Image AddDungeonRegionArtwork(Transform parent, DungeonMapRegion region, string name, float size)
    {
        var sprite = region == null ? null : DungeonRegionVisualSprite(region.key);
        if (parent == null || sprite == null)
        {
            return null;
        }

        var artwork = AddFlatPanel(name, parent, Color.white);
        ConstrainLayoutSize(artwork, size, size);
        var image = artwork.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private void AddDungeonRoutePreview(Transform parent, int floorCount, Color accent, bool completed)
    {
        if (parent == null)
        {
            return;
        }

        var preview = AddFlatPanel("Dungeon Route Preview", parent, Color.clear);
        AddLayoutSize(preview, -1f, 54f);
        preview.GetComponent<Image>().raycastTarget = false;
        var layout = preview.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(26, 26, 4, 4);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var count = Mathf.Clamp(floorCount, 2, 4);
        for (var index = 0; index < count; index++)
        {
            var bossNode = index == count - 1;
            var nodeColor = bossNode
                ? (completed ? goodColor : goldColor)
                : Color.Lerp(accent, Color.white, 0.18f);
            var node = AddFlatPanel(
                bossNode ? "Dungeon Route Boss Node" : "Dungeon Route Stage Node",
                preview,
                new Color(nodeColor.r, nodeColor.g, nodeColor.b, 0.30f));
            AddLayoutSize(node, 46f, 46f);
            var nodeImage = node.GetComponent<Image>();
            nodeImage.sprite = MapCircleSprite();
            nodeImage.raycastTarget = false;
            var nodeLabel = AddText(
                node,
                bossNode ? "B" : (index + 1).ToString(),
                18,
                FontStyle.Bold,
                nodeColor,
                TextAnchor.MiddleCenter,
                46f);
            Stretch(nodeLabel.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

            if (!bossNode)
            {
                var connector = AddFlatPanel(
                    "Dungeon Route Connector " + (index + 1),
                    preview,
                    new Color(accent.r, accent.g, accent.b, 0.58f));
                AddLayoutSize(connector, count >= 4 ? 74f : 112f, 4f);
                connector.GetComponent<Image>().raycastTarget = false;
            }
        }
    }

    private void AddGameClearSealNetwork(RectTransform grid)
    {
        if (grid == null || grid.Find("Game Clear Constellation") != null)
        {
            return;
        }

        var network = AddFlatPanel("Game Clear Constellation", grid, Color.clear);
        Stretch(network, 0f, 0f, 0f, 0f);
        network.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        network.SetAsFirstSibling();
        network.GetComponent<Image>().raycastTarget = false;
        var accent = ActiveCharacterAccent(goldColor);

        var rowY = new[] { 0.17f, 0.50f, 0.83f };
        for (var index = 0; index < rowY.Length; index++)
        {
            var line = AddFlatPanel("Conquest Link Row " + index, network, new Color(accent.r, accent.g, accent.b, 0.26f));
            line.anchorMin = new Vector2(0.08f, rowY[index]);
            line.anchorMax = new Vector2(0.92f, rowY[index]);
            line.sizeDelta = new Vector2(0f, 3f);
            line.anchoredPosition = Vector2.zero;
            line.GetComponent<Image>().raycastTarget = false;
        }

        var columnX = new[] { 0.17f, 0.50f, 0.83f };
        for (var index = 0; index < columnX.Length; index++)
        {
            var line = AddFlatPanel("Conquest Link Column " + index, network, new Color(goldColor.r, goldColor.g, goldColor.b, 0.18f));
            line.anchorMin = new Vector2(columnX[index], 0.10f);
            line.anchorMax = new Vector2(columnX[index], 0.90f);
            line.sizeDelta = new Vector2(3f, 0f);
            line.anchoredPosition = Vector2.zero;
            line.GetComponent<Image>().raycastTarget = false;
        }
    }

    private Image AddCombatGroundRune(Transform parent, Color accent, bool active, bool playerSide)
    {
        var sprite = LoadGeneratedSprite(CombatGroundRunePath, Vector4.zero);
        if (parent == null || sprite == null)
        {
            return null;
        }

        var tint = Color.Lerp(Color.white, new Color(accent.r, accent.g, accent.b, 1f), 0.20f);
        var rune = AddFlatPanel(
            playerSide ? "Hero Combat Ground Rune" : "Enemy Combat Ground Rune",
            parent,
            new Color(tint.r, tint.g, tint.b, active ? 0.32f : 0.10f));
        rune.anchorMin = new Vector2(0.13f, 0.012f);
        rune.anchorMax = new Vector2(0.87f, 0.215f);
        rune.offsetMin = Vector2.zero;
        rune.offsetMax = Vector2.zero;
        var image = rune.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = false;
        image.raycastTarget = false;
        var outline = rune.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.015f, 0.05f, 0.08f, active ? 0.52f : 0.32f);
        outline.effectDistance = new Vector2(1.6f, -1.6f);
        outline.useGraphicAlpha = true;

        var presenter = rune.gameObject.AddComponent<UiCombatGroundRunePresenter>();
        presenter.Configure(image, tint, active, playerSide ? 0f : Mathf.PI);
        return image;
    }

    private void AddEnemyIntentPointer(RectTransform intent, Color accent)
    {
        if (intent == null || intent.Find("Enemy Intent Pointer") != null)
        {
            return;
        }

        var stem = AddFlatPanel("Enemy Intent Pointer", intent, new Color(accent.r, accent.g, accent.b, 0.78f));
        stem.anchorMin = new Vector2(0.5f, 0f);
        stem.anchorMax = stem.anchorMin;
        stem.pivot = new Vector2(0.5f, 1f);
        stem.anchoredPosition = new Vector2(0f, -2f);
        stem.sizeDelta = new Vector2(4f, 22f);
        stem.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        stem.GetComponent<Image>().raycastTarget = false;

        var tip = AddFlatPanel("Enemy Intent Pointer Tip", intent, new Color(accent.r, accent.g, accent.b, 0.92f));
        tip.anchorMin = new Vector2(0.5f, 0f);
        tip.anchorMax = tip.anchorMin;
        tip.pivot = new Vector2(0.5f, 0.5f);
        tip.anchoredPosition = new Vector2(0f, -25f);
        tip.sizeDelta = new Vector2(12f, 12f);
        tip.localRotation = Quaternion.Euler(0f, 0f, 45f);
        tip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        tip.GetComponent<Image>().raycastTarget = false;
    }

    private void ConfigureEnemyIntentPulse(RectTransform icon, bool resolving)
    {
        if (icon == null)
        {
            return;
        }

        var presenter = icon.GetComponent<UiEnemyIntentPulsePresenter>();
        if (presenter == null)
        {
            presenter = icon.gameObject.AddComponent<UiEnemyIntentPulsePresenter>();
        }
        presenter.Configure(icon, resolving);
    }

    private Color CombatBasicCardAccent()
    {
        return Color.Lerp(new Color32(235, 122, 34, 255), ActiveCharacterAccent(goldColor), 0.22f);
    }

    private Color CombatCardRoleAccent(SkillState skill)
    {
        Color roleColor;
        if (skill == null)
        {
            roleColor = new Color32(235, 122, 34, 255);
        }
        else if (skill.healsSelf)
        {
            roleColor = new Color32(38, 166, 103, 255);
        }
        else if (skill.grantsShield || skill.noDamage || skill.selfStatusType == "shield")
        {
            roleColor = new Color32(32, 154, 167, 255);
        }
        else if (CombatSkillHasControl(skill))
        {
            roleColor = new Color32(139, 92, 214, 255);
        }
        else if (skill.magic)
        {
            roleColor = new Color32(54, 116, 220, 255);
        }
        else
        {
            roleColor = new Color32(225, 112, 31, 255);
        }

        return Color.Lerp(roleColor, ActiveCharacterAccent(roleColor), 0.20f);
    }

    private string CombatCardRoleLabel(SkillState skill)
    {
        if (skill == null) return "공격";
        if (skill.healsSelf) return "회복";
        if (skill.grantsShield || skill.noDamage || skill.selfStatusType == "shield") return "방어";
        if (CombatSkillHasControl(skill)) return "제어";
        return skill.magic ? "마법" : "물리";
    }

    private static bool CombatSkillHasControl(SkillState skill)
    {
        return skill != null
            && (skill.stunChance > 0f
                || skill.freezeChance > 0f
                || skill.blindChance > 0f
                || skill.weakenChance > 0f
                || skill.vulnerableChance > 0f
                || skill.silenceChance > 0f
                || skill.manaBurnChance > 0f
                || skill.burnChance > 0f
                || skill.poisonChance > 0f
                || skill.bleedChance > 0f
                || skill.shockChance > 0f);
    }

    private void AddCombatCardRoleBadge(RectTransform card, string roleLabel, Color accent)
    {
        if (card == null || string.IsNullOrEmpty(roleLabel))
        {
            return;
        }

        var badge = AddReadabilityPlate(card, "Card Role " + roleLabel, Rgb(10, 24, 40));
        badge.anchorMin = new Vector2(0f, 1f);
        badge.anchorMax = badge.anchorMin;
        badge.pivot = new Vector2(0f, 1f);
        badge.anchoredPosition = new Vector2(18f, -16f);
        badge.sizeDelta = new Vector2(78f, 28f);
        badge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        badge.GetComponent<Image>().raycastTarget = false;
        var label = AddText(badge, roleLabel, 16, FontStyle.Bold, accent, TextAnchor.MiddleCenter, 28f);
        Stretch(label.GetComponent<RectTransform>(), 4f, 0f, 4f, 0f);
        badge.SetAsLastSibling();
    }

    private sealed class UiCombatGroundRunePresenter : MonoBehaviour
    {
        private RectTransform rect;
        private Image image;
        private Color tint;
        private bool active;
        private float phase;

        public void Configure(Image sourceImage, Color sourceTint, bool sourceActive, float sourcePhase)
        {
            image = sourceImage;
            rect = sourceImage != null ? sourceImage.rectTransform : null;
            tint = sourceTint;
            active = sourceActive;
            phase = sourcePhase;
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (image == null || rect == null)
            {
                return;
            }

            var wave = (Mathf.Sin(Time.unscaledTime * (active ? 2.8f : 1.7f) + phase) + 1f) * 0.5f;
            var alpha = active ? Mathf.Lerp(0.44f, 0.56f, wave) : Mathf.Lerp(0.20f, 0.29f, wave);
            image.color = new Color(tint.r, tint.g, tint.b, alpha);
            var scale = active ? Mathf.Lerp(1f, 1.035f, wave) : Mathf.Lerp(0.995f, 1.01f, wave);
            rect.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private sealed class UiEnemyIntentPulsePresenter : MonoBehaviour
    {
        private RectTransform rect;
        private bool resolving;

        public void Configure(RectTransform sourceRect, bool sourceResolving)
        {
            rect = sourceRect;
            resolving = sourceResolving;
        }

        private void Update()
        {
            if (rect == null)
            {
                return;
            }

            var wave = (Mathf.Sin(Time.unscaledTime * (resolving ? 5.4f : 2.2f)) + 1f) * 0.5f;
            var scale = resolving ? Mathf.Lerp(1.04f, 1.12f, wave) : Mathf.Lerp(0.99f, 1.02f, wave);
            rect.localScale = Vector3.one * scale;
        }
    }
}
