using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueQuest", menuName = "Quest/DialogueQuest")]
public class DialogueQuest : Quest
{
    [System.Serializable]
    public class DialogueTarget
    {
        [Header("Dialogue Target Info")]
        public string targetNpcId;
        public string targetNpcName;

        [Header("Dialogue Settings")]
        [Tooltip("퀘스트 수락 시 자동으로 완료될지 여부")]
        public bool autoCompleteOnAccept = false;

        [Tooltip("특정 대화 ID가 필요한 경우 (비어있으면 아무 대화나 OK)")]
        public string requiredDialogueId = "";

        [HideInInspector]
        public bool isCompleted = false;
    }

    [Header("Dialogue Quest Info")]
    public List<DialogueTarget> DialogueTargets = new List<DialogueTarget>();

    [Header("Quest Completion Settings")]
    [Tooltip("모든 대화를 완료해야 하는지, 아니면 하나만 완료하면 되는지")]
    public bool requireAllDialogues = true;

    private void OnEnable()
    {
        QuestType = QuestType.Dialogue;
        UpdateTotalProgress();
    }

    public override string GetProgressText()
    {
        if (DialogueTargets.Count == 1)
        {
            var target = DialogueTargets[0];
            string status = target.isCompleted ? "완료" : "진행 중";
            return $"{target.targetNpcName}와 대화하기:{status}";
        }
        else
        {
            int completedCount = 0;
            foreach (var target in DialogueTargets)
            {
                if (target.isCompleted) completedCount++;
            }

            if (requireAllDialogues)
            {
                return $"대화 완료: {completedCount} / {DialogueTargets.Count}";
            }
            else
            {
                return completedCount > 0 ? "대화 완료" : $"대화 대상 중 1명과 대화";
            }
        }
    }

    public override bool CheckCondition(string conditionType, string conditionId)
    {
        if (conditionType == "Dialogue" && State == QuestState.InProgress)
        {
            foreach (var target in DialogueTargets)
            {
                if (target.targetNpcId == conditionId)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public override void AcceptQuest()
    {
        // 퀘스트 수락 시 모든 타겟 초기화
        foreach (var target in DialogueTargets)
        {
            if (!target.autoCompleteOnAccept)
            {
                target.isCompleted = false; // autoComplete가 아닌 타겟만 초기화
            }
        }

        base.AcceptQuest();

        if (State == QuestState.InProgress)
        {
            bool hasAutoComplete = false;

            for (int i = 0; i < DialogueTargets.Count; i++)
            {
                var target = DialogueTargets[i];

                if (target.autoCompleteOnAccept)
                {
                    if (!target.isCompleted)
                    {
                        target.isCompleted = true;
                        hasAutoComplete = true;
                    }
                    else
                    {
                        hasAutoComplete = true; // 이미 완료되어 있어도 업데이트 필요
                    }
                }
            }

            if (hasAutoComplete)
            {
                UpdateTotalProgress();
            }
            else
            {
                Debug.Log($"[DialogueQuest] autoComplete된 타겟이 없음");
            }
        }
        else
        {
            Debug.Log($"[DialogueQuest] 퀘스트가 InProgress 상태가 아님: {State}");
        }
    }

    public void ProcessDialogue(string npcId, string dialogueId = "")
    {
        if (State != QuestState.InProgress) return;
        if (string.IsNullOrEmpty(dialogueId)) return; // 대화 ID가 없으면 처리 안함

        bool updated = false;

        foreach (var target in DialogueTargets)
        {
            if (target.targetNpcId == npcId && !target.isCompleted && !target.autoCompleteOnAccept)
            {
                bool shouldComplete = false;

                if (!string.IsNullOrEmpty(target.requiredDialogueId))
                {
                    // 특정 대화 ID가 필요한 경우
                    shouldComplete = (target.requiredDialogueId == dialogueId);
                }
                else
                {
                    // requiredDialogueId가 비어있으면 아무 대화나 OK
                    shouldComplete = true;
                }

                if (shouldComplete)
                {
                    target.isCompleted = true;
                    updated = true;

                    if (!requireAllDialogues)
                    {
                        break;
                    }
                }
            }
            else
            {
                if (target.targetNpcId != npcId)
                {
                    Debug.Log($"[DialogueQuest] NPC ID 불일치 - 스킵");
                }
                else if (target.isCompleted)
                {
                    Debug.Log($"[DialogueQuest] 이미 완료된 타겟 - 스킵");
                }
                else if (target.autoCompleteOnAccept)
                {
                    Debug.Log($"[DialogueQuest] autoCompleteOnAccept 타겟 - 스킵");
                }
            }
        }

        if (updated)
        {
            UpdateTotalProgress();
        }
        else
        {
            Debug.Log($"[DialogueQuest] 업데이트 없음");
        }
    }


    private void UpdateTotalProgress()
    {
        if (requireAllDialogues)
        {
            int totalRequired = DialogueTargets.Count;
            int totalCompleted = 0;

            foreach (var target in DialogueTargets)
            {
                if (target.isCompleted) totalCompleted++;
            }
            requiredProgress = totalRequired;
            UpdateProgress(totalCompleted);
        }
        else
        {
            requiredProgress = 1;
            bool anyCompleted = false;

            foreach (var target in DialogueTargets)
            {
                if (target.isCompleted)
                {
                    anyCompleted = true;
                    break;
                }
            }
            UpdateProgress(anyCompleted ? 1 : 0);
        }
    }

    // 간단한 헬퍼 메서드들
    public bool IsDialogueTarget(string npcId)
    {
        foreach (var target in DialogueTargets)
        {
            if (target.targetNpcId == npcId)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsDialogueCompletedWith(string npcId)
    {
        foreach (var target in DialogueTargets)
        {
            if (target.targetNpcId == npcId && target.isCompleted)
            {
                return true;
            }
        }
        return false;
    }
}