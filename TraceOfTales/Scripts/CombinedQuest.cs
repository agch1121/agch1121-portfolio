using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCombinedQuest", menuName = "Quest/CombinedQuest")]
public class CombinedQuest : Quest
{
    [System.Serializable]
    public class EnemyHuntTarget
    {
        [Tooltip("사냥할 적 프리팹")]
        public Enemy enemyPrefab;           // Enemy 프리팹 참조
        public int requiredCount;           // 사냥해야 할 적 수
        [HideInInspector]
        public int currentCount;            // 현재 사냥한 적 수

        // 몬스터 정보를 프리팹에서 가져옴
        public string GetMonsterId() => enemyPrefab != null ? enemyPrefab.MonsterId : "";
        public string GetMonsterName() => enemyPrefab != null ? enemyPrefab.MonsterName : "알 수 없음";
    }

    [System.Serializable]
    public class ItemCollectTarget
    {
        public Item item;                   // 수집할 아이템
        public int requiredCount;           // 수집해야 할 아이템 수
        [HideInInspector]
        public int currentCount;            // 현재 수집한 아이템 수

        [Tooltip("아이템 수집 후 인벤토리에서 제거할지 여부")]
        public bool removeAfterCollection = false;
    }

    [Header("Hunt Quest Info")]
    public List<EnemyHuntTarget> HuntTargets = new List<EnemyHuntTarget>();

    [Header("Collect Quest Info")]
    public List<ItemCollectTarget> CollectTargets = new List<ItemCollectTarget>();

    [Header("Collection Options")]
    [Tooltip("인벤토리에 있는 아이템을 자동으로 감지하여 수집 처리할지 여부")]
    public bool autoDetectInventoryItems = true;

    [Tooltip("자동 감지 시 확인 간격 (초)")]
    public float autoDetectInterval = 1f;

    // 자동 감지 타이머
    private float autoDetectTimer = 0f;

    private void OnEnable()
    {
        QuestType = QuestType.Combined; // 퀘스트 타입 설정 (QuestType enum에 Combined 추가 필요)

        // 전체 진행도 초기화
        UpdateTotalProgress();
    }

    private void OnValidate()
    {
        // 필요한 수량이 0 이하로 설정되지 않도록 검증
        foreach (var huntTarget in HuntTargets)
        {
            if (huntTarget.requiredCount <= 0)
            {
                huntTarget.requiredCount = 1;
                Debug.LogWarning($"CombinedQuest: {huntTarget.GetMonsterName()}의 필요 수량이 0 이하로 설정되어 1로 조정되었습니다.");
            }
        }

        foreach (var collectTarget in CollectTargets)
        {
            if (collectTarget.requiredCount <= 0)
            {
                collectTarget.requiredCount = 1;
                Debug.LogWarning($"CombinedQuest: {collectTarget.item?.Name}의 필요 수량이 0 이하로 설정되어 1로 조정되었습니다.");
            }
        }
    }

    public override string GetProgressText()
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();

        // 사냥 타겟 진행상황
        if (HuntTargets.Count > 0)
        {
            result.AppendLine("< 사냥 목표 >");
            foreach (var target in HuntTargets)
            {
                result.AppendLine($"{target.GetMonsterName()} 사냥:{target.currentCount} / {target.requiredCount}");
            }
        }

        // 수집 타겟 진행상황
        if (CollectTargets.Count > 0)
        {
            if (HuntTargets.Count > 0)
            {
                result.AppendLine();
            }
            result.AppendLine("< 수집 목표 >");
            foreach (var target in CollectTargets)
            {
                result.AppendLine($"{target.item.Name} 수집:{target.currentCount} / {target.requiredCount}");
            }
        }

        return result.ToString().TrimEnd();
    }

    public override bool CheckCondition(string conditionType, string conditionId)
    {
        // 몬스터 사냥 이벤트 확인
        if (conditionType == "Kill" && State == QuestState.InProgress)
        {
            foreach (var target in HuntTargets)
            {
                if (target.GetMonsterId() == conditionId)
                {
                    return true;
                }
            }
        }

        // 아이템 수집 이벤트 확인
        if (conditionType == "Collect" && State == QuestState.InProgress)
        {
            foreach (var target in CollectTargets)
            {
                if (target.item != null && target.item.ID == conditionId)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // 몬스터 사냥 시 호출될 메서드
    public void ProcessMonsterKill(string monsterId)
    {
        if (State != QuestState.InProgress) return;

        bool updated = false;

        foreach (var target in HuntTargets)
        {
            if (target.GetMonsterId() == monsterId && target.currentCount < target.requiredCount)
            {
                target.currentCount++;
                updated = true;
                // 같은 ID의 첫 번째 몬스터만 업데이트
                if (State == QuestState.InProgress)
                {
                    break;
                }
            }
        }

        if (updated)
        {
            UpdateTotalProgress();
        }
    }

    // 아이템 수집 시 호출될 메서드
    public void ItemCollected(string itemId, int amount = 1)
    {
        if (State != QuestState.InProgress) return;

        bool updated = false;

        foreach (var target in CollectTargets)
        {
            if (target.item != null && target.item.ID == itemId && target.currentCount < target.requiredCount)
            {
                // 수집 수량 증가 (최대값 제한)
                int oldCount = target.currentCount;
                target.currentCount = Mathf.Min(target.currentCount + amount, target.requiredCount);
                int addedCount = target.currentCount - oldCount;

                if (addedCount > 0)
                {
                    updated = true;

                    // 아이템 수집 후 인벤토리에서 제거 옵션이 활성화된 경우
                    if (target.removeAfterCollection && Inventory.Instance != null)
                    {
                        RemoveItemFromInventory(itemId, addedCount);
                    }
                }
            }
        }

        if (updated)
        {
            UpdateTotalProgress();
        }
    }

    // 인벤토리에서 아이템 제거
    private void RemoveItemFromInventory(string itemId, int amount)
    {
        if (Inventory.Instance == null) return;

        int remainingToRemove = amount;

        // 인벤토리 슬롯을 순회하며 일치하는 아이템 제거
        for (int i = 0; i < Inventory.Instance.InventorySize; i++)
        {
            Item inventoryItem = Inventory.Instance.InventoryItems[i];
            if (inventoryItem != null && inventoryItem.ID == itemId)
            {
                if (inventoryItem.Quantity <= remainingToRemove)
                {
                    // 슬롯 전체 제거
                    remainingToRemove -= inventoryItem.Quantity;
                    Inventory.Instance.RemoveItem(i);
                }
                else
                {
                    // 일부만 제거
                    inventoryItem.Quantity -= remainingToRemove;
                    InventoryUI.Instance.DrawItem(inventoryItem, i);
                    remainingToRemove = 0;
                }

                if (remainingToRemove <= 0) break;
            }
        }
    }

    // 인벤토리 기반 아이템 수집 상태 확인
    public void CheckInventoryItems()
    {
        if (State != QuestState.InProgress || Inventory.Instance == null) return;

        bool updated = false;

        // 모든 필요 아이템 확인
        foreach (var collectItem in CollectTargets)
        {
            if (collectItem.item == null) continue;

            // 이미 목표 수량을 달성했다면 스킵
            if (collectItem.currentCount >= collectItem.requiredCount) continue;

            // 인벤토리에서 해당 아이템의 총 개수 계산
            int inventoryCount = CountItemInInventory(collectItem.item.ID);

            // 현재 카운트를 인벤토리 기준으로 업데이트
            if (inventoryCount > collectItem.currentCount)
            {
                int oldCount = collectItem.currentCount;
                collectItem.currentCount = Mathf.Min(inventoryCount, collectItem.requiredCount);

                if (collectItem.currentCount > oldCount)
                {
                    updated = true;
                }
            }
        }

        if (updated)
        {
            // 전체 진행도 업데이트
            UpdateTotalProgress();
        }
    }

    // 인벤토리에서 특정 아이템 ID의 총 개수 확인
    private int CountItemInInventory(string itemId)
    {
        if (Inventory.Instance == null) return 0;

        int count = 0;
        for (int i = 0; i < Inventory.Instance.InventorySize; i++)
        {
            Item inventoryItem = Inventory.Instance.InventoryItems[i];
            if (inventoryItem != null && inventoryItem.ID == itemId)
            {
                count += inventoryItem.Quantity;
            }
        }

        return count;
    }

    // 자동 감지 업데이트 (Update 루프에서 호출)
    public void UpdateAutoDetect(float deltaTime)
    {
        if (!autoDetectInventoryItems || State != QuestState.InProgress) return;

        autoDetectTimer += deltaTime;

        if (autoDetectTimer >= autoDetectInterval)
        {
            autoDetectTimer = 0f;
            CheckInventoryItems();
        }
    }

    // 전체 진행도 계산
    private void UpdateTotalProgress()
    {
        int totalRequired = 0;
        int totalCurrent = 0;

        // 사냥 목표 계산
        foreach (var target in HuntTargets)
        {
            totalRequired += target.requiredCount;
            totalCurrent += target.currentCount;
        }

        // 수집 목표 계산
        foreach (var target in CollectTargets)
        {
            totalRequired += target.requiredCount;
            totalCurrent += target.currentCount;
        }

        // 필요한 진행도와 현재 진행도 설정
        requiredProgress = totalRequired;
        UpdateProgress(totalCurrent);
    }

    // 퀘스트 완료 여부 확인 (모든 목표가 완료되었는지)
    public override bool IsCompleted()
    {
        // 사냥 목표 확인
        foreach (var target in HuntTargets)
        {
            if (target.currentCount < target.requiredCount)
            {
                return false;
            }
        }

        // 수집 목표 확인
        foreach (var target in CollectTargets)
        {
            if (target.currentCount < target.requiredCount)
            {
                return false;
            }
        }

        // 모든 목표가 완료됨
        return true;
    }
}