using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "AetheriaHudLayoutProfile", menuName = "Aetheria/HUD Layout Profile")]
public sealed class AetheriaHudLayoutProfile : ScriptableObject
{
    public List<AetheriaHudLayoutEntry> entries = new List<AetheriaHudLayoutEntry>();
}

[Serializable]
public sealed class AetheriaHudLayoutEntry
{
    public string screenName;
    public string hierarchyPath;
    public string hierarchyNamePath;
    public string displayName;
    public string frozenLayoutPath;
    public string frozenLayoutNamePath;

    public Vector2 anchorMin;
    public Vector2 anchorMax;
    public Vector2 pivot;
    public Vector2 anchoredPosition;
    public Vector2 sizeDelta;
    public Vector3 localEulerAngles;
    public Vector3 localScale = Vector3.one;

    public bool overrideRenderOrder;
    public int renderOrder;

    public bool overrideImage;
    public Sprite imageSprite;
    public Color imageColor = Color.white;
    public bool preserveImageAspect;

    public bool overrideText;
    [TextArea(2, 8)] public string text;
    public Color textColor = Color.white;
    public int fontSize = 18;

    public bool overrideParticle;
    public float particleSimulationSpeed = 1f;
    public float particleStartSize = 1f;
    public float particleStartSpeed = 1f;
    public float particleEmissionRate = 10f;
}

[DisallowMultipleComponent]
public sealed class AetheriaHudTextColorOverride : MonoBehaviour
{
    [SerializeField] private bool hasOverride;
    [SerializeField] private Color overrideColor = Color.white;

    public bool HasOverride
    {
        get { return hasOverride; }
    }

    public Color OverrideColor
    {
        get { return overrideColor; }
    }

    public void Set(Color color)
    {
        hasOverride = true;
        overrideColor = color;
    }

    public void Clear()
    {
        hasOverride = false;
    }
}

public static class AetheriaHudLayoutRuntime
{
    public const string ResourcesPath = "UI/AetheriaHudLayoutProfile";

    private const string CharacterThemeButtonFrameName = "Character Theme Button Frame";
    private const string ObjectActionFeatureFrameName = "Object Action Feature Frame";
    private const string InventoryScreenName = "Inventory";
    private const string InventoryItemRowName = "Inventory Item Row";
    private const int InventoryItemRowTemplateIndex = 1;

    private static readonly string[] DynamicHudNamePrefixes =
    {
        "Growth Badge ",
        "Visual Status Chip ",
        "Combat Action Card ",
        "Crafting Recipe ",
        "Result Chip ",
        "Delta ",
        "Item Comparison ",
        "Dungeon Card ",
        "Dungeon Row ",
        "Map Dungeon "
    };

    private static AetheriaHudLayoutProfile cachedProfile;

    public static void InvalidateCache()
    {
        cachedProfile = null;
    }

    public static void LockManualSize(RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        var parent = target.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        if ((target.anchorMax - target.anchorMin).sqrMagnitude <= 0.000001f)
        {
            return;
        }

        var size = target.rect.size;
        var worldPivot = target.position;
        var parentRect = parent.rect;
        var localPivot = parent.InverseTransformPoint(worldPivot);
        var anchor = new Vector2(
            parentRect.width > 0.001f
                ? Mathf.Clamp01((localPivot.x - parentRect.xMin) / parentRect.width)
                : 0.5f,
            parentRect.height > 0.001f
                ? Mathf.Clamp01((localPivot.y - parentRect.yMin) / parentRect.height)
                : 0.5f);

        target.anchorMin = anchor;
        target.anchorMax = anchor;
        target.position = worldPivot;
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, size.x));
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, size.y));
    }

    public static void ApplyRenderOrder(RectTransform target, int renderOrder)
    {
        if (target == null)
        {
            return;
        }

        var canvas = target.GetComponent<Canvas>();
        if (renderOrder > 0)
        {
            if (canvas == null)
            {
                canvas = target.gameObject.AddComponent<Canvas>();
            }
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Clamp(renderOrder, 1, 32767);
        }
        else if (canvas != null)
        {
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
        }
    }

    public static bool IsButtonAttachedDecoration(RectTransform target)
    {
        if (target == null || target.parent == null || target.parent.GetComponent<Button>() == null)
        {
            return false;
        }

        return target.name == CharacterThemeButtonFrameName
            || target.name == ObjectActionFeatureFrameName;
    }

    public static void ApplyToScreen(RectTransform screenRoot)
    {
        if (screenRoot == null)
        {
            return;
        }

        var profile = cachedProfile != null
            ? cachedProfile
            : Resources.Load<AetheriaHudLayoutProfile>(ResourcesPath);
        cachedProfile = profile;
        ApplyToScreen(screenRoot, profile);
    }

    public static void ApplyToScreen(RectTransform screenRoot, AetheriaHudLayoutProfile profile)
    {
        if (screenRoot == null || profile == null || profile.entries == null || profile.entries.Count == 0)
        {
            return;
        }

        var screenEntries = new List<AetheriaHudLayoutEntry>();
        for (var i = 0; i < profile.entries.Count; i++)
        {
            var entry = profile.entries[i];
            if (entry != null && entry.screenName == screenRoot.name)
            {
                screenEntries.Add(entry);
            }
        }

        if (screenEntries.Count == 0)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(screenRoot);

        // Capture the layout result once, then detach edited items from automatic sibling reflow.
        var frozenLayouts = new HashSet<LayoutGroup>();
        for (var i = 0; i < screenEntries.Count; i++)
        {
            var entry = screenEntries[i];
            var entryTarget = ResolveRelativePath(
                screenRoot,
                entry.hierarchyPath,
                entry.hierarchyNamePath) as RectTransform;
            if (IsButtonAttachedDecoration(entryTarget))
            {
                continue;
            }

            var layout = ResolveFrozenLayout(screenRoot, entry);
            if (layout == null || !frozenLayouts.Add(layout))
            {
                continue;
            }

            layout.enabled = false;
            var fitter = layout.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.enabled = false;
            }
        }

        for (var i = 0; i < screenEntries.Count; i++)
        {
            ApplyEntry(screenRoot, screenEntries[i]);
        }

        ApplyInventoryItemRowTemplate(screenRoot);

        Canvas.ForceUpdateCanvases();
    }

    private static void ApplyInventoryItemRowTemplate(RectTransform screenRoot)
    {
        if (screenRoot.name != InventoryScreenName)
        {
            return;
        }

        var allRects = screenRoot.GetComponentsInChildren<RectTransform>(true);
        var rows = new List<RectTransform>();
        for (var i = 0; i < allRects.Length; i++)
        {
            if (allRects[i] != null && allRects[i].name == InventoryItemRowName)
            {
                rows.Add(allRects[i]);
            }
        }
        if (rows.Count == 0)
        {
            return;
        }

        var template = rows[Mathf.Min(InventoryItemRowTemplateIndex, rows.Count - 1)];
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row == null || row == template)
            {
                continue;
            }

            CopyInventoryRowLayout(template, row);
        }
    }

    private static void CopyInventoryRowLayout(RectTransform template, RectTransform destination)
    {
        CopyLayoutElement(template, destination);
        CopyHorizontalLayout(template, destination);

        var childCount = Mathf.Min(template.childCount, destination.childCount);
        for (var i = 0; i < childCount; i++)
        {
            CopyInventoryRowChildLayout(
                template.GetChild(i) as RectTransform,
                destination.GetChild(i) as RectTransform);
        }
    }

    private static void CopyInventoryRowChildLayout(RectTransform template, RectTransform destination)
    {
        if (template == null || destination == null)
        {
            return;
        }

        destination.anchorMin = template.anchorMin;
        destination.anchorMax = template.anchorMax;
        destination.pivot = template.pivot;
        destination.anchoredPosition = template.anchoredPosition;
        destination.sizeDelta = template.sizeDelta;
        destination.localEulerAngles = template.localEulerAngles;
        destination.localScale = template.localScale;

        CopyLayoutElement(template, destination);
        CopyHorizontalLayout(template, destination);

        var childCount = Mathf.Min(template.childCount, destination.childCount);
        for (var i = 0; i < childCount; i++)
        {
            CopyInventoryRowChildLayout(
                template.GetChild(i) as RectTransform,
                destination.GetChild(i) as RectTransform);
        }
    }

    private static void CopyLayoutElement(RectTransform template, RectTransform destination)
    {
        var source = template.GetComponent<LayoutElement>();
        var target = destination.GetComponent<LayoutElement>();
        if (source == null || target == null)
        {
            return;
        }

        target.ignoreLayout = source.ignoreLayout;
        target.minWidth = source.minWidth;
        target.minHeight = source.minHeight;
        target.preferredWidth = source.preferredWidth;
        target.preferredHeight = source.preferredHeight;
        target.flexibleWidth = source.flexibleWidth;
        target.flexibleHeight = source.flexibleHeight;
        target.layoutPriority = source.layoutPriority;
    }

    private static void CopyHorizontalLayout(RectTransform template, RectTransform destination)
    {
        var source = template.GetComponent<HorizontalLayoutGroup>();
        var target = destination.GetComponent<HorizontalLayoutGroup>();
        if (source == null || target == null)
        {
            return;
        }

        target.padding = new RectOffset(
            source.padding.left,
            source.padding.right,
            source.padding.top,
            source.padding.bottom);
        target.spacing = source.spacing;
        target.childAlignment = source.childAlignment;
        target.childControlWidth = source.childControlWidth;
        target.childControlHeight = source.childControlHeight;
        target.childForceExpandWidth = source.childForceExpandWidth;
        target.childForceExpandHeight = source.childForceExpandHeight;
        target.childScaleWidth = source.childScaleWidth;
        target.childScaleHeight = source.childScaleHeight;
        target.reverseArrangement = source.reverseArrangement;
        target.enabled = source.enabled;
    }

    private static LayoutGroup ResolveFrozenLayout(RectTransform screenRoot, AetheriaHudLayoutEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.frozenLayoutPath)
            || !string.IsNullOrEmpty(entry.frozenLayoutNamePath))
        {
            var layoutTransform = ResolveRelativePath(
                screenRoot,
                entry.frozenLayoutPath,
                entry.frozenLayoutNamePath);
            return layoutTransform != null ? layoutTransform.GetComponent<LayoutGroup>() : null;
        }

        // The root itself has an empty relative path. Distinguish that valid path
        // from entries that were edited without any parent layout to freeze.
        var target = ResolveRelativePath(
            screenRoot,
            entry.hierarchyPath,
            entry.hierarchyNamePath);
        for (var current = target != null ? target.parent : null;
             current != null;
             current = current.parent)
        {
            var layout = current.GetComponent<LayoutGroup>();
            if (layout != null)
            {
                return current == screenRoot ? layout : null;
            }
            if (current == screenRoot)
            {
                break;
            }
        }
        return null;
    }

    public static string BuildRelativePath(Transform target, Transform root)
    {
        if (target == null || root == null || target == root)
        {
            return string.Empty;
        }

        var segments = new List<string>();
        for (var current = target; current != null && current != root; current = current.parent)
        {
            segments.Add(current.GetSiblingIndex().ToString());
        }
        segments.Reverse();
        return string.Join("/", segments.ToArray());
    }

    public static string BuildRelativeNamePath(Transform target, Transform root)
    {
        if (target == null || root == null || target == root)
        {
            return string.Empty;
        }

        var segments = new List<string>();
        for (var current = target; current != null && current != root; current = current.parent)
        {
            var occurrence = 0;
            if (current.parent != null)
            {
                for (var i = 0; i < current.GetSiblingIndex(); i++)
                {
                    if (current.parent.GetChild(i).name == current.name)
                    {
                        occurrence++;
                    }
                }
            }
            segments.Add(EscapeSegment(current.name) + "#" + occurrence);
        }
        segments.Reverse();
        return string.Join("/", segments.ToArray());
    }

    public static Transform ResolveRelativePath(
        Transform root,
        string indexedPath,
        string namePath = null)
    {
        if (root == null)
        {
            return null;
        }

        var resolved = ResolveIndexedPath(root, indexedPath);
        var resolvedNamePath = resolved != null
            ? BuildRelativeNamePath(resolved, root)
            : string.Empty;
        if (resolved != null &&
            (string.IsNullOrEmpty(namePath)
             || resolvedNamePath == namePath
             || IsLegacyStablePathMatch(resolvedNamePath, namePath)
             || IsDataRefreshPathMatch(root, indexedPath, resolvedNamePath, namePath)))
        {
            return resolved;
        }

        return ResolveNamePath(root, namePath);
    }

    private static bool IsLegacyStablePathMatch(string currentPath, string savedPath)
    {
        if (string.IsNullOrEmpty(currentPath) || string.IsNullOrEmpty(savedPath))
        {
            return false;
        }

        var currentSegments = currentPath.Split('/');
        var savedSegments = savedPath.Split('/');
        if (currentSegments.Length != savedSegments.Length)
        {
            return false;
        }

        for (var i = 0; i < currentSegments.Length; i++)
        {
            if (currentSegments[i] == savedSegments[i])
            {
                continue;
            }

            var separator = currentSegments[i].LastIndexOf('#');
            var escapedName = separator >= 0
                ? currentSegments[i].Substring(0, separator)
                : currentSegments[i];
            var currentName = UnescapeSegment(escapedName);
            if (!IsStableDynamicHudName(currentName))
            {
                return false;
            }

            var currentOccurrence = separator >= 0
                ? currentSegments[i].Substring(separator + 1)
                : "0";
            var savedSeparator = savedSegments[i].LastIndexOf('#');
            var savedOccurrence = savedSeparator >= 0
                ? savedSegments[i].Substring(savedSeparator + 1)
                : "0";
            if (currentOccurrence != savedOccurrence)
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsDataRefreshPathMatch(
        Transform root,
        string indexedPath,
        string currentPath,
        string savedPath)
    {
        if (root == null
            || string.IsNullOrEmpty(indexedPath)
            || string.IsNullOrEmpty(currentPath)
            || string.IsNullOrEmpty(savedPath))
        {
            return false;
        }

        var indexes = indexedPath.Split('/');
        var currentSegments = currentPath.Split('/');
        var savedSegments = savedPath.Split('/');
        if (indexes.Length != currentSegments.Length || indexes.Length != savedSegments.Length)
        {
            return false;
        }

        var current = root;
        for (var i = 0; i < indexes.Length; i++)
        {
            int childIndex;
            if (!int.TryParse(indexes[i], out childIndex)
                || childIndex < 0
                || childIndex >= current.childCount)
            {
                return false;
            }
            current = current.GetChild(childIndex);

            if (currentSegments[i] == savedSegments[i])
            {
                continue;
            }

            string currentName;
            string currentOccurrence;
            SplitNamePathSegment(currentSegments[i], out currentName, out currentOccurrence);
            string savedName;
            string savedOccurrence;
            SplitNamePathSegment(savedSegments[i], out savedName, out savedOccurrence);
            if (currentOccurrence != savedOccurrence)
            {
                return false;
            }

            // AddButton uses its visible label as the GameObject name. Prices,
            // availability and other live values may therefore change the name
            // even though this is still the same button at the same path.
            if (current.GetComponent<Button>() != null
                || SameDynamicHudNameFamily(currentName, savedName))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static void SplitNamePathSegment(string segment, out string name, out string occurrence)
    {
        var separator = segment.LastIndexOf('#');
        var escapedName = separator >= 0 ? segment.Substring(0, separator) : segment;
        name = UnescapeSegment(escapedName);
        occurrence = separator >= 0 ? segment.Substring(separator + 1) : "0";
    }

    private static bool SameDynamicHudNameFamily(string currentName, string savedName)
    {
        var currentFamily = DynamicHudNameFamily(currentName);
        return !string.IsNullOrEmpty(currentFamily)
            && currentFamily == DynamicHudNameFamily(savedName);
    }

    private static string DynamicHudNameFamily(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        for (var i = 0; i < DynamicHudNamePrefixes.Length; i++)
        {
            if (name.StartsWith(DynamicHudNamePrefixes[i], StringComparison.Ordinal))
            {
                return DynamicHudNamePrefixes[i];
            }
        }
        return string.Empty;
    }

    private static bool IsStableDynamicHudName(string name)
    {
        return name.StartsWith("Inventory Sort Button ", StringComparison.Ordinal)
            || name == "Inventory Item Select Button"
            || name.StartsWith("Inventory Item ", StringComparison.Ordinal) && name.EndsWith(" Badge", StringComparison.Ordinal)
            || name.StartsWith("Selected Item ", StringComparison.Ordinal) && (name.EndsWith(" Badge", StringComparison.Ordinal) || name.EndsWith(" Button", StringComparison.Ordinal))
            || name.StartsWith("Accessory Slot Button ", StringComparison.Ordinal)
            || name.StartsWith("Equipped Slot ", StringComparison.Ordinal);
    }

    private static void ApplyEntry(RectTransform screenRoot, AetheriaHudLayoutEntry entry)
    {
        var target = ResolveRelativePath(screenRoot, entry.hierarchyPath, entry.hierarchyNamePath) as RectTransform;
        if (target == null)
        {
            return;
        }

        var attachedDecoration = IsButtonAttachedDecoration(target);

        if (attachedDecoration)
        {
            ResetButtonAttachedDecoration(target);
        }
        else
        {
            var fitter = target.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.enabled = false;
            }
            var aspect = target.GetComponent<AspectRatioFitter>();
            if (aspect != null)
            {
                aspect.enabled = false;
            }

            target.anchorMin = entry.anchorMin;
            target.anchorMax = entry.anchorMax;
            target.pivot = entry.pivot;
            target.anchoredPosition = entry.anchoredPosition;
            target.sizeDelta = entry.sizeDelta;
            target.localEulerAngles = entry.localEulerAngles;
            target.localScale = entry.localScale;
            LockManualSize(target);
        }

        if (!attachedDecoration && entry.overrideRenderOrder)
        {
            ApplyRenderOrder(target, entry.renderOrder);
        }

        if (entry.overrideImage)
        {
            var image = target.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = entry.imageSprite;
                image.color = entry.imageColor;
                image.preserveAspect = entry.preserveImageAspect;
            }
        }

        if (entry.overrideText)
        {
            var label = target.GetComponent<Text>();
            if (label != null)
            {
                label.text = entry.text ?? string.Empty;
                label.color = entry.textColor;
                label.fontSize = Mathf.Max(1, entry.fontSize);
                label.resizeTextMaxSize = Mathf.Max(label.resizeTextMinSize, label.fontSize);

                // Button state presenters refresh every frame, so keep an explicit
                // HUD color marker they can honor after this one-time profile pass.
                var colorOverride = label.GetComponent<AetheriaHudTextColorOverride>();
                if (colorOverride == null)
                {
                    colorOverride = label.gameObject.AddComponent<AetheriaHudTextColorOverride>();
                }
                colorOverride.Set(entry.textColor);
            }
        }
        else
        {
            var label = target.GetComponent<Text>();
            var colorOverride = label != null ? label.GetComponent<AetheriaHudTextColorOverride>() : null;
            if (colorOverride != null)
            {
                colorOverride.Clear();
            }
        }

        if (entry.overrideParticle)
        {
            var particles = target.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                var main = particles.main;
                main.simulationSpeed = Mathf.Max(0f, entry.particleSimulationSpeed);
                main.startSizeMultiplier = Mathf.Max(0f, entry.particleStartSize);
                main.startSpeedMultiplier = entry.particleStartSpeed;
                var emission = particles.emission;
                emission.rateOverTimeMultiplier = Mathf.Max(0f, entry.particleEmissionRate);
            }
        }
    }

    private static void ResetButtonAttachedDecoration(RectTransform target)
    {
        var inset = target.name == ObjectActionFeatureFrameName ? 2f : 0f;
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.pivot = new Vector2(0.5f, 0.5f);
        target.offsetMin = new Vector2(inset, inset);
        target.offsetMax = new Vector2(-inset, -inset);
        target.localEulerAngles = Vector3.zero;
        target.localScale = Vector3.one;

        var layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }
    }

    private static Transform ResolveIndexedPath(Transform root, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return root;
        }

        var current = root;
        var segments = path.Split('/');
        for (var i = 0; i < segments.Length; i++)
        {
            int index;
            if (!int.TryParse(segments[i], out index) || index < 0 || index >= current.childCount)
            {
                return null;
            }
            current = current.GetChild(index);
        }
        return current;
    }

    private static Transform ResolveNamePath(Transform root, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return root;
        }

        var current = root;
        var segments = path.Split('/');
        for (var i = 0; i < segments.Length; i++)
        {
            var separator = segments[i].LastIndexOf('#');
            var escapedName = separator >= 0 ? segments[i].Substring(0, separator) : segments[i];
            var name = UnescapeSegment(escapedName);
            var requestedOccurrence = 0;
            if (separator >= 0)
            {
                int.TryParse(segments[i].Substring(separator + 1), out requestedOccurrence);
            }

            Transform match = null;
            var occurrence = 0;
            for (var childIndex = 0; childIndex < current.childCount; childIndex++)
            {
                var child = current.GetChild(childIndex);
                if (child.name != name)
                {
                    continue;
                }
                if (occurrence == requestedOccurrence)
                {
                    match = child;
                    break;
                }
                occurrence++;
            }

            if (match == null)
            {
                return null;
            }
            current = match;
        }
        return current;
    }

    private static string EscapeSegment(string value)
    {
        return (value ?? string.Empty)
            .Replace("%", "%25")
            .Replace("/", "%2F")
            .Replace("#", "%23");
    }

    private static string UnescapeSegment(string value)
    {
        return (value ?? string.Empty)
            .Replace("%23", "#")
            .Replace("%2F", "/")
            .Replace("%25", "%");
    }
}
