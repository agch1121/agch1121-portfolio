#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class QuestEditorWindow : EditorWindow
{
    // 현재 선택된 퀘스트
    private Quest selectedQuest;

    // 모든 퀘스트 목록
    private List<Quest> allQuests = new List<Quest>();

    // 트리 뷰 스크롤 위치
    private Vector2 treeScrollPosition;
    private Vector2 detailScrollPosition;

    // 에디터 설정
    private bool showPrerequisites = true;
    private bool showQuestInfo = true;
    private bool showNpcInfo = true;
    private bool showRewards = true;

    // 노드 스타일
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle prerequisiteNodeStyle;
    private GUIStyle completedNodeStyle;

    // 노드 관련 데이터
    private Dictionary<Quest, Rect> nodeRects = new Dictionary<Quest, Rect>();
    private Dictionary<Quest, List<Quest>> nodeConnections = new Dictionary<Quest, List<Quest>>();
    private Vector2 graphScrollPosition;
    private bool isDragging;
    private Vector2 dragOffset;
    private Quest draggedQuest;
    private float zoomLevel = 1.0f;
    private Rect graphAreaRect;

    // 노드 초기 위치 설정을 위한 값
    private const float NODE_WIDTH = 200f;
    private const float NODE_HEIGHT = 80f;
    private const float NODE_MARGIN_X = 250f;
    private const float NODE_MARGIN_Y = 150f;

    // 에디터 윈도우 열기
    [MenuItem("Tools/Quest Editor")]
    public static void ShowWindow()
    {
        GetWindow<QuestEditorWindow>("퀘스트 에디터");
    }

    // 에셋 더블클릭으로 윈도우 열기
    [OnOpenAsset(1)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        Object obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj is Quest)
        {
            QuestEditorWindow window = GetWindow<QuestEditorWindow>("퀘스트 에디터");
            window.SelectQuest(obj as Quest);
            return true;
        }
        return false;
    }

    private void OnEnable()
    {
        // 스타일 초기화
        InitializeStyles();

        // 모든 퀘스트 로드
        LoadAllQuests();

        // 노드 위치 초기화
        CalculateNodePositions();
    }

    private void OnGUI()
    {
        if (nodeStyle == null)
            InitializeStyles();

        // 툴바 그리기
        DrawToolbar();

        // 메인 레이아웃
        EditorGUILayout.BeginHorizontal();

        // 왼쪽 패널 - 퀘스트 목록 트리
        DrawQuestListPanel();

        // 중앙 패널 - 퀘스트 그래프
        DrawQuestGraphPanel();

        // 오른쪽 패널 - 퀘스트 세부 정보
        DrawQuestDetailPanel();

        EditorGUILayout.EndHorizontal();
    }

    private void InitializeStyles()
    {
        // 기본 노드 스타일
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = MakeTexture(200, 80, new Color(0.3f, 0.3f, 0.3f, 0.8f));
        nodeStyle.border = new RectOffset(10, 10, 10, 10);
        nodeStyle.alignment = TextAnchor.MiddleCenter;
        nodeStyle.normal.textColor = Color.white;
        nodeStyle.fontStyle = FontStyle.Bold;
        nodeStyle.wordWrap = true;
        nodeStyle.padding = new RectOffset(10, 10, 10, 10);

        // 선택된 노드 스타일
        selectedNodeStyle = new GUIStyle(nodeStyle);
        selectedNodeStyle.normal.background = MakeTexture(200, 80, new Color(0.2f, 0.5f, 0.8f, 0.8f));

        // 선행 퀘스트 노드 스타일
        prerequisiteNodeStyle = new GUIStyle(nodeStyle);
        prerequisiteNodeStyle.normal.background = MakeTexture(200, 80, new Color(0.8f, 0.5f, 0.2f, 0.8f));

        // 완료된 퀘스트 노드 스타일
        completedNodeStyle = new GUIStyle(nodeStyle);
        completedNodeStyle.normal.background = MakeTexture(200, 80, new Color(0.2f, 0.6f, 0.3f, 0.8f));
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private void LoadAllQuests()
    {
        // 프로젝트에서 모든 퀘스트 애셋 찾기
        string[] guids = AssetDatabase.FindAssets("t:Quest");
        allQuests.Clear();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Quest quest = AssetDatabase.LoadAssetAtPath<Quest>(path);
            if (quest != null)
            {
                allQuests.Add(quest);
            }
        }
    }

    private void CalculateNodePositions()
    {
        nodeRects.Clear();
        nodeConnections.Clear();

        // 기본 위치 계산 (위상 정렬을 사용하면 더 좋겠지만, 간단한 구현을 위해 레벨 기반 배치)
        Dictionary<Quest, int> questLevels = CalculateQuestLevels();

        // 레벨 별로 그룹화
        Dictionary<int, List<Quest>> levelGroups = new Dictionary<int, List<Quest>>();

        foreach (var kvp in questLevels)
        {
            if (!levelGroups.ContainsKey(kvp.Value))
            {
                levelGroups[kvp.Value] = new List<Quest>();
            }
            levelGroups[kvp.Value].Add(kvp.Key);
        }

        // 레벨 별로 위치 지정
        foreach (var kvp in levelGroups)
        {
            int level = kvp.Key;
            List<Quest> quests = kvp.Value;

            for (int i = 0; i < quests.Count; i++)
            {
                float x = NODE_MARGIN_X * level;
                float y = NODE_MARGIN_Y * i;
                nodeRects[quests[i]] = new Rect(x, y, NODE_WIDTH, NODE_HEIGHT);
            }
        }

        // 연결 정보 구성
        foreach (Quest quest in allQuests)
        {
            if (quest.PrerequisiteQuests != null && quest.PrerequisiteQuests.Count > 0)
            {
                foreach (Quest prereq in quest.PrerequisiteQuests)
                {
                    if (prereq == null) continue;

                    if (!nodeConnections.ContainsKey(prereq))
                    {
                        nodeConnections[prereq] = new List<Quest>();
                    }
                    nodeConnections[prereq].Add(quest);
                }
            }
        }
    }

    private Dictionary<Quest, int> CalculateQuestLevels()
    {
        Dictionary<Quest, int> levels = new Dictionary<Quest, int>();

        // 첫번째 패스: 선행 퀘스트가 없는 퀘스트에 레벨 0 할당
        foreach (Quest quest in allQuests)
        {
            if (quest.PrerequisiteQuests == null || quest.PrerequisiteQuests.Count == 0)
            {
                levels[quest] = 0;
            }
        }

        // 두번째 패스: 나머지 퀘스트의 레벨을 선행 퀘스트의 최대 레벨 + 1로 설정
        bool changed = true;
        while (changed)
        {
            changed = false;

            foreach (Quest quest in allQuests)
            {
                if (levels.ContainsKey(quest)) continue;

                if (quest.PrerequisiteQuests != null && quest.PrerequisiteQuests.Count > 0)
                {
                    bool allPrerequisitesHaveLevel = true;
                    int maxPrerequisiteLevel = -1;

                    foreach (Quest prereq in quest.PrerequisiteQuests)
                    {
                        if (prereq == null) continue;

                        if (!levels.ContainsKey(prereq))
                        {
                            allPrerequisitesHaveLevel = false;
                            break;
                        }

                        maxPrerequisiteLevel = Mathf.Max(maxPrerequisiteLevel, levels[prereq]);
                    }

                    if (allPrerequisitesHaveLevel)
                    {
                        levels[quest] = maxPrerequisiteLevel + 1;
                        changed = true;
                    }
                }
            }
        }

        // 남은 퀘스트들 (순환 의존성이 있을 경우)
        foreach (Quest quest in allQuests)
        {
            if (!levels.ContainsKey(quest))
            {
                levels[quest] = 0;
            }
        }

        return levels;
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            LoadAllQuests();
            CalculateNodePositions();
        }

        if (GUILayout.Button("새 퀘스트", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            CreateNewQuest();
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawQuestListPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(250));

        EditorGUILayout.LabelField("퀘스트 목록", EditorStyles.boldLabel);

        treeScrollPosition = EditorGUILayout.BeginScrollView(treeScrollPosition);

        // 타입별로 퀘스트 분류
        Dictionary<QuestType, List<Quest>> questsByType = new Dictionary<QuestType, List<Quest>>();

        foreach (Quest quest in allQuests)
        {
            if (!questsByType.ContainsKey(quest.QuestType))
            {
                questsByType[quest.QuestType] = new List<Quest>();
            }
            questsByType[quest.QuestType].Add(quest);
        }

        // 타입별로 퀘스트 표시
        foreach (var kvp in questsByType)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(kvp.Key.ToString() + " 퀘스트", EditorStyles.boldLabel);

            foreach (Quest quest in kvp.Value)
            {
                EditorGUILayout.BeginHorizontal();

                // 퀘스트 상태 표시
                string stateSymbol = "?"; // 기본 상태
                if (quest.State == QuestState.InProgress)
                    stateSymbol = "▶";
                else if (quest.State == QuestState.Completed)
                    stateSymbol = "?";
                else if (quest.State == QuestState.Finished)
                    stateSymbol = "★";

                Color originalColor = GUI.color;
                // 상태에 따라 색상 변경
                if (quest.State == QuestState.InProgress)
                    GUI.color = Color.yellow;
                else if (quest.State == QuestState.Completed || quest.State == QuestState.Finished)
                    GUI.color = Color.green;

                GUILayout.Label(stateSymbol, GUILayout.Width(20));
                GUI.color = originalColor;

                GUIStyle style = new GUIStyle(EditorStyles.label);
                if (quest == selectedQuest)
                    style.fontStyle = FontStyle.Bold;

                if (GUILayout.Button(quest.QuestName, style))
                {
                    SelectQuest(quest);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawQuestGraphPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        // 그래프 조작 도구
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("확대", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            zoomLevel = Mathf.Min(zoomLevel + 0.1f, 2.0f);
        }

        if (GUILayout.Button("축소", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            zoomLevel = Mathf.Max(zoomLevel - 0.1f, 0.5f);
        }

        if (GUILayout.Button("리셋", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            zoomLevel = 1.0f;
            CalculateNodePositions();
        }

        EditorGUILayout.EndHorizontal();

        // 그래프 영역
        graphAreaRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUI.Box(graphAreaRect, "");

        graphScrollPosition = GUI.BeginScrollView(graphAreaRect, graphScrollPosition,
            new Rect(0, 0, 3000 * zoomLevel, 2000 * zoomLevel));

        // 연결선 먼저 그리기
        DrawConnections();

        // 노드 그리기
        DrawNodes();

        // 노드 이벤트 처리
        HandleNodeEvents();

        GUI.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawConnections()
    {
        if (Event.current.type != EventType.Repaint)
            return;

        foreach (var kvp in nodeConnections)
        {
            Quest sourceQuest = kvp.Key;
            List<Quest> targetQuests = kvp.Value;

            if (!nodeRects.ContainsKey(sourceQuest))
                continue;

            Rect sourceRect = nodeRects[sourceQuest];
            Vector2 sourceCenter = new Vector2(
                sourceRect.x + sourceRect.width * 0.5f,
                sourceRect.y + sourceRect.height * 0.5f) * zoomLevel;

            foreach (Quest targetQuest in targetQuests)
            {
                if (!nodeRects.ContainsKey(targetQuest))
                    continue;

                Rect targetRect = nodeRects[targetQuest];
                Vector2 targetCenter = new Vector2(
                    targetRect.x + targetRect.width * 0.5f,
                    targetRect.y + targetRect.height * 0.5f) * zoomLevel;

                // 소스 노드 오른쪽에서 시작
                Vector2 sourcePoint = new Vector2(
                    sourceRect.x + sourceRect.width,
                    sourceRect.y + sourceRect.height * 0.5f) * zoomLevel;

                // 타겟 노드 왼쪽으로 끝
                Vector2 targetPoint = new Vector2(
                    targetRect.x,
                    targetRect.y + targetRect.height * 0.5f) * zoomLevel;

                // 소스 퀘스트의 상태에 따라 선 색상 변경
                Color connectionColor = Color.gray;
                if (sourceQuest.State == QuestState.Completed || sourceQuest.State == QuestState.Finished)
                    connectionColor = Color.green;
                else if (sourceQuest.State == QuestState.InProgress)
                    connectionColor = Color.yellow;

                Handles.color = connectionColor;

                // Bezier 곡선으로 연결
                float tangentLength = (targetPoint.x - sourcePoint.x) * 0.5f;
                Vector2 sourceControlPoint = sourcePoint + Vector2.right * tangentLength;
                Vector2 targetControlPoint = targetPoint + Vector2.left * tangentLength;

                Handles.DrawBezier(
                    sourcePoint, targetPoint,
                    sourceControlPoint, targetControlPoint,
                    connectionColor, null, 2f);

                // 화살표 그리기
                Vector2 direction = (targetControlPoint - targetPoint).normalized;
                float arrowSize = 10f;
                Vector2 arrowHead = targetPoint;
                Vector2 arrowLeft = arrowHead + new Vector2(direction.x - direction.y, direction.y + direction.x) * arrowSize;
                Vector2 arrowRight = arrowHead + new Vector2(direction.x + direction.y, direction.y - direction.x) * arrowSize;

                Handles.DrawLine(arrowHead, arrowLeft);
                Handles.DrawLine(arrowHead, arrowRight);
            }
        }
    }

    private void DrawNodes()
    {
        foreach (Quest quest in allQuests)
        {
            if (!nodeRects.ContainsKey(quest))
                continue;

            // 노드의 원래 좌표에 줌 레벨 적용
            Rect zoomedRect = new Rect(
                nodeRects[quest].x * zoomLevel,
                nodeRects[quest].y * zoomLevel,
                nodeRects[quest].width * zoomLevel,
                nodeRects[quest].height * zoomLevel);

            // 적절한 스타일 선택
            GUIStyle style = nodeStyle;

            if (quest == selectedQuest)
                style = selectedNodeStyle;
            else if (selectedQuest != null && selectedQuest.PrerequisiteQuests.Contains(quest))
                style = prerequisiteNodeStyle;
            else if (quest.State == QuestState.Completed || quest.State == QuestState.Finished)
                style = completedNodeStyle;

            // 노드 그리기
            GUI.Box(zoomedRect, quest.QuestName, style);

            // 퀘스트 ID 표시
            Rect idRect = new Rect(zoomedRect.x, zoomedRect.y + zoomedRect.height - 20, zoomedRect.width, 20);
            GUI.Label(idRect, quest.QuestId, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });

            // 퀘스트 상태 표시
            string stateText = quest.State.ToString();
            GUIStyle stateStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            Rect stateRect = new Rect(zoomedRect.x, zoomedRect.y + 5, zoomedRect.width - 10, 20);

            GUI.Label(stateRect, stateText, stateStyle);
        }
    }

    private void HandleNodeEvents()
    {
        Event e = Event.current;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0) // 좌클릭
                {
                    Vector2 mousePos = e.mousePosition + graphScrollPosition;
                    mousePos /= zoomLevel; // 줌 레벨 고려

                    // 노드 선택 또는 드래그 시작
                    foreach (var kvp in nodeRects)
                    {
                        if (kvp.Value.Contains(mousePos))
                        {
                            SelectQuest(kvp.Key);
                            isDragging = true;
                            draggedQuest = kvp.Key;
                            dragOffset = mousePos - new Vector2(kvp.Value.x, kvp.Value.y);
                            e.Use();
                            break;
                        }
                    }
                }
                break;

            case EventType.MouseDrag:
                if (isDragging && draggedQuest != null)
                {
                    Vector2 mousePos = e.mousePosition + graphScrollPosition;
                    mousePos /= zoomLevel; // 줌 레벨 고려

                    // 노드 위치 업데이트
                    Rect rect = nodeRects[draggedQuest];
                    rect.x = mousePos.x - dragOffset.x;
                    rect.y = mousePos.y - dragOffset.y;
                    nodeRects[draggedQuest] = rect;

                    e.Use();
                    Repaint();
                }
                break;

            case EventType.MouseUp:
                if (isDragging)
                {
                    isDragging = false;
                    draggedQuest = null;
                    e.Use();
                }
                break;
        }
    }

    private void DrawQuestDetailPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(300));

        if (selectedQuest != null)
        {
            EditorGUILayout.LabelField(selectedQuest.QuestName, EditorStyles.boldLabel);

            detailScrollPosition = EditorGUILayout.BeginScrollView(detailScrollPosition);

            // 퀘스트 에디터 - 기본 정보
            showQuestInfo = EditorGUILayout.Foldout(showQuestInfo, "퀘스트 정보", true);
            if (showQuestInfo)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                string newQuestName = EditorGUILayout.TextField("퀘스트 이름", selectedQuest.QuestName);
                string newQuestId = EditorGUILayout.TextField("퀘스트 ID", selectedQuest.QuestId);
                QuestType newQuestType = (QuestType)EditorGUILayout.EnumPopup("퀘스트 타입", selectedQuest.QuestType);
                QuestRegion newRegion = (QuestRegion)EditorGUILayout.EnumPopup("지역", selectedQuest.Region);
                string newDescription = EditorGUILayout.TextArea(selectedQuest.QuestDescription, GUILayout.Height(60));

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selectedQuest, "Edit Quest Info");

                    selectedQuest.QuestName = newQuestName;
                    selectedQuest.QuestId = newQuestId;
                    selectedQuest.QuestType = newQuestType;
                    selectedQuest.Region = newRegion;
                    selectedQuest.QuestDescription = newDescription;

                    EditorUtility.SetDirty(selectedQuest);
                }

                EditorGUI.indentLevel--;
            }

            // 퀘스트 에디터 - NPC 정보
            showNpcInfo = EditorGUILayout.Foldout(showNpcInfo, "NPC 정보", true);
            if (showNpcInfo)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                string newStartNpcId = EditorGUILayout.TextField("시작 NPC ID", selectedQuest.StartNpcId);
                string newCompleteNpcId = EditorGUILayout.TextField("완료 NPC ID", selectedQuest.CompleteNpcId);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selectedQuest, "Edit Quest NPC Info");

                    selectedQuest.StartNpcId = newStartNpcId;
                    selectedQuest.CompleteNpcId = newCompleteNpcId;

                    EditorUtility.SetDirty(selectedQuest);
                }

                EditorGUI.indentLevel--;
            }

            // 퀘스트 에디터 - 선행 퀘스트
            showPrerequisites = EditorGUILayout.Foldout(showPrerequisites, "선행 퀘스트", true);
            if (showPrerequisites)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                PrerequisiteLogic newLogic = (PrerequisiteLogic)EditorGUILayout.EnumPopup(
                    "선행 조건 로직", selectedQuest.PrerequisiteType);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selectedQuest, "Edit Prerequisite Logic");
                    selectedQuest.PrerequisiteType = newLogic;
                    EditorUtility.SetDirty(selectedQuest);
                }

                // 선행 퀘스트 목록
                EditorGUILayout.LabelField("선행 퀘스트 목록");

                if (selectedQuest.PrerequisiteQuests == null)
                    selectedQuest.PrerequisiteQuests = new List<Quest>();

                // 선행 퀘스트 표시 및 편집
                for (int i = 0; i < selectedQuest.PrerequisiteQuests.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUI.BeginChangeCheck();
                    Quest newPrereq = EditorGUILayout.ObjectField(
                        selectedQuest.PrerequisiteQuests[i], typeof(Quest), false) as Quest;

                    if (EditorGUI.EndChangeCheck())
                    {
                        // 순환 참조 방지
                        if (newPrereq == selectedQuest ||
                            (newPrereq != null && HasCyclicDependency(newPrereq, selectedQuest)))
                        {
                            EditorUtility.DisplayDialog("경고", "순환 참조가 발생하여 선행 퀘스트로 추가할 수 없습니다.", "확인");
                        }
                        else
                        {
                            Undo.RecordObject(selectedQuest, "Change Prerequisite Quest");
                            selectedQuest.PrerequisiteQuests[i] = newPrereq;
                            EditorUtility.SetDirty(selectedQuest);
                        }
                    }

                    if (GUILayout.Button("제거", GUILayout.Width(60)))
                    {
                        Undo.RecordObject(selectedQuest, "Remove Prerequisite Quest");
                        selectedQuest.PrerequisiteQuests.RemoveAt(i);
                        EditorUtility.SetDirty(selectedQuest);
                        i--;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                // 선행 퀘스트 추가 버튼
                if (GUILayout.Button("선행 퀘스트 추가"))
                {
                    Undo.RecordObject(selectedQuest, "Add Prerequisite Quest");
                    selectedQuest.PrerequisiteQuests.Add(null);
                    EditorUtility.SetDirty(selectedQuest);
                }

                EditorGUI.indentLevel--;
            }

            // 퀘스트 에디터 - 보상 정보
            showRewards = EditorGUILayout.Foldout(showRewards, "보상 정보", true);
            if (showRewards)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                int newGoldReward = EditorGUILayout.IntField("골드 보상", selectedQuest.GoldReward);
                int newExpReward = EditorGUILayout.IntField("경험치 보상", selectedQuest.ExpReward);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selectedQuest, "Edit Quest Rewards");

                    selectedQuest.GoldReward = newGoldReward;
                    selectedQuest.ExpReward = newExpReward;

                    EditorUtility.SetDirty(selectedQuest);
                }

                EditorGUI.indentLevel--;
            }

            // 진행 상태 표시
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("진행 상태", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            QuestState newState = (QuestState)EditorGUILayout.EnumPopup("상태", selectedQuest.State);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selectedQuest, "Change Quest State");
                selectedQuest.State = newState;
                EditorUtility.SetDirty(selectedQuest);
            }

            // 진행도 표시
            EditorGUILayout.LabelField("진행도", $"{selectedQuest.currentProgress} / {selectedQuest.requiredProgress}");

            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.LabelField("퀘스트를 선택하세요");
        }

        EditorGUILayout.EndVertical();
    }

    private void SelectQuest(Quest quest)
    {
        selectedQuest = quest;
        Selection.activeObject = quest;
        Repaint();
    }

    private void CreateNewQuest()
    {
        // 퀘스트 타입 선택
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("수집 퀘스트"), false, () => CreateNewQuestOfType(typeof(CollectQuest)));
        menu.AddItem(new GUIContent("처치 퀘스트"), false, () => CreateNewQuestOfType(typeof(KillQuest)));
        menu.AddItem(new GUIContent("도달 퀘스트"), false, () => CreateNewQuestOfType(typeof(ReachQuest)));

        menu.ShowAsContext();
    }

    private void CreateNewQuestOfType(System.Type questType)
    {
        // 저장 경로 선택
        string path = EditorUtility.SaveFilePanelInProject(
            "새 퀘스트 생성",
            "New" + questType.Name,
            "asset",
            "퀘스트를 저장할 위치를 선택하세요.");

        if (string.IsNullOrEmpty(path))
            return;

        // 새 퀘스트 생성
        Quest newQuest = ScriptableObject.CreateInstance(questType) as Quest;
        if (newQuest == null)
            return;

        // 기본값 설정
        newQuest.QuestName = "새 퀘스트";
        newQuest.QuestId = System.Guid.NewGuid().ToString().Substring(0, 8);

        // 에셋 저장
        AssetDatabase.CreateAsset(newQuest, path);
        AssetDatabase.SaveAssets();

        // 퀘스트 목록 새로고침
        LoadAllQuests();
        CalculateNodePositions();

        // 생성된 퀘스트 선택
        SelectQuest(newQuest);
        EditorGUIUtility.PingObject(newQuest);
    }

    private bool HasCyclicDependency(Quest questToAdd, Quest targetQuest)
    {
        // QuestManager가 없는 경우 기본 구현 제공
        if (questToAdd.PrerequisiteQuests == null || questToAdd.PrerequisiteQuests.Count == 0)
            return false;

        foreach (Quest prereq in questToAdd.PrerequisiteQuests)
        {
            if (prereq == targetQuest)
                return true;

            if (prereq != null && HasCyclicDependency(prereq, targetQuest))
                return true;
        }

        return false;
    }
}
#endif