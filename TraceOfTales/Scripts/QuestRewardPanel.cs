using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class QuestRewardPanel : MonoBehaviour
{
    [Header("Reward UI")]
    [SerializeField] private Transform rewardContainer;
    [SerializeField] private GameObject rewardItemPrefab;
    [SerializeField] private GridLayoutGroup gridLayout; // 그리드 레이아웃 참조 추가

    [Header("Reward Prefabs")]
    [SerializeField] private Item goldItemPrefab;    // 골드 아이템 프리팹
    [SerializeField] private Item expItemPrefab;     // 경험치 아이템 프리팹

    private List<GameObject> rewardItems = new List<GameObject>();
    private Quest currentQuest;

    private void Awake()
    {
        ClearRewards();

        // GridLayoutGroup 참조 가져오기
        if (gridLayout == null)
            gridLayout = rewardContainer.GetComponent<GridLayoutGroup>();

        // GridLayoutGroup 설정 확인
        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4; // 4열로 설정
            gridLayout.cellSize = new Vector2(70, 70); // 적절한 크기로 조정
            gridLayout.spacing = new Vector2(5, 5); // 간격 설정
        }
        else
        {
            Debug.LogError("GridLayoutGroup 컴포넌트를 찾을 수 없습니다!");
        }
    }

    public void ShowRewards(Quest quest)
    {
        if (quest == null) return;

        // 기존 보상 아이템 제거
        ClearRewards();

        // 현재 표시 중인 퀘스트 저장
        currentQuest = quest;

        // 보상 아이템 순서대로 생성
        int itemCount = 0;

        // 골드 보상
        if (quest.GoldReward > 0 && goldItemPrefab != null)
        {
            bool success = CreateRewardItem(quest.GoldReward, RewardType.Gold, goldItemPrefab);
            if (success) itemCount++;
        }

        // 경험치 보상
        if (quest.ExpReward > 0 && expItemPrefab != null)
        {
            bool success = CreateRewardItem(quest.ExpReward, RewardType.Exp, expItemPrefab);
            if (success) itemCount++;
        }

        // 아이템 보상 (수정)
        if (quest.ItemRewards != null && quest.ItemRewards.Count > 0)
        {
            foreach (var itemReward in quest.ItemRewards)
            {
                if (itemReward.item != null && itemReward.quantity > 0)
                {
                    bool success = CreateRewardItem(itemReward.quantity, RewardType.Item, itemReward.item);
                    if (success) itemCount++;
                }
            }
        }

        // 레이아웃 강제 업데이트
        Canvas.ForceUpdateCanvases();
        if (gridLayout != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardContainer as RectTransform);
    }

    private bool CreateRewardItem(int amount, RewardType type, Item item)
    {
        if (item == null || rewardItemPrefab == null)
        {
            Debug.LogError($"[QuestRewardPanel] CreateRewardItem 실패: item 또는 rewardItemPrefab이 null입니다 (Type: {type})");
            return false;
        }

        GameObject rewardObj = Instantiate(rewardItemPrefab, rewardContainer);
        QuestRewardItem rewardItem = rewardObj.GetComponent<QuestRewardItem>();

        if (rewardItem != null)
        {
            rewardItem.Initialize(amount, type, item);
            rewardItems.Add(rewardObj);
            return true;
        }
        else
        {
            Debug.LogError($"[QuestRewardPanel] CreateRewardItem 실패: rewardObj에서 QuestRewardItem 컴포넌트를 찾을 수 없음");
            Destroy(rewardObj);
            return false;
        }
    }

    public void ClearRewards()
    {
        // 1. 리스트에 있는 모든 게임 오브젝트 삭제
        foreach (GameObject item in rewardItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        rewardItems.Clear();

        // 2. rewardContainer의 모든 자식 오브젝트 직접 삭제
        if (rewardContainer != null)
        {
            int childCount = rewardContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Destroy(rewardContainer.GetChild(i).gameObject);
            }
        }
    }
}