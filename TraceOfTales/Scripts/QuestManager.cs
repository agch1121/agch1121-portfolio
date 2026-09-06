using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestManager : CustomSingletone<QuestManager>,ISaveable
{
    [Header("Quest Database")]
    [SerializeField] private List<Quest> allQuests = new List<Quest>();

    [Header("Active Quests")]
    [SerializeField] private List<Quest> activeQuests = new List<Quest>();

    [Header("Completed Quests")]
    [SerializeField] private List<Quest> completedQuests = new List<Quest>();

    [Header("Finished Quests")]
    [SerializeField] private List<Quest> finishedQuests = new List<Quest>();


    // 몬스터 ID별 활성화된 킬 퀘스트 빠른 검색 (O(1))
    private Dictionary<string, List<KillQuest>> activeKillQuestsByMonsterId = new Dictionary<string, List<KillQuest>>();

    // 아이템 ID별 활성화된 수집 퀘스트 빠른 검색 (O(1))
    private Dictionary<string, List<CollectQuest>> activeCollectQuestsByItemId = new Dictionary<string, List<CollectQuest>>();

    // 위치 ID별 활성화된 도달 퀘스트 빠른 검색 (O(1))
    private Dictionary<string, List<ReachQuest>> activeReachQuestsByLocationId = new Dictionary<string, List<ReachQuest>>();

    // Dictionary로 퀘스트 ID와 생성된 targetPrefab 오브젝트를 관리
    private Dictionary<string, GameObject> activeReachQuestPrefabs = new Dictionary<string, GameObject>();

    // 몬스터 ID별 활성화된 퀘스트 딕셔너리에 Combined 추가
    private Dictionary<string, List<CombinedQuest>> activeCombinedQuestsByMonsterId = new Dictionary<string, List<CombinedQuest>>();
    private Dictionary<string, List<CombinedQuest>> activeCombinedQuestsByItemId = new Dictionary<string, List<CombinedQuest>>();

    // NPC ID별 활성화된 대화 퀘스트 빠른 검색 (O(1))
    private Dictionary<string, List<DialogueQuest>> activeDialogueQuestsByNpcId = new Dictionary<string, List<DialogueQuest>>();

    // 퀘스트 상태 변경 이벤트
    public event Action<Quest> OnQuestStateChanged;
    public event Action<Quest> OnQuestAccepted;
    public event Action<Quest> OnQuestCompleted;
    public event Action<Quest> OnQuestFinished;

    // 현재 선택된 퀘스트
    private Quest selectedQuest;

    // 퀘스트 패널 활성화 상태
    public bool IsQuestPanelActive { get; private set; }

    // 자동 체크 타이머
    private float checkTimer = 0f;
    private float checkInterval = 1f;

    protected override void Awake()
    {
        base.Awake();
        GameSessionManager.Instance?.RegisterSaveable(this);
        InitializeQuests();
    }

    // 주기적인 퀘스트 진행 상황 확인
    private void AutoCheckQuests()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;

            // 리스트의 복사본 생성
            List<Quest> questsToCheck = new List<Quest>(activeQuests);

            foreach (Quest quest in questsToCheck)
            {
                if (quest == null || quest.State != QuestState.InProgress) continue;

                // 수집 퀘스트 인벤토리 체크
                if (quest is CollectQuest collectQuest)
                {
                    collectQuest.CheckInventoryItems();
                }
                // 복합 퀘스트 인벤토리 체크
                else if (quest is CombinedQuest combinedQuest)
                {
                    combinedQuest.UpdateAutoDetect(checkInterval);
                }
            }
        }
    }


    private void Start()
    {
        GameEvents.OnEnemyKilled += TriggerKillEvent;
        GameEvents.OnItemCollected += TriggerCollectEvent;
        GameEvents.OnLocationReached += TriggerReachEvent;
    }

    private void Update()
    {
        // 자동 체크 - 수집 및 도달 퀘스트를 주기적으로 확인
        AutoCheckQuests();
    }
    public void InitializeQuests()
    {
        // 모든 퀘스트 초기화
        foreach (Quest quest in allQuests)
        {
            if (quest == null) continue;

            // 퀘스트 상태를 NotAccepted로 초기화
            quest.State = QuestState.NotAccepted;
            quest.currentProgress = 0;

            // 특정 타입 퀘스트 초기화 추가
            if (quest is CollectQuest collectQuest && collectQuest.RequiredItems != null)
            {
                foreach (var item in collectQuest.RequiredItems)
                {
                    item.currentCount = 0;
                }
            }
            else if (quest is KillQuest killQuest && killQuest.KillTargets != null)
            {
                foreach (var target in killQuest.KillTargets)
                {
                    target.currentCount = 0;
                }
            }

            // 퀘스트 이벤트 구독
            quest.OnQuestAccepted += HandleQuestAccepted;
            quest.OnQuestCompleted += HandleQuestCompleted;
            quest.OnQuestFinished += HandleQuestFinished;
            quest.OnQuestProgressUpdated += HandleQuestProgressUpdated;
        }

        // 기본 상태 기반으로 퀘스트 분류
        activeQuests.Clear();
        completedQuests.Clear();
        finishedQuests.Clear();
    }

    private void OnDestroy()
    {
        // 구독 해제
        foreach (Quest quest in allQuests)
        {
            if (quest == null) continue;

            quest.OnQuestAccepted -= HandleQuestAccepted;
            quest.OnQuestCompleted -= HandleQuestCompleted;
            quest.OnQuestFinished -= HandleQuestFinished;
            quest.OnQuestProgressUpdated -= HandleQuestProgressUpdated;
        }

        GameEvents.OnEnemyKilled -= TriggerKillEvent;
        GameEvents.OnItemCollected -= TriggerCollectEvent;
        GameEvents.OnLocationReached -= TriggerReachEvent;
    }

    #region 퀘스트 이벤트 핸들러
    private void HandleQuestAccepted(Quest quest)
    {
        if (!activeQuests.Contains(quest))
        {
            activeQuests.Add(quest);
        }

        // 퀘스트 타입별로 추가 처리
        RegisterQuestByType(quest);

        OnQuestAccepted?.Invoke(quest);
        OnQuestStateChanged?.Invoke(quest);
    }

    private void HandleQuestCompleted(Quest quest)
    {
        OnQuestCompleted?.Invoke(quest);
        OnQuestStateChanged?.Invoke(quest);
    }

    private void HandleQuestFinished(Quest quest)
    {
        if (activeQuests.Contains(quest))
        {
            activeQuests.Remove(quest);

            // 활성화된 퀘스트 목록에서 제거
            UnregisterQuestByType(quest);
        }

        if (!completedQuests.Contains(quest))
        {
            completedQuests.Add(quest);
        }

        if (!finishedQuests.Contains(quest) && quest.State == QuestState.Finished)
        {
            finishedQuests.Add(quest);
        }

        OnQuestFinished?.Invoke(quest);
        OnQuestStateChanged?.Invoke(quest);

    }

    private void HandleQuestProgressUpdated(Quest quest)
    {
        // 진행 상황 변경 시 UI 업데이트 등의 처리
        OnQuestStateChanged?.Invoke(quest);
    }
    #endregion

    #region 퀘스트 타입별 등록/해제
    // 퀘스트 타입에 따라 적절한 딕셔너리에 등록
    private void RegisterQuestByType(Quest quest)
    {
        if (quest is KillQuest killQuest)
        {
            RegisterKillQuest(killQuest);
        }
        else if (quest is CollectQuest collectQuest)
        {
            RegisterCollectQuest(collectQuest);
        }
        else if (quest is ReachQuest reachQuest)
        {
            RegisterReachQuest(reachQuest);
        }
        else if (quest is CombinedQuest combinedQuest)
        {
            RegisterCombinedQuest(combinedQuest);
        }
        else if (quest is DialogueQuest dialogueQuest)
        {
            RegisterDialogueQuest(dialogueQuest);
        }
    }


    // 퀘스트 타입에 따라 적절한 딕셔너리에서 제거
    private void UnregisterQuestByType(Quest quest)
    {
        if (quest is KillQuest killQuest)
        {
            UnregisterKillQuest(killQuest);
        }
        else if (quest is CollectQuest collectQuest)
        {
            UnregisterCollectQuest(collectQuest);
        }
        else if (quest is ReachQuest reachQuest)
        {
            UnregisterReachQuest(reachQuest);
        }
        else if (quest is CombinedQuest combinedQuest)
        {
            UnregisterCombinedQuest(combinedQuest);
        }
        else if (quest is DialogueQuest dialogueQuest)
        {
            UnregisterDialogueQuest(dialogueQuest);
        }
    }

    // 킬 퀘스트 등록
    private void RegisterKillQuest(KillQuest quest)
    {
        foreach (var target in quest.KillTargets)
        {
            if (!activeKillQuestsByMonsterId.ContainsKey(target.MonsterId))
            {
                activeKillQuestsByMonsterId[target.MonsterId] = new List<KillQuest>();
            }

            if (!activeKillQuestsByMonsterId[target.MonsterId].Contains(quest))
            {
                activeKillQuestsByMonsterId[target.MonsterId].Add(quest);
            }
        }
    }

    // 킬 퀘스트 해제
    private void UnregisterKillQuest(KillQuest quest)
    {
        foreach (var target in quest.KillTargets)
        {
            if (activeKillQuestsByMonsterId.ContainsKey(target.MonsterId))
            {
                activeKillQuestsByMonsterId[target.MonsterId].Remove(quest);

                // 빈 리스트는 제거
                if (activeKillQuestsByMonsterId[target.MonsterId].Count == 0)
                {
                    activeKillQuestsByMonsterId.Remove(target.MonsterId);
                }
            }
        }
    }

    // 수집 퀘스트 등록
    private void RegisterCollectQuest(CollectQuest quest)
    {
        foreach (var item in quest.RequiredItems)
        {
            if (item.item == null) continue;

            string itemId = item.item.ID;
            if (!activeCollectQuestsByItemId.ContainsKey(itemId))
            {
                activeCollectQuestsByItemId[itemId] = new List<CollectQuest>();
            }

            if (!activeCollectQuestsByItemId[itemId].Contains(quest))
            {
                activeCollectQuestsByItemId[itemId].Add(quest);
            }
        }
    }

    // 수집 퀘스트 해제
    private void UnregisterCollectQuest(CollectQuest quest)
    {
        foreach (var item in quest.RequiredItems)
        {
            if (item.item == null) continue;

            string itemId = item.item.ID;
            if (activeCollectQuestsByItemId.ContainsKey(itemId))
            {
                activeCollectQuestsByItemId[itemId].Remove(quest);

                // 빈 리스트는 제거
                if (activeCollectQuestsByItemId[itemId].Count == 0)
                {
                    activeCollectQuestsByItemId.Remove(itemId);
                }
            }
        }
    }

    // 도달 퀘스트 등록
    private void RegisterReachQuest(ReachQuest quest)
    {
        if (string.IsNullOrEmpty(quest.locationId)) return;

        // 딕셔너리 초기화
        if (!activeReachQuestsByLocationId.ContainsKey(quest.locationId))
        {
            activeReachQuestsByLocationId[quest.locationId] = new List<ReachQuest>();
        }

        // 퀘스트 추가
        if (!activeReachQuestsByLocationId[quest.locationId].Contains(quest))
        {
            activeReachQuestsByLocationId[quest.locationId].Add(quest);
        }

        // targetPrefab이 있는 경우 씬에 생성
        if (quest.targetPrefab != null)
        {
            string questPrefabKey = quest.QuestId; // 퀘스트 ID를 키로 사용

            // 이미 생성된 프리팹이 있는지 확인
            if (!activeReachQuestPrefabs.ContainsKey(questPrefabKey))
            {
                // 프리팹 인스턴스 생성
                Vector3 targetPosition = quest.TargetLocation;
                GameObject targetInstance = Instantiate(quest.targetPrefab, targetPosition, Quaternion.identity);
                targetInstance.name = $"TargetPrefab_{quest.QuestName}";

                // 트리거 존 설정
                QuestTriggerZone triggerZone = targetInstance.GetComponent<QuestTriggerZone>();
                if (triggerZone != null)
                {
                    quest.SetTargetZone(triggerZone);
                    triggerZone.LinkQuest(quest);
                }

                // 딕셔너리에 저장
                activeReachQuestPrefabs[questPrefabKey] = targetInstance;
            }
        }
    }

    // 도달 퀘스트 해제
    private void UnregisterReachQuest(ReachQuest quest)
    {
        if (string.IsNullOrEmpty(quest.locationId)) return;

        // 딕셔너리에서 제거
        if (activeReachQuestsByLocationId.ContainsKey(quest.locationId))
        {
            activeReachQuestsByLocationId[quest.locationId].Remove(quest);

            // 빈 리스트는 제거
            if (activeReachQuestsByLocationId[quest.locationId].Count == 0)
            {
                activeReachQuestsByLocationId.Remove(quest.locationId);
            }
        }

        // 생성된 targetPrefab 인스턴스 제거
        string questPrefabKey = quest.QuestId;
        if (activeReachQuestPrefabs.ContainsKey(questPrefabKey))
        {
            GameObject targetInstance = activeReachQuestPrefabs[questPrefabKey];
            if (targetInstance != null)
            {
                Destroy(targetInstance);
            }

            activeReachQuestPrefabs.Remove(questPrefabKey);
        }
    }

    // 복합 퀘스트 등록 메서드
    private void RegisterCombinedQuest(CombinedQuest quest)
    {
        // 1. 사냥 목표 등록
        foreach (var target in quest.HuntTargets)
        {
            string monsterId = target.GetMonsterId();
            if (string.IsNullOrEmpty(monsterId)) continue;

            if (!activeCombinedQuestsByMonsterId.ContainsKey(monsterId))
            {
                activeCombinedQuestsByMonsterId[monsterId] = new List<CombinedQuest>();
            }

            if (!activeCombinedQuestsByMonsterId[monsterId].Contains(quest))
            {
                activeCombinedQuestsByMonsterId[monsterId].Add(quest);
            }
        }

        // 2. 수집 목표 등록
        foreach (var target in quest.CollectTargets)
        {
            if (target.item == null) continue;

            string itemId = target.item.ID;
            if (!activeCombinedQuestsByItemId.ContainsKey(itemId))
            {
                activeCombinedQuestsByItemId[itemId] = new List<CombinedQuest>();
            }

            if (!activeCombinedQuestsByItemId[itemId].Contains(quest))
            {
                activeCombinedQuestsByItemId[itemId].Add(quest);
            }
        }
    }

    // 복합 퀘스트 해제 메서드
    private void UnregisterCombinedQuest(CombinedQuest quest)
    {
        // 1. 사냥 목표 해제
        foreach (var target in quest.HuntTargets)
        {
            string monsterId = target.GetMonsterId();
            if (string.IsNullOrEmpty(monsterId)) continue;

            if (activeCombinedQuestsByMonsterId.ContainsKey(monsterId))
            {
                activeCombinedQuestsByMonsterId[monsterId].Remove(quest);

                // 빈 리스트는 제거
                if (activeCombinedQuestsByMonsterId[monsterId].Count == 0)
                {
                    activeCombinedQuestsByMonsterId.Remove(monsterId);
                }
            }
        }

        // 2. 수집 목표 해제
        foreach (var target in quest.CollectTargets)
        {
            if (target.item == null) continue;

            string itemId = target.item.ID;
            if (activeCombinedQuestsByItemId.ContainsKey(itemId))
            {
                activeCombinedQuestsByItemId[itemId].Remove(quest);

                // 빈 리스트는 제거
                if (activeCombinedQuestsByItemId[itemId].Count == 0)
                {
                    activeCombinedQuestsByItemId.Remove(itemId);
                }
            }
        }
    }
    // 대화 퀘스트 등록
    private void RegisterDialogueQuest(DialogueQuest quest)
    {
        foreach (var target in quest.DialogueTargets)
        {
            string npcId = target.targetNpcId;
            if (string.IsNullOrEmpty(npcId)) continue;

            if (!activeDialogueQuestsByNpcId.ContainsKey(npcId))
            {
                activeDialogueQuestsByNpcId[npcId] = new List<DialogueQuest>();
            }

            if (!activeDialogueQuestsByNpcId[npcId].Contains(quest))
            {
                activeDialogueQuestsByNpcId[npcId].Add(quest);
            }
        }
    }

    // 대화 퀘스트 해제
    private void UnregisterDialogueQuest(DialogueQuest quest)
    {
        foreach (var target in quest.DialogueTargets)
        {
            string npcId = target.targetNpcId;
            if (string.IsNullOrEmpty(npcId)) continue;

            if (activeDialogueQuestsByNpcId.ContainsKey(npcId))
            {
                activeDialogueQuestsByNpcId[npcId].Remove(quest);

                // 빈 리스트는 제거
                if (activeDialogueQuestsByNpcId[npcId].Count == 0)
                {
                    activeDialogueQuestsByNpcId.Remove(npcId);
                }
            }
        }
    }
    #endregion

    #region 퀘스트 접근 메서드
    // ID로 퀘스트 찾기
    public Quest GetQuestById(string questId)
    {
        foreach (Quest quest in allQuests)
        {
            if (quest != null && quest.QuestId == questId)
            {
                return quest;
            }
        }
        return null;
    }

    // 모든 퀘스트 목록 반환
    public List<Quest> GetAllQuests()
    {
        return allQuests;
    }

    // 활성화된 퀘스트 목록 반환
    public List<Quest> GetActiveQuests()
    {
        return activeQuests;
    }

    // 완료된 퀘스트 목록 반환
    public List<Quest> GetCompletedQuests()
    {
        return completedQuests;
    }

    // 보상 받은 퀘스트 목록 반환
    public List<Quest> GetFinishedQuests()
    {
        return finishedQuests;
    }

    // 지역별 퀘스트 목록 반환
    public List<Quest> GetQuestsByRegion(QuestRegion region)
    {
        List<Quest> regionQuests = new List<Quest>();
        foreach (Quest quest in allQuests)
        {
            if (quest != null && quest.Region == region)
            {
                regionQuests.Add(quest);
            }
        }
        return regionQuests;
    }

    // NPC ID로 관련된 퀘스트 찾기
    public List<Quest> GetQuestsByNpcId(string npcId, bool includeCompleted = false)
    {
        List<Quest> result = new List<Quest>();

        foreach (Quest quest in allQuests)
        {
            if (quest == null) continue;

            bool isCompleted = quest.State == QuestState.Completed || quest.State == QuestState.Finished;

            // 완료된 퀘스트 포함 여부 확인
            if (isCompleted && !includeCompleted) continue;

            // NPC가 시작 또는 완료 NPC인지 확인
            if (quest.StartNpcId == npcId || quest.CompleteNpcId == npcId)
            {
                result.Add(quest);
            }
        }

        return result;
    }

    // 특정 NPC에게 수락 가능한 퀘스트 목록 반환
    public List<Quest> GetAvailableQuestsForNpc(string npcId)
    {
        // 1. NPC 컨트롤러 찾기
        NPCController npcController = FindNpcControllerById(npcId);
        if (npcController == null) return new List<Quest>();

        // 2. NPC_QuestProvider 찾기
        NPC_QuestProvider questProvider = npcController.GetInteractionObject(InteractionType.Quest) as NPC_QuestProvider;
        if (questProvider == null) return new List<Quest>();

        // 3. NPC_QuestProvider의 메서드 활용하여 수락 가능한 퀘스트 가져오기
        return questProvider.GetAvailableQuestsForPlayer();
    }

    // 특정 NPC에게 완료할 수 있는 퀘스트 목록 반환
    public List<Quest> GetCompletableQuestsForNpc(string npcId)
    {
        List<Quest> result = new List<Quest>();

        // 모든 활성 퀘스트 확인
        foreach (Quest quest in activeQuests)
        {
            if (quest == null) continue;

            // 진행 중이지만 완료 조건을 만족한 경우도 포함 (중요 수정)
            if (quest.State == QuestState.InProgress &&
                quest.IsCompleted() &&
                quest.CompleteNpcId == npcId)
            {
                result.Add(quest);
            }
            // 이미 Completed 상태인 퀘스트
            else if (quest.State == QuestState.Completed && quest.CompleteNpcId == npcId)
            {
                result.Add(quest);
            }
        }

        return result;
    }

    // 특정 NPC에게 진행 중인 퀘스트 목록 반환
    public List<Quest> GetInProgressQuestsForNPC(string npcId)
    {
        List<Quest> result = new List<Quest>();

        foreach (Quest quest in activeQuests)
        {
            if (quest == null) continue;

            // 퀘스트가 진행 중이고, 시작 NPC가 현재 NPC인 경우
            if (quest.State == QuestState.InProgress &&
                (quest.StartNpcId == npcId || quest.CompleteNpcId == npcId))
            {
                result.Add(quest);
            }
        }

        return result;
    }

    // NPC 컨트롤러 ID로 찾기
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
    #endregion

    #region 퀘스트 조건 처리 메서드
    // Kill 이벤트 트리거 (몬스터 처치)
    public void TriggerKillEvent(Enemy enemy)
    {
        string monsterId = enemy.Stats.enemyId;

        // Kill 퀘스트 처리
        if (activeKillQuestsByMonsterId.TryGetValue(monsterId, out var killQuests))
        {
            foreach (KillQuest quest in new List<KillQuest>(killQuests))
            {
                if (quest.State == QuestState.InProgress)
                {
                    quest.ProcessMonsterKill(monsterId);
                }
            }
        }

        // Combined 퀘스트 처리
        if (activeCombinedQuestsByMonsterId.TryGetValue(monsterId, out var combinedQuests))
        {
            foreach (CombinedQuest quest in new List<CombinedQuest>(combinedQuests))
            {
                if (quest.State == QuestState.InProgress)
                {
                    quest.ProcessMonsterKill(monsterId);
                }
            }
        }
    }

    // Collect 이벤트 트리거 (아이템 수집)
    public void TriggerCollectEvent(string itemId, int amount = 1)
    {
        // Collect 퀘스트 처리
        if (activeCollectQuestsByItemId.TryGetValue(itemId, out var collectQuests))
        {
            foreach (CollectQuest quest in new List<CollectQuest>(collectQuests))
            {
                if (quest.State == QuestState.InProgress)
                {
                    quest.ItemCollected(itemId, amount);
                }
            }
        }

        // Combined 퀘스트 처리
        if (activeCombinedQuestsByItemId.TryGetValue(itemId, out var combinedQuests))
        {
            foreach (CombinedQuest quest in new List<CombinedQuest>(combinedQuests))
            {
                if (quest.State == QuestState.InProgress)
                {
                    quest.ItemCollected(itemId, amount);
                }
            }
        }
    }


    // Reach 이벤트 트리거 (위치 도달)
    public void TriggerReachEvent(string locationId, Vector3 playerPosition)
    {
        // 처리 가능한 퀘스트 수 추적
        int processedQuests = 0;

        // 해당 위치 ID에 관련된 도달 퀘스트가 있는지 확인
        if (activeReachQuestsByLocationId.TryGetValue(locationId, out var quests))
        {
            // 복사본 생성
            List<ReachQuest> questsToProcess = new List<ReachQuest>(quests);

            foreach (ReachQuest quest in questsToProcess)
            {
                if (quest.State == QuestState.InProgress)
                {
                    processedQuests++;
                    bool inRange = quest.CheckReached(playerPosition);

                    if (inRange)
                    {
                        quest.LocationReached();
                    }
                }
            }
        }
    }
    #endregion

    #region 대화 이벤트 처리

    // 대화 이벤트 트리거 (DialogueManager에서 호출)
    public void TriggerDialogueEvent(string npcId, string dialogueId = "")
    {
        // 해당 NPC와 관련된 대화 퀘스트 처리
        if (activeDialogueQuestsByNpcId.TryGetValue(npcId, out var dialogueQuests))
        {
            foreach (DialogueQuest quest in new List<DialogueQuest>(dialogueQuests))
            {
                if (quest.State == QuestState.InProgress)
                {
                    quest.ProcessDialogue(npcId, dialogueId);
                }
            }
        }
    }

    // 특정 NPC가 대화 퀘스트 대상인지 확인
    public bool HasDialogueQuestForNpc(string npcId)
    {
        return activeDialogueQuestsByNpcId.ContainsKey(npcId) && activeDialogueQuestsByNpcId[npcId].Count > 0;
    }

    // 특정 NPC와 관련된 진행 중인 대화 퀘스트 목록 반환
    public List<DialogueQuest> GetActiveDialogueQuestsForNpc(string npcId)
    {
        if (activeDialogueQuestsByNpcId.TryGetValue(npcId, out var quests))
        {
            List<DialogueQuest> activeQuests = new List<DialogueQuest>();
            foreach (var quest in quests)
            {
                if (quest.State == QuestState.InProgress)
                {
                    activeQuests.Add(quest);
                }
            }
            return activeQuests;
        }
        return new List<DialogueQuest>();
    }
    #endregion

    #region 저장 시스템
    public string SaveKey => "Quest";


    public object Save()
    {
        /// <summary>
        /// 현재 퀘스트 상태를 QuestSaveData로 수집하여 반환
        /// 퀘스트 ID 기반으로 저장
        /// </summary>

        QuestSaveData questSaveData = new QuestSaveData();

        // 1. 완료된 퀘스트 ID들 수집
        foreach (Quest quest in allQuests)
        {
            if (quest != null && quest.State == QuestState.Finished)
            {
                questSaveData.finishedQuestIds.Add(quest.QuestId);
            }
        }

        // 2. 진행중인 퀘스트들 정보 수집 (모든 리스트가 같은 순서로 저장됨)
        foreach (Quest quest in allQuests)
        {
            if (quest != null && quest.State == QuestState.InProgress)
            {
                questSaveData.activeQuestIds.Add(quest.QuestId);
                questSaveData.questCurrentProgress.Add(quest.currentProgress);
                questSaveData.questRequiredProgress.Add(quest.requiredProgress);
                questSaveData.questDetailedProgress.Add(GetQuestDetailedProgress(quest));
            }
        }

        return questSaveData;
    }

    public void Load(object data)
    {
        /// <summary>
        /// QuestSaveData를 받아서 퀘스트 상태 복원
        /// 퀘스트 ID로 검색하여 복원
        /// </summary>

        QuestSaveData questSaveData = data as QuestSaveData;

        if (questSaveData == null) return;

        // 1. 모든 퀘스트 초기화
        //ResetAllQuests();

        // 2. 완료된 퀘스트들 상태 설정
        foreach (string questId in questSaveData.finishedQuestIds)
        {
            Quest quest = FindQuestById(questId);
            if (quest != null)
            {
                quest.State = QuestState.Finished;
                if (!finishedQuests.Contains(quest))
                {
                    finishedQuests.Add(quest);
                }
            }
        }

        // 3. 진행중인 퀘스트들 복원 (모든 리스트가 같은 순서로 저장되어 있음)
        for (int i = 0; i < questSaveData.activeQuestIds.Count; i++)
        {
            string questId = questSaveData.activeQuestIds[i];
            Quest quest = FindQuestById(questId);

            if (quest != null)
            {
                quest.State = QuestState.InProgress;

                // 기본 진행도 복원
                if (i < questSaveData.questCurrentProgress.Count)
                    quest.currentProgress = questSaveData.questCurrentProgress[i];
                if (i < questSaveData.questRequiredProgress.Count)
                    quest.requiredProgress = questSaveData.questRequiredProgress[i];

                // 세부 진행도 복원
                if (i < questSaveData.questDetailedProgress.Count)
                    SetQuestDetailedProgress(quest, questSaveData.questDetailedProgress[i]);

                if (!activeQuests.Contains(quest))
                {
                    activeQuests.Add(quest);
                }
                RegisterQuestByType(quest);
            }
        }
    }

    /// <summary>
    /// 퀘스트 ID로 allQuests에서 퀘스트 찾기
    /// </summary>
    private Quest FindQuestById(string questId)
    {
        foreach (Quest quest in allQuests)
        {
            if (quest != null && quest.QuestId == questId)
            {
                return quest;
            }
        }

        Debug.LogWarning($"[QuestManager] 퀘스트 ID '{questId}'를 찾을 수 없습니다.");
        return null;
    }

    private int[] GetQuestDetailedProgress(Quest quest)
    {
        /// <summary>
        /// 퀘스트 타입별 세부 진행도를 배열로 변환
        /// KillQuest: [몬스터1 처치수, 몬스터2 처치수, ...]
        /// CollectQuest: [아이템1 수집수, 아이템2 수집수, ...]
        /// CombinedQuest: [몬스터1, 몬스터2, 아이템1, 아이템2, ...] 순서
        /// </summary>

        switch (quest.QuestType)
        {
            case QuestType.Kill:
                if (quest is KillQuest killQuest)
                {
                    int[] progress = new int[killQuest.KillTargets.Count];
                    for (int i = 0; i < killQuest.KillTargets.Count; i++)
                    {
                        progress[i] = killQuest.KillTargets[i].currentCount;
                    }
                    return progress;
                }
                break;

            case QuestType.Collect:
                if (quest is CollectQuest collectQuest)
                {
                    int[] progress = new int[collectQuest.RequiredItems.Count];
                    for (int i = 0; i < collectQuest.RequiredItems.Count; i++)
                    {
                        progress[i] = collectQuest.RequiredItems[i].currentCount;
                    }
                    return progress;
                }
                break;

            case QuestType.Combined:
                if (quest is CombinedQuest combinedQuest)
                {
                    List<int> progressList = new List<int>();

                    // 사냥 타겟들 진행도 추가
                    foreach (var huntTarget in combinedQuest.HuntTargets)
                    {
                        progressList.Add(huntTarget.currentCount);
                    }

                    // 수집 타겟들 진행도 추가
                    foreach (var collectTarget in combinedQuest.CollectTargets)
                    {
                        progressList.Add(collectTarget.currentCount);
                    }

                    return progressList.ToArray();
                }
                break;

            case QuestType.Reach:
            case QuestType.Dialogue:
            default:
                // 단순 퀘스트는 빈 배열 반환
                return new int[0];
        }

        return new int[0];
    }
    private void SetQuestDetailedProgress(Quest quest, int[] detailedProgress)
    {
        /// <summary>
        /// 배열 데이터를 퀘스트 객체의 세부 진행도로 복원
        /// </summary>

        if (detailedProgress == null || detailedProgress.Length == 0) return;

        switch (quest.QuestType)
        {
            case QuestType.Kill:
                if (quest is KillQuest killQuest)
                {
                    for (int i = 0; i < killQuest.KillTargets.Count && i < detailedProgress.Length; i++)
                    {
                        killQuest.KillTargets[i].currentCount = detailedProgress[i];
                    }
                }
                break;

            case QuestType.Collect:
                if (quest is CollectQuest collectQuest)
                {
                    for (int i = 0; i < collectQuest.RequiredItems.Count && i < detailedProgress.Length; i++)
                    {
                        collectQuest.RequiredItems[i].currentCount = detailedProgress[i];
                    }
                }
                break;

            case QuestType.Combined:
                if (quest is CombinedQuest combinedQuest)
                {
                    int index = 0;

                    // 사냥 타겟들 진행도 복원
                    for (int i = 0; i < combinedQuest.HuntTargets.Count && index < detailedProgress.Length; i++, index++)
                    {
                        combinedQuest.HuntTargets[i].currentCount = detailedProgress[index];
                    }

                    // 수집 타겟들 진행도 복원
                    for (int i = 0; i < combinedQuest.CollectTargets.Count && index < detailedProgress.Length; i++, index++)
                    {
                        combinedQuest.CollectTargets[i].currentCount = detailedProgress[index];
                    }
                }
                break;
        }
    }
    #endregion
}