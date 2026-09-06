using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class QuestUIManager : CustomSingletone<QuestUIManager>
{
    [Header("Quest Panels")]
    [SerializeField] private GameObject questUIPanel;
    [SerializeField] private GameObject questNoticePanel;

    [Header("Tab Buttons")]
    [SerializeField] private Button acceptableTabButton;
    [SerializeField] private Button activeTabButton;
    [SerializeField] private Button finishedTabButton;

    [Header("List Content")]
    [SerializeField] private Transform questListContent;
    [SerializeField] private Transform noticeQuestContent;

    [Header("Quest Prefabs")]
    [SerializeField] private GameObject questListItemPrefab;
    [SerializeField] private GameObject questNoticeItemPrefab;

    [Header("Region Group")]
    [SerializeField] private GameObject questRegionGroupPrefab;
    private Dictionary<QuestRegion, QuestRegionGroup> regionGroups = new Dictionary<QuestRegion, QuestRegionGroup>();

    [Header("Quest Reward Panel")]
    [SerializeField] private QuestRewardPanel questRewardPanel; // 퀘스트 보상 패널

    [Header("Quest Accept Panel")]
    [SerializeField] private QuestAcceptPanel questAcceptPanel;

    [Header("Description Panel")]
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private Image npcImage;

    [Header("Description Scroll View")]
    [SerializeField] private ScrollRect questDescScrollView;    // 퀘스트 설명 스크롤뷰
    [SerializeField] private TextMeshProUGUI questDescText;     // 스크롤뷰 내부 설명 텍스트
    [SerializeField] private ScrollRect objectiveScrollView;    // 퀘스트 목표 스크롤뷰
    [SerializeField] private TextMeshProUGUI questObjectiveText; // 스크롤뷰 내부 목표 텍스트

    [Header("Close Buttons")]
    [SerializeField] private Button closeQuestPanelButton;
    [SerializeField] private Button closeNoticeButton; // 알림 패널 닫기 버튼

    // 퀘스트 목록 데이터
    private List<Quest> acceptableQuests = new List<Quest>();
    private List<Quest> activeQuests = new List<Quest>();
    private List<Quest> finishedQuests = new List<Quest>();

    // 현재 탭의 퀘스트 목록
    private List<Quest> currentQuests = new List<Quest>();

    // 알림 패널에 표시할 퀘스트 목록
    private List<Quest> noticeQuests = new List<Quest>();

    // 현재 선택된 탭
    private enum TabType { Acceptable, Active, Finished }
    private TabType currentTab = TabType.Active;

    // 현재 선택된 퀘스트
    private Quest selectedQuest = null;

    // 현재 페이지의 UI 아이템 관리
    private List<GameObject> currentPageQuestItems = new List<GameObject>();
    private Dictionary<string, GameObject> noticeQuestItems = new Dictionary<string, GameObject>();

    private void Start()
    {
        InitializeUI();

        // QuestManager 이벤트 구독
        QuestManager.Instance.OnQuestStateChanged += OnQuestStateChanged;
        QuestManager.Instance.OnQuestAccepted += OnQuestAccepted;
        QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
        QuestManager.Instance.OnQuestFinished += OnQuestFinished;

        // 퀘스트 수락 패널 이벤트 등록
        if (questAcceptPanel != null)
        {
            questAcceptPanel.OnQuestAccepted += HandleQuestAccepted;
            questAcceptPanel.OnQuestDeclined += HandleQuestDeclined;
        }

        // 초기 패널 상태 설정 (퀘스트 패널, 알림 패널 비활성화)
        questUIPanel.SetActive(false);
        questNoticePanel.SetActive(false);
    }

    private void Update()
    {
        // O 키로 퀘스트 패널 열기/닫기
        if (Input.GetKeyDown(KeyCode.O))
        {
            ToggleQuestPanel();
        }
        // ESC 키로 퀘스트 패널 닫기
        if (Input.GetKeyDown(KeyCode.Escape) && questUIPanel.activeSelf)
        {
            CloseQuestPanel();
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStateChanged -= OnQuestStateChanged;
            QuestManager.Instance.OnQuestAccepted -= OnQuestAccepted;
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
            QuestManager.Instance.OnQuestFinished -= OnQuestFinished;
        }

        // 퀘스트 수락 패널 이벤트 해제
        if (questAcceptPanel != null)
        {
            questAcceptPanel.OnQuestAccepted -= HandleQuestAccepted;
            questAcceptPanel.OnQuestDeclined -= HandleQuestDeclined;
        }
    }

    private void InitializeUI()
    {
        // 탭 버튼 이벤트 등록
        acceptableTabButton.onClick.AddListener(() => SwitchTab(TabType.Acceptable));
        activeTabButton.onClick.AddListener(() => SwitchTab(TabType.Active));
        finishedTabButton.onClick.AddListener(() => SwitchTab(TabType.Finished));

        // 닫기 버튼 이벤트 등록
        closeQuestPanelButton.onClick.AddListener(CloseQuestPanel);
        closeNoticeButton.onClick.AddListener(CloseNoticePanel);

        // 초기 탭 설정
        SwitchTab(TabType.Active);
    }

    // 퀘스트 패널 토글
    public void ToggleQuestPanel()
    {
        if (questUIPanel.activeSelf)
        {
            CloseQuestPanel();
        }
        else
        {
            OpenQuestPanel();
        }
    }

    // 퀘스트 패널 열기
    public void OpenQuestPanel()
    {
        questUIPanel.SetActive(true);
        RefreshQuestLists();
        SwitchTab(currentTab);
    }

    // 퀘스트 패널 닫기
    public void CloseQuestPanel()
    {
        questUIPanel.SetActive(false);
    }

    public void CloseNoticePanel()
    {
        questNoticePanel.SetActive(false);
    }

    // 탭 전환
    private void SwitchTab(TabType tab)
    {
        currentTab = tab;

        // 선택된 탭에 따라 퀘스트 목록 갱신
        switch (tab)
        {
            case TabType.Acceptable:
                currentQuests = acceptableQuests;
                break;
            case TabType.Active:
                currentQuests = activeQuests;
                break;
            case TabType.Finished:
                currentQuests = finishedQuests;
                break;
        }

        // 퀘스트 목록 갱신
        RefreshQuestDisplay();

        // 선택된 퀘스트 초기화
        ClearQuestDetail();

        // 탭 버튼 상태 업데이트 (추가된 개선 사항)
        acceptableTabButton.interactable = currentTab != TabType.Acceptable;
        activeTabButton.interactable = currentTab != TabType.Active;
        finishedTabButton.interactable = currentTab != TabType.Finished;
    }

    // 퀘스트 목록 갱신
    private void RefreshQuestLists()
    {
        if (QuestManager.Instance == null) return;

        // 수락 가능한 퀘스트
        acceptableQuests.Clear();
        foreach (Quest quest in QuestManager.Instance.GetAllQuests())
        {
            if (quest.State == QuestState.NotAccepted && quest.CheckPrerequisites())
            {
                acceptableQuests.Add(quest);
            }
        }

        // 진행 중인 퀘스트
        activeQuests = new List<Quest>(QuestManager.Instance.GetActiveQuests());

        // 완료된 퀘스트
        finishedQuests = new List<Quest>(QuestManager.Instance.GetFinishedQuests());
    }

    // 현재 탭의 퀘스트 목록 표시
    private void RefreshQuestDisplay()
    {
        // 기존 항목 제거
        foreach (GameObject item in currentPageQuestItems)
        {
            Destroy(item);
        }
        currentPageQuestItems.Clear();

        // 지역 그룹도 제거
        foreach (var group in regionGroups.Values)
        {
            Destroy(group.gameObject);
        }
        regionGroups.Clear();

        // 지역별로 그룹화 (카테고리별 정렬 추가)
        Dictionary<QuestRegion, List<Quest>> questsByRegion = new Dictionary<QuestRegion, List<Quest>>();

        // 모든 퀘스트를 지역별로 분류
        foreach (Quest quest in currentQuests)
        {
            if (!questsByRegion.ContainsKey(quest.Region))
            {
                questsByRegion[quest.Region] = new List<Quest>();
            }

            questsByRegion[quest.Region].Add(quest);
        }

        // 지역별 정렬된 리스트 생성 (enum 순서대로)
        List<QuestRegion> sortedRegions = new List<QuestRegion>(questsByRegion.Keys);
        sortedRegions.Sort(); // enum 값 순서대로 정렬

        // 지역별 그룹 생성 및 퀘스트 추가 (정렬된 순서대로)
        foreach (var region in sortedRegions)
        {
            List<Quest> regionQuests = questsByRegion[region];

            // 퀘스트를 카테고리별로 정렬 (메인 퀘스트가 먼저 오도록)
            regionQuests.Sort((q1, q2) => q1.Category.CompareTo(q2.Category));

            // 지역 이름 가져오기
            string regionName = GetRegionName(region);

            // 지역 그룹 생성
            GameObject groupObj = Instantiate(questRegionGroupPrefab, questListContent);
            QuestRegionGroup groupComponent = groupObj.GetComponent<QuestRegionGroup>();

            if (groupComponent != null)
            {
                // 그룹 초기화
                groupComponent.Initialize(region, regionName, regionQuests.Count);
                regionGroups[region] = groupComponent;

                // 그룹에 퀘스트 아이템 추가
                foreach (Quest quest in regionQuests)
                {
                    GameObject questItemObj = CreateQuestListItem(quest);
                    if (questItemObj != null)
                    {
                        // LayoutElement 확인
                        LayoutElement layoutElement = questItemObj.GetComponent<LayoutElement>();
                        if (layoutElement == null)
                        {
                            layoutElement = questItemObj.AddComponent<LayoutElement>();
                            layoutElement.minHeight = 40f; // 최소 높이 명시적 설정
                            layoutElement.preferredHeight = 40f; // 선호 높이 설정
                        }

                        groupComponent.AddQuestItem(questItemObj);
                        currentPageQuestItems.Add(questItemObj);
                    }
                }

                // 퀘스트 개수에 따른 스페이서 추가 (그룹 아래 여백)
                // 마지막 지역이 아닌 경우에만 스페이서 추가
                if (region != sortedRegions[sortedRegions.Count - 1])
                {
                    // 그룹 다음에 스페이서 추가
                    GameObject spacer = new GameObject("GroupSpacer_" + region.ToString());
                    spacer.transform.SetParent(questListContent, false);

                    // LayoutElement 컴포넌트 추가
                    LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();

                    // 퀘스트 개수에 비례한 높이 설정
                    float spacerHeight = regionQuests.Count * 10f; // 퀘스트 하나당 10 픽셀

                    // 최소 및 최대 값 제한
                    spacerHeight = Mathf.Clamp(spacerHeight, 20f, 60f);

                    spacerLayout.minHeight = spacerHeight;
                    spacerLayout.preferredHeight = spacerHeight;

                    // 현재 페이지 아이템에 추가 (관리를 위해)
                    currentPageQuestItems.Add(spacer);
                }

                // 각 그룹 추가 후 강제 레이아웃 갱신
                Canvas.ForceUpdateCanvases();
            }
        }

        // 최종 전체 레이아웃 갱신
        StartCoroutine(DelayedLayoutRebuild());
    }

    // 약간의 지연 후 레이아웃 갱신 (모든 항목이 추가된 후)
    private IEnumerator DelayedLayoutRebuild()
    {
        yield return null; // 한 프레임 대기

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(questListContent as RectTransform);

        // 각 그룹도 강제 업데이트
        foreach (var group in regionGroups.Values)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(group.transform as RectTransform);
        }
    }

    // 퀘스트 선택
    private void SelectQuest(Quest quest)
    {
        if (quest == null) return;

        // 디버그 로그 추가
        Debug.Log($"퀘스트 선택됨: {quest.QuestName} (ID: {quest.QuestId})");

        selectedQuest = quest;
        UpdateQuestDetail(quest);
    }

    // 퀘스트 상세 정보 업데이트
    private void UpdateQuestDetail(Quest quest)
    {
        if (quest == null)
        {
            ClearQuestDetail();
            return;
        }

        // 퀘스트 이름
        questNameText.text = quest.QuestName;

        // NPC 이미지 설정 (시작 또는 완료 NPC)
        string npcId = (quest.State == QuestState.NotAccepted || quest.State == QuestState.InProgress)
            ? quest.StartNpcId : quest.CompleteNpcId;

        NPCController npc = FindNpcControllerById(npcId);
        if (npc != null && npc.GetNPCBase() != null && npc.GetNPCBase().NpcImage != null)
        {
            npcImage.sprite = npc.GetNPCBase().NpcImage;
            npcImage.gameObject.SetActive(true);
        }
        else
        {
            npcImage.gameObject.SetActive(false);
        }

        // 퀘스트 설명
        questDescText.text = quest.QuestDescription;

        // 스크롤뷰 스크롤 위치 초기화
        if (questDescScrollView != null)
        {
            Canvas.ForceUpdateCanvases();
            questDescScrollView.normalizedPosition = new Vector2(0, 1);
        }

        // 퀘스트 목표
        questObjectiveText.text = quest.GetProgressText();

        // 목표 스크롤뷰 스크롤 위치 초기화
        if (objectiveScrollView != null)
        {
            Canvas.ForceUpdateCanvases();
            objectiveScrollView.normalizedPosition = new Vector2(0, 1);
        }

        // 기본 텍스트 색상
        questObjectiveText.color = Color.black;

        // 진행 중인 퀘스트가 완료 조건 충족 시 텍스트 색상 변경
        if (quest.State == QuestState.InProgress && quest.IsCompleted())
        {
            // 완료 가능 상태일 때 텍스트 색상을 강조
            questObjectiveText.color = Color.red;
        }

        // 보상 패널 갱신 - 보상 패널이 문제의 원인일 수 있으므로 try-catch로 보호
        try
        {
            if (questRewardPanel != null)
            {
                questRewardPanel.ShowRewards(quest);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"보상 패널 갱신 오류: {e.Message}");
        }
    }

    // 퀘스트 상세 정보 초기화
    private void ClearQuestDetail()
    {
        questNameText.text = "퀘스트를 선택해주세요.";
        npcImage.gameObject.SetActive(false);
        questDescText.text = "";
        questObjectiveText.text = "";

        // 스크롤뷰 초기화
        if (questDescScrollView != null)
        {
            questDescScrollView.normalizedPosition = new Vector2(0, 1);
        }
        if (objectiveScrollView != null)
        {
            objectiveScrollView.normalizedPosition = new Vector2(0, 1);
        }

        questRewardPanel.ClearRewards();
    }

    // 퀘스트 알림 토글
    private void ToggleQuestNotice(Quest quest, bool isOn)
    {
        if (quest == null) return;

        if (isOn)
        {
            // 알림 추가
            AddQuestToNotice(quest);

            // 알림 패널이 비활성화되어 있다면 활성화
            if (questNoticePanel != null && !questNoticePanel.activeSelf)
            {
                questNoticePanel.SetActive(true);
            }
        }
        else
        {
            // 알림 제거
            RemoveQuestFromNotice(quest);
        }
    }

    // 퀘스트 목록 아이템 생성
    private GameObject CreateQuestListItem(Quest quest)
    {
        if (questListItemPrefab == null)
        {
            Debug.LogError("퀘스트 목록 아이템 프리팹이 할당되지 않았습니다!");
            return null;
        }

        GameObject questItemObj = null;
        try
        {
            questItemObj = Instantiate(questListItemPrefab);

            // 버튼 컴포넌트 확인 및 추가
            Button questButton = questItemObj.GetComponent<Button>();
            if (questButton == null)
            {
                questButton = questItemObj.AddComponent<Button>();
            }

            // 버튼 이미지 확인 및 추가
            Image buttonImage = questButton.GetComponent<Image>();
            if (buttonImage == null)
            {
                buttonImage = questItemObj.AddComponent<Image>();
                buttonImage.color = new Color(0, 0, 0, 0); // 투명 이미지
            }
            buttonImage.raycastTarget = true;

            QuestListItem questItem = questItemObj.GetComponent<QuestListItem>();
            if (questItem != null)
            {
                questItem.Initialize(quest);

                // 이벤트 연결 - 모든 이벤트 제거 후 재연결
                questItem.OnQuestSelected -= SelectQuest; // 기존 이벤트 제거
                questItem.OnQuestSelected += SelectQuest; // 새 이벤트 연결

                // 알림 이벤트 제거 및 연결
                questItem.OnNoticeToggled -= ToggleQuestNotice;
                questItem.OnNoticeToggled += ToggleQuestNotice;

                // 진행 중인 퀘스트만 알림 활성화
                if (quest.State == QuestState.InProgress)
                {
                    questItem.SetNoticeState(noticeQuests.Contains(quest));
                }
                else
                {
                    questItem.SetNoticeState(false);
                }
            }

            // 모든 하위 UI 요소의 RaycastTarget 활성화
            foreach (Graphic graphic in questItemObj.GetComponentsInChildren<Graphic>())
            {
                graphic.raycastTarget = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"퀘스트 목록 아이템 생성 중 오류 발생: {e.Message}");
        }

        return questItemObj;
    }

    // 퀘스트를 알림 목록에 추가
    private void AddQuestToNotice(Quest quest)
    {
        // 이미 알림 목록에 있는 퀘스트인지 확인
        if (noticeQuests.Contains(quest))
            return;

        // 알림 개수 제한 (최대 5개)
        if (noticeQuests.Count >= 5)
        {
            Debug.Log("퀘스트 알림은 최대 5개까지만 등록할 수 있습니다.");
            return;
        }

        // 알림 목록에 추가
        noticeQuests.Add(quest);

        // 알림 UI 생성
        GameObject noticeItemObj = Instantiate(questNoticeItemPrefab, noticeQuestContent);
        QuestNoticeItem noticeItem = noticeItemObj.GetComponent<QuestNoticeItem>();

        if (noticeItem != null)
        {
            noticeItem.Initialize(quest);
            noticeItem.OnRemoveClicked += RemoveQuestFromNotice;

            // 딕셔너리에 저장
            noticeQuestItems[quest.QuestId] = noticeItemObj;
        }

        // 모든 퀘스트 목록 아이템의 알림 상태 갱신
        RefreshAllQuestNoticeStates();
    }

    // 퀘스트를 알림 목록에서 제거
    private void RemoveQuestFromNotice(Quest quest)
    {
        // 알림 목록에 있는 퀘스트인지 확인
        if (!noticeQuests.Contains(quest))
            return;

        // 알림 목록에서 제거
        noticeQuests.Remove(quest);

        // 알림 UI 제거
        if (noticeQuestItems.TryGetValue(quest.QuestId, out GameObject noticeItemObj))
        {
            Destroy(noticeItemObj);
            noticeQuestItems.Remove(quest.QuestId);
        }

        // 모든 퀘스트 목록 아이템의 알림 상태 갱신
        RefreshAllQuestNoticeStates();

        // 알림 목록이 비어있으면 알림 패널 비활성화
        if (noticeQuests.Count == 0 && questNoticePanel != null)
        {
            questNoticePanel.SetActive(false);
        }
    }

    // 모든 퀘스트 목록 아이템의 알림 상태 갱신
    private void RefreshAllQuestNoticeStates()
    {
        foreach (GameObject item in currentPageQuestItems)
        {
            QuestListItem questItem = item.GetComponent<QuestListItem>();
            if (questItem != null)
            {
                questItem.SetNoticeState(noticeQuests.Contains(questItem.Quest));
            }
        }
    }

    // NPC ID로 NPC 컨트롤러 찾기
    private NPCController FindNpcControllerById(string npcId)
    {
        NPCController[] npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);

        foreach (var npc in npcs)
        {
            if (npc.GetNPCBase() != null && npc.GetNPCBase().NpcId == npcId)
            {
                return npc;
            }
        }

        return null;
    }

    // 지역 이름 가져오기 (Enum을 사람이 읽기 쉬운 이름으로 변환)
    private string GetRegionName(QuestRegion region)
    {
        switch (region)
        {
            case QuestRegion.Gwinoid_Forest:
                return "그위오니드 숲";
            case QuestRegion.Golden_Plain:
                return "황금 평원";
            case QuestRegion.Red_Mountain:
                return "붉은 산맥";
            case QuestRegion.White_Forest:
                return "하얀 숲";
            case QuestRegion.Hasla:
                return "하슬라";
            default:
                return "기타 지역";
        }
    }

    #region 퀘스트 매니저 이벤트 처리
    private void OnQuestStateChanged(Quest quest)
    {
        // 퀘스트 상태 변경 시 목록 및 페이지 갱신
        RefreshQuestLists();

        // 현재 열려있는 탭 갱신
        if (questUIPanel.activeSelf)
        {
            // 현재 선택된 탭 기준으로 다시 표시
            SwitchTab(currentTab);
        }

        // 선택된 퀘스트가 있다면 상세 정보 갱신
        if (selectedQuest == quest)
        {
            UpdateQuestDetail(quest);
        }

        // *** 수정된 부분 시작 ***
        // 퀘스트가 완료되거나 종료되면 알림 목록에서 제거
        if (quest.State == QuestState.Completed || quest.State == QuestState.Finished)
        {
            if (noticeQuests.Contains(quest))
            {
                RemoveQuestFromNotice(quest);
            }
        }
        // 퀘스트가 진행중일 때 알림 UI 업데이트
        else if (noticeQuests.Contains(quest) &&
                 noticeQuestItems.TryGetValue(quest.QuestId, out GameObject noticeItemObj) &&
                 noticeItemObj != null)
        {
            QuestNoticeItem noticeItem = noticeItemObj.GetComponent<QuestNoticeItem>();
            if (noticeItem != null)
            {
                noticeItem.UpdateUI();
            }
        }
        // *** 수정된 부분 끝 ***
    }

    private void OnQuestAccepted(Quest quest)
    {
        // 필요한 경우 퀘스트 수락 이벤트 처리
        RefreshQuestLists();

        if (questUIPanel.activeSelf)
        {
            SwitchTab(currentTab);
        }
    }

    private void OnQuestCompleted(Quest quest)
    {
        // 퀘스트 완료 이벤트 처리
        RefreshQuestLists();

        if (questUIPanel.activeSelf)
        {
            SwitchTab(currentTab);
        }
    }

    private void OnQuestFinished(Quest quest)
    {
        // 퀘스트 종료 이벤트 처리
        RefreshQuestLists();

        if (questUIPanel.activeSelf)
        {
            SwitchTab(currentTab);
        }
    }
    // 퀘스트 수락 패널 표시 메서드
    public void ShowQuestAcceptPanel(Quest quest)
    {
        if (questAcceptPanel == null) return;

        // 퀘스트 패널이 열려있다면 닫기
        if (questUIPanel.activeSelf)
        {
            CloseQuestPanel();
        }

        // 퀘스트 수락 패널에 정보 표시
        questAcceptPanel.ShowQuestDetails(quest);
    }

    // 퀘스트 수락 이벤트 핸들러
    private void HandleQuestAccepted(Quest quest)
    {
        if (quest == null) return;

        // 퀘스트 수락 처리
        if (quest.CheckPrerequisites())
        {
            quest.AcceptQuest();

            // 알림창이나 토스트 메시지 표시 (선택적)
        }
        else
        {
            // 선행 퀘스트가 완료되지 않았을 경우 메시지
            string prerequisiteMessage = quest.GetPrerequisiteQuestsText();
        }
    }

    // 퀘스트 거절 이벤트 핸들러
    private void HandleQuestDeclined()
    {
        // 거절 시 추가 동작이 필요한 경우 여기에 구현
    }
    #endregion
}