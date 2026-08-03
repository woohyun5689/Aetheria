using System;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private const string GrowthVisualRoot = "UI/VisualRefresh/Growth/";
    private const string ResultVisualRoot = "UI/VisualRefresh/Results/";

    private void EnsureGrowthResultVisualPresenter()
    {
        if (root == null)
        {
            return;
        }

        var presenter = root.GetComponent<GrowthResultVisualPresenter>();
        if (presenter == null)
        {
            presenter = root.gameObject.AddComponent<GrowthResultVisualPresenter>();
        }
        presenter.Bind(this);
    }

    private void DecorateGrowthAndResultVisuals()
    {
        if (root == null)
        {
            return;
        }

        switch (currentScreen)
        {
            case AetheriaScreen.Inventory:
                DecorateInventoryGrowthVisuals();
                break;
            case AetheriaScreen.Enhancement:
                DecorateEnhancementGrowthVisuals();
                break;
            case AetheriaScreen.SkillTraining:
                CompactSkillTrainingCopy();
                break;
            case AetheriaScreen.Crafting:
                DecorateCraftingGrowthVisuals();
                break;
            case AetheriaScreen.Victory:
            case AetheriaScreen.Defeat:
            case AetheriaScreen.GameClear:
                DecorateResultVisuals();
                break;
        }
    }

    private void DecorateInventoryGrowthVisuals()
    {
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            var target = transforms[i];
            if (target == null)
            {
                continue;
            }
            if (target.name == "Inventory Item Row")
            {
                var path = EquipmentVisualPath(target);
                var button = DirectChildButton(target);
                if (button != null && !string.IsNullOrEmpty(path))
                {
                    AddFixedVisual(button.transform, "Growth Equipment Icon", path, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(7f, 0f), new Vector2(42f, 42f));
                    ReserveButtonCopySpace(button, 50f, 12f);
                }
            }
            else if (target.name == "Equipped Six Slots")
            {
                DecorateEquippedSlotButtons(target);
            }
            else if (target.name == "Selected Item Read Plate")
            {
                var path = EquipmentVisualPath(target);
                if (!string.IsNullOrEmpty(path))
                {
                    AddFixedVisual(target, "Growth Selected Equipment Art", path, Vector2.one, Vector2.one, Vector2.one, new Vector2(-12f, -10f), new Vector2(86f, 86f));
                }
            }
            else if (target.name == "Inventory Selection Read Plate")
            {
                ReplaceCopy(target, "종류 · 희귀도", "장비를 고르면 현재 장비와\n전투력 변화를 바로 비교합니다.");
            }
        }
    }

    private void DecorateEquippedSlotButtons(Transform parent)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var button = parent.GetChild(i).GetComponent<Button>();
            if (button == null)
            {
                continue;
            }

            var path = EquipmentVisualPath(button.transform);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            AddFixedVisual(button.transform, "Growth Equipment Icon", path, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(86f, 86f));
            ReserveButtonCopySpace(button, 108f, 16f);
        }
    }

    private void DecorateEnhancementGrowthVisuals()
    {
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            var target = transforms[i];
            if (target == null)
            {
                continue;
            }
            if (target.name == "Forge Anvil Graphic")
            {
                ReplaceForgeArtwork(target);
                continue;
            }

            if (target.name.StartsWith("Enhancement Card ", StringComparison.Ordinal)
                && target.name != "Enhancement Card Heading")
            {
                var path = EquipmentVisualPath(target);
                if (!string.IsNullOrEmpty(path))
                {
                    var heading = target.Find("Enhancement Card Heading");
                    var visualParent = heading != null ? heading : target;
                    AddFixedVisual(
                        visualParent,
                        "Growth Enhancement Equipment Art",
                        path,
                        new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f),
                        new Vector2(5f, 0f),
                        new Vector2(40f, 40f));
                }
            }

            if (target.name == "Forge Anvil Read Plate")
            {
                ReplaceCopy(target, "모루에 올린 장비", "장비를 골라 강화 단계를 올립니다.");
                ReplaceCopy(target, "현재 장착한 6개", "장착 부위만 강화 가능 · 실패해도 장비 유지");
            }
        }
    }

    private void ReplaceForgeArtwork(Transform forge)
    {
        var sprite = LoadGeneratedSprite(GrowthVisualRoot + "forge_anvil", Vector4.zero);
        if (sprite == null)
        {
            return;
        }

        var existing = forge.Find("Generated Anvil Artwork");
        if (existing != null)
        {
            var existingImage = existing.GetComponent<Image>();
            if (existingImage != null)
            {
                existingImage.sprite = sprite;
                existingImage.color = Color.white;
                existingImage.preserveAspect = true;
            }
            return;
        }

        for (var i = 0; i < forge.childCount; i++)
        {
            var child = forge.GetChild(i);
            if (child.name.StartsWith("Anvil ", StringComparison.Ordinal)
                || child.name.StartsWith("Forge Spark", StringComparison.Ordinal))
            {
                child.gameObject.SetActive(false);
            }

            var label = child.GetComponent<Text>();
            if (label != null && label.text == "모  루")
            {
                child.gameObject.SetActive(false);
            }
        }

        AddFixedVisual(forge, "Generated Anvil Artwork", GrowthVisualRoot + "forge_anvil", new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.96f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    private void CompactSkillTrainingCopy()
    {
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            var target = transforms[i];
            if (target == null)
            {
                continue;
            }
            if (target.name == "Skill Character Read Plate")
            {
                ReplaceCopy(target, "각 기술은 최대", "스킬 아이콘을 골라\n현재와 다음 레벨을 비교하세요.");
            }
            else if (target.name.StartsWith("Fixed Skill Card ", StringComparison.Ordinal))
            {
                CompactDirectCopy(target);
            }
        }
    }

    private void DecorateCraftingGrowthVisuals()
    {
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            var target = transforms[i];
            if (target == null)
            {
                continue;
            }
            if (target.name == "Crafting Material Flow")
            {
                DecorateCraftingFlow(target);
            }
        }
    }

    private void DecorateCraftingFlow(Transform flow)
    {
        for (var i = 0; i < flow.childCount; i++)
        {
            var child = flow.GetChild(i);
            if (child.name.StartsWith("Growth Badge 재료", StringComparison.Ordinal))
            {
                DecorateGrowthBadge(child, GrowthVisualRoot + "crafting_material", 40f);
            }
            else if (child.name.StartsWith("Growth Badge 합성진", StringComparison.Ordinal))
            {
                DecorateGrowthBadge(child, GrowthVisualRoot + "crafting_circle", 46f);
            }
            else if (child.name.StartsWith("Growth Badge 결과", StringComparison.Ordinal))
            {
                DecorateGrowthBadge(child, GrowthVisualRoot + "crafting_result", 46f);
            }
        }
    }

    private void DecorateGrowthBadge(Transform badge, string path, float iconSize)
    {
        if (badge == null)
        {
            return;
        }

        AddFixedVisual(badge, "Growth Badge Artwork", path, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(iconSize, iconSize));
        var texts = badge.GetComponentsInChildren<Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i].transform.parent != badge)
            {
                continue;
            }
            Stretch(texts[i].GetComponent<RectTransform>(), iconSize + 7f, 3f, 4f, 3f);
            texts[i].alignment = TextAnchor.MiddleLeft;
        }
    }

    private void DecorateResultVisuals()
    {
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            var emblem = transforms[i];
            if (emblem == null)
            {
                continue;
            }
            if (emblem.name == "Result Chip Emblem")
            {
                var placeholder = DirectChildText(emblem);
                var path = ResultVisualPath(placeholder != null ? placeholder.text : null);
                if (!string.IsNullOrEmpty(path)
                    && FillVisual(emblem, "Generated Result Icon", path, 4f))
                {
                    placeholder.gameObject.SetActive(false);
                }
            }
            else if (emblem.name == "Result Object Emblem")
            {
                var placeholder = DirectChildText(emblem);
                var path = ResultObjectVisualPath(placeholder != null ? placeholder.text : null);
                if (!string.IsNullOrEmpty(path)
                    && FillVisual(emblem, "Generated Result Icon", path, 3f))
                {
                    placeholder.gameObject.SetActive(false);
                }
            }
        }
    }

    private string ResultVisualPath(string placeholder)
    {
        switch (placeholder)
        {
            case "WIN": return ResultVisualRoot + "level";
            case "SAFE": return "UI/VisualRefresh/Combat/status_shield";
            case "XP": return ResultVisualRoot + "xp";
            case "G": return ResultVisualRoot + "gold";
            case "ITEM": return ResultVisualRoot + "bag";
            case "LV": return ResultVisualRoot + "level";
            case "BAG": return ResultVisualRoot + "bag";
            case "HP": return "UI/VisualRefresh/Combat/status_health";
            case "MP": return "UI/VisualRefresh/Combat/intent_magic";
            case "GEAR": return GrowthVisualRoot + "equipment_armor";
            case "★": return ResultVisualRoot + "level";
            default: return null;
        }
    }

    private string ResultObjectVisualPath(string placeholder)
    {
        switch (placeholder)
        {
            case "GATE": return ResultVisualRoot + "gate";
            case "A": return "UI/VisualRefresh/Objects/menu";
            case "★": return ResultVisualRoot + "level";
            case "UP": return "UI/VisualRefresh/Objects/portal";
            default: return null;
        }
    }

    private string EquipmentVisualPath(Transform target)
    {
        if (target == null)
        {
            return null;
        }

        var texts = target.GetComponentsInChildren<Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i].text == "무기") return GrowthVisualRoot + "equipment_weapon";
            if (texts[i].text == "방어구") return GrowthVisualRoot + "equipment_armor";
            if (texts[i].text.StartsWith("장신구", StringComparison.Ordinal)) return GrowthVisualRoot + "equipment_charm";
        }

        if (target.name.Contains("무기")) return GrowthVisualRoot + "equipment_weapon";
        if (target.name.Contains("방어구")) return GrowthVisualRoot + "equipment_armor";
        if (target.name.Contains("장신구")) return GrowthVisualRoot + "equipment_charm";
        return null;
    }

    private RectTransform AddFixedVisual(
        Transform parent,
        string marker,
        string resourcePath,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        if (parent == null || parent.Find(marker) != null)
        {
            return null;
        }

        var sprite = LoadGeneratedSprite(resourcePath, Vector4.zero);
        if (sprite == null)
        {
            return null;
        }

        var visualObject = new GameObject(marker, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        visualObject.transform.SetParent(parent, false);
        var rect = visualObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = visualObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        visualObject.GetComponent<LayoutElement>().ignoreLayout = true;
        rect.SetAsLastSibling();
        return rect;
    }

    private bool FillVisual(Transform parent, string marker, string resourcePath, float inset)
    {
        if (parent != null && parent.Find(marker) != null)
        {
            return true;
        }

        var visual = AddFixedVisual(parent, marker, resourcePath, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        if (visual != null)
        {
            visual.offsetMin = new Vector2(inset, inset);
            visual.offsetMax = new Vector2(-inset, -inset);
            return true;
        }
        return false;
    }

    private void ReserveButtonCopySpace(Button button, float left, float right)
    {
        if (button == null)
        {
            return;
        }

        var texts = button.GetComponentsInChildren<Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i].transform.parent != button.transform)
            {
                continue;
            }
            Stretch(texts[i].GetComponent<RectTransform>(), left, 4f, right, 4f);
            texts[i].alignment = TextAnchor.MiddleLeft;
        }
    }

    private static Button DirectChildButton(Transform parent)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var button = parent.GetChild(i).GetComponent<Button>();
            if (button != null)
            {
                return button;
            }
        }
        return null;
    }

    private static Text DirectChildText(Transform parent)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var text = parent.GetChild(i).GetComponent<Text>();
            if (text != null)
            {
                return text;
            }
        }
        return null;
    }

    private static Transform FirstDirectChildWithPrefix(Transform parent, string prefix)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return child;
            }
        }
        return null;
    }

    private static void ReplaceCopy(Transform parent, string startsWith, string replacement)
    {
        var texts = parent.GetComponentsInChildren<Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i].text.StartsWith(startsWith, StringComparison.Ordinal))
            {
                texts[i].text = replacement;
            }
        }
    }

    private static void CompactDirectCopy(Transform card)
    {
        for (var i = 0; i < card.childCount; i++)
        {
            var text = card.GetChild(i).GetComponent<Text>();
            if (text == null || string.IsNullOrEmpty(text.text))
            {
                continue;
            }

            // Keep the full Korean effect description. The global fit policy
            // reduces typography within the card instead of deleting copy by
            // character count, which was especially misleading for long skills.
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(text.resizeTextMinSize > 0 ? text.resizeTextMinSize : 12, 12);
            text.resizeTextMaxSize = Mathf.Max(text.resizeTextMaxSize, text.fontSize);
            text.lineSpacing = Mathf.Min(text.lineSpacing, 1.10f);
        }
    }

    private int GrowthResultHierarchySignature()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (int)currentScreen;
            hash = hash * 31 + root.childCount;
            for (var i = 0; i < root.childCount; i++)
            {
                hash = hash * 31 + root.GetChild(i).GetInstanceID();
            }
            return hash;
        }
    }

    private sealed class GrowthResultVisualPresenter : MonoBehaviour
    {
        private AetheriaGame owner;
        private int lastSignature = int.MinValue;

        public void Bind(AetheriaGame game)
        {
            owner = game;
            lastSignature = int.MinValue;
        }

        private void LateUpdate()
        {
            if (owner == null || owner.root == null)
            {
                return;
            }

            var signature = owner.GrowthResultHierarchySignature();
            if (signature == lastSignature)
            {
                return;
            }

            lastSignature = signature;
            owner.DecorateGrowthAndResultVisuals();
        }
    }
}
