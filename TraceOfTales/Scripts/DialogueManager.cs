using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : CustomSingletone<DialogueManager>
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject dialogueBtnPanel;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image npcImage;

    [Header("Quest Integration")]
    [SerializeField] private GameObject questAvailableIndicator;
    [SerializeField] private GameObject questInProgressIndicator;
    [SerializeField] private GameObject questCompletedIndicator;

    private NPC currentNPC;
    private NPCMovement npcMove;
    private DialogueData currentDialogue;
    private NPC_Dialogue currentNPCDialogue;

    private string[] greetingSentences;
    private bool isInGreetingMode = false;

    [Header("Typing Effect & Choices")]
    [SerializeField] private DialogueTypingEffect typingEffect;
    [SerializeField] private DialogueChoiceManager choiceManager;
    [SerializeField] private DialogueNoticeManager promptManager;

    private Dictionary<QuestState, List<Quest>> npcQuestsByState = new Dictionary<QuestState, List<Quest>>();
    private QuestManager questManager;

    public GameObject DialoguePanel => dialoguePanel;
    public GameObject DialogueBtnPanel => dialogueBtnPanel;
    public bool IsTyping => typingEffect.IsTyping;
    public DialogueData GetCurrentDialogue() => currentDialogue;
    public DialogueNoticeManager GetPromptManager() => promptManager;
    public NPC CurrentNPC => currentNPC;
    public bool IsInGreetingMode => isInGreetingMode;
    public string[] GetGreetingSentences() => greetingSentences;

    private List<NPCInteraction> nearbyNPCs = new List<NPCInteraction>();

    private void Start()
    {
        if (typingEffect == null)
            typingEffect = GetComponent<DialogueTypingEffect>();
        if (choiceManager == null)
            choiceManager = GetComponent<DialogueChoiceManager>();
        if (promptManager == null)
            promptManager = GetComponent<DialogueNoticeManager>();

        questManager = QuestManager.Instance;

        typingEffect.Initialize(dialogueText);
        choiceManager.Initialize(this);
        promptManager.Initialize();

        typingEffect.OnTypingCompleted += OnTypingCompleted;

        InitializeQuestIntegration();
    }

    private void OnDestroy()
    {
        if (typingEffect != null)
        {
            typingEffect.OnTypingCompleted -= OnTypingCompleted;
        }

        if (questManager != null)
        {
            questManager.OnQuestStateChanged -= RefreshDialogueOptions;
        }
    }

    private void InitializeQuestIntegration()
    {
        if (questManager == null)
            questManager = QuestManager.Instance;

        if (questManager != null)
        {
            questManager.OnQuestStateChanged += RefreshDialogueOptions;
        }

        npcQuestsByState = new Dictionary<QuestState, List<Quest>>
        {
            { QuestState.NotAccepted, new List<Quest>() },
            { QuestState.InProgress, new List<Quest>() },
            { QuestState.Completed, new List<Quest>() },
            { QuestState.Finished, new List<Quest>() }
        };
    }

    private void OnTypingCompleted()
    {
        bool hasMoreSentences;
        bool hasChoices;

        if (isInGreetingMode)
        {
            hasMoreSentences = typingEffect.CurrentSentenceIndex < greetingSentences.Length - 1;
            hasChoices = !hasMoreSentences && currentNPC.Interactions != null &&
                         HasDialogueInteraction(currentNPC.Interactions);
        }
        else
        {
            hasMoreSentences = typingEffect.HasMoreSentences(currentDialogue);
            hasChoices = currentDialogue.Choices != null && currentDialogue.Choices.Length > 0;
        }

        promptManager.UpdateContinueNotice(false, hasMoreSentences, hasChoices);
    }

    private bool HasDialogueInteraction(List<Interaction> interactions)
    {
        foreach (var interaction in interactions)
        {
            if (interaction.Type == InteractionType.Dialogue &&
                interaction.GetInteractionObject() is NPC_Dialogue)
            {
                return true;
            }
        }
        return false;
    }

    public void Initialize(NPC npc, NPCMovement npcMove)
    {
        currentNPC = npc;
        this.npcMove = npcMove;
        currentDialogue = null;
        isInGreetingMode = false;

        currentNPCDialogue = null;
        if (currentNPC != null && currentNPC.Interactions != null)
        {
            foreach (var interaction in currentNPC.Interactions)
            {
                if (interaction.Type == InteractionType.Dialogue)
                {
                    currentNPCDialogue = interaction.GetInteractionObject() as NPC_Dialogue;
                    break;
                }
            }
        }

        UpdateNPCInfo();
        CheckNPCQuests();
    }

    /// <summary>
    /// 간단한 대화 접근성 확인 - DialogueQuest가 진행중인지만 체크
    /// </summary>
    private bool CanAccessDialogue(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return true;

        // DialogueQuest 중에서 이 대화 ID가 필요한 퀘스트가 있는지 확인
        var activeQuests = questManager.GetActiveQuests();

        foreach (var quest in activeQuests)
        {
            if (quest is DialogueQuest dialogueQuest)
            {
                // 이 NPC와 관련된 대화 퀘스트인지 확인
                if (dialogueQuest.IsDialogueTarget(currentNPC.NpcId))
                {
                    // 특정 대화 ID가 요구되는 경우
                    foreach (var target in dialogueQuest.DialogueTargets)
                    {
                        if (target.targetNpcId == currentNPC.NpcId &&
                            !string.IsNullOrEmpty(target.requiredDialogueId) &&
                            target.requiredDialogueId == dialogueId)
                        {
                            // 퀘스트가 진행중이면 접근 가능
                            return dialogueQuest.State == QuestState.InProgress;
                        }
                    }
                }
            }
        }

        // 관련 DialogueQuest가 없으면 접근 가능
        return true;
    }

    /// <summary>
    /// 접근 불가능한 선택지 제거
    /// </summary>
    private DialogueChoice[] FilterChoices(DialogueChoice[] originalChoices)
    {
        if (originalChoices == null) return null;

        List<DialogueChoice> validChoices = new List<DialogueChoice>();

        foreach (var choice in originalChoices)
        {
            // NextDialogueId가 있는 선택지만 체크
            if (!string.IsNullOrEmpty(choice.NextDialogueId))
            {
                if (CanAccessDialogue(choice.NextDialogueId))
                {
                    validChoices.Add(choice);
                }
            }
            else
            {
                // NextDialogueId가 없는 선택지는 그대로 포함
                validChoices.Add(choice);
            }
        }

        return validChoices.ToArray();
    }

    private void UpdateNPCInfo()
    {
        if (currentNPC != null)
        {
            npcNameText.text = currentNPC.NpcName;
            if (npcImage != null && currentNPC.NpcImage != null)
            {
                npcImage.sprite = currentNPC.NpcImage;
            }
        }
    }

    public void RegisterNearbyNPC(NPCInteraction npc)
    {
        if (!nearbyNPCs.Contains(npc))
            nearbyNPCs.Add(npc);
    }

    public void UnregisterNearbyNPC(NPCInteraction npc)
    {
        nearbyNPCs.Remove(npc);
    }

    public void OnDialogueButtonClicked()
    {
        foreach (var npc in nearbyNPCs)
        {
            npc.TryStartDialogue();
        }
    }

    public void StartDialogue()
    {
        if (currentNPC == null)
        {
            Debug.LogError("StartDialogue: NPC가 설정되지 않았습니다!");
            return;
        }

        dialoguePanel.SetActive(true);
        if (dialogueBtnPanel.activeSelf)
        {
            dialogueBtnPanel.SetActive(false);
        }

        if (currentNPC.DefaultDialogue != null && currentNPC.DefaultDialogue.Length > 0)
        {
            StartGreeting();
        }
        else
        {
            StartDialogueData();
        }
    }

    /// <summary>
    /// 간단한 대화 퀘스트 트리거
    /// </summary>
    private void TriggerDialogueQuests()
    {
        if (questManager != null && currentNPC != null)
        {
            questManager.TriggerDialogueEvent(currentNPC.NpcId, "dialogue_start");
        }
    }

    private void StartGreeting()
    {
        isInGreetingMode = true;
        greetingSentences = currentNPC.DefaultDialogue;
        typingEffect.ResetIndex();
        ShowNextSentence();
    }

    private void StartDialogueData()
    {
        isInGreetingMode = false;

        if (currentNPCDialogue == null || currentNPCDialogue.Dialogues == null || currentNPCDialogue.Dialogues.Length == 0)
        {
            Debug.LogWarning("StartDialogueData: NPC의 대화 데이터가 없습니다. 대화를 종료합니다.");
            EndDialogue();
            return;
        }

        currentDialogue = currentNPCDialogue.Dialogues[0];

        if (currentDialogue == null || currentDialogue.Sentences == null || currentDialogue.Sentences.Length == 0)
        {
            Debug.LogError("StartDialogueData: 대화 문장이 비어 있습니다!");
            EndDialogue();
            return;
        }

        typingEffect.ResetIndex();
        ShowNextSentence();
    }

    public void ShowNextSentence()
    {
        if (typingEffect.IsTyping)
        {
            typingEffect.CompleteCurrentSentence();

            bool hasMoreSentences, hasChoices;

            if (isInGreetingMode)
            {
                hasMoreSentences = typingEffect.CurrentSentenceIndex < greetingSentences.Length - 1;
                hasChoices = !hasMoreSentences && HasDialogueInteraction(currentNPC.Interactions);
            }
            else
            {
                hasMoreSentences = typingEffect.HasMoreSentences(currentDialogue);
                hasChoices = currentDialogue.Choices != null && currentDialogue.Choices.Length > 0;
            }

            promptManager.UpdateContinueNotice(false, hasMoreSentences, hasChoices);
            return;
        }

        if (isInGreetingMode)
        {
            if (typingEffect.CurrentSentenceIndex < greetingSentences.Length)
            {
                typingEffect.TypeSentence(greetingSentences[typingEffect.CurrentSentenceIndex]);
            }
            else
            {
                promptManager.StopBlinkEffect();

                if (HasDialogueInteraction(currentNPC.Interactions))
                {
                    choiceManager.ShowGreetingOptions(HandleGreetingChoice);
                }
                else
                {
                    EndDialogue();
                }
            }
        }
        else
        {
            if (currentDialogue == null || !typingEffect.HasMoreSentences(currentDialogue))
            {
                promptManager.StopBlinkEffect();

                // 접근 가능한 선택지만 필터링
                DialogueChoice[] filteredChoices = FilterChoices(currentDialogue.Choices);

                // 필터링된 선택지로 대화 데이터 생성
                DialogueData filteredDialogue = new DialogueData
                {
                    DialogueId = currentDialogue.DialogueId,
                    Sentences = currentDialogue.Sentences,
                    Choices = filteredChoices
                };

                choiceManager.ShowDialogueChoices(filteredDialogue, HandleChoiceSelection);
                return;
            }

            if (currentDialogue.Sentences != null && typingEffect.CurrentSentenceIndex < currentDialogue.Sentences.Length)
            {
                string sentence = currentDialogue.Sentences[typingEffect.CurrentSentenceIndex];
                typingEffect.TypeSentence(sentence);
            }
            else
            {
                Debug.LogError($"[DialogueManager] 문장 인덱스 오류 - 인덱스: {typingEffect.CurrentSentenceIndex}, 배열 크기: {currentDialogue.Sentences?.Length ?? 0}");
            }
        }
    }


    private void HandleGreetingChoice(DialogueChoice choice)
    {
        if (choice.isQuest)
        {
            HandleQuestChoice(choice);
            return;
        }

        if (choice.isShop)
        {
            ShopDefinition shopDefinition = FindShopById(choice.shopId);
            if (shopDefinition != null)
            {
                EndDialogue();
                ShopManager.Instance.OpenShop(shopDefinition, npcMove);
                npcMove.IsInteract = true;
            }
            else
            {
                EndDialogue();
            }
            return;
        }

        if (choice.ChoiceText == "대화하기")
        {
            choiceManager.ClearChoices();
            StartDialogueData();
        }
        else
        {
            EndDialogue();
        }
    }

    private void HandleChoiceSelection(DialogueChoice choice)
    {
        if (choice.isQuest)
        {
            HandleQuestChoice(choice);
            return;
        }

        if (choice.isShop)
        {
            ShopDefinition shopDefinition = FindShopById(choice.shopId);
            if (shopDefinition != null)
            {
                EndDialogue();
                npcMove.IsInteract = true;
                ShopManager.Instance.OpenShop(shopDefinition, npcMove);
            }
            else
            {
                EndDialogue();
            }
            return;
        }

        if (!string.IsNullOrEmpty(choice.NextDialogueId))
        {
            StartNewDialogue(choice.NextDialogueId);
            choiceManager.ClearChoices();
        }
        else
        {
            EndDialogue();
        }
    }

    private ShopDefinition FindShopById(string shopId)
    {
        if (currentNPC == null || currentNPC.Interactions == null) return null;

        foreach (var interaction in currentNPC.Interactions)
        {
            if (interaction.Type == InteractionType.Shop)
            {
                ShopDefinition shop = interaction.GetInteractionObject() as ShopDefinition;
                if (shop != null && shop.ShopID == shopId)
                {
                    return shop;
                }
            }
        }

        foreach (var interaction in currentNPC.Interactions)
        {
            if (interaction.Type == InteractionType.Shop)
            {
                return interaction.GetInteractionObject() as ShopDefinition;
            }
        }

        return null;
    }

    public void EndDialogue()
    {
        typingEffect.StopTyping();
        promptManager.StopBlinkEffect();
        dialoguePanel.SetActive(false);
        dialogueBtnPanel.SetActive(false);

        choiceManager.ClearChoices();
        npcMove.IsInteract = false;
        currentDialogue = null;
        isInGreetingMode = false;
        typingEffect.ResetIndex();
    }

    private void StartNewDialogue(string dialogueId)
    {
        DialogueData newDialogue = FindDialogueById(dialogueId);

        if (newDialogue != null)
        {
            currentDialogue = newDialogue;
            isInGreetingMode = false; // 중요: 인사말 모드 해제
            typingEffect.ResetIndex();
            ShowNextSentence();
        }
        else
        {
            Debug.LogError($"[DialogueManager] StartNewDialogue: ID '{dialogueId}'에 해당하는 대화가 없음!");
            EndDialogue();
        }
    }

    private DialogueData FindDialogueById(string dialogueId)
    {
        if (currentNPCDialogue == null) return null;

        foreach (var dialogue in currentNPCDialogue.Dialogues)
        {
            if (dialogue.DialogueId == dialogueId)
            {
                return dialogue;
            }
        }

        return null;
    }

    public void CheckNPCQuests()
    {
        if (currentNPC == null || questManager == null) return;

        string npcId = currentNPC.NpcId;

        foreach (var state in npcQuestsByState.Keys)
        {
            npcQuestsByState[state].Clear();
        }

        NPC_QuestProvider questProvider = null;

        if (currentNPC.Interactions != null)
        {
            foreach (var interaction in currentNPC.Interactions)
            {
                if (interaction.Type == InteractionType.Quest)
                {
                    questProvider = interaction.GetInteractionObject() as NPC_QuestProvider;
                    break;
                }
            }
        }

        if (questProvider != null)
        {
            var allQuests = questProvider.GetAllQuestsByState(npcId);

            foreach (var state in allQuests.Keys)
            {
                npcQuestsByState[state].AddRange(allQuests[state]);
            }
        }
        else
        {
            npcQuestsByState[QuestState.NotAccepted].AddRange(
                questManager.GetAvailableQuestsForNpc(npcId));

            npcQuestsByState[QuestState.Completed].AddRange(
                questManager.GetCompletableQuestsForNpc(npcId));

            npcQuestsByState[QuestState.InProgress].AddRange(
                questManager.GetInProgressQuestsForNPC(npcId));
        }

        UpdateQuestIndicators();
    }

    private void UpdateQuestIndicators()
    {
        bool hasAvailableQuest = npcQuestsByState[QuestState.NotAccepted].Count > 0;
        bool hasInProgressQuest = npcQuestsByState[QuestState.InProgress].Count > 0;
        bool hasCompletedQuest = npcQuestsByState[QuestState.Completed].Count > 0;

        if (questAvailableIndicator != null)
            questAvailableIndicator.SetActive(hasAvailableQuest);

        if (questInProgressIndicator != null)
            questInProgressIndicator.SetActive(hasInProgressQuest);

        if (questCompletedIndicator != null)
            questCompletedIndicator.SetActive(hasCompletedQuest);
    }

    private void RefreshDialogueOptions(Quest quest)
    {
        if (currentNPC == null || !IsDialogueActive()) return;

        if (quest.StartNpcId == currentNPC.NpcId || quest.CompleteNpcId == currentNPC.NpcId)
        {
            CheckNPCQuests();

            if (IsInGreetingMode)
            {
                choiceManager.ShowGreetingOptions(HandleGreetingChoice);
            }
            else if (currentDialogue != null && currentDialogue.Choices != null && currentDialogue.Choices.Length > 0)
            {
                DialogueChoice[] filteredChoices = FilterChoices(currentDialogue.Choices);

                DialogueData filteredDialogue = new DialogueData
                {
                    DialogueId = currentDialogue.DialogueId,
                    Sentences = currentDialogue.Sentences,
                    Choices = filteredChoices
                };

                choiceManager.ShowDialogueChoices(filteredDialogue, HandleChoiceSelection);
            }
        }
    }

    private bool IsDialogueActive()
    {
        return DialoguePanel != null && DialoguePanel.activeSelf;
    }

    public void HandleQuestChoice(DialogueChoice choice)
    {
        if (!choice.isQuest || string.IsNullOrEmpty(choice.questId)) return;

        Quest selectedQuest = questManager.GetQuestById(choice.questId);
        if (selectedQuest == null) return;
        NPC_QuestProvider questProvider = null;
        foreach (var interaction in currentNPC.Interactions)
        {
            if (interaction.Type == InteractionType.Quest)
            {
                questProvider = interaction.GetInteractionObject() as NPC_QuestProvider;
                break;
            }
        }

        switch (selectedQuest.State)
        {
            case QuestState.NotAccepted:
                EndDialogue();
                QuestUIManager.Instance.ShowQuestAcceptPanel(selectedQuest);
                return;

            case QuestState.InProgress:
                string nextDialogueId = null;

                if (selectedQuest.IsCompleted() && currentNPC.NpcId == selectedQuest.CompleteNpcId)
                {
                    selectedQuest.CompleteQuest();
                    selectedQuest.GiveReward();

                    if (questProvider != null)
                    {
                        nextDialogueId = questProvider.GetDialogueIdForQuestState(selectedQuest.QuestId, QuestState.Finished);
                    }
                }
                else
                {
                    if (questProvider != null)
                    {
                        nextDialogueId = questProvider.GetDialogueIdForQuestState(selectedQuest.QuestId, QuestState.InProgress);
                    }
                }

                if (!string.IsNullOrEmpty(nextDialogueId))
                {
                    choiceManager.ClearChoices();
                    StartNewDialogue(nextDialogueId);
                }
                else
                {
                    StartCoroutine(RestartDialogueAfterDelay(0.1f));
                }
                break;

            case QuestState.Completed:
                selectedQuest.GiveReward();

                nextDialogueId = null;
                if (questProvider != null)
                {
                    nextDialogueId = questProvider.GetDialogueIdForQuestState(selectedQuest.QuestId, QuestState.Finished);
                }

                if (!string.IsNullOrEmpty(nextDialogueId))
                {
                    choiceManager.ClearChoices();
                    StartNewDialogue(nextDialogueId);
                }
                else
                {
                    StartCoroutine(RestartDialogueAfterDelay(0.1f));
                }
                break;
        }

        CheckNPCQuests();
    }

    private IEnumerator RestartDialogueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        choiceManager.ClearChoices();
        isInGreetingMode = true;

        if (currentNPC.DefaultDialogue != null && currentNPC.DefaultDialogue.Length > 0)
        {
            greetingSentences = currentNPC.DefaultDialogue;
            typingEffect.ResetIndex();
            ShowNextSentence();
        }
        else
        {
            choiceManager.ShowGreetingOptions(HandleGreetingChoice);
        }
    }

    public void AddQuestChoicesToGreeting(List<DialogueChoice> choices)
    {
        if (currentNPC == null || choices == null) return;

        foreach (var quest in npcQuestsByState[QuestState.Completed])
        {
            choices.Add(new DialogueChoice
            {
                ChoiceText = GetFormattedQuestText(quest, QuestState.Completed),
                NextDialogueId = null,
                isQuest = true,
                questId = quest.QuestId
            });
        }

        foreach (var quest in npcQuestsByState[QuestState.NotAccepted])
        {
            choices.Add(new DialogueChoice
            {
                ChoiceText = GetFormattedQuestText(quest, QuestState.NotAccepted),
                NextDialogueId = null,
                isQuest = true,
                questId = quest.QuestId
            });
        }

        foreach (var quest in npcQuestsByState[QuestState.InProgress])
        {
            bool alreadyInCompletableList = false;
            foreach (var completableQuest in npcQuestsByState[QuestState.Completed])
            {
                if (completableQuest.QuestId == quest.QuestId)
                {
                    alreadyInCompletableList = true;
                    break;
                }
            }

            if (!alreadyInCompletableList)
            {
                choices.Add(new DialogueChoice
                {
                    ChoiceText = GetFormattedQuestText(quest, QuestState.InProgress),
                    NextDialogueId = null,
                    isQuest = true,
                    questId = quest.QuestId
                });
            }
        }
    }

    private string GetFormattedQuestText(Quest quest, QuestState state)
    {
        string statePrefix = "";

        switch (state)
        {
            case QuestState.NotAccepted:
                statePrefix = "[수락 가능]";
                break;
            case QuestState.InProgress:
                if (quest.IsCompleted() && quest.CompleteNpcId == currentNPC.NpcId)
                {
                    statePrefix = "[완료 가능]";
                }
                else
                {
                    statePrefix = $"[진행 중 {quest.currentProgress}/{quest.requiredProgress}]";
                }
                break;
            case QuestState.Completed:
                statePrefix = "[보상 수령]";
                break;
            case QuestState.Finished:
                statePrefix = "[완료됨]";
                break;
        }

        return $"{statePrefix} {quest.QuestName}";
    }
}