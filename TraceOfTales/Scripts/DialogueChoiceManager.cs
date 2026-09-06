using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueChoiceManager : MonoBehaviour
{
    [Header("Choice UI")]
    [SerializeField] private GameObject choicesContainer; // 스크롤 뷰 오브젝트
    [SerializeField] private GameObject choiceButtonPrefab; // 기본 선택지 버튼 프리팹 (챗팅용)
    [SerializeField] private GameObject choiceQuestButtonPrefab; // 기본 퀘스트 선택지 버튼 프리팹 (챗팅용)
    [SerializeField] private RectTransform contentPanel; // Scroll View의 Content 패널

    [Header("Quest Button Prefabs")]
    [SerializeField] private GameObject acceptableQuestButtonPrefab; // 수락 가능한 퀘스트 버튼 프리팹
    [SerializeField] private GameObject inProgressQuestButtonPrefab; // 진행 중인 퀘스트 버튼 프리팹
    [SerializeField] private GameObject completableQuestButtonPrefab; // 완료 가능한 퀘스트 버튼 프리팹

    private List<GameObject> choiceButtons = new List<GameObject>(); // 생성된 버튼 저장
    private DialogueManager dialogueManager;

    public void Initialize(DialogueManager manager)
    {
        dialogueManager = manager;

        // contentPanel이 인스펙터에서 할당되지 않았다면 찾기
        if (contentPanel == null)
        {
            contentPanel = choicesContainer.transform.Find("Viewport/Content") as RectTransform;
            if (contentPanel == null)
            {
                Debug.LogError("Scroll View의 Content를 찾을 수 없습니다!");
            }
        }
    }

    // 선택지 버튼 생성
    public void ShowDialogueChoices(DialogueData dialogue, System.Action<DialogueChoice> choiceCallback)
    {
        ClearChoices(); // 기존 선택지 삭제

        choicesContainer.SetActive(true);
        List<DialogueChoice> choiceList = new List<DialogueChoice>();

        // 기본 선택지를 리스트에 추가
        if (dialogue.Choices != null && dialogue.Choices.Length > 0)
        {
            choiceList.AddRange(dialogue.Choices);
        }

        // 항상 존재하는 기본 선택지 추가 (대화 종료하기)
        if (dialogue.DialogueId != "greeting_choice") // 인사 선택지가 아닌 경우에만 추가
        {
            DialogueChoice exitChoice = new DialogueChoice
            {
                ChoiceText = "대화 종료하기",
                NextDialogueId = null,
                isQuest = false,
                isShop = false
            };
            choiceList.Add(exitChoice);
        }

        foreach (DialogueChoice choice in choiceList) // 선택지 버튼 생성
        {
            GameObject choiceObj = CreateChoiceButton(choice);

            TextMeshProUGUI choiceText = choiceObj.GetComponentInChildren<TextMeshProUGUI>();

            // 퀘스트 선택지인 경우 이름 길이 제한 및 카테고리 색상 적용
            if (choice.isQuest)
            {
                string displayText = choice.ChoiceText;

                // 퀘스트 이름 추출 (패턴: "퀘스트 수락: [이름]", "퀘스트 완료: [이름]" 등)
                int colonIndex = displayText.IndexOf(':');
                if (colonIndex >= 0 && colonIndex + 2 < displayText.Length)
                {
                    string prefix = displayText.Substring(0, colonIndex + 1); // "퀘스트 수락:" 부분
                    string questName = displayText.Substring(colonIndex + 1).Trim(); // 퀘스트 이름 부분

                    // 퀘스트 이름 길이 제한 적용 (단순 방식)
                    if (questName.Length > 8)
                    {
                        questName = questName.Substring(0, 8) + "...";
                    }

                    displayText = prefix + " " + questName;
                }

                choiceText.text = displayText;

                // 퀘스트 카테고리 색상 적용
                Quest quest = QuestManager.Instance.GetQuestById(choice.questId);
                if (quest != null)
                {
                    choiceText.color = quest.GetCategoryColor();
                }
            }
            else
            {
                choiceText.text = choice.ChoiceText;
            }

            Button choiceButton = choiceObj.GetComponent<Button>();
            choiceButton.onClick.AddListener(() => choiceCallback(choice));

            choiceObj.SetActive(true);
            choiceButtons.Add(choiceObj);
        }

        // Scroll View의 Content 크기를 조정 (선택적으로 필요한 경우)
        Canvas.ForceUpdateCanvases();

        // 스크롤 위치를 상단으로 리셋
        if (choicesContainer.GetComponent<ScrollRect>() != null)
        {
            choicesContainer.GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1);
        }
    }

    // 인사말 후 대화 옵션 표시
    public void ShowGreetingOptions(System.Action<DialogueChoice> choiceCallback)
    {
        // 기존 선택지 생성
        List<DialogueChoice> choices = new List<DialogueChoice>();
        string npcId = dialogueManager.CurrentNPC?.NpcId ?? "";

        // 퀘스트 선택지 데이터 가져오기
        if (dialogueManager.CurrentNPC != null && QuestManager.Instance != null)
        {
            // 1. 완료 가능한 퀘스트를 최우선 표시
            List<Quest> completableQuests = QuestManager.Instance.GetCompletableQuestsForNpc(npcId);

            foreach (var quest in completableQuests)
            {
                // 퀘스트 이름 길이 제한 적용 (단순 방식)
                string questName = quest.QuestName;
                if (questName.Length > 8)
                {
                    questName = questName.Substring(0, 8) + "...";
                }

                // 완료 가능한 퀘스트를 직접 선택지로 추가
                choices.Add(new DialogueChoice
                {
                    ChoiceText = $"퀘스트 완료: {questName}",
                    NextDialogueId = null,
                    isQuest = true,
                    questId = quest.QuestId
                });
            }

            // 2. 수락 가능한 퀘스트 직접 표시
            List<Quest> availableQuests = QuestManager.Instance.GetAvailableQuestsForNpc(npcId);

            foreach (var quest in availableQuests)
            {
                // 퀘스트 이름 길이 제한 적용 (단순 방식)
                string questName = quest.QuestName;
                if (questName.Length > 8)
                {
                    questName = questName.Substring(0, 8) + "...";
                }

                // 수락 가능한 퀘스트를 직접 선택지로 추가
                choices.Add(new DialogueChoice
                {
                    ChoiceText = $"퀘스트 수락: {questName}",
                    NextDialogueId = null,
                    isQuest = true,
                    questId = quest.QuestId
                });
            }

            // 3. 진행 중인 퀘스트는 (완료 가능한 퀘스트와 중복되지 않게) 표시
            List<Quest> inProgressQuests = QuestManager.Instance.GetInProgressQuestsForNPC(npcId);

            foreach (var quest in inProgressQuests)
            {
                // 이미 완료 가능 목록에 있는지 확인 (중복 방지)
                bool alreadyInCompletableList = false;
                foreach (var completableQuest in completableQuests)
                {
                    if (completableQuest.QuestId == quest.QuestId)
                    {
                        alreadyInCompletableList = true;
                        break;
                    }
                }

                // 중복이 아닌 경우만 진행 중 퀘스트로 표시
                if (!alreadyInCompletableList)
                {
                    // 퀘스트 이름 길이 제한 적용 (단순 방식)
                    string questName = quest.QuestName;
                    if (questName.Length > 8)
                    {
                        questName = questName.Substring(0, 8) + "...";
                    }

                    choices.Add(new DialogueChoice
                    {
                        ChoiceText = $"진행 중: {questName} ({quest.currentProgress}/{quest.requiredProgress})",
                        NextDialogueId = null,
                        isQuest = true,
                        questId = quest.QuestId
                    });
                }
            }
        }

        // 4. 일반 대화 선택지 추가
        choices.Add(new DialogueChoice
        {
            ChoiceText = "대화하기",
            NextDialogueId = "", // 빈 문자열로 설정하여 HandleGreetingChoice에서 특별 처리
            isQuest = false,
            isShop = false
        });

        // 5. 상점 상호작용이 있으면 추가
        if (dialogueManager.CurrentNPC != null && dialogueManager.CurrentNPC.Interactions != null)
        {
            foreach (var interaction in dialogueManager.CurrentNPC.Interactions)
            {
                if (interaction.Type == InteractionType.Shop)
                {
                    ShopDefinition shop = interaction.GetInteractionObject() as ShopDefinition;
                    if (shop != null)
                    {
                        choices.Add(new DialogueChoice
                        {
                            ChoiceText = "상점 이용하기",
                            NextDialogueId = null,
                            isQuest = false,
                            isShop = true,
                            shopId = shop.ShopID
                        });
                    }
                    break;
                }
            }
        }

        // 6. 마지막으로 '대화 종료하기' 선택지 추가
        choices.Add(new DialogueChoice
        {
            ChoiceText = "대화 종료하기",
            NextDialogueId = null,
            isQuest = false,
            isShop = false
        });

        // 대화 데이터 생성 및 선택지 표시
        DialogueData greetingChoices = new DialogueData
        {
            DialogueId = "greeting_choice",
            Sentences = new string[0],
            Choices = choices.ToArray()
        };

        ShowDialogueChoices(greetingChoices, choiceCallback);
    }

    // 선택지에 맞는 버튼 프리팹 생성
    private GameObject CreateChoiceButton(DialogueChoice choice)
    {
        GameObject prefabToUse = null;

        // 퀘스트 상태에 따라 적절한 버튼 프리팹 선택
        if (choice.isQuest)
        {
            // 퀘스트 ID로 해당 퀘스트 가져오기
            Quest quest = QuestManager.Instance.GetQuestById(choice.questId);

            if (quest != null)
            {
                // 퀘스트 상태에 따라 다른 프리팹 사용
                if (quest.State == QuestState.Completed ||
                    (quest.State == QuestState.InProgress && quest.IsCompleted()))
                {
                    // 완료 가능한 퀘스트 버튼
                    prefabToUse = completableQuestButtonPrefab;
                }
                else if (quest.State == QuestState.InProgress)
                {
                    // 진행 중인 퀘스트 버튼
                    prefabToUse = inProgressQuestButtonPrefab;
                }
                else if (quest.State == QuestState.NotAccepted)
                {
                    // 수락 가능한 퀘스트 버튼
                    prefabToUse = acceptableQuestButtonPrefab;
                }
            }
        }

        // NPC의 Interactions 배열에서 적절한 버튼 프리팹 찾기 (위에서 설정되지 않은 경우)
        if (prefabToUse == null && dialogueManager.CurrentNPC != null && dialogueManager.CurrentNPC.Interactions != null)
        {
            foreach (var interaction in dialogueManager.CurrentNPC.Interactions)
            {
                // 상점 선택지면 상점 상호작용의 버튼 프리팹 사용
                if (choice.isShop && interaction.Type == InteractionType.Shop &&
                    interaction.ButtonPrefab != null)
                {
                    prefabToUse = interaction.ButtonPrefab;
                    break;
                }
                // 퀘스트 선택지면 퀘스트 상호작용의 버튼 프리팹 사용 (위에서 설정되지 않은 경우)
                else if (choice.isQuest && interaction.Type == InteractionType.Quest &&
                         interaction.ButtonPrefab != null && prefabToUse == null)
                {
                    prefabToUse = interaction.ButtonPrefab;
                    break;
                }
                // 일반 대화 선택지면 대화 상호작용의 버튼 프리팹 사용
                else if (!choice.isQuest && !choice.isShop &&
                         interaction.Type == InteractionType.Dialogue &&
                         interaction.ButtonPrefab != null)
                {
                    prefabToUse = interaction.ButtonPrefab;
                    break;
                }
            }
        }

        // 적절한 버튼 프리팹을 찾지 못했으면 기본 프리팹 사용
        if (prefabToUse == null)
        {
            if (choice.isQuest)
            {
                prefabToUse = choiceQuestButtonPrefab;
            }
            else
            {
                prefabToUse = choiceButtonPrefab;
            }
        }

        // 버튼 인스턴스 생성
        return Instantiate(prefabToUse, contentPanel);
    }

    public void ClearChoices()
    {
        foreach (GameObject button in choiceButtons)
        {
            Destroy(button);
        }
        choiceButtons.Clear();
        choicesContainer.SetActive(false);
    }
}