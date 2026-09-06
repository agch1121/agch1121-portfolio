using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 퀘스트 카테고리 열거형
/// </summary>
public enum QuestCategory
{
    Main,    // 메인 퀘스트
    Sub      // 서브 퀘스트
}

/// <summary>
/// 모든 퀘스트의 기본 클래스
/// </summary>
public abstract class Quest : ScriptableObject
{
    [Header("Prerequisite Logic")]
    public PrerequisiteLogic PrerequisiteType = PrerequisiteLogic.All;

    [Header("Quest Info")]
    public string QuestId;         // 퀘스트 고유 ID
    public string QuestName;       // 퀘스트 이름
    [TextArea(3, 5)]
    public string QuestDescription; // 퀘스트 설명
    public QuestRegion Region;     // 퀘스트 지역
    public QuestType QuestType;    // 퀘스트 타입

    [Header("Quest Category")]
    public QuestCategory Category = QuestCategory.Sub; // 퀘스트 카테고리 (기본값: 서브)

    [Header("NPC Info")]
    public string StartNpcId;      // 퀘스트 시작 NPC ID
    public string CompleteNpcId;   // 퀘스트 완료 NPC ID

    [Header("Reward Info")]
    public List<QuestItemReward> ItemRewards = new List<QuestItemReward>(); // 여러 아이템 보상
    public int GoldReward;         // 골드 보상
    public int ExpReward;          // 경험치 보상

    [Header("Prerequisites")]
    public List<Quest> PrerequisiteQuests = new List<Quest>(); // 선행 퀘스트 목록

    [Header("Quest State")]
    public QuestState State = QuestState.NotAccepted; // 퀘스트 상태
    [HideInInspector] public int currentProgress;    // 현재 진행 상황
    [HideInInspector] public int requiredProgress;   // 필요한 진행 상황

    // 이벤트 정의
    public event Action<Quest> OnQuestAccepted;
    public event Action<Quest> OnQuestCompleted;
    public event Action<Quest> OnQuestFinished;
    public event Action<Quest> OnQuestProgressUpdated;

    /// <summary>
    /// 퀘스트 카테고리에 따른 색상 반환
    /// </summary>
    public Color GetCategoryColor()
    {
        switch (Category)
        {
            case QuestCategory.Main:
                return new Color(1f, 0.8f, 0.2f); // 황금색 (메인 퀘스트)
            case QuestCategory.Sub:
                return Color.white; // 흰색 (서브 퀘스트)
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// 퀘스트 카테고리에 따른 텍스트 접두사 반환
    /// </summary>
    public string GetCategoryPrefix()
    {
        switch (Category)
        {
            case QuestCategory.Main:
                return "[메인]";
            case QuestCategory.Sub:
                return "[서브]";
            default:
                return "[서브]";
        }
    }

    /// <summary>
    /// 선행 퀘스트 조건이 충족되었는지 확인
    /// </summary>
    public bool CheckPrerequisites()
    {
        // 선행 퀘스트가 없으면 조건 충족
        if (PrerequisiteQuests == null || PrerequisiteQuests.Count == 0)
            return true;

        // 선행 퀘스트 조건 로직
        switch (PrerequisiteType)
        {
            case PrerequisiteLogic.Any:
                // 하나라도 완료된 퀘스트가 있으면 조건 충족
                foreach (Quest prerequisite in PrerequisiteQuests)
                {
                    if (prerequisite == null)
                        continue;

                    if (prerequisite.State == QuestState.Completed ||
                        prerequisite.State == QuestState.Finished)
                    {
                        return true;
                    }
                }
                return false;

            case PrerequisiteLogic.All:
            default:
                // 모든 선행 퀘스트가 완료되어야 조건 충족 (기존 코드 유지)
                foreach (Quest prerequisite in PrerequisiteQuests)
                {
                    if (prerequisite == null)
                        continue;

                    // 완료되지 않은 퀘스트가 하나라도 있으면 조건 미충족
                    if (prerequisite.State != QuestState.Completed &&
                        prerequisite.State != QuestState.Finished)
                    {
                        return false;
                    }
                }
                return true;
        }
    }

    /// <summary>
    /// 퀘스트 수락
    /// </summary>
    public virtual void AcceptQuest()
    {
        // 이미 수락된 퀘스트인지 확인
        if (State != QuestState.NotAccepted)
            return;

        // 선행 퀘스트 조건 확인
        if (!CheckPrerequisites())
        {
            return;
        }

        // 퀘스트 상태 업데이트
        State = QuestState.InProgress;

        // 이벤트 발생
        OnQuestAccepted?.Invoke(this);
    }

    /// <summary>
    /// 퀘스트 완료
    /// </summary>
    public virtual void CompleteQuest()
    {
        if (State != QuestState.InProgress)
            return;

        State = QuestState.Completed;
        OnQuestCompleted?.Invoke(this);
    }

    /// <summary>
    /// 퀘스트 보상 지급
    /// </summary>
    public virtual void GiveReward()
    {
        if (State != QuestState.Completed)
            return;

        // 보상 지급
        GiveItemReward();
        GiveGoldReward();
        GiveExpReward();

        State = QuestState.Finished;
        OnQuestFinished?.Invoke(this);
    }

    /// <summary>
    /// 퀘스트 진행도 업데이트
    /// </summary>
    public virtual void UpdateProgress(int progress)
    {
        currentProgress = Mathf.Clamp(progress, 0, requiredProgress);
        OnQuestProgressUpdated?.Invoke(this);

        // 완료 조건 확인
        if (State == QuestState.InProgress && IsCompleted())
        {
            // 완료 NPC가 없으면 자동 완료 처리
            if (string.IsNullOrEmpty(CompleteNpcId))
            {
                CompleteQuest();
                GiveReward();
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>
    /// 퀘스트 진행도 증가
    /// </summary>
    public virtual void IncrementProgress(int amount)
    {
        UpdateProgress(currentProgress + amount);
    }

    /// <summary>
    /// 퀘스트가 완료되었는지 확인
    /// </summary>
    public virtual bool IsCompleted()
    {
        return currentProgress >= requiredProgress;
    }

    // 아이템 보상 지급
    protected virtual void GiveItemReward()
    {
        if (ItemRewards == null || ItemRewards.Count == 0) return;

        foreach (var itemReward in ItemRewards)
        {
            if (itemReward.item == null || itemReward.quantity <= 0) continue;

            if (Inventory.Instance != null)
            {
                Inventory.Instance.AddItem(itemReward.item, itemReward.quantity);
            }
        }
    }

    // 골드 보상 지급
    protected virtual void GiveGoldReward()
    {
        if (GoldReward <= 0) return;

        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.AddGold(GoldReward);
        }
    }

    // 경험치 보상 지급
    protected virtual void GiveExpReward()
    {
        if (ExpReward <= 0) return;

        Player player = GameManager.Instance?.Player;
        if (player != null)
        {
            PlayerExp playerExp = player.GetComponent<PlayerExp>();
            if (playerExp != null)
            {
                playerExp.GetExp(ExpReward);
            }
        }
    }

    /// <summary>
    /// 선행 퀘스트 텍스트 정보 반환 (UI 표시용)
    /// </summary>
    public string GetPrerequisiteQuestsText()
    {
        if (PrerequisiteQuests == null || PrerequisiteQuests.Count == 0)
            return "선행 퀘스트 없음";

        string result = "";
        int count = 0;

        foreach (Quest prereq in PrerequisiteQuests)
        {
            if (prereq == null) continue;

            if (count > 0)
                result += ", ";

            result += prereq.QuestName;
            count++;
        }

        return result;
    }

    /// <summary>
    /// 현재 퀘스트의 NPC 관련 정보를 포함한 상세 설명
    /// </summary>
    public string GetDetailedDescription()
    {
        string description = QuestDescription;

        // 추가 정보 (NPC 등)
        if (!string.IsNullOrEmpty(StartNpcId) || !string.IsNullOrEmpty(CompleteNpcId))
        {
            description += "\n\n";

            if (!string.IsNullOrEmpty(StartNpcId))
            {
                description += $"시작 NPC: {StartNpcId}\n";
            }

            if (!string.IsNullOrEmpty(CompleteNpcId))
            {
                description += $"완료 NPC: {CompleteNpcId}\n";
            }
        }

        // 보상 정보
        if (GoldReward > 0 || ExpReward > 0)
        {
            description += "\n보상:";

            if (GoldReward > 0)
            {
                description += $" {GoldReward} 골드";
            }

            if (ExpReward > 0)
            {
                if (GoldReward > 0)
                    description += ",";

                description += $" {ExpReward} 경험치";
            }
        }

        return description;
    }

    // 보상 상세 텍스트 반환 (수정된 버전)
    public string GetRewardDetailsText()
    {
        string rewardText = "";

        // 골드 보상
        if (GoldReward > 0)
        {
            rewardText += $"• {GoldReward} 골드\n";
        }

        // 경험치 보상
        if (ExpReward > 0)
        {
            rewardText += $"• {ExpReward} 경험치\n";
        }

        // 아이템 보상들
        if (ItemRewards != null && ItemRewards.Count > 0)
        {
            foreach (var itemReward in ItemRewards)
            {
                if (itemReward.item != null && itemReward.quantity > 0)
                {
                    rewardText += $"• {itemReward.item.Name} x{itemReward.quantity}\n";
                }
            }
        }

        // 보상이 없는 경우
        if (string.IsNullOrEmpty(rewardText))
        {
            rewardText = "보상 없음";
        }

        return rewardText;
    }
    /// <summary>
    /// 퀘스트 진행도 텍스트 반환
    /// </summary>
    public abstract string GetProgressText();

    /// <summary>
    /// 특정 조건이 현재 퀘스트 진행에 영향을 주는지 확인
    /// </summary>
    public abstract bool CheckCondition(string conditionType, string conditionId);
}

/// <summary>
/// 퀘스트 지역 타입
/// </summary>
public enum QuestRegion
{
    Gwinoid_Forest,
    Golden_Plain,
    Red_Mountain,
    White_Forest,
    Hasla
}

/// <summary>
/// 퀘스트 타입
/// </summary>
public enum QuestType
{
    Kill,
    Collect,
    Reach,
    Combined,    // 복합 퀘스트 (사냥+수집)
    Dialogue     // 대화 퀘스트 (추가)
}

/// <summary>
/// 퀘스트 상태
/// </summary>
public enum QuestState
{
    NotAccepted,  // 수락되지 않음
    InProgress,   // 진행 중
    Completed,    // 완료됨
    Finished      // 보상 지급 완료
}

public enum PrerequisiteLogic
{
    All,    // 모든 선행 퀘스트 완료 필요
    Any     // 하나 이상의 선행 퀘스트 완료 필요
}
[System.Serializable]
public class QuestItemReward
{
    [Header("Item Reward")]
    public Item item;           // 보상 아이템
    public int quantity = 1;    // 아이템 수량

    public QuestItemReward()
    {
        quantity = 1;
    }

    public QuestItemReward(Item item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}