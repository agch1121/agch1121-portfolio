using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class QuestNoticeItem : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI questObjectiveText;
    [SerializeField] private Button removeButton;

    [Header("Status Colors")]
    [SerializeField] private Color inProgressColor = Color.yellow;
    [SerializeField] private Color completedColor = Color.green;

    private Quest quest;

    // 이벤트 선언
    public event Action<Quest> OnRemoveClicked;

    public void Initialize(Quest quest)
    {
        this.quest = quest;
        UpdateUI();

        // 제거 버튼 이벤트 연결
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(() => OnRemoveClicked?.Invoke(quest));
        }
    }

    public void UpdateUI()
    {
        if (quest == null) return;

        // 퀘스트 이름 설정 (10자 제한)
        if (questNameText != null)
        {
            string questName = quest.QuestName;
            if (questName.Length > 8)
            {
                questName = questName.Substring(0, 8) + "...";
            }
            // *** 수정된 부분: 퀘스트 이름에 대괄호 추가 ***
            questNameText.text = $"[ {questName} ]";

            // 퀘스트 이름 폰트 크기 조절
            questNameText.fontSize = 22;

            // 퀘스트 카테고리 색상 적용
            questNameText.color = quest.GetCategoryColor();
        }

        // 퀘스트 목표 설정
        if (questObjectiveText != null)
        {
            questObjectiveText.text = quest.GetProgressText();

            // 퀘스트 목표 폰트 크기 조절
            questObjectiveText.fontSize = 20;

            // 기본 색상은 흰색
            questObjectiveText.color = Color.white;

            // 진행도를 다 채웠을 때만 노란색으로 변경
            if (quest.State == QuestState.InProgress && quest.IsCompleted())
            {
                questObjectiveText.color = Color.yellow;
            }
        }
    }
}