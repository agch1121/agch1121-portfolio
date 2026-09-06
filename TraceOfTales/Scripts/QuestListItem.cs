using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class QuestListItem : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private Toggle noticeToggle;
    [SerializeField] private Button questButton; // 퀘스트 선택용 버튼 참조 추가

    private Quest quest;

    // 이벤트 선언
    public event Action<Quest> OnQuestSelected;
    public event Action<Quest, bool> OnNoticeToggled;

    // 프로퍼티
    public Quest Quest => quest;

    private void Awake()
    {
        // 버튼 컴포넌트 확인 및 추가
        if (questButton == null)
        {
            questButton = GetComponent<Button>();

            // 버튼이 없다면 추가
            if (questButton == null)
            {
                questButton = gameObject.AddComponent<Button>();
            }
        }
    }

    public void Initialize(Quest quest)
    {
        this.quest = quest;
        UpdateUI();

        // 버튼 이벤트 리스너 설정 - 모든 리스너 제거 후 새로 추가
        if (questButton != null)
        {
            questButton.onClick.RemoveAllListeners();
            questButton.onClick.AddListener(() => {
                Debug.Log($"퀘스트 선택: {quest.QuestName}");
                OnQuestSelected?.Invoke(quest);
            });

            // 퀘스트 선택을 위한 이벤트 트리거 설정 - raycastTarget 확인
            Image buttonImage = questButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = true; // 레이캐스트 대상 설정
            }
        }

        // 토글 버튼 활성화 상태 설정 - 진행 중인 퀘스트만 활성화
        if (noticeToggle != null)
        {
            // 진행 중인 퀘스트만 토글 활성화
            noticeToggle.interactable = (quest.State == QuestState.InProgress);

            // 토글 이벤트 리스너 모두 제거 후 다시 추가
            noticeToggle.onValueChanged.RemoveAllListeners();
            noticeToggle.onValueChanged.AddListener((isOn) => OnNoticeToggled?.Invoke(quest, isOn));

            // 토글의 레이캐스트 대상 설정 확인
            Image toggleImage = noticeToggle.GetComponent<Image>();
            if (toggleImage != null)
            {
                toggleImage.raycastTarget = true;
            }
        }
    }

    public void UpdateUI()
    {
        if (quest == null) return;

        // 퀘스트 이름 설정 (너무 길면 잘라내기)
        if (questNameText != null)
        {
            string questName = quest.QuestName;
            if (questName.Length > 8) // 길이 제한 증가
            {
                questName = questName.Substring(0, 8) + "...";
            }
            questNameText.text = questName;

            // 퀘스트 카테고리에 따른 색상 적용
            Color questColor = quest.GetCategoryColor();

            // 퀘스트 상태에 따른 추가 색상 조정
            switch (quest.State)
            {
                case QuestState.NotAccepted:
                    // 카테고리 색상 유지
                    break;
                case QuestState.InProgress:
                    // 완료 가능한 퀘스트는 다른 색으로 표시
                    if (quest.IsCompleted())
                        questColor = Color.yellow;
                    // 그렇지 않으면 카테고리 색상 유지
                    break;
                case QuestState.Completed:
                    questColor = new Color(0.2f, 0.8f, 0.2f); // 연한 녹색
                    break;
                case QuestState.Finished:
                    questColor = new Color(0.4f, 0.4f, 0.4f); // 어두운 회색
                    break;
            }

            questNameText.color = questColor;
        }
    }

    // 알림 상태 설정
    public void SetNoticeState(bool isNoticed)
    {
        if (noticeToggle != null)
        {
            // 토글 이벤트 발생 방지
            noticeToggle.onValueChanged.RemoveAllListeners();

            // 토글 상태 설정
            noticeToggle.isOn = isNoticed;

            // 이벤트 다시 연결
            noticeToggle.onValueChanged.AddListener((isOn) => OnNoticeToggled?.Invoke(quest, isOn));
        }
    }

    // 추가: 직접 퀘스트 선택 메서드 - 외부에서 호출하기 위한 메서드
    public void SelectQuest()
    {
        if (quest != null)
        {
            Debug.Log($"퀘스트 직접 선택: {quest.QuestName}");
            OnQuestSelected?.Invoke(quest);
        }
    }

    // 레이캐스트를 위한 추가 메서드
    private void OnEnable()
    {
        // 모든 하위 UI 요소의 raycastTarget 활성화 확인
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>())
        {
            graphic.raycastTarget = true;
        }
    }
}