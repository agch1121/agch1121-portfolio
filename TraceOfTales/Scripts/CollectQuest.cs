using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCollectQuest", menuName = "Quest/CollectQuest")]
public class CollectQuest : Quest
{
    [System.Serializable]
    public class CollectItem
    {
        public Item item;           // 수집할 아이템
        public int requiredCount;   // 수집해야 할 아이템 수
        [HideInInspector]
        public int currentCount;    // 현재 수집한 아이템 수

        [Tooltip("아이템 수집 후 인벤토리에서 제거할지 여부")]
        public bool removeAfterCollection = false;
    }

    [Header("Collect Quest Info")]
    public List<CollectItem> RequiredItems = new List<CollectItem>();

    [Header("Collection Options")]
    [Tooltip("인벤토리에 있는 아이템을 자동으로 감지하여 수집 처리할지 여부")]
    public bool autoDetectInventoryItems = true;

    [Tooltip("자동 감지 시 확인 간격 (초)")]
    public float autoDetectInterval = 1f;

    // 자동 감지 타이머
    private float autoDetectTimer = 0f;

    private void OnEnable()
    {
        QuestType = QuestType.Collect; // 퀘스트 타입 설정

        // 전체 진행도 초기화
        UpdateTotalProgress();
    }

    private void OnValidate()
    {
        // 필요한 수량이 0 이하로 설정되지 않도록 검증
        foreach (var item in RequiredItems)
        {
            if (item.requiredCount <= 0)
            {
                item.requiredCount = 1;
                Debug.LogWarning($"CollectQuest: {item.item?.Name}의 필요 수량이 0 이하로 설정되어 1로 조정되었습니다.");
            }
        }
    }

    public override string GetProgressText()
    {
        if (RequiredItems.Count == 1)
        {
            return $"{RequiredItems[0].item.Name} 수집:{RequiredItems[0].currentCount} / {RequiredItems[0].requiredCount}";
        }
        else
        {
            string result = "";
            foreach (var item in RequiredItems)
            {
                result += $"{item.item.Name} 수집:{item.currentCount} / {item.requiredCount}\n";
            }
            return result;
        }
    }

    public override bool CheckCondition(string conditionType, string conditionId)
    {
        // 아이템 획득 이벤트 확인
        if (conditionType == "Collect" && State == QuestState.InProgress)
        {
            foreach (CollectItem item in RequiredItems)
            {
                if (item.item.ID == conditionId)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // 아이템 수집 시 호출될 메서드
    public void ItemCollected(string itemId, int amount = 1)
    {
        if (State != QuestState.InProgress) return;

        bool updated = false;

        foreach (CollectItem item in RequiredItems)
        {
            if (item.item.ID == itemId && item.currentCount < item.requiredCount)
            {
                // 수집 수량 증가 (최대값 제한)
                int oldCount = item.currentCount;
                item.currentCount = Mathf.Min(item.currentCount + amount, item.requiredCount);
                int addedCount = item.currentCount - oldCount;

                if (addedCount > 0)
                {
                    updated = true;
                    Debug.Log($"퀘스트 '{QuestName}': {item.item.Name} 수집 진행 ({item.currentCount}/{item.requiredCount})");

                    // 아이템 수집 후 인벤토리에서 제거 옵션이 활성화된 경우
                    if (item.removeAfterCollection && Inventory.Instance != null)
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

        Debug.Log($"퀘스트 '{QuestName}'를 위해 인벤토리에서 {itemId} {amount - remainingToRemove}개 제거됨");
    }

    // 인벤토리 기반 아이템 수집 상태 확인
    public void CheckInventoryItems()
    {
        if (State != QuestState.InProgress || Inventory.Instance == null) return;

        bool updated = false;

        // 모든 필요 아이템 확인
        foreach (CollectItem collectItem in RequiredItems)
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
                    Debug.Log($"[CollectQuest] '{collectItem.item.Name}' 아이템 카운트 업데이트: {oldCount} -> {collectItem.currentCount}");
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

        foreach (CollectItem item in RequiredItems)
        {
            totalRequired += item.requiredCount;
            totalCurrent += item.currentCount;
        }

        // 필요한 진행도와 현재 진행도 설정
        requiredProgress = totalRequired;
        UpdateProgress(totalCurrent);
    }
}