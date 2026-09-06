using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New QuestProvider", menuName = "NPC/QuestProvider")]
public class NPC_QuestProvider : ScriptableObject
{
    [Header("Quests")]
    [SerializeField] private List<QuestReference> availableQuests = new List<QuestReference>();

    [Header("Dialogue Mapping")]
    [SerializeField] private DialogueMapping dialogueMapping = new DialogueMapping();

    // 사용 가능한 퀘스트 목록 가져오기
    public List<QuestReference> GetAvailableQuests()
    {
        return availableQuests;
    }

    // 특정 상태의 퀘스트에 맞는 대화 ID 가져오기
    public string GetDialogueIdForQuestState(string questId, QuestState state)
    {
        return dialogueMapping.GetDialogueId(questId, state);
    }

    // NPC에게 제공 가능한 퀘스트 목록 반환
    public List<Quest> GetAvailableQuestsForPlayer()
    {
        List<Quest> quests = new List<Quest>();

        foreach (QuestReference questRef in availableQuests)
        {
            if (string.IsNullOrEmpty(questRef.QuestId))
            {
                Debug.LogWarning("[NPC_QuestProvider] 퀘스트 참조에 ID가 없습니다!");
                continue;
            }

            Quest quest = QuestManager.Instance.GetQuestById(questRef.QuestId);

            // 퀘스트가 존재하고, 아직 수락하지 않았으며, 선행 퀘스트 조건이 충족된 경우
            if (quest != null &&
                quest.State == QuestState.NotAccepted &&
                quest.CheckPrerequisites())
            {
                quests.Add(quest);
            }
        }

        return quests;
    }

    // NPC에게 완료 가능한 퀘스트 목록 반환
    public List<Quest> GetCompletableQuestsForPlayer(string npcId)
    {
        List<Quest> quests = new List<Quest>();

        foreach (QuestReference questRef in availableQuests)
        {
            Quest quest = QuestManager.Instance.GetQuestById(questRef.QuestId);

            // 퀘스트가 존재하고, 완료되었지만 보상을 받지 않은 경우
            if (quest != null && quest.CompleteNpcId == npcId)
            {
                // 진행 중이지만 완료 조건을 만족한 경우
                if (quest.State == QuestState.InProgress && quest.IsCompleted())
                {
                    quests.Add(quest);
                }
                // 이미 완료 상태인 경우
                else if (quest.State == QuestState.Completed)
                {
                    quests.Add(quest);
                }
            }
        }

        return quests;
    }

    // NPC에게 진행 중인 퀘스트 목록 반환
    public List<Quest> GetInProgressQuestsForPlayer(string npcId)
    {
        List<Quest> quests = new List<Quest>();

        foreach (QuestReference questRef in availableQuests)
        {
            Quest quest = QuestManager.Instance.GetQuestById(questRef.QuestId);

            // 퀘스트가 존재하고 진행 중인 경우
            if (quest != null &&
                quest.State == QuestState.InProgress &&
                (quest.StartNpcId == npcId || quest.CompleteNpcId == npcId))
            {
                quests.Add(quest);
            }
        }

        return quests;
    }

    // 모든 관련 퀘스트 목록 반환 (상태별로 필터링)
    public Dictionary<QuestState, List<Quest>> GetAllQuestsByState(string npcId)
    {
        Dictionary<QuestState, List<Quest>> result = new Dictionary<QuestState, List<Quest>>();

        // 딕셔너리 초기화
        result[QuestState.NotAccepted] = new List<Quest>();
        result[QuestState.InProgress] = new List<Quest>();
        result[QuestState.Completed] = new List<Quest>();
        result[QuestState.Finished] = new List<Quest>();

        foreach (QuestReference questRef in availableQuests)
        {
            Quest quest = QuestManager.Instance.GetQuestById(questRef.QuestId);

            if (quest == null) continue;

            // NPC가 관련되어 있고 요구 사항을 만족하는 퀘스트만 추가
            if (quest.State == QuestState.NotAccepted &&
                quest.StartNpcId == npcId &&
                quest.CheckPrerequisites())
            {
                result[QuestState.NotAccepted].Add(quest);
            }
            else if (quest.State == QuestState.InProgress &&
                    (quest.StartNpcId == npcId || quest.CompleteNpcId == npcId))
            {
                result[QuestState.InProgress].Add(quest);
            }
            else if (quest.State == QuestState.Completed && quest.CompleteNpcId == npcId)
            {
                result[QuestState.Completed].Add(quest);
            }
            else if (quest.State == QuestState.Finished &&
                    (quest.StartNpcId == npcId || quest.CompleteNpcId == npcId))
            {
                result[QuestState.Finished].Add(quest);
            }
        }

        return result;
    }
}

// 퀘스트 참조 클래스
[System.Serializable]
public class QuestReference
{
    public string QuestId;
    [Tooltip("인스펙터에서 확인용으로 표시되는 이름")]
    public string QuestName;
}

// 퀘스트 상태별 대화 ID 맵핑 클래스
[System.Serializable]
public class DialogueMapping
{
    [System.Serializable]
    public class QuestDialogueMap
    {
        public string QuestId;
        [NonSerialized] public string NotAcceptedDialogueId; // 수락하지 않은 상태의 대화 ID
        public string InProgressDialogueId;  // 진행 중인 상태의 대화 ID
        [NonSerialized]  public string CompletedDialogueId;   // 완료했지만 보상을 받지 않은 상태의 대화 ID
        public string FinishedDialogueId;    // 모두 완료된 상태의 대화 ID
    }

    [SerializeField] private List<QuestDialogueMap> questDialogueMaps = new List<QuestDialogueMap>();

    // 퀘스트 ID와 상태에 따른 대화 ID 반환
    public string GetDialogueId(string questId, QuestState state)
    {
        foreach (QuestDialogueMap map in questDialogueMaps)
        {
            if (map.QuestId == questId)
            {
                switch (state)
                {
                    case QuestState.NotAccepted:
                        return map.NotAcceptedDialogueId;
                    case QuestState.InProgress:
                        return map.InProgressDialogueId;
                    case QuestState.Completed:
                        return map.CompletedDialogueId;
                    case QuestState.Finished:
                        return map.FinishedDialogueId;
                }
            }
        }

        return null; // 해당 퀘스트에 대한 대화 ID가 없는 경우
    }
}