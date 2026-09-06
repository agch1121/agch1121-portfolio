using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    private NPC npcBase;
    private NPCMovement npcMove;
    private DialogueManager dialogueManager;
    private GameObject interactionBubble;
    private bool canInteract;
    private bool isPlayerNearby;
    private QuestManager questManager;

    // 초기화 상태 추적
    private bool isInitialized = false;

    public void Initialize(NPC npcBase, GameObject interactionBubble, DialogueManager dialogueManager)
    {
        this.npcBase = npcBase;
        this.interactionBubble = interactionBubble;
        this.dialogueManager = dialogueManager;

        if (npcBase != null)
        {
            canInteract = npcBase.CanInteract;
        }

        npcMove = GetComponent<NPCMovement>();

        if (interactionBubble != null)
        {
            interactionBubble.SetActive(canInteract);
        }

        // QuestManager는 나중에 초기화될 수 있으므로 안전하게 처리
        questManager = QuestManager.Instance;

        isInitialized = true;
    }

    private void Start()
    {
        // NPCController에서 Awake/Start에서 Initialize가 호출되므로 
        // 여기서는 간단한 확인만 수행
        if (!isInitialized)
        {
            // 다음 프레임에 다시 확인
            Invoke(nameof(CheckInitialization), 0.1f);
        }

        // QuestManager가 아직 초기화되지 않았다면 다시 시도
        if (questManager == null)
        {
            questManager = QuestManager.Instance;
        }
    }

    private void CheckInitialization()
    {
        if (!isInitialized)
        {
            //Debug.LogError($"{gameObject.name}: NPCInteraction이 초기화되지 않았습니다! NPCController를 확인해주세요.");
        }
    }

    private void Update()
    {
        // 초기화되지 않은 상태에서는 Update 로직 실행하지 않음
        if (!isInitialized || npcBase == null)
        {
            return;
        }

        // Manager들의 안전한 접근
        bool shopOpen = SafeCheckShopOpen();
        bool questPanelOpen = SafeCheckQuestPanelOpen();

        // 상점이나 퀘스트 UI가 열려있는 경우 대화 불가능
        if (shopOpen || questPanelOpen)
        {
            return;
        }

        if (isPlayerNearby && canInteract && Input.GetKeyDown(KeyCode.F))
        {
            if (SafeCheckDialoguePanelActive())
            {
                // 대화 패널이 활성화되어 있을 때 F키를 누르면
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.ShowNextSentence();
                }
            }
            else
            {
                // 대화가 시작되지 않은 상태에서 F키를 누르면 대화 시작
                if (DialogueManager.Instance != null && npcMove != null)
                {
                    DialogueManager.Instance.Initialize(npcBase, npcMove);
                    npcMove.IsInteract = true;
                    DialogueManager.Instance.StartDialogue();
                }
            }
        }

        if (SafeCheckDialoguePanelActive() && Input.GetKeyDown(KeyCode.Escape))
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.EndDialogue();
                if (npcMove != null)
                {
                    npcMove.IsInteract = false;
                }
            }
        }
    }

    // 대화 버튼이 눌리면 실행될 함수
    public void TryStartDialogue()
    {
        // 초기화 체크
        if (!isInitialized || npcBase == null)
        {
            Debug.LogWarning("NPCInteraction이 초기화되지 않았거나 npcBase가 null입니다!");
            return;
        }

        // 상점이나 퀘스트 UI가 열려있는 경우 대화 불가능
        if (SafeCheckShopOpen() || SafeCheckQuestPanelOpen())
        {
            return;
        }

        if (isPlayerNearby) // 플레이어가 근처에 있을 때만 실행
        {
            // 인사말이 있거나 대화 상호작용이 있는지 확인
            bool canStartDialogue = (npcBase.DefaultDialogue != null && npcBase.DefaultDialogue.Length > 0);

            // 인사말이 없다면 대화 상호작용이 있는지 확인
            if (!canStartDialogue && npcBase.Interactions != null)
            {
                foreach (var interaction in npcBase.Interactions)
                {
                    if (interaction.Type == InteractionType.Dialogue &&
                        interaction.GetInteractionObject() is NPC_Dialogue dialogue)
                    {
                        // 대화 데이터가 있고 문장이 있는지 확인
                        if (dialogue.Dialogues != null && dialogue.Dialogues.Length > 0 &&
                            dialogue.Dialogues[0].Sentences != null && dialogue.Dialogues[0].Sentences.Length > 0)
                        {
                            canStartDialogue = true;
                            break;
                        }
                    }
                }
            }

            // 대화가 없더라도 퀘스트 제공자가 있는지 확인
            if (!canStartDialogue && npcBase.Interactions != null)
            {
                foreach (var interaction in npcBase.Interactions)
                {
                    if (interaction.Type == InteractionType.Quest &&
                        interaction.GetInteractionObject() is NPC_QuestProvider questProvider)
                    {
                        canStartDialogue = true;
                        break;
                    }
                }
            }

            if (!canStartDialogue)
            {
                Debug.LogWarning($"NPC {npcBase.NpcName}에 표시할 대화 내용이 없습니다!");
                return;
            }

            // 대화 및 퀘스트 상호작용 시작
            if (DialogueManager.Instance != null && npcMove != null)
            {
                DialogueManager.Instance.Initialize(npcBase, npcMove);
                npcMove.IsInteract = true;
                DialogueManager.Instance.StartDialogue();

                // NPC 퀘스트 정보 확인
                SafeCheckNPCQuests();
            }
        }
        else
        {
            Debug.LogWarning("플레이어가 NPC 근처에 없음!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 가장 기본적인 null 체크
        if (other == null)
        {
            Debug.LogWarning("OnTriggerEnter2D: other Collider가 null입니다!");
            return;
        }

        // 초기화되지 않은 상태에서는 트리거 처리하지 않음
        if (!isInitialized)
        {
            return;
        }

        // npcBase null 체크
        if (npcBase == null)
        {
            Debug.LogError($"{gameObject.name}: npcBase가 null입니다!");
            return;
        }

        if (other.CompareTag("Player") && npcBase.CanInteract)
        {
            isPlayerNearby = true;
            canInteract = true;

            // 다이얼로그 매니저에 이 NPC 등록
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.RegisterNearbyNPC(this);
            }
            else
            {
                Debug.LogWarning("DialogueManager.Instance가 아직 초기화되지 않았습니다!");
            }

            // 상점이나 퀘스트 창이 열려있지 않을 때만 대화 버튼 UI 표시
            if (!SafeCheckShopOpen() && !SafeCheckQuestPanelOpen())
            {
                if (DialogueManager.Instance != null && DialogueManager.Instance.DialogueBtnPanel != null)
                {
                    DialogueManager.Instance.DialogueBtnPanel.SetActive(true);
                }
            }

            // 퀘스트 관련 처리
            SafeCheckNPCQuests();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null || !isInitialized) return;

        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;

            // 다이얼로그 매니저에서 이 NPC 제거
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.UnregisterNearbyNPC(this);

                if (DialogueManager.Instance.DialogueBtnPanel != null)
                {
                    DialogueManager.Instance.DialogueBtnPanel.SetActive(false);
                }
            }
        }
    }

    // 안전한 Manager 접근 메서드들
    private bool SafeCheckShopOpen()
    {
        return ShopManager.Instance != null && ShopManager.Instance.IsShopOpen;
    }

    private bool SafeCheckQuestPanelOpen()
    {
        return QuestManager.Instance != null && QuestManager.Instance.IsQuestPanelActive;
    }

    private bool SafeCheckDialoguePanelActive()
    {
        return DialogueManager.Instance != null &&
               DialogueManager.Instance.DialoguePanel != null &&
               DialogueManager.Instance.DialoguePanel.activeSelf;
    }

    private void SafeCheckNPCQuests()
    {
        // 여러 단계에 걸쳐 안전하게 퀘스트 체크 시도
        try
        {
            if (dialogueManager != null)
            {
                dialogueManager.CheckNPCQuests();
            }
            else if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.CheckNPCQuests();
            }
            else
            {
                Debug.LogWarning("DialogueManager를 찾을 수 없어 퀘스트 체크를 건너뜁니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SafeCheckNPCQuests에서 오류 발생: {e.Message}\n{e.StackTrace}");
        }
    }
}