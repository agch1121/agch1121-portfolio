using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class QuestAcceptPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI questTitleText;       // 퀘스트 제목
    [SerializeField] private TextMeshProUGUI npcNameText;          // NPC 이름
    [SerializeField] private Image questImage;                     // 퀘스트/NPC 이미지

    [Header("Description Scroll View")]
    [SerializeField] private ScrollRect descriptionScrollView;     // 설명 스크롤뷰
    [SerializeField] private TextMeshProUGUI descriptionText;      // 스크롤뷰 내부 설명 텍스트

    [SerializeField] private Button acceptButton;                  // 수락 버튼
    [SerializeField] private Button declineButton;                 // 거절 버튼
    [SerializeField] private Button closeButton;                   // X 버튼

    // 이벤트 선언
    public event Action<Quest> OnQuestAccepted;
    public event Action OnQuestDeclined;

    private Quest currentQuest;

    private void Awake()
    {
        // 버튼 이벤트 연결
        acceptButton.onClick.AddListener(AcceptQuest);
        declineButton.onClick.AddListener(DeclineQuest);

        // X 버튼 이벤트 연결
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(DeclineQuest); // 거절과 동일한 동작
        }

        // 스크롤뷰 초기 설정
        if (descriptionScrollView != null)
        {
            // 스크롤 위치 초기화
            descriptionScrollView.normalizedPosition = new Vector2(0, 1);
        }
    }

    // 패널 초기화 및 표시
    public void ShowQuestDetails(Quest quest)
    {
        currentQuest = quest;

        // UI 업데이트
        questTitleText.text = quest.QuestName;

        // 설명 텍스트 설정
        if (descriptionText != null)
        {
            descriptionText.text = quest.QuestDescription;

            // 스크롤 위치 초기화
            if (descriptionScrollView != null)
            {
                // 다음 프레임에 스크롤 위치 초기화 (레이아웃 업데이트 이후)
                Canvas.ForceUpdateCanvases();
                descriptionScrollView.normalizedPosition = new Vector2(0, 1);
            }
        }

        // NPC 정보 찾기 및 표시
        NPCController npc = FindNpcControllerById(quest.StartNpcId);
        if (npc != null)
        {
            // NPC 이름 표시
            npcNameText.text = npc.GetNPCBase().NpcName;

            // NPC 이미지가 있으면 표시
            if (npc.GetNPCBase().NpcImage != null)
            {
                questImage.sprite = npc.GetNPCBase().NpcImage;
                questImage.gameObject.SetActive(true);
            }
            else
            {
                questImage.gameObject.SetActive(false);
            }
        }
        else
        {
            // NPC를 찾지 못한 경우
            npcNameText.text = "알 수 없음";
            questImage.gameObject.SetActive(false);
        }

        // 패널 활성화
        gameObject.SetActive(true);
    }

    // 퀘스트 수락 로직
    private void AcceptQuest()
    {
        if (currentQuest != null)
        {
            OnQuestAccepted?.Invoke(currentQuest);
        }

        // 패널 닫기
        gameObject.SetActive(false);
    }

    // 퀘스트 거절 로직
    private void DeclineQuest()
    {
        OnQuestDeclined?.Invoke();

        // 패널 닫기
        gameObject.SetActive(false);
    }

    // NPC ID로 컨트롤러 찾기
    private NPCController FindNpcControllerById(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;

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
}