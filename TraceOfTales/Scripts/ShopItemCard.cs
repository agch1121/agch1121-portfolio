using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class ShopItemCard : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // 아이템 설명창 UI 요소
    [Header("UI Elements")]
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemPriceText;
    [SerializeField] private TextMeshProUGUI itemQuantityText;  // 아이템 개수 표시 텍스트

    private object itemData; // ShopItem 또는 Item 참조
    private int itemSlotIndex = -1; // 인벤토리 슬롯 인덱스 (판매 시 사용)
    private ShopManager.ShopTab currentTab;

    // 마우스 오버 상태 관리
    private static GameObject lastHoveredCard = null;
    private ShopUI shopUI;

    private void Awake()
    {
        shopUI = Object.FindAnyObjectByType<ShopUI>();
    }

    // 구매 탭 아이템 초기화 메서드
    public void Initialize(Item item, ShopManager.ShopTab tab)
    {
        // 공통 필드 설정
        itemData = item;
        currentTab = tab;
        itemSlotIndex = -1;

        // UI 설정
        itemImage.sprite = item.Icon;
        itemNameText.text = item.Name;
        itemPriceText.text = $"{item.BasePrice} 골드";

        // 수량 텍스트 설정
        if (itemQuantityText != null) itemQuantityText.gameObject.SetActive(false);
    }

    // 인벤토리 아이템 초기화 메서드 (판매 시 사용)
    public void InitializePlayerItem(Item item, int slotIndex)
    {
        // 공통 필드 설정
        itemData = item;
        currentTab = ShopManager.ShopTab.Sell;
        itemSlotIndex = slotIndex;

        // UI 설정
        itemImage.sprite = item.Icon;
        itemNameText.text = item.Name;
        itemPriceText.text = $"{ShopManager.Instance.CalculateSellPrice(item)} 골드";

        // 수량 텍스트 설정 (스택 가능한 아이템이고 수량이 1보다 많은 경우에만 표시)
        SetQuantityText(item.IsStackable && item.Quantity > 1, item.Quantity);
    }

    
    // 판매 전용 초기화 메서드
    public void InitForSellOnly(Item item)
    {
        if (item == null) return;

        // 공통 필드 설정
        itemData = item;
        currentTab = ShopManager.ShopTab.Sell;
        itemSlotIndex = -1;

        // UI 설정
        itemImage.sprite = item.Icon;
        itemNameText.text = item.Name;

        // 인벤토리에 아이템이 있는지 확인
        bool canSell = false;
        int quantity = 0;

        if (Inventory.Instance != null)
        {
            for (int i = 0; i < Inventory.Instance.InventorySize; i++)
            {
                Item inventoryItem = Inventory.Instance.InventoryItems[i];
                if (inventoryItem != null && inventoryItem.ID == item.ID && inventoryItem.CanSell)
                {
                    canSell = true;
                    quantity += inventoryItem.Quantity;
                }
            }
        }

        // 판매 가능 여부에 따라 가격 텍스트 설정
        if (canSell)
        {
            int sellPrice = ShopManager.Instance.CalculateSellPrice(item);
            itemPriceText.text = $"{sellPrice} 골드";
            itemPriceText.color = Color.white;
        }
        else
        {
            itemPriceText.text = "판매 불가";
            itemPriceText.color = Color.red;
        }

        // 수량 텍스트 설정
        if (quantity > 0 && itemQuantityText != null)
        {
            itemQuantityText.gameObject.SetActive(true);
            itemQuantityText.text = $"x{quantity}";
        }
        else if (itemQuantityText != null)
        {
            itemQuantityText.gameObject.SetActive(false);
        }
    }

    // 수량 텍스트 설정
    private void SetQuantityText(bool isVisible, int quantity = 0)
    {
        if (itemQuantityText != null)
        {
            itemQuantityText.gameObject.SetActive(isVisible);
            if (isVisible)
                itemQuantityText.text = $"x{quantity}";
        }
    }

    // 아이템 카드 클릭 이벤트 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        // 우클릭 여부 확인
        bool isRightClick = (eventData.button == PointerEventData.InputButton.Right);
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // ShopManager로 이벤트 전달
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnItemClicked(itemData, itemSlotIndex, isRightClick, isShiftPressed);
        }
    }

    // 마우스가 아이템 카드 위에 올라갔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        lastHoveredCard = gameObject;
        ShowItemInfo();
    }

    // 마우스가 아이템 카드에서 빠져나갔을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        if (lastHoveredCard == gameObject)
            StartCoroutine(DelayedHideInfo());
    }

    // 아이템 정보 패널 표시
    private void ShowItemInfo()
    {
        Vector3 infoPosition = GetInfoPanelPosition();

        if (itemData is Item shopItem)
        {
            if (currentTab == ShopManager.ShopTab.Buy)
                shopUI.ShowBuyItemInfo(shopItem, infoPosition);
        }
        else if (itemData is Item item)
        {
            shopUI.ShowSellItemInfo(item, infoPosition);
        }
    }

    // 아이템 정보 패널 생성 위치 계산
    private Vector3 GetInfoPanelPosition()
    {
        Vector3 basePosition = transform.position;
        float panelWidth = 300f;

        return basePosition.x + panelWidth > Screen.width
            ? basePosition + new Vector3(-panelWidth, 0, 0)
            : basePosition + new Vector3(250, 0, 0);
    }

    // 마우스가 아이템 카드에서 빠진 후 일정 시간 뒤에 정보 숨김(버그 방지용)
    private IEnumerator DelayedHideInfo()
    {
        yield return new WaitForEndOfFrame();

        if (lastHoveredCard == gameObject)
        {
            lastHoveredCard = null;
            shopUI.HideItemInfo();
        }
    }

    // 모든 정보 패널 강제 숨김 (디버그용)
    public static void ForceHideAllInfo()
    {
        lastHoveredCard = null;
        Object.FindAnyObjectByType<ShopUI>()?.HideItemInfo();
    }
}