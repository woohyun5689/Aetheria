using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[FilePath("Library/AetheriaHudDraftState.asset", FilePathAttribute.Location.ProjectFolder)]
internal sealed class AetheriaHudDraftState : ScriptableSingleton<AetheriaHudDraftState>
{
    [SerializeField] private List<AetheriaHudLayoutEntry> entries = new List<AetheriaHudLayoutEntry>();

    public List<AetheriaHudLayoutEntry> Entries
    {
        get { return entries; }
    }

    public void SaveDraft()
    {
        Save(true);
    }
}

public sealed class AetheriaHudEditorWindow : EditorWindow
{
    private const string ProfileAssetPath = "Assets/Resources/UI/AetheriaHudLayoutProfile.asset";
    private const int PreviewTextureWidth = 1600;
    private const int PreviewTextureHeight = 900;
    private const int PreviewLayer = 30;
    private const float ToolbarHeight = 30f;
    private const float LeftPanelWidth = 270f;
    private const float RightPanelWidth = 330f;
    private const float HandleSize = 9f;
    private const float DragThreshold = 4f;
    private const float MinPreviewZoom = 0.25f;
    private const float MaxPreviewZoom = 4f;
    private const float ResizeEdgeHitSize = 10f;
    private const int FrontRenderOrder = 5000;

    private static readonly string[] ScreenKeys =
    {
        "Title",
        "MainMenu",
        "ClassSelect",
        "Guide",
        "Town",
        "Inventory",
        "Enhancement",
        "SkillTraining",
        "Crafting",
        "DungeonSelect",
        "Combat",
        "Victory",
        "Defeat",
        "GameClear"
    };

    private static readonly string[] ScreenLabels =
    {
        "타이틀",
        "메인 메뉴",
        "캐릭터 선택",
        "플레이 방법",
        "마을",
        "가방·장비",
        "강화소",
        "스킬 수련",
        "제작소",
        "던전 선택",
        "전투",
        "승리",
        "패배",
        "게임 클리어"
    };

    private readonly List<RectTransform> editableRects = new List<RectTransform>();
    private readonly List<RectTransform> selection = new List<RectTransform>();
    private readonly List<DragItem> dragItems = new List<DragItem>();
    private readonly List<Text> previewTexts = new List<Text>();
    private readonly Dictionary<Text, GUIStyle> previewTextStyles = new Dictionary<Text, GUIStyle>();

    private AetheriaGame game;
    private RectTransform sourceRoot;
    private RectTransform sourcePage;
    private RectTransform previewRoot;
    private RectTransform previewPage;
    private Canvas previewCanvas;
    private Camera previewCamera;
    private RenderTexture previewTexture;
    private GameObject previewCameraObject;
    private GameObject previewCanvasObject;

    private Vector2 objectScroll;
    private Vector2 inspectorScroll;
    private string search = string.Empty;
    private bool showAllRects;
    private bool showAllBounds;
    private int selectedScreenIndex = 1;
    private string status = "플레이 모드에서 화면을 선택하세요.";
    private Rect previewGuiRect;
    private Rect previewFitRect;
    private Rect previewViewportRect;
    private float previewZoom = 1f;
    private Vector2 previewPan;
    private bool previewPanning;
    private Vector2 previewPanStartMouse;
    private Vector2 previewPanStart;
    private bool capturePending;
    private double captureAt;
    private string requestedScreenKey;
    private double requestedScreenDeadline;
    private int requestedScreenStableTicks;

    private DragMode dragMode;
    private ResizeHandle resizeHandle;
    private Vector2 dragStartMouse;
    private Rect selectionStartBounds;
    private int undoGroup = -1;
    private bool pointerDownPending;
    private bool pointerDownInsideSelection;
    private bool pointerDownAdditive;
    private Vector2 pointerDownMouse;
    private RectTransform pointerDownHit;

    [MenuItem("Aetheria/HUD Editor (Staged) %#h", priority = 40)]
    public static void Open()
    {
        var window = GetWindow<AetheriaHudEditorWindow>();
        window.titleContent = new GUIContent("HUD Editor", EditorGUIUtility.IconContent("RectTool On").image);
        window.minSize = new Vector2(1120f, 680f);
        window.Show();
    }

    private AetheriaHudDraftState Draft
    {
        get { return AetheriaHudDraftState.instance; }
    }

    private void OnEnable()
    {
        showAllRects = false;
        showAllBounds = false;
        wantsMouseMove = true;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += EditorTick;
        Undo.undoRedoPerformed += OnUndoRedo;
        if (EditorApplication.isPlaying)
        {
            QueueCapture(0.15f);
        }
    }

    private void OnDisable()
    {
        Draft.SaveDraft();
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.update -= EditorTick;
        Undo.undoRedoPerformed -= OnUndoRedo;
        if (EditorApplication.isPlaying)
        {
            var runningGame = FindGame();
            if (runningGame != null)
            {
                runningGame.EditorEndHudPreview();
            }
        }
        DestroyPreview();
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            QueueCapture(0.5f);
        }
        else if (state == PlayModeStateChange.ExitingPlayMode
                 || state == PlayModeStateChange.EnteredEditMode)
        {
            capturePending = false;
            requestedScreenKey = null;
            requestedScreenStableTicks = 0;
            DestroyPreview();
            game = null;
            sourceRoot = null;
            sourcePage = null;
            status = "플레이 모드가 종료되었습니다. 초안은 보존됩니다.";
            Repaint();
        }
    }

    private void EditorTick()
    {
        if (capturePending && EditorApplication.timeSinceStartup >= captureAt
            && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            if (string.IsNullOrEmpty(requestedScreenKey))
            {
                capturePending = false;
                CaptureCurrentScreen();
            }
            else
            {
                TryCaptureRequestedScreen();
            }
        }

        if (EditorApplication.isPlaying && previewCamera != null)
        {
            Repaint();
        }
    }

    private void OnUndoRedo()
    {
        Draft.SaveDraft();
        QueueCapture(0.05f);
    }

    private void OnGUI()
    {
        DrawToolbar();

        var body = new Rect(0f, ToolbarHeight, position.width, position.height - ToolbarHeight);
        var leftWidth = Mathf.Min(LeftPanelWidth, body.width * 0.28f);
        var rightWidth = Mathf.Min(RightPanelWidth, body.width * 0.32f);
        var leftRect = new Rect(body.x, body.y, leftWidth, body.height);
        var rightRect = new Rect(body.xMax - rightWidth, body.y, rightWidth, body.height);
        var centerRect = new Rect(leftRect.xMax, body.y, Mathf.Max(100f, body.width - leftWidth - rightWidth), body.height);

        DrawObjectPanel(leftRect);
        DrawPreview(centerRect);
        DrawInspector(rightRect);
        HandleKeyboardShortcuts();
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight));

        if (!EditorApplication.isPlaying)
        {
            if (GUILayout.Button(new GUIContent("▶", "플레이 모드 시작"), EditorStyles.toolbarButton, GUILayout.Width(32f)))
            {
                EditorApplication.isPlaying = true;
            }
        }
        else
        {
            EditorGUI.BeginChangeCheck();
            selectedScreenIndex = EditorGUILayout.Popup(
                selectedScreenIndex,
                ScreenLabels,
                EditorStyles.toolbarPopup,
                GUILayout.Width(150f));
            if (EditorGUI.EndChangeCheck())
            {
                OpenSelectedScreen();
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh", "현재 실행 화면 다시 가져오기"), EditorStyles.toolbarButton, GUILayout.Width(30f)))
            {
                CaptureCurrentScreen();
            }
        }

        GUILayout.Space(6f);
        showAllBounds = GUILayout.Toggle(showAllBounds, new GUIContent("영역", "모든 UI 선택 영역 표시"), EditorStyles.toolbarButton, GUILayout.Width(48f));

        GUILayout.Space(4f);
        EditorGUI.BeginDisabledGroup(previewPage == null);
        if (GUILayout.Button(new GUIContent("−", "미리보기 축소"), EditorStyles.toolbarButton, GUILayout.Width(26f)))
        {
            SetPreviewZoom(previewZoom / 1.25f, previewViewportRect.center);
        }
        if (GUILayout.Button(
                new GUIContent(Mathf.RoundToInt(previewZoom * 100f) + "%", "미리보기를 화면에 맞춤"),
                EditorStyles.toolbarButton,
                GUILayout.Width(52f)))
        {
            ResetPreviewView();
        }
        if (GUILayout.Button(new GUIContent("+", "미리보기 확대"), EditorStyles.toolbarButton, GUILayout.Width(26f)))
        {
            SetPreviewZoom(previewZoom * 1.25f, previewViewportRect.center);
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button(new GUIContent("↶", "HUD 초안 실행 취소"), EditorStyles.toolbarButton, GUILayout.Width(30f)))
        {
            Undo.PerformUndo();
        }
        if (GUILayout.Button(new GUIContent("↷", "HUD 초안 다시 실행"), EditorStyles.toolbarButton, GUILayout.Width(30f)))
        {
            Undo.PerformRedo();
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("초안 " + Draft.Entries.Count + "개 · 원본 미적용", EditorStyles.miniLabel);
        GUILayout.Space(8f);

        EditorGUI.BeginDisabledGroup(Draft.Entries.Count == 0);
        if (GUILayout.Button(new GUIContent("초안 버리기", "저장하지 않은 HUD 변경을 모두 삭제"), EditorStyles.toolbarButton, GUILayout.Width(84f)))
        {
            DiscardAllDrafts();
        }
        var previousColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.42f, 0.78f, 0.52f, 1f);
        if (GUILayout.Button(new GUIContent("완료하여 적용", "모든 HUD 초안을 프로필에 저장하고 게임 화면에 적용"), EditorStyles.toolbarButton, GUILayout.Width(112f)))
        {
            CommitAllDrafts();
        }
        GUI.backgroundColor = previousColor;
        EditorGUI.EndDisabledGroup();

        GUILayout.EndHorizontal();
    }

    private void DrawObjectPanel(Rect rect)
    {
        GUILayout.BeginArea(rect, EditorStyles.helpBox);

        GUILayout.Label("게임 화면 (" + ScreenLabels.Length + ")", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        var nextScreenIndex = GUILayout.SelectionGrid(
            selectedScreenIndex,
            ScreenLabels,
            2,
            GUILayout.Height(Mathf.CeilToInt(ScreenLabels.Length / 2f) * 24f));
        EditorGUI.EndDisabledGroup();
        if (nextScreenIndex != selectedScreenIndex)
        {
            selectedScreenIndex = nextScreenIndex;
            OpenSelectedScreen();
        }

        GUILayout.Space(6f);
        GUILayout.Label("현재 화면 객체", EditorStyles.boldLabel);
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
        showAllRects = EditorGUILayout.ToggleLeft("그룹·빈 컨테이너 포함", showAllRects);

        objectScroll = EditorGUILayout.BeginScrollView(objectScroll);
        for (var i = 0; i < editableRects.Count; i++)
        {
            var item = editableRects[i];
            if (item == null || !MatchesSearch(item) || (!showAllRects && !HasVisibleUi(item)))
            {
                continue;
            }

            var selected = selection.Contains(item);
            var depth = HierarchyDepth(item, previewPage);
            GUILayout.BeginHorizontal(selected ? "SelectionRect" : GUIStyle.none);
            GUILayout.Space(Mathf.Min(42f, depth * 10f));
            var displayName = ObjectDisplayName(item);
            var iconObject = PrimaryUiComponent(item);
            var icon = iconObject != null
                ? EditorGUIUtility.ObjectContent(iconObject, iconObject.GetType()).image
                : EditorGUIUtility.ObjectContent(item.gameObject, typeof(RectTransform)).image;
            var tooltip = AetheriaHudLayoutRuntime.BuildRelativeNamePath(item, previewPage);
            if (GUILayout.Button(new GUIContent(displayName, icon, tooltip), EditorStyles.label, GUILayout.Height(20f)))
            {
                SelectFromList(item, Event.current.control || Event.current.command);
            }
            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(4f);
        GUILayout.Label(status, EditorStyles.wordWrappedMiniLabel);
        GUILayout.EndArea();
    }

    private void DrawPreview(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0.075f, 0.082f, 0.095f, 1f));
        var inner = new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f);

        if (previewTexture == null || previewCamera == null || previewPage == null)
        {
            DrawCenteredMessage(inner, EditorApplication.isPlaying
                ? "현재 HUD 화면을 가져오는 중입니다."
                : "플레이 모드를 시작하면 HUD 미리보기가 표시됩니다.");
            return;
        }

        previewViewportRect = inner;
        previewFitRect = FitAspect(inner, (float)PreviewTextureWidth / PreviewTextureHeight);
        ClampPreviewPan();
        previewGuiRect = ZoomedPreviewRect();

        var absolutePreviewRect = previewGuiRect;
        GUI.BeginGroup(inner);
        previewGuiRect.position -= inner.position;
        if (Event.current.type == EventType.Repaint)
        {
            Canvas.ForceUpdateCanvases();
            previewCamera.Render();
            GUI.DrawTexture(previewGuiRect, previewTexture, ScaleMode.StretchToFill, false);
            DrawPreviewTexts();
        }
        DrawSelectionOverlay();
        GUI.EndGroup();
        previewGuiRect = absolutePreviewRect;

        HandlePreviewInput(Event.current);
    }

    private void DrawInspector(Rect rect)
    {
        GUILayout.BeginArea(rect, EditorStyles.helpBox);
        GUILayout.Label("선택 UI", EditorStyles.boldLabel);
        inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);

        var target = PrimarySelection();
        if (target == null)
        {
            GUILayout.Label("미리보기나 UI 목록에서 항목을 선택하세요.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }

        EditorGUILayout.LabelField(target.name, EditorStyles.boldLabel);
        if (selection.Count > 1)
        {
            EditorGUILayout.HelpBox(selection.Count + "개 UI가 선택되었습니다. 이동, 외곽 크기와 표시 순서가 함께 적용됩니다.", MessageType.None);
        }
        EditorGUILayout.LabelField("화면", previewPage.name);
        EditorGUILayout.LabelField("경로", AetheriaHudLayoutRuntime.BuildRelativeNamePath(target, previewPage), EditorStyles.wordWrappedMiniLabel);

        GUILayout.Space(6f);
        DrawTransformInspector(target);
        DrawRenderOrderInspector(target);
        DrawImageInspector(target);
        DrawTextInspector(target);
        DrawParticleInspector(target);

        GUILayout.Space(10f);
        var parentLayout = FindNearestLayout(target);
        EditorGUILayout.LabelField(
            "레이아웃",
            parentLayout == null
                ? "독립"
                : parentLayout.enabled ? "편집 시 자동 고정" : "초안에서 고정됨");

        if (GUILayout.Button("선택 UI 초안 되돌리기", GUILayout.Height(28f)))
        {
            ResetSelectedDrafts();
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawTransformInspector(RectTransform target)
    {
        GUILayout.Label("배치", EditorStyles.boldLabel);

        var originalPosition = target.anchoredPosition;
        EditorGUI.BeginChangeCheck();
        var positionValue = EditorGUILayout.Vector2Field("위치", target.anchoredPosition);
        var positionChanged = EditorGUI.EndChangeCheck();

        EditorGUI.BeginChangeCheck();
        var sizeValue = EditorGUILayout.Vector2Field("크기", target.rect.size);
        var sizeChanged = EditorGUI.EndChangeCheck();

        EditorGUI.BeginChangeCheck();
        var rotationValue = EditorGUILayout.FloatField("회전", target.localEulerAngles.z);
        var rotationChanged = EditorGUI.EndChangeCheck();

        EditorGUI.BeginChangeCheck();
        var scaleValue = EditorGUILayout.Vector2Field("배율", new Vector2(target.localScale.x, target.localScale.y));
        var scaleChanged = EditorGUI.EndChangeCheck();
        if (!positionChanged && !sizeChanged && !rotationChanged && !scaleChanged)
        {
            return;
        }

        RecordDraftUndo("HUD 배치 변경");
        PrepareSelectionForIndependentEdit();
        if (positionChanged)
        {
            target.anchoredPosition += positionValue - originalPosition;
        }
        if (sizeChanged)
        {
            SetManualSize(target, sizeValue);
        }
        if (rotationChanged)
        {
            target.localEulerAngles = new Vector3(0f, 0f, rotationValue);
        }
        if (scaleChanged)
        {
            target.localScale = new Vector3(scaleValue.x, scaleValue.y, target.localScale.z);
        }
        UpdateDraftFromTarget(target);
        FinishDraftChange();
    }

    private void DrawImageInspector(RectTransform target)
    {
        var image = target.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        GUILayout.Space(8f);
        GUILayout.Label("이미지", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        var sprite = (Sprite)EditorGUILayout.ObjectField("스프라이트", image.sprite, typeof(Sprite), false);
        var color = EditorGUILayout.ColorField("색상", image.color);
        var preserveAspect = EditorGUILayout.Toggle("비율 유지", image.preserveAspect);
        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        RecordDraftUndo("HUD 이미지 변경");
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = preserveAspect;
        var record = UpdateDraftFromTarget(target);
        record.overrideImage = true;
        record.imageSprite = sprite;
        record.imageColor = color;
        record.preserveImageAspect = preserveAspect;
        FinishDraftChange();
    }

    private void DrawRenderOrderInspector(RectTransform target)
    {
        var canvas = target.GetComponent<Canvas>();
        var isForeground = canvas != null && canvas.overrideSorting && canvas.sortingOrder > 0;

        GUILayout.Space(8f);
        GUILayout.Label("표시 순서", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("현재", isForeground ? "맨 앞으로" : "기본 순서");
        EditorGUILayout.HelpBox("위치를 옮긴 UI가 다른 패널에 가려질 때 맨 앞으로 올릴 수 있습니다.", MessageType.None);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("맨 앞으로", "선택한 UI를 다른 HUD 위에 표시합니다."), GUILayout.Height(26f)))
        {
            SetSelectionRenderOrder(FrontRenderOrder);
        }
        if (GUILayout.Button(new GUIContent("기본 순서", "원래 계층의 표시 순서를 사용합니다."), GUILayout.Height(26f)))
        {
            SetSelectionRenderOrder(0);
        }
        GUILayout.EndHorizontal();
    }

    private void SetSelectionRenderOrder(int renderOrder)
    {
        if (selection.Count == 0)
        {
            return;
        }

        RecordDraftUndo(renderOrder > 0 ? "HUD 맨 앞으로" : "HUD 기본 표시 순서");
        for (var i = 0; i < selection.Count; i++)
        {
            var target = selection[i];
            if (target == null)
            {
                continue;
            }

            var record = UpdateDraftFromTarget(target);
            record.overrideRenderOrder = true;
            record.renderOrder = renderOrder;
            AetheriaHudLayoutRuntime.ApplyRenderOrder(target, renderOrder);
        }
        FinishDraftChange();
        Repaint();
    }

    private void DrawTextInspector(RectTransform target)
    {
        var text = target.GetComponent<Text>();
        if (text == null)
        {
            return;
        }

        GUILayout.Space(8f);
        GUILayout.Label("글자", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        var value = EditorGUILayout.TextArea(text.text, GUILayout.MinHeight(52f));
        var color = EditorGUILayout.ColorField("색상", text.color);
        var fontSize = EditorGUILayout.IntField("크기", text.fontSize);
        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        RecordDraftUndo("HUD 글자 변경");
        text.text = value;
        text.color = color;
        text.fontSize = Mathf.Max(1, fontSize);
        text.resizeTextMaxSize = Mathf.Max(text.resizeTextMinSize, text.fontSize);
        var record = UpdateDraftFromTarget(target);
        record.overrideText = true;
        record.text = value;
        record.textColor = color;
        record.fontSize = text.fontSize;
        FinishDraftChange();
    }

    private void DrawParticleInspector(RectTransform target)
    {
        var particles = target.GetComponent<ParticleSystem>();
        if (particles == null)
        {
            return;
        }

        var main = particles.main;
        var emission = particles.emission;
        GUILayout.Space(8f);
        GUILayout.Label("파티클", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        var simulationSpeed = EditorGUILayout.FloatField("재생 속도", main.simulationSpeed);
        var startSize = EditorGUILayout.FloatField("시작 크기", main.startSizeMultiplier);
        var startSpeed = EditorGUILayout.FloatField("이동 속도", main.startSpeedMultiplier);
        var emissionRate = EditorGUILayout.FloatField("생성량", emission.rateOverTimeMultiplier);
        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        RecordDraftUndo("HUD 파티클 변경");
        main.simulationSpeed = Mathf.Max(0f, simulationSpeed);
        main.startSizeMultiplier = Mathf.Max(0f, startSize);
        main.startSpeedMultiplier = startSpeed;
        emission.rateOverTimeMultiplier = Mathf.Max(0f, emissionRate);
        var record = UpdateDraftFromTarget(target);
        record.overrideParticle = true;
        record.particleSimulationSpeed = main.simulationSpeed;
        record.particleStartSize = main.startSizeMultiplier;
        record.particleStartSpeed = main.startSpeedMultiplier;
        record.particleEmissionRate = emission.rateOverTimeMultiplier;
        FinishDraftChange();
    }

    private void OpenSelectedScreen()
    {
        game = FindGame();
        if (game == null)
        {
            status = "Aetheria 실행 인스턴스를 찾지 못했습니다.";
            return;
        }

        requestedScreenKey = ScreenKeys[Mathf.Clamp(selectedScreenIndex, 0, ScreenKeys.Length - 1)];
        requestedScreenDeadline = EditorApplication.timeSinceStartup + 5d;
        requestedScreenStableTicks = 0;
        DestroyPreview();
        game.EditorOpenHudPreview(requestedScreenKey);
        status = ScreenLabels[selectedScreenIndex] + " 화면을 준비하고 있습니다.";
        QueueCapture(0.2f);
    }

    private void TryCaptureRequestedScreen()
    {
        game = FindGame();
        var ready = game != null
            && game.EditorCurrentHudScreenKey == requestedScreenKey
            && game.EditorCurrentHudPage != null;

        if (!ready)
        {
            requestedScreenStableTicks = 0;
            if (EditorApplication.timeSinceStartup >= requestedScreenDeadline)
            {
                capturePending = false;
                status = "요청한 화면 전환을 확인하지 못했습니다. 다시 선택해 주세요.";
                requestedScreenKey = null;
                Repaint();
                return;
            }

            captureAt = EditorApplication.timeSinceStartup + 0.1d;
            return;
        }

        // ClearRoot destroys the previous page at the end of the frame. Waiting
        // for two editor ticks prevents cloning that outgoing page by mistake.
        requestedScreenStableTicks++;
        if (requestedScreenStableTicks < 2)
        {
            captureAt = EditorApplication.timeSinceStartup + 0.08d;
            return;
        }

        capturePending = false;
        requestedScreenKey = null;
        requestedScreenStableTicks = 0;
        CaptureCurrentScreen();
    }

    private void CaptureCurrentScreen()
    {
        if (!EditorApplication.isPlaying)
        {
            status = "플레이 모드가 아닙니다.";
            DestroyPreview();
            return;
        }

        game = FindGame();
        sourceRoot = game != null ? game.EditorHudRoot : null;
        sourcePage = game != null ? game.EditorCurrentHudPage : null;
        if (sourceRoot == null || sourcePage == null)
        {
            status = "현재 HUD 화면을 찾지 못했습니다.";
            DestroyPreview();
            return;
        }

        var currentScreenKey = game.EditorCurrentHudScreenKey;
        var currentScreenIndex = Array.IndexOf(ScreenKeys, currentScreenKey);
        if (currentScreenIndex >= 0)
        {
            selectedScreenIndex = currentScreenIndex;
        }

        DestroyPreview();
        CreatePreview(sourceRoot, sourcePage);
        status = sourcePage.name + " · 복제 미리보기 · 원본 미적용";
        Repaint();
    }

    private void CreatePreview(RectTransform liveRoot, RectTransform livePage)
    {
        // Editing happens on this hidden clone; the live HUD is untouched until CommitAllDrafts.
        Canvas.ForceUpdateCanvases();
        previewCameraObject = EditorUtility.CreateGameObjectWithHideFlags(
            "Aetheria HUD Preview Camera",
            HideFlags.HideAndDontSave,
            typeof(Camera));
        previewCameraObject.layer = PreviewLayer;
        previewCamera = previewCameraObject.GetComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.025f, 0.03f, 0.045f, 1f);
        previewCamera.orthographic = true;
        previewCamera.transform.position = new Vector3(0f, 0f, -10f);
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 100f;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.enabled = false;

        previewTexture = new RenderTexture(
            PreviewTextureWidth,
            PreviewTextureHeight,
            24,
            RenderTextureFormat.ARGB32)
        {
            name = "Aetheria HUD Preview",
            hideFlags = HideFlags.HideAndDontSave,
            antiAliasing = 1
        };
        previewTexture.Create();
        previewCamera.targetTexture = previewTexture;

        previewCanvasObject = EditorUtility.CreateGameObjectWithHideFlags(
            "Aetheria HUD Preview Canvas",
            HideFlags.HideAndDontSave,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        previewCanvasObject.layer = PreviewLayer;
        previewCanvas = previewCanvasObject.GetComponent<Canvas>();
        previewCanvas.renderMode = RenderMode.WorldSpace;
        previewCanvas.worldCamera = previewCamera;

        var destinationScaler = previewCanvasObject.GetComponent<CanvasScaler>();
        var sourceScaler = liveRoot.GetComponentInParent<CanvasScaler>();
        var referenceResolution = new Vector2(1920f, 1080f);
        if (sourceScaler != null)
        {
            referenceResolution = sourceScaler.referenceResolution;
            destinationScaler.referencePixelsPerUnit = sourceScaler.referencePixelsPerUnit;
        }
        else
        {
            destinationScaler.referencePixelsPerUnit = 100f;
        }
        destinationScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        destinationScaler.scaleFactor = 1f;
        destinationScaler.dynamicPixelsPerUnit = 100f;

        var previewCanvasRect = previewCanvasObject.GetComponent<RectTransform>();
        previewCanvasRect.sizeDelta = referenceResolution;
        previewCanvasRect.localScale = Vector3.one * 0.01f;
        previewCanvasRect.position = Vector3.zero;
        previewCamera.orthographicSize = referenceResolution.y * 0.005f;

        var livePagePath = AetheriaHudLayoutRuntime.BuildRelativePath(livePage, liveRoot);
        var livePageNamePath = AetheriaHudLayoutRuntime.BuildRelativeNamePath(livePage, liveRoot);
        var cloneObject = Instantiate(liveRoot.gameObject);
        cloneObject.name = liveRoot.name;
        cloneObject.transform.SetParent(previewCanvasObject.transform, false);
        PreparePreviewTexts(liveRoot.gameObject, cloneObject);
        SetLayerAndHideFlags(cloneObject.transform);
        DisablePreviewBehaviours(cloneObject);

        previewRoot = cloneObject.GetComponent<RectTransform>();
        previewPage = AetheriaHudLayoutRuntime.ResolveRelativePath(
            previewRoot,
            livePagePath,
            livePageNamePath) as RectTransform;
        HideOutgoingPreviewPages();

        Canvas.ForceUpdateCanvases();
        if (previewRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(previewRoot);
        }
        ApplyDraftsToPreview();
        RebuildEditableList();
    }

    private void HideOutgoingPreviewPages()
    {
        if (previewRoot == null || previewPage == null || game == null)
        {
            return;
        }

        // Play-mode Destroy is deferred until the end of the frame. A quick HUD
        // switch can therefore leave the outgoing page beside the new one while
        // the root is cloned. Keep global backdrops/FX, but hide stale page roots.
        for (var i = 0; i < previewRoot.childCount; i++)
        {
            var child = previewRoot.GetChild(i);
            if (child != previewPage && game.EditorIsHudScreenPageName(child.name))
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void DestroyPreview()
    {
        editableRects.Clear();
        selection.Clear();
        dragItems.Clear();
        dragMode = DragMode.None;
        previewRoot = null;
        previewPage = null;
        previewCanvas = null;
        previewPanning = false;
        pointerDownPending = false;
        pointerDownHit = null;
        previewTexts.Clear();
        previewTextStyles.Clear();

        if (previewCamera != null)
        {
            previewCamera.targetTexture = null;
        }
        previewCamera = null;

        if (previewCanvasObject != null)
        {
            DestroyImmediate(previewCanvasObject);
            previewCanvasObject = null;
        }
        if (previewCameraObject != null)
        {
            DestroyImmediate(previewCameraObject);
            previewCameraObject = null;
        }

        if (previewTexture != null)
        {
            previewTexture.Release();
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }

    }

    private void PreparePreviewTexts(GameObject liveRootObject, GameObject cloneRootObject)
    {
        var liveTexts = liveRootObject.GetComponentsInChildren<Text>(true);
        var cloneTexts = cloneRootObject.GetComponentsInChildren<Text>(true);
        var count = Mathf.Min(liveTexts.Length, cloneTexts.Length);
        for (var i = 0; i < count; i++)
        {
            var source = liveTexts[i];
            var clone = cloneTexts[i];
            if (source == null || clone == null)
            {
                continue;
            }

            var renderedSize = source.fontSize;
            if (source.resizeTextForBestFit)
            {
                var generatedSize = source.cachedTextGenerator.fontSizeUsedForBestFit;
                if (generatedSize > 0)
                {
                    renderedSize = generatedSize;
                }
                renderedSize = Mathf.Clamp(renderedSize, source.resizeTextMinSize, source.resizeTextMaxSize);
            }

            clone.resizeTextForBestFit = false;
            clone.fontSize = Mathf.Max(1, renderedSize);
            clone.enabled = false;
            previewTexts.Add(clone);
        }
    }

    private void DrawPreviewTexts()
    {
        if (previewPage == null || previewGuiRect.width <= 0f)
        {
            return;
        }

        var scale = previewGuiRect.width / Mathf.Max(1f, previewPage.rect.width);
        for (var i = 0; i < previewTexts.Count; i++)
        {
            var text = previewTexts[i];
            if (text == null || !text.gameObject.activeInHierarchy || string.IsNullOrEmpty(text.text))
            {
                continue;
            }

            GUIStyle style;
            if (!previewTextStyles.TryGetValue(text, out style))
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(),
                    margin = new RectOffset(),
                    richText = text.supportRichText
                };
                previewTextStyles[text] = style;
            }

            var rect = RectToGui(text.rectTransform);
            style.alignment = text.alignment;
            style.fontStyle = text.fontStyle;
            style.fontSize = Mathf.Max(8, Mathf.RoundToInt(text.fontSize * scale));
            style.wordWrap = text.horizontalOverflow == HorizontalWrapMode.Wrap;
            style.clipping = TextClipping.Clip;

            var color = text.color;
            color.a *= InheritedCanvasAlpha(text.transform);
            if (color.a <= 0.001f)
            {
                continue;
            }

            var outline = text.GetComponent<Outline>();
            var shadow = text.GetComponent<Shadow>();
            if (outline != null && outline.enabled)
            {
                var distance = Mathf.Max(1f, Mathf.Abs(outline.effectDistance.x * scale));
                DrawTextEffect(rect, text.text, style, outline.effectColor, color.a, distance, true);
            }
            else if (shadow != null && shadow.enabled)
            {
                var offset = shadow.effectDistance * scale;
                var shadowRect = new Rect(rect.position + new Vector2(offset.x, -offset.y), rect.size);
                var effectColor = shadow.effectColor;
                effectColor.a *= color.a;
                style.normal.textColor = effectColor;
                GUI.Label(shadowRect, text.text, style);
            }

            style.normal.textColor = color;
            GUI.Label(rect, text.text, style);
        }
    }

    private static void DrawTextEffect(
        Rect rect,
        string value,
        GUIStyle style,
        Color effectColor,
        float inheritedAlpha,
        float distance,
        bool outline)
    {
        effectColor.a *= inheritedAlpha;
        style.normal.textColor = effectColor;
        var offsets = outline
            ? new[]
            {
                new Vector2(-distance, 0f), new Vector2(distance, 0f),
                new Vector2(0f, -distance), new Vector2(0f, distance)
            }
            : new[] { new Vector2(distance, distance) };
        for (var i = 0; i < offsets.Length; i++)
        {
            GUI.Label(new Rect(rect.position + offsets[i], rect.size), value, style);
        }
    }

    private static float InheritedCanvasAlpha(Transform target)
    {
        var alpha = 1f;
        for (var current = target; current != null; current = current.parent)
        {
            var group = current.GetComponent<CanvasGroup>();
            if (group != null)
            {
                alpha *= group.alpha;
                if (group.ignoreParentGroups)
                {
                    break;
                }
            }
        }
        return alpha;
    }

    private void ApplyDraftsToPreview()
    {
        if (previewPage == null)
        {
            return;
        }

        var temporaryProfile = CreateInstance<AetheriaHudLayoutProfile>();
        for (var i = 0; i < Draft.Entries.Count; i++)
        {
            var entry = Draft.Entries[i];
            if (entry != null && entry.screenName == previewPage.name)
            {
                temporaryProfile.entries.Add(entry);
            }
        }
        AetheriaHudLayoutRuntime.ApplyToScreen(previewPage, temporaryProfile);
        DestroyImmediate(temporaryProfile);

        var migrated = false;
        for (var i = 0; i < Draft.Entries.Count; i++)
        {
            var entry = Draft.Entries[i];
            if (entry == null || entry.screenName != previewPage.name)
            {
                continue;
            }

            var target = AetheriaHudLayoutRuntime.ResolveRelativePath(
                previewPage,
                entry.hierarchyPath,
                entry.hierarchyNamePath) as RectTransform;
            if (target == null)
            {
                continue;
            }

            if (entry.anchorMin != target.anchorMin
                || entry.anchorMax != target.anchorMax
                || entry.anchoredPosition != target.anchoredPosition
                || entry.sizeDelta != target.sizeDelta)
            {
                UpdateDraftTransform(entry, target);
                migrated = true;
            }
        }
        if (migrated)
        {
            EditorUtility.SetDirty(Draft);
            Draft.SaveDraft();
        }
    }

    private void RebuildEditableList()
    {
        editableRects.Clear();
        selection.Clear();
        if (previewPage == null)
        {
            return;
        }

        var rects = previewPage.GetComponentsInChildren<RectTransform>(true);
        for (var i = 0; i < rects.Length; i++)
        {
            if (rects[i] != null && rects[i] != previewPage)
            {
                editableRects.Add(rects[i]);
            }
        }
    }

    private void DisablePreviewBehaviours(GameObject rootObject)
    {
        var behaviours = rootObject.GetComponentsInChildren<MonoBehaviour>(true);
        for (var i = 0; i < behaviours.Length; i++)
        {
            var behaviour = behaviours[i];
            if (behaviour == null || IsRequiredPreviewBehaviour(behaviour))
            {
                continue;
            }
            behaviour.enabled = false;
        }

        var animators = rootObject.GetComponentsInChildren<Animator>(true);
        for (var i = 0; i < animators.Length; i++)
        {
            animators[i].enabled = false;
        }
    }

    private static bool IsRequiredPreviewBehaviour(MonoBehaviour behaviour)
    {
        return behaviour is Graphic
            || behaviour is LayoutGroup
            || behaviour is LayoutElement
            || behaviour is ContentSizeFitter
            || behaviour is AspectRatioFitter
            || behaviour is Mask
            || behaviour is RectMask2D
            || behaviour is Shadow
            || behaviour is CanvasScaler;
    }

    private static void SetLayerAndHideFlags(Transform target)
    {
        target.gameObject.layer = PreviewLayer;
        target.gameObject.hideFlags = HideFlags.HideAndDontSave;
        for (var i = 0; i < target.childCount; i++)
        {
            SetLayerAndHideFlags(target.GetChild(i));
        }
    }

    private void DrawSelectionOverlay()
    {
        if (previewPage == null || previewCamera == null)
        {
            return;
        }

        if (showAllBounds)
        {
            for (var i = 0; i < editableRects.Count; i++)
            {
                var item = editableRects[i];
                if (item == null || !item.gameObject.activeInHierarchy || selection.Contains(item))
                {
                    continue;
                }
                DrawRectOutline(RectToGui(item), new Color(0.35f, 0.72f, 0.95f, 0.22f), 1f);
            }
        }

        if (selection.Count == 0)
        {
            return;
        }

        var bounds = SelectionGuiBounds();
        DrawRectOutline(bounds, new Color(0.12f, 0.82f, 1f, 1f), 2f);
        DrawResizeHandles(bounds);
    }

    private void HandlePreviewInput(Event currentEvent)
    {
        if (previewPage == null)
        {
            return;
        }

        var insideViewport = previewViewportRect.Contains(currentEvent.mousePosition);
        var insidePreview = previewGuiRect.Contains(currentEvent.mousePosition) && insideViewport;
        var selectionBounds = selection.Count > 0 ? SelectionGuiBounds() : default(Rect);
        var resizeInteractionRect = ExpandedRect(previewViewportRect, HandleSize * 0.5f);
        var hoveredResizeHandle = selection.Count > 0 && resizeInteractionRect.Contains(currentEvent.mousePosition)
            ? HitResizeHandle(selectionBounds, currentEvent.mousePosition)
            : ResizeHandle.None;

        if (currentEvent.type == EventType.ScrollWheel && insideViewport)
        {
            var factor = Mathf.Pow(1.12f, -currentEvent.delta.y);
            SetPreviewZoom(previewZoom * factor, currentEvent.mousePosition);
            currentEvent.Use();
            return;
        }

        if (currentEvent.type == EventType.MouseDown
            && insideViewport
            && (currentEvent.button == 2 || (currentEvent.button == 0 && currentEvent.alt)))
        {
            previewPanning = true;
            previewPanStartMouse = currentEvent.mousePosition;
            previewPanStart = previewPan;
            pointerDownPending = false;
            GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
            currentEvent.Use();
            return;
        }

        if (previewPanning)
        {
            if (currentEvent.type == EventType.MouseDrag)
            {
                previewPan = previewPanStart + currentEvent.mousePosition - previewPanStartMouse;
                ClampPreviewPan();
                Repaint();
                currentEvent.Use();
                return;
            }
            if (currentEvent.type == EventType.MouseUp
                && (currentEvent.button == 2 || currentEvent.button == 0))
            {
                previewPanning = false;
                GUIUtility.hotControl = 0;
                currentEvent.Use();
                return;
            }
        }

        if (!insidePreview
            && hoveredResizeHandle == ResizeHandle.None
            && dragMode == DragMode.None
            && !pointerDownPending)
        {
            return;
        }

        if (insideViewport || hoveredResizeHandle != ResizeHandle.None)
        {
            AddHandleCursors(selectionBounds);
        }
        if (insideViewport && (previewPanning || currentEvent.alt))
        {
            EditorGUIUtility.AddCursorRect(previewViewportRect, MouseCursor.Pan);
        }

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1 && insidePreview)
        {
            ShowObjectSelectionMenu(currentEvent.mousePosition);
            currentEvent.Use();
            return;
        }

        if (currentEvent.type == EventType.MouseDown
            && currentEvent.button == 0
            && hoveredResizeHandle != ResizeHandle.None)
        {
            pointerDownPending = false;
            BeginDrag(DragMode.Resize, hoveredResizeHandle, currentEvent.mousePosition);
            currentEvent.Use();
            return;
        }

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && insidePreview)
        {
            pointerDownMouse = currentEvent.mousePosition;
            pointerDownHit = HitTest(currentEvent.mousePosition);
            pointerDownAdditive = currentEvent.control || currentEvent.command;
            pointerDownInsideSelection = !pointerDownAdditive && IsPointInsideSelection(currentEvent.mousePosition);
            pointerDownPending = true;

            if (!pointerDownInsideSelection)
            {
                if (pointerDownHit == null)
                {
                    if (!pointerDownAdditive)
                    {
                        selection.Clear();
                    }
                    pointerDownPending = false;
                }
                else
                {
                    SelectFromPreview(pointerDownHit, pointerDownAdditive);
                }
            }
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseDrag && pointerDownPending)
        {
            if (Vector2.Distance(pointerDownMouse, currentEvent.mousePosition) >= DragThreshold
                && selection.Count > 0)
            {
                BeginDrag(DragMode.Move, ResizeHandle.None, pointerDownMouse);
                pointerDownPending = false;
                UpdateDrag(currentEvent.mousePosition, currentEvent.shift);
            }
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseDrag && dragMode != DragMode.None)
        {
            UpdateDrag(currentEvent.mousePosition, currentEvent.shift);
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 && dragMode != DragMode.None)
        {
            EndDrag();
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 && pointerDownPending)
        {
            if (pointerDownInsideSelection)
            {
                if (pointerDownHit != null)
                {
                    SelectFromPreview(pointerDownHit, false);
                }
                else
                {
                    selection.Clear();
                }
            }
            pointerDownPending = false;
            pointerDownHit = null;
            Repaint();
            currentEvent.Use();
        }
    }

    private void BeginDrag(DragMode mode, ResizeHandle handle, Vector2 mousePosition)
    {
        if (selection.Count == 0)
        {
            return;
        }

        RecordDraftUndo(mode == DragMode.Move ? "HUD UI 이동" : "HUD UI 크기 조절");
        PrepareSelectionForIndependentEdit();
        Canvas.ForceUpdateCanvases();

        dragMode = mode;
        resizeHandle = handle;
        dragStartMouse = mousePosition;
        selectionStartBounds = SelectionPageBounds();
        dragItems.Clear();

        for (var i = 0; i < selection.Count; i++)
        {
            var target = selection[i];
            if (target == null)
            {
                continue;
            }
            dragItems.Add(new DragItem
            {
                target = target,
                anchoredPosition = target.anchoredPosition,
                size = target.rect.size,
                pivotPositionInPage = previewPage.InverseTransformPoint(target.position)
            });
        }
        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
    }

    private void UpdateDrag(Vector2 mousePosition, bool axisLock)
    {
        if (dragItems.Count == 0)
        {
            return;
        }

        var pageDelta = GuiDeltaToPage(mousePosition - dragStartMouse);
        if (dragMode == DragMode.Move)
        {
            if (axisLock)
            {
                pageDelta = Mathf.Abs(pageDelta.x) >= Mathf.Abs(pageDelta.y)
                    ? new Vector2(pageDelta.x, 0f)
                    : new Vector2(0f, pageDelta.y);
            }

            for (var i = 0; i < dragItems.Count; i++)
            {
                dragItems[i].target.anchoredPosition = dragItems[i].anchoredPosition + pageDelta;
                SetManualSize(dragItems[i].target, dragItems[i].size);
                UpdateDraftFromTarget(dragItems[i].target);
            }
        }
        else if (dragMode == DragMode.Resize)
        {
            ResizeSelection(pageDelta, axisLock);
        }
        Repaint();
    }

    private void ResizeSelection(Vector2 pageDelta, bool uniform)
    {
        var oldBounds = selectionStartBounds;
        var newBounds = oldBounds;
        if (HandleUsesLeft(resizeHandle))
        {
            newBounds.xMin += pageDelta.x;
        }
        if (HandleUsesRight(resizeHandle))
        {
            newBounds.xMax += pageDelta.x;
        }
        if (HandleUsesBottom(resizeHandle))
        {
            newBounds.yMin += pageDelta.y;
        }
        if (HandleUsesTop(resizeHandle))
        {
            newBounds.yMax += pageDelta.y;
        }

        newBounds.width = Mathf.Max(12f, newBounds.width);
        newBounds.height = Mathf.Max(12f, newBounds.height);
        var scaleX = newBounds.width / Mathf.Max(1f, oldBounds.width);
        var scaleY = newBounds.height / Mathf.Max(1f, oldBounds.height);
        if (uniform)
        {
            var scale = Mathf.Abs(scaleX - 1f) >= Mathf.Abs(scaleY - 1f) ? scaleX : scaleY;
            scaleX = scale;
            scaleY = scale;
            newBounds.width = oldBounds.width * scale;
            newBounds.height = oldBounds.height * scale;
        }

        for (var i = 0; i < dragItems.Count; i++)
        {
            var item = dragItems[i];
            var normalizedX = oldBounds.width <= 0.001f ? 0.5f : (item.pivotPositionInPage.x - oldBounds.xMin) / oldBounds.width;
            var normalizedY = oldBounds.height <= 0.001f ? 0.5f : (item.pivotPositionInPage.y - oldBounds.yMin) / oldBounds.height;
            var newPivotInPage = new Vector2(
                Mathf.Lerp(newBounds.xMin, newBounds.xMax, normalizedX),
                Mathf.Lerp(newBounds.yMin, newBounds.yMax, normalizedY));

            SetManualSize(item.target, new Vector2(item.size.x * scaleX, item.size.y * scaleY));
            item.target.position = previewPage.TransformPoint(newPivotInPage);
            UpdateDraftFromTarget(item.target);
        }
    }

    private void EndDrag()
    {
        dragMode = DragMode.None;
        resizeHandle = ResizeHandle.None;
        dragItems.Clear();
        pointerDownPending = false;
        pointerDownHit = null;
        GUIUtility.hotControl = 0;
        FinishDraftChange();
        if (undoGroup >= 0)
        {
            Undo.CollapseUndoOperations(undoGroup);
            undoGroup = -1;
        }
    }

    private void PrepareSelectionForIndependentEdit()
    {
        Canvas.ForceUpdateCanvases();
        if (previewPage != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(previewPage);
        }

        for (var i = 0; i < selection.Count; i++)
        {
            PrepareForIndependentEdit(selection[i]);
        }
    }

    private void PrepareForIndependentEdit(RectTransform target)
    {
        if (target == null || previewPage == null)
        {
            return;
        }

        var layout = FindNearestLayout(target);
        var record = GetOrCreateDraft(target);
        if (layout != null)
        {
            // Freeze the calculated layout before moving one item so its siblings keep their current geometry.
            record.frozenLayoutPath = AetheriaHudLayoutRuntime.BuildRelativePath(layout.transform, previewPage);
            record.frozenLayoutNamePath = AetheriaHudLayoutRuntime.BuildRelativeNamePath(layout.transform, previewPage);
            layout.enabled = false;
            var layoutFitter = layout.GetComponent<ContentSizeFitter>();
            if (layoutFitter != null)
            {
                layoutFitter.enabled = false;
            }
        }

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
        AetheriaHudLayoutRuntime.LockManualSize(target);
        UpdateDraftTransform(record, target);
    }

    private static void SetManualSize(RectTransform target, Vector2 size)
    {
        if (target == null)
        {
            return;
        }

        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(8f, size.x));
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(8f, size.y));
    }

    private static void UpdateDraftTransform(AetheriaHudLayoutEntry record, RectTransform target)
    {
        record.anchorMin = target.anchorMin;
        record.anchorMax = target.anchorMax;
        record.pivot = target.pivot;
        record.anchoredPosition = target.anchoredPosition;
        record.sizeDelta = target.sizeDelta;
        record.localEulerAngles = target.localEulerAngles;
        record.localScale = target.localScale;
    }

    private AetheriaHudLayoutEntry UpdateDraftFromTarget(RectTransform target)
    {
        var record = GetOrCreateDraft(target);
        UpdateDraftTransform(record, target);
        return record;
    }

    private AetheriaHudLayoutEntry GetOrCreateDraft(RectTransform target)
    {
        var path = AetheriaHudLayoutRuntime.BuildRelativePath(target, previewPage);
        for (var i = 0; i < Draft.Entries.Count; i++)
        {
            var existing = Draft.Entries[i];
            if (existing.screenName == previewPage.name && existing.hierarchyPath == path)
            {
                return existing;
            }
        }

        var namePath = AetheriaHudLayoutRuntime.BuildRelativeNamePath(target, previewPage);
        var entry = new AetheriaHudLayoutEntry();
        var profile = AssetDatabase.LoadAssetAtPath<AetheriaHudLayoutProfile>(ProfileAssetPath);
        var savedEntry = FindProfileEntry(profile, previewPage.name, path, namePath);
        if (savedEntry != null)
        {
            CopyEntry(savedEntry, entry);
        }
        entry.screenName = previewPage.name;
        entry.hierarchyPath = path;
        entry.hierarchyNamePath = namePath;
        entry.displayName = target.name;
        UpdateDraftTransform(entry, target);
        Draft.Entries.Add(entry);
        return entry;
    }

    private void RecordDraftUndo(string label)
    {
        Undo.IncrementCurrentGroup();
        undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(label);
        Undo.RecordObject(Draft, label);
    }

    private void FinishDraftChange()
    {
        EditorUtility.SetDirty(Draft);
        Draft.SaveDraft();
    }

    private void CommitAllDrafts()
    {
        if (Draft.Entries.Count == 0)
        {
            return;
        }

        EnsureAssetFolder("Assets/Resources/UI");
        var profile = AssetDatabase.LoadAssetAtPath<AetheriaHudLayoutProfile>(ProfileAssetPath);
        if (profile == null)
        {
            profile = CreateInstance<AetheriaHudLayoutProfile>();
            AssetDatabase.CreateAsset(profile, ProfileAssetPath);
        }

        Undo.RecordObject(profile, "HUD 초안 완료 적용");
        for (var i = 0; i < Draft.Entries.Count; i++)
        {
            var draftEntry = Draft.Entries[i];
            var destination = FindProfileEntry(
                profile,
                draftEntry.screenName,
                draftEntry.hierarchyPath,
                draftEntry.hierarchyNamePath);
            if (destination == null)
            {
                destination = new AetheriaHudLayoutEntry();
                profile.entries.Add(destination);
            }
            CopyEntry(draftEntry, destination);
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AetheriaHudLayoutRuntime.InvalidateCache();

        if (EditorApplication.isPlaying)
        {
            game = FindGame();
            var currentPage = game != null ? game.EditorCurrentHudPage : null;
            if (currentPage != null)
            {
                AetheriaHudLayoutRuntime.ApplyToScreen(currentPage, profile);
            }
        }

        Draft.Entries.Clear();
        FinishDraftChange();
        status = "HUD 초안을 완료하여 프로필에 적용했습니다.";
        QueueCapture(0.15f);
    }

    private void DiscardAllDrafts()
    {
        if (!EditorUtility.DisplayDialog("HUD 초안 버리기", "완료하지 않은 HUD 변경을 모두 삭제할까요?", "삭제", "취소"))
        {
            return;
        }

        Undo.RecordObject(Draft, "HUD 초안 버리기");
        Draft.Entries.Clear();
        FinishDraftChange();
        status = "HUD 초안을 삭제했습니다. 원본은 변경되지 않았습니다.";
        CaptureCurrentScreen();
    }

    private void ResetSelectedDrafts()
    {
        if (selection.Count == 0 || previewPage == null)
        {
            return;
        }

        Undo.RecordObject(Draft, "선택 HUD 초안 되돌리기");
        for (var selectionIndex = 0; selectionIndex < selection.Count; selectionIndex++)
        {
            var path = AetheriaHudLayoutRuntime.BuildRelativePath(selection[selectionIndex], previewPage);
            for (var i = Draft.Entries.Count - 1; i >= 0; i--)
            {
                if (Draft.Entries[i].screenName == previewPage.name && Draft.Entries[i].hierarchyPath == path)
                {
                    Draft.Entries.RemoveAt(i);
                }
            }
        }
        FinishDraftChange();
        CaptureCurrentScreen();
    }

    private void SelectFromList(RectTransform target, bool additive)
    {
        if (!additive)
        {
            selection.Clear();
        }
        if (selection.Contains(target))
        {
            if (additive)
            {
                selection.Remove(target);
            }
        }
        else
        {
            selection.Add(target);
        }
        Repaint();
    }

    private void SelectFromPreview(RectTransform target, bool additive)
    {
        if (!additive)
        {
            if (!selection.Contains(target) || selection.Count > 1)
            {
                selection.Clear();
                selection.Add(target);
            }
            return;
        }

        if (selection.Contains(target))
        {
            selection.Remove(target);
        }
        else
        {
            selection.Add(target);
        }
    }

    private RectTransform HitTest(Vector2 mousePosition)
    {
        var hits = HitTestAll(mousePosition);
        return hits.Count > 0 ? hits[0] : null;
    }

    private List<RectTransform> HitTestAll(Vector2 mousePosition)
    {
        var hits = new List<RectTransform>();
        for (var i = editableRects.Count - 1; i >= 0; i--)
        {
            var candidate = editableRects[i];
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }
            var rect = RectToGui(candidate);
            var area = rect.width * rect.height;
            if (rect.Contains(mousePosition) && area > 8f)
            {
                hits.Add(candidate);
            }
        }

        hits.Sort(delegate(RectTransform left, RectTransform right)
        {
            var visibleCompare = HasVisibleUi(right).CompareTo(HasVisibleUi(left));
            if (visibleCompare != 0)
            {
                return visibleCompare;
            }
            var depthCompare = HierarchyDepth(right, previewPage).CompareTo(HierarchyDepth(left, previewPage));
            if (depthCompare != 0)
            {
                return depthCompare;
            }
            var leftRect = RectToGui(left);
            var rightRect = RectToGui(right);
            return (leftRect.width * leftRect.height).CompareTo(rightRect.width * rightRect.height);
        });
        return hits;
    }

    private void ShowObjectSelectionMenu(Vector2 mousePosition)
    {
        var hits = HitTestAll(mousePosition);
        var menu = new GenericMenu();
        if (hits.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("선택할 객체 없음"));
        }
        for (var i = 0; i < hits.Count; i++)
        {
            var target = hits[i];
            var label = (i + 1) + ". " + ObjectDisplayName(target).Replace("/", "／");
            menu.AddItem(new GUIContent(label), selection.Contains(target), delegate
            {
                selection.Clear();
                selection.Add(target);
                Repaint();
            });
        }
        menu.ShowAsContext();
    }

    private RectTransform PrimarySelection()
    {
        for (var i = selection.Count - 1; i >= 0; i--)
        {
            if (selection[i] != null)
            {
                return selection[i];
            }
        }
        return null;
    }

    private Rect RectToGui(RectTransform target)
    {
        if (target == null || previewCamera == null)
        {
            return default(Rect);
        }

        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        for (var i = 0; i < corners.Length; i++)
        {
            var viewport = previewCamera.WorldToViewportPoint(corners[i]);
            var point = new Vector2(
                previewGuiRect.x + viewport.x * previewGuiRect.width,
                previewGuiRect.yMax - viewport.y * previewGuiRect.height);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private Rect SelectionGuiBounds()
    {
        var hasBounds = false;
        var result = default(Rect);
        for (var i = 0; i < selection.Count; i++)
        {
            if (selection[i] == null)
            {
                continue;
            }
            var rect = RectToGui(selection[i]);
            result = hasBounds ? Union(result, rect) : rect;
            hasBounds = true;
        }
        return result;
    }

    private bool IsPointInsideSelection(Vector2 mousePosition)
    {
        for (var i = 0; i < selection.Count; i++)
        {
            var target = selection[i];
            if (target != null && RectToGui(target).Contains(mousePosition))
            {
                return true;
            }
        }
        return false;
    }

    private Rect SelectionPageBounds()
    {
        var hasBounds = false;
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        var corners = new Vector3[4];
        for (var i = 0; i < selection.Count; i++)
        {
            var target = selection[i];
            if (target == null)
            {
                continue;
            }
            target.GetWorldCorners(corners);
            for (var cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
            {
                var local = (Vector2)previewPage.InverseTransformPoint(corners[cornerIndex]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            hasBounds = true;
        }
        return hasBounds ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : default(Rect);
    }

    private Vector2 GuiDeltaToPage(Vector2 guiDelta)
    {
        if (previewPage == null || previewGuiRect.width <= 0f || previewGuiRect.height <= 0f)
        {
            return Vector2.zero;
        }
        return new Vector2(
            guiDelta.x * previewPage.rect.width / previewGuiRect.width,
            -guiDelta.y * previewPage.rect.height / previewGuiRect.height);
    }

    private void DrawResizeHandles(Rect bounds)
    {
        var points = HandlePoints(bounds);
        for (var i = 0; i < points.Length; i++)
        {
            var handle = new Rect(points[i].x - HandleSize * 0.5f, points[i].y - HandleSize * 0.5f, HandleSize, HandleSize);
            EditorGUI.DrawRect(handle, new Color(0.05f, 0.10f, 0.14f, 1f));
            DrawRectOutline(handle, new Color(0.12f, 0.82f, 1f, 1f), 1f);
        }
    }

    private void AddHandleCursors(Rect bounds)
    {
        if (selection.Count == 0)
        {
            return;
        }

        EditorGUIUtility.AddCursorRect(bounds, MouseCursor.MoveArrow);

        var horizontalEdge = Mathf.Max(0f, bounds.width - ResizeEdgeHitSize * 2f);
        var verticalEdge = Mathf.Max(0f, bounds.height - ResizeEdgeHitSize * 2f);
        EditorGUIUtility.AddCursorRect(
            new Rect(bounds.xMin + ResizeEdgeHitSize, bounds.yMin - ResizeEdgeHitSize, horizontalEdge, ResizeEdgeHitSize * 2f),
            MouseCursor.ResizeVertical);
        EditorGUIUtility.AddCursorRect(
            new Rect(bounds.xMin + ResizeEdgeHitSize, bounds.yMax - ResizeEdgeHitSize, horizontalEdge, ResizeEdgeHitSize * 2f),
            MouseCursor.ResizeVertical);
        EditorGUIUtility.AddCursorRect(
            new Rect(bounds.xMin - ResizeEdgeHitSize, bounds.yMin + ResizeEdgeHitSize, ResizeEdgeHitSize * 2f, verticalEdge),
            MouseCursor.ResizeHorizontal);
        EditorGUIUtility.AddCursorRect(
            new Rect(bounds.xMax - ResizeEdgeHitSize, bounds.yMin + ResizeEdgeHitSize, ResizeEdgeHitSize * 2f, verticalEdge),
            MouseCursor.ResizeHorizontal);

        var points = HandlePoints(bounds);
        for (var i = 0; i < points.Length; i++)
        {
            var cursor = i == 0 || i == 7 ? MouseCursor.ResizeUpLeft
                : i == 2 || i == 5 ? MouseCursor.ResizeUpRight
                : i == 1 || i == 6 ? MouseCursor.ResizeVertical
                : MouseCursor.ResizeHorizontal;
            EditorGUIUtility.AddCursorRect(
                new Rect(points[i].x - 7f, points[i].y - 7f, 14f, 14f),
                cursor);
        }
    }

    private static Vector2[] HandlePoints(Rect bounds)
    {
        return new[]
        {
            new Vector2(bounds.xMin, bounds.yMin),
            new Vector2(bounds.center.x, bounds.yMin),
            new Vector2(bounds.xMax, bounds.yMin),
            new Vector2(bounds.xMin, bounds.center.y),
            new Vector2(bounds.xMax, bounds.center.y),
            new Vector2(bounds.xMin, bounds.yMax),
            new Vector2(bounds.center.x, bounds.yMax),
            new Vector2(bounds.xMax, bounds.yMax)
        };
    }

    private static ResizeHandle HitResizeHandle(Rect bounds, Vector2 mousePosition)
    {
        var distanceLeft = Mathf.Abs(mousePosition.x - bounds.xMin);
        var distanceRight = Mathf.Abs(mousePosition.x - bounds.xMax);
        var distanceTop = Mathf.Abs(mousePosition.y - bounds.yMin);
        var distanceBottom = Mathf.Abs(mousePosition.y - bounds.yMax);
        var withinHorizontalRange = mousePosition.x >= bounds.xMin - ResizeEdgeHitSize
            && mousePosition.x <= bounds.xMax + ResizeEdgeHitSize;
        var withinVerticalRange = mousePosition.y >= bounds.yMin - ResizeEdgeHitSize
            && mousePosition.y <= bounds.yMax + ResizeEdgeHitSize;

        if (!withinHorizontalRange || !withinVerticalRange)
        {
            return ResizeHandle.None;
        }

        var nearLeft = distanceLeft <= ResizeEdgeHitSize;
        var nearRight = distanceRight <= ResizeEdgeHitSize;
        var nearTop = distanceTop <= ResizeEdgeHitSize;
        var nearBottom = distanceBottom <= ResizeEdgeHitSize;

        if ((nearLeft || nearRight) && (nearTop || nearBottom))
        {
            var useLeft = !nearRight || (nearLeft && distanceLeft <= distanceRight);
            var useTop = !nearBottom || (nearTop && distanceTop <= distanceBottom);
            if (useLeft)
            {
                return useTop ? ResizeHandle.TopLeft : ResizeHandle.BottomLeft;
            }
            return useTop ? ResizeHandle.TopRight : ResizeHandle.BottomRight;
        }

        if (nearTop)
        {
            return ResizeHandle.Top;
        }
        if (nearBottom)
        {
            return ResizeHandle.Bottom;
        }
        if (nearLeft)
        {
            return ResizeHandle.Left;
        }
        if (nearRight)
        {
            return ResizeHandle.Right;
        }
        return ResizeHandle.None;
    }

    private void HandleKeyboardShortcuts()
    {
        var currentEvent = Event.current;
        if (currentEvent.type != EventType.KeyDown)
        {
            return;
        }

        if ((currentEvent.control || currentEvent.command) && currentEvent.keyCode == KeyCode.Z)
        {
            if (currentEvent.shift)
            {
                Undo.PerformRedo();
            }
            else
            {
                Undo.PerformUndo();
            }
            currentEvent.Use();
        }
        else if (currentEvent.keyCode == KeyCode.Escape && dragMode != DragMode.None)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            dragMode = DragMode.None;
            dragItems.Clear();
            pointerDownPending = false;
            pointerDownHit = null;
            QueueCapture(0.05f);
            currentEvent.Use();
        }
    }

    private void QueueCapture(float delay)
    {
        capturePending = true;
        captureAt = EditorApplication.timeSinceStartup + delay;
    }

    private static AetheriaGame FindGame()
    {
        var games = Resources.FindObjectsOfTypeAll<AetheriaGame>();
        for (var i = 0; i < games.Length; i++)
        {
            if (games[i] != null && games[i].isActiveAndEnabled && games[i].gameObject.scene.IsValid())
            {
                return games[i];
            }
        }
        return null;
    }

    private bool MatchesSearch(RectTransform target)
    {
        return string.IsNullOrEmpty(search)
            || target.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || ObjectDisplayName(target).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ObjectDisplayName(RectTransform target)
    {
        var text = target != null ? target.GetComponent<Text>() : null;
        if (text != null)
        {
            var value = CompactLabel(text.text, 24);
            return "텍스트 · " + (string.IsNullOrEmpty(value) ? target.name : value);
        }

        var selectable = target != null ? target.GetComponent<Selectable>() : null;
        if (selectable != null)
        {
            return "버튼 · " + target.name;
        }

        var particles = target != null ? target.GetComponent<ParticleSystem>() : null;
        if (particles != null)
        {
            return "파티클 · " + target.name;
        }

        var image = target != null ? target.GetComponent<Image>() : null;
        if (image != null)
        {
            var spriteName = image.sprite != null ? image.sprite.name : target.name;
            return "이미지 · " + CompactLabel(spriteName, 24);
        }

        var rawImage = target != null ? target.GetComponent<RawImage>() : null;
        if (rawImage != null)
        {
            var textureName = rawImage.texture != null ? rawImage.texture.name : target.name;
            return "이미지 · " + CompactLabel(textureName, 24);
        }

        return "그룹 · " + (target != null ? target.name : "UI");
    }

    private static string CompactLabel(string value, int maxLength)
    {
        var compact = (value ?? string.Empty)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
        while (compact.Contains("  "))
        {
            compact = compact.Replace("  ", " ");
        }
        return compact.Length <= maxLength
            ? compact
            : compact.Substring(0, Mathf.Max(1, maxLength - 1)) + "…";
    }

    private static Component PrimaryUiComponent(RectTransform target)
    {
        if (target == null)
        {
            return null;
        }
        var text = target.GetComponent<Text>();
        if (text != null) return text;
        var selectable = target.GetComponent<Selectable>();
        if (selectable != null) return selectable;
        var particles = target.GetComponent<ParticleSystem>();
        if (particles != null) return particles;
        var image = target.GetComponent<Image>();
        if (image != null) return image;
        var rawImage = target.GetComponent<RawImage>();
        if (rawImage != null) return rawImage;
        return target.GetComponent<Graphic>();
    }

    private static bool HasVisibleUi(RectTransform target)
    {
        return target.GetComponent<Graphic>() != null
            || target.GetComponent<Selectable>() != null
            || target.GetComponent<ParticleSystem>() != null;
    }

    private static int HierarchyDepth(Transform target, Transform root)
    {
        var depth = 0;
        for (var current = target.parent; current != null && current != root; current = current.parent)
        {
            depth++;
        }
        return depth;
    }

    private LayoutGroup FindNearestLayout(RectTransform target)
    {
        if (target == null || previewPage == null)
        {
            return null;
        }

        for (var current = target.parent; current != null; current = current.parent)
        {
            var layout = current.GetComponent<LayoutGroup>();
            if (layout != null)
            {
                return layout;
            }
            if (current == previewPage)
            {
                break;
            }
        }
        return null;
    }

    private static void DrawCenteredMessage(Rect rect, string message)
    {
        var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontSize = 13
        };
        GUI.Label(rect, message, style);
    }

    private static Rect FitAspect(Rect available, float aspect)
    {
        var width = available.width;
        var height = width / aspect;
        if (height > available.height)
        {
            height = available.height;
            width = height * aspect;
        }
        return new Rect(
            available.center.x - width * 0.5f,
            available.center.y - height * 0.5f,
            width,
            height);
    }

    private static Rect ExpandedRect(Rect rect, float amount)
    {
        return new Rect(
            rect.xMin - amount,
            rect.yMin - amount,
            rect.width + amount * 2f,
            rect.height + amount * 2f);
    }

    private Rect ZoomedPreviewRect()
    {
        var size = previewFitRect.size * previewZoom;
        return new Rect(
            previewFitRect.center + previewPan - size * 0.5f,
            size);
    }

    private void SetPreviewZoom(float value, Vector2 focusPoint)
    {
        var clampedZoom = Mathf.Clamp(value, MinPreviewZoom, MaxPreviewZoom);
        if (previewFitRect.width <= 0f || previewFitRect.height <= 0f)
        {
            previewZoom = clampedZoom;
            previewPan = Vector2.zero;
            Repaint();
            return;
        }

        var currentRect = ZoomedPreviewRect();
        var normalizedFocus = new Vector2(
            Mathf.Clamp01((focusPoint.x - currentRect.xMin) / currentRect.width),
            Mathf.Clamp01((focusPoint.y - currentRect.yMin) / currentRect.height));

        previewZoom = clampedZoom;
        var newSize = previewFitRect.size * previewZoom;
        var newTopLeft = focusPoint - Vector2.Scale(normalizedFocus, newSize);
        previewPan = newTopLeft + newSize * 0.5f - previewFitRect.center;
        ClampPreviewPan();
        Repaint();
    }

    private void ResetPreviewView()
    {
        previewZoom = 1f;
        previewPan = Vector2.zero;
        Repaint();
    }

    private void ClampPreviewPan()
    {
        previewZoom = Mathf.Clamp(previewZoom, MinPreviewZoom, MaxPreviewZoom);
        if (previewFitRect.width <= 0f
            || previewFitRect.height <= 0f
            || previewViewportRect.width <= 0f
            || previewViewportRect.height <= 0f)
        {
            previewPan = Vector2.zero;
            return;
        }

        var zoomedSize = previewFitRect.size * previewZoom;
        var maxPanX = Mathf.Max(0f, (zoomedSize.x - previewViewportRect.width) * 0.5f);
        var maxPanY = Mathf.Max(0f, (zoomedSize.y - previewViewportRect.height) * 0.5f);
        previewPan.x = maxPanX > 0f ? Mathf.Clamp(previewPan.x, -maxPanX, maxPanX) : 0f;
        previewPan.y = maxPanY > 0f ? Mathf.Clamp(previewPan.y, -maxPanY, maxPanY) : 0f;
    }

    private static void DrawRectOutline(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
    }

    private static Rect Union(Rect a, Rect b)
    {
        return Rect.MinMaxRect(
            Mathf.Min(a.xMin, b.xMin),
            Mathf.Min(a.yMin, b.yMin),
            Mathf.Max(a.xMax, b.xMax),
            Mathf.Max(a.yMax, b.yMax));
    }

    private static bool HandleUsesLeft(ResizeHandle handle)
    {
        return handle == ResizeHandle.Left || handle == ResizeHandle.TopLeft || handle == ResizeHandle.BottomLeft;
    }

    private static bool HandleUsesRight(ResizeHandle handle)
    {
        return handle == ResizeHandle.Right || handle == ResizeHandle.TopRight || handle == ResizeHandle.BottomRight;
    }

    private static bool HandleUsesTop(ResizeHandle handle)
    {
        return handle == ResizeHandle.Top || handle == ResizeHandle.TopLeft || handle == ResizeHandle.TopRight;
    }

    private static bool HandleUsesBottom(ResizeHandle handle)
    {
        return handle == ResizeHandle.Bottom || handle == ResizeHandle.BottomLeft || handle == ResizeHandle.BottomRight;
    }

    private static AetheriaHudLayoutEntry FindProfileEntry(
        AetheriaHudLayoutProfile profile,
        string screenName,
        string path,
        string namePath = null)
    {
        if (profile == null || profile.entries == null)
        {
            return null;
        }

        for (var i = 0; i < profile.entries.Count; i++)
        {
            var entry = profile.entries[i];
            if (entry != null && entry.screenName == screenName && entry.hierarchyPath == path)
            {
                return entry;
            }
        }

        if (!string.IsNullOrEmpty(namePath))
        {
            for (var i = 0; i < profile.entries.Count; i++)
            {
                var entry = profile.entries[i];
                if (entry != null && entry.screenName == screenName && entry.hierarchyNamePath == namePath)
                {
                    return entry;
                }
            }
        }
        return null;
    }

    private static void CopyEntry(AetheriaHudLayoutEntry source, AetheriaHudLayoutEntry destination)
    {
        destination.screenName = source.screenName;
        destination.hierarchyPath = source.hierarchyPath;
        destination.hierarchyNamePath = source.hierarchyNamePath;
        destination.displayName = source.displayName;
        destination.frozenLayoutPath = source.frozenLayoutPath;
        destination.frozenLayoutNamePath = source.frozenLayoutNamePath;
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.pivot = source.pivot;
        destination.anchoredPosition = source.anchoredPosition;
        destination.sizeDelta = source.sizeDelta;
        destination.localEulerAngles = source.localEulerAngles;
        destination.localScale = source.localScale;
        destination.overrideRenderOrder = source.overrideRenderOrder;
        destination.renderOrder = source.renderOrder;
        destination.overrideImage = source.overrideImage;
        destination.imageSprite = source.imageSprite;
        destination.imageColor = source.imageColor;
        destination.preserveImageAspect = source.preserveImageAspect;
        destination.overrideText = source.overrideText;
        destination.text = source.text;
        destination.textColor = source.textColor;
        destination.fontSize = source.fontSize;
        destination.overrideParticle = source.overrideParticle;
        destination.particleSimulationSpeed = source.particleSimulationSpeed;
        destination.particleStartSize = source.particleStartSize;
        destination.particleStartSpeed = source.particleStartSpeed;
        destination.particleEmissionRate = source.particleEmissionRate;
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        var parts = folderPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private sealed class DragItem
    {
        public RectTransform target;
        public Vector2 anchoredPosition;
        public Vector2 size;
        public Vector2 pivotPositionInPage;
    }

    private enum DragMode
    {
        None,
        Move,
        Resize
    }

    private enum ResizeHandle
    {
        None,
        TopLeft,
        Top,
        TopRight,
        Left,
        Right,
        BottomLeft,
        Bottom,
        BottomRight
    }
}
