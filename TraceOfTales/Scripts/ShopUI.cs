using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopUI : MonoBehaviour
{
    [Header("Main Panels")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private GameObject quantitySelectorPanel;
    [SerializeField] private GameObject confirmDialogPanel;

    [Header("Tab System")]
    [SerializeField] private Button buyTabButton;       // 구매 탭 버튼
    [SerializeField] private Button sellTabButton;      // 판매 탭 버튼
    [SerializeField] private GameObject buyPanel;       // 구매 패널 (구매 장바구니 포함)
    [SerializeField] private GameObject sellPanel;      // 판매 패널 (판매 장바구니 포함)

    [Header("Shop Items")]
    [SerializeField] private Transform shopItemContainer; // 상점 아이템 그리드

    [Header("UI Text")]
    [SerializeField] private TextMeshProUGUI shopNameText;
    [SerializeField] private TextMeshProUGUI playerGoldText;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Buy Cart")]
    [SerializeField] private GameObject buyCartPanel;
    [SerializeField] private GameObject buyCartSlotPrefab;
    [SerializeField] private Transform buyCartContainer;
    [SerializeField] private List<GameObject> buyCartSlots = new List<GameObject>();
    [SerializeField] private int initialBuyCartSlotCount = 6;
    [SerializeField] private TextMeshProUGUI buyCartTotalPriceText;
    [SerializeField] private Button executeBuyButton;

    [Header("Sell Cart")]
    [SerializeField] private GameObject sellCartPanel;
    [SerializeField] private GameObject sellCartSlotPrefab;
    [SerializeField] private Transform sellCartContainer;
    [SerializeField] private List<GameObject> sellCartSlots = new List<GameObject>();
    [SerializeField] private int initialSellCartSlotCount = 6;
    [SerializeField] private TextMeshProUGUI sellCartTotalPriceText;
    [SerializeField] private Button executeSellButton;

    [Header("Common Cart Controls")]
    [SerializeField] private Button clearAllCartsButton; // 모든 장바구니 비우기 버튼

    [Header("Quantity Selector")]
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private Button decreaseQuantityButton;
    [SerializeField] private Button increaseQuantityButton;
    [SerializeField] private Button confirmQuantityButton;
    [SerializeField] private Button cancelQuantityButton;
    [SerializeField] private TextMeshProUGUI quantityTitleText;

    [Header("Confirmation Dialog")]
    [SerializeField] private TextMeshProUGUI confirmMessageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private int currentQuantity = 1;
    private int maxQuantity = 99;
    private Action<int> onQuantityConfirm;
    private Action onConfirmDialogConfirm;
    private ShopManager.ShopTab currentTab = ShopManager.ShopTab.Buy; // 현재 활성화된 탭

    private void Start()
    {
        InitializeButtons();
        InitializePanels(false);

        // 초기 장바구니 슬롯 수 저장
        if (initialBuyCartSlotCount <= 0)
            initialBuyCartSlotCount = buyCartSlots.Count > 0 ? buyCartSlots.Count : 6;
        if (initialSellCartSlotCount <= 0)
            initialSellCartSlotCount = sellCartSlots.Count > 0 ? sellCartSlots.Count : 6;
    }

    private void Update()
    {
        // ESC 키로 팝업창 닫기
        if ((quantitySelectorPanel.activeSelf || confirmDialogPanel.activeSelf) && Input.GetKeyDown(KeyCode.Escape))
            HideAllDialogs();
    }

    private void InitializeButtons()
    {
        // 장바구니 버튼 설정
        if (executeBuyButton != null)
            executeBuyButton.onClick.AddListener(() => ShopManager.Instance.ExecutePurchase());
        if (executeSellButton != null)
            executeSellButton.onClick.AddListener(() => ShopManager.Instance.ExecuteSell());
        if (clearAllCartsButton != null)
            clearAllCartsButton.onClick.AddListener(() => ShopManager.Instance.ClearAllCarts());

        // 탭 버튼 설정
        if (buyTabButton != null)
            buyTabButton.onClick.AddListener(() => SwitchTab(ShopManager.ShopTab.Buy));
        if (sellTabButton != null)
            sellTabButton.onClick.AddListener(() => SwitchTab(ShopManager.ShopTab.Sell));

        // 수량 선택 창 버튼
        decreaseQuantityButton.onClick.AddListener(DecreaseQuantity);
        increaseQuantityButton.onClick.AddListener(IncreaseQuantity);
        confirmQuantityButton.onClick.AddListener(ConfirmQuantity);
        cancelQuantityButton.onClick.AddListener(CancelQuantity);

        // 수량 입력 필드
        quantityInput.onValueChanged.AddListener(OnQuantityInputChanged);
        quantityInput.onEndEdit.AddListener(OnQuantityInputEndEdit);

        // 확인 창 버튼
        confirmButton.onClick.AddListener(ConfirmDialog);
        cancelButton.onClick.AddListener(CancelDialog);
    }

    private void InitializePanels(bool active)
    {
        shopPanel.SetActive(active);
        messagePanel.SetActive(false);
        quantitySelectorPanel.SetActive(false);
        confirmDialogPanel.SetActive(false);

        // 탭 패널 초기화 (기본값: 구매 탭)
        if (buyPanel != null) buyPanel.SetActive(active);
        if (sellPanel != null) sellPanel.SetActive(false);

        // 장바구니 패널 초기화
        if (buyCartPanel != null) buyCartPanel.SetActive(active);
        if (sellCartPanel != null) sellCartPanel.SetActive(false);
    }

    // 탭 전환 메서드
    public void SwitchTab(ShopManager.ShopTab tab)
    {
        currentTab = tab;

        // 패널 활성화/비활성화
        if (buyPanel != null) buyPanel.SetActive(tab == ShopManager.ShopTab.Buy);
        if (sellPanel != null) sellPanel.SetActive(tab == ShopManager.ShopTab.Sell);

        // 장바구니 패널도 적절히 설정
        if (buyCartPanel != null) buyCartPanel.SetActive(tab == ShopManager.ShopTab.Buy);
        if (sellCartPanel != null) sellCartPanel.SetActive(tab == ShopManager.ShopTab.Sell);

        // 탭 버튼 상태 업데이트 (활성화된 탭 시각적 표시)
        if (buyTabButton != null)
        {
            buyTabButton.interactable = tab != ShopManager.ShopTab.Buy;
        }

        if (sellTabButton != null)
        {
            sellTabButton.interactable = tab != ShopManager.ShopTab.Sell;
        }

        // ShopManager에 탭 변경 알림
        ShopManager.Instance.OnTabChanged(tab);
    }

    // UI 관리 메서드
    public void ActivateShopPanel(bool active)
    {
        shopPanel.SetActive(active);

        // 현재 탭에 따라 패널 활성화
        if (active)
        {
            SwitchTab(ShopManager.ShopTab.Buy); // 항상 구매 탭으로 시작
        }
        else
        {
            // 모든 패널 비활성화
            if (buyPanel != null) buyPanel.SetActive(false);
            if (sellPanel != null) sellPanel.SetActive(false);
            if (buyCartPanel != null) buyCartPanel.SetActive(false);
            if (sellCartPanel != null) sellCartPanel.SetActive(false);
        }
    }

    #region 장바구니 슬롯 관리
    // 장바구니 슬롯 관련 메서드
    public List<GameObject> GetBuyCartSlots() => buyCartSlots;
    public List<GameObject> GetSellCartSlots() => sellCartSlots;

    // 구매 장바구니 슬롯 생성
    public GameObject CreateBuyCartSlot()
    {
        if (buyCartSlotPrefab == null || buyCartContainer == null)
            return null;

        GameObject newSlot = Instantiate(buyCartSlotPrefab, buyCartContainer);
        ResetCartSlotUI(newSlot);
        buyCartSlots.Add(newSlot);
        Canvas.ForceUpdateCanvases();

        return newSlot;
    }

    // 판매 장바구니 슬롯 생성
    public GameObject CreateSellCartSlot()
    {
        if (sellCartSlotPrefab == null || sellCartContainer == null)
            return null;

        GameObject newSlot = Instantiate(sellCartSlotPrefab, sellCartContainer);
        ResetCartSlotUI(newSlot);
        sellCartSlots.Add(newSlot);
        Canvas.ForceUpdateCanvases();

        return newSlot;
    }

    // 슬롯 UI 초기화
    public void ResetCartSlotUI(GameObject slot)
    {
        if (slot == null) return;

        Button button = slot.transform.Find("Button")?.GetComponent<Button>();
        if (button == null) return;

        Image img = button.transform.Find("Image")?.GetComponent<Image>();
        if (img != null) img.enabled = false;

        TextMeshProUGUI text = button.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = "";
            text.enabled = false;
        }

        button.onClick.RemoveAllListeners();
        button.interactable = false;
    }

    // 장바구니 슬롯 UI 설정
    public void SetupCartSlot(GameObject slot, Sprite icon, int quantity, Action rightClickCallback, bool isBuyItem = true)
    {
        if (slot == null) return;

        Button button = slot.transform.Find("Button")?.GetComponent<Button>();
        if (button == null) return;

        Image img = button.transform.Find("Image")?.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = icon;
            img.enabled = true;

            // 구매/판매 아이템 색상 구분
            if (img.GetComponent<RectTransform>() != null)
            {
                img.color = isBuyItem ? Color.white : new Color(1f, 0.8f, 0.8f); // 판매는 약간 붉은 색조
            }
        }

        TextMeshProUGUI text = button.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"x{quantity}";
            text.enabled = true;
        }

        // 기존 이벤트 리스너 제거
        button.onClick.RemoveAllListeners();

        // 우클릭 이벤트 트리거 설정
        EventTrigger buttonTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (buttonTrigger == null)
            buttonTrigger = button.gameObject.AddComponent<EventTrigger>();

        buttonTrigger.triggers.Clear();

        EventTrigger.Entry rightClickEntry = new EventTrigger.Entry();
        rightClickEntry.eventID = EventTriggerType.PointerClick;
        rightClickEntry.callback.AddListener((data) =>
        {
            PointerEventData pointerData = (PointerEventData)data;
            if (pointerData.button == PointerEventData.InputButton.Right && rightClickCallback != null)
            {
                rightClickCallback();
            }
        });

        buttonTrigger.triggers.Add(rightClickEntry);

        // 버튼 활성화
        button.interactable = true;
    }

    // 모든 장바구니 슬롯 초기화
    public void ResetAllCartSlots()
    {
        ResetBuyCartSlots();
        ResetSellCartSlots();
    }

    // 구매 장바구니 슬롯 초기화
    public void ResetBuyCartSlots()
    {
        if (buyCartSlots.Count <= initialBuyCartSlotCount)
            return;

        for (int i = buyCartSlots.Count - 1; i >= initialBuyCartSlotCount; i--)
        {
            if (buyCartSlots[i] != null)
                Destroy(buyCartSlots[i]);
            buyCartSlots.RemoveAt(i);
        }

        Canvas.ForceUpdateCanvases();
    }

    // 판매 장바구니 슬롯 초기화
    public void ResetSellCartSlots()
    {
        if (sellCartSlots.Count <= initialSellCartSlotCount)
            return;

        for (int i = sellCartSlots.Count - 1; i >= initialSellCartSlotCount; i--)
        {
            if (sellCartSlots[i] != null)
                Destroy(sellCartSlots[i]);
            sellCartSlots.RemoveAt(i);
        }

        Canvas.ForceUpdateCanvases();
    }
    #endregion

    #region 상점 아이템 관리
    // 상점 아이템 클리어
    public void ClearShopItems()
    {
        foreach (Transform child in shopItemContainer)
            Destroy(child.gameObject);
    }

    // 아이템 컨테이너 반환
    public Transform GetShopItemContainer() => shopItemContainer;

    // 현재 활성화된 탭 반환
    public ShopManager.ShopTab GetCurrentTab() => currentTab;
    #endregion

    #region UI 업데이트 메서드
    // 기본 UI 업데이트 메서드
    public void UpdateShopName(string name) => shopNameText.text = name;
    public void UpdatePlayerGold(int gold) => playerGoldText.text = $"보유 골드: {gold}";

    // 장바구니 요약 정보 업데이트
    public void UpdateBuyCartSummary(int itemCount, int totalPrice)
    {
        if (buyCartTotalPriceText != null)
            buyCartTotalPriceText.text = $"총 구매비용: {totalPrice} 골드";
    }

    public void UpdateSellCartSummary(int itemCount, int totalPrice)
    {
        if (sellCartTotalPriceText != null)
            sellCartTotalPriceText.text = $"총 판매수입: {totalPrice} 골드";
    }
    #endregion

    #region 메시지 및 다이얼로그 표시
    // 알림 메시지 표시
    public void ShowMessage(string message, float delay, Action callback = null)
    {
        messageText.text = message;
        messagePanel.SetActive(true);
        StartCoroutine(HideMessageAfterDelay(delay, callback));
    }

    // 콜백이 있는 메시지 숨김 코루틴
    private IEnumerator HideMessageAfterDelay(float delay, Action callback = null)
    {
        yield return new WaitForSeconds(delay);
        messagePanel.SetActive(false);
        callback?.Invoke();
    }

    // 수량 선택 창 표시
    public void ShowQuantitySelector(string title, int defaultQuantity, int maxQuantity, Action<int> onConfirm)
    {
        HideAllDialogs();

        quantityTitleText.text = title;
        this.maxQuantity = Mathf.Max(1, maxQuantity);
        this.currentQuantity = Mathf.Clamp(defaultQuantity, 1, this.maxQuantity);
        this.onQuantityConfirm = onConfirm;
        quantityInput.text = currentQuantity.ToString();

        quantitySelectorPanel.SetActive(true);
    }

    // 확인 대화상자 표시
    public void ShowConfirmDialog(string message, Action onConfirm)
    {
        HideAllDialogs();
        confirmMessageText.text = message;
        this.onConfirmDialogConfirm = onConfirm;
        confirmDialogPanel.SetActive(true);
    }
    #endregion

    #region 아이템 정보 표시
    // 아이템 정보 패널 표시 (구매용)
    public void ShowBuyItemInfo(Item item, Vector3 position)
    {
        ItemInfoUIManager.Instance.ShowBuyItemInfo(item, position);
    }

    // 아이템 정보 패널 표시 (판매용)
    public void ShowSellItemInfo(Item item, Vector3 position)
    {
        // 인벤토리에 아이템이 있는지와 판매 가능 여부 확인
        bool itemExists = IsItemInInventory(item.ID) && item.CanSell;

        ItemInfoUIManager.Instance.ShowSellItemInfo(item, position, itemExists);
    }

        private bool IsItemInInventory(string itemId)
    {
        if (Inventory.Instance == null) return false;

        for (int i = 0; i < Inventory.Instance.InventorySize; i++)
        {
            Item item = Inventory.Instance.InventoryItems[i];
            if (item != null && item.ID == itemId)
                return true;
        }
        return false;
    }

    public void HideItemInfo()
    {
        ItemInfoUIManager.Instance.HideItemInfo();
    }
    #endregion

    #region 유틸리티 메서드
    // 모든 대화상자 숨기기
    private void HideAllDialogs()
    {
        quantitySelectorPanel.SetActive(false);
        confirmDialogPanel.SetActive(false);
    }

    // 마우스 이벤트 간섭 방지
    private void DisableRaycastTargets(Transform parent)
    {
        if (parent == null) return;

        if (parent.TryGetComponent(out Graphic graphic))
            graphic.raycastTarget = false;

        foreach (Transform child in parent)
            DisableRaycastTargets(child);
    }
    #endregion

    #region 수량 컨트롤
    // 수량 감소
    private void DecreaseQuantity()
    {
        if (currentQuantity > 1)
        {
            currentQuantity--;
            quantityInput.text = currentQuantity.ToString();
        }
    }

    // 수량 증가
    private void IncreaseQuantity()
    {
        if (currentQuantity < maxQuantity)
        {
            currentQuantity++;
            quantityInput.text = currentQuantity.ToString();
        }
    }

    // 수량 입력 변경 처리
    private void OnQuantityInputChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        if (int.TryParse(value, out int quantity))
        {
            currentQuantity = Mathf.Clamp(quantity, 1, maxQuantity);
            if (quantity != currentQuantity)
                quantityInput.text = currentQuantity.ToString();
        }
        else
            quantityInput.text = currentQuantity.ToString();
    }

    // 수량 입력 완료 처리
    private void OnQuantityInputEndEdit(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            currentQuantity = 1;
            quantityInput.text = currentQuantity.ToString();
        }
    }

    // 수량 확인
    private void ConfirmQuantity()
    {
        onQuantityConfirm?.Invoke(currentQuantity);
        quantitySelectorPanel.SetActive(false);
    }

    // 수량 취소
    private void CancelQuantity() => quantitySelectorPanel.SetActive(false);

    // 확인 다이얼로그 확인
    private void ConfirmDialog()
    {
        onConfirmDialogConfirm?.Invoke();
        confirmDialogPanel.SetActive(false);
    }

    // 확인 다이얼로그 취소
    private void CancelDialog() => confirmDialogPanel.SetActive(false);
    #endregion
}