using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

// 보상 타입 enum 정의
public enum RewardType
{
    Gold,
    Exp,
    Item
}

public class QuestRewardItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI amountText;
    //[SerializeField] private TextMeshProUGUI typeText;

    private Item itemData;
    private GameObject lastHoveredItem;
    private int rewardAmount;
    private RewardType rewardType;

    public void Initialize(int amount, RewardType type, Item item)
    {
        if (item == null) return;

        // 데이터 저장
        itemData = item;
        rewardAmount = amount;
        rewardType = type;

        // 수량 설정 (개선된 표시)
        if (amountText != null)
        {
            // 수량이 1이면 표시하지 않거나, 골드/경험치는 항상 표시
            if (amount > 1 || type == RewardType.Gold || type == RewardType.Exp)
            {
                amountText.text = FormatAmount(amount, type);
                amountText.gameObject.SetActive(true);
            }
            else
            {
                amountText.gameObject.SetActive(false);
            }
        }

        // 아이콘 설정
        if (itemIcon != null)
        {
            itemIcon.sprite = item.Icon;
        }
    }

    /// <summary>
    /// 보상 타입에 따라 수량 표시 포맷 결정
    /// </summary>
    private string FormatAmount(int amount, RewardType type)
    {
        switch (type)
        {
            case RewardType.Gold:
                return amount.ToString("N0"); // 천 단위 콤마
            case RewardType.Exp:
                return amount.ToString("N0"); // 천 단위 콤마
            case RewardType.Item:
                return amount > 1 ? amount.ToString() : ""; // 1개면 숨김
            default:
                return amount.ToString();
        }
    }

    // 마우스가 아이템 위에 올라갔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemData != null)
        {
            lastHoveredItem = gameObject;
            ShowItemInfo();
        }
    }

    // 마우스가 아이템에서 벗어났을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        if (lastHoveredItem == gameObject)
        {
            StartCoroutine(DelayedHideItemInfo());
        }
    }

    // 아이템 정보 패널 표시
    private void ShowItemInfo()
    {
        Vector3 infoPosition = GetInfoPanelPosition();
        // 보상 정보도 함께 표시
        ShowRewardInfo(infoPosition);
    }

    // 보상 정보 표시 (아이템 정보와 함께)
    private void ShowRewardInfo(Vector3 position)
    {
        if (itemData == null) return;

        // 보상 정보를 포함한 아이템 정보 표시
        ItemInfoUIManager.Instance.ShowItemInfo(itemData, position);
        ItemInfoUIManager.Instance.SetRewardAmount(rewardAmount, rewardType);
    }

    // 패널 위치 계산
    private Vector3 GetInfoPanelPosition()
    {
        Vector3 basePosition = transform.position;
        float panelWidth = 300f;

        return basePosition.x + panelWidth > Screen.width
            ? basePosition + new Vector3(-panelWidth, 0, 0)
            : basePosition + new Vector3(220, 0, 0);
    }

    // 지연 숨김 코루틴
    private IEnumerator DelayedHideItemInfo()
    {
        yield return new WaitForEndOfFrame();

        if (lastHoveredItem == gameObject)
        {
            lastHoveredItem = null;
            ItemInfoUIManager.Instance.HideItemInfo();
        }
    }
}