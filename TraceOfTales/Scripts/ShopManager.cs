using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : CustomSingletone<ShopManager>
{
    [Header("References")]
    [SerializeField] private GameObject shopItemCardPrefab;
    [SerializeField] private ShopUI shopUI;

    private ShopDefinition currentShop;
    private NPCMovement currentNpcMovement;

    // 현재 상점이 열려있는지 확인하는 속성
    public bool IsShopOpen { get; private set; } = false;

    // 구매 및 판매 장바구니 데이터 구조체
    [System.Serializable]
    public struct CartItem
    {
        public Item Item; // 구매용 아이템
        public Item InventoryItem; // 판매용 아이템
        public int Quantity;
        public int SlotIndex; // 인벤토리 슬롯 인덱스 (판매용)
        public bool IsBuyItem; // true: 구매용, false: 판매용

        // 총 가격 계산 메서드
        public int GetTotalPrice()
        {
            if (IsBuyItem)
            {
                return Item != null ? Item.BasePrice * Quantity : 0;
            }
            else
            {
                return InventoryItem != null ? ShopManager.Instance.CalculateSellPrice(InventoryItem) * Quantity : 0;
            }
        }

        // 구매용 아이템 생성자
        public CartItem(Item item, int quantity = 1)
        {
            Item = item;
            InventoryItem = null;
            Quantity = Mathf.Clamp(quantity, 1, item.MaxStack);
            SlotIndex = -1;
            IsBuyItem = true;
        }

        // 판매용 아이템 생성자
        public CartItem(Item item, int slotIndex, int quantity = 1)
        {
            Item = null;
            InventoryItem = item;
            Quantity = Mathf.Clamp(quantity, 1, item.Quantity);
            SlotIndex = slotIndex;
            IsBuyItem = false;
        }
    }

    // 구매/판매 장바구니
    private List<CartItem> buyCartItems = new List<CartItem>();
    private List<CartItem> sellCartItems = new List<CartItem>();

    // 상점 탭 열거형
    public enum ShopTab
    {
        Buy,
        Sell
    }

    #region 상점 초기화 및 관리
    // 상점 초기화
    public void Initialize(ShopDefinition shopDefinition, NPCMovement npcMovement)
    {
        currentShop = shopDefinition;
        currentNpcMovement = npcMovement;
        IsShopOpen = true;

        ClearAllCartsWithoutMessage();
        shopUI.UpdateShopName(currentShop.ShopName);
        shopUI.UpdatePlayerGold(GetPlayerGold());
        InitializeShopItems();
        shopUI.ActivateShopPanel(true);
    }

    // 상점 열기
    public void OpenShop(ShopDefinition shopDefinition, NPCMovement npcMovement = null)
    {
        Initialize(shopDefinition, npcMovement);
    }

    // 상점 닫기
    public void CloseShop()
    {
        if (currentNpcMovement != null)
        {
            currentNpcMovement.IsInteract = false;
            currentNpcMovement = null;
        }

        currentShop = null;
        IsShopOpen = false;
        ClearAllCartsWithoutMessage();
        shopUI.ActivateShopPanel(false);
    }

    #endregion

    #region 아이템 목록 관리
    // 상점 아이템 로드
    private void InitializeShopItems()
    {
        shopUI.ClearShopItems();
        Transform shopItemContainer = shopUI.GetShopItemContainer();

        if (currentShop?.ShopItems != null && shopItemContainer != null)
        {
            foreach (Item item in currentShop.ShopItems)
            {
                if (item == null) continue;
                GameObject card = Instantiate(shopItemCardPrefab, shopItemContainer);
                if (card?.GetComponent<ShopItemCard>() is ShopItemCard itemCard)
                    itemCard.Initialize(item, ShopTab.Buy);
            }
        }
    }

    // 판매 탭에서 상점 아이템 로드
    private void InitializeSellTabItems()
    {
        shopUI.ClearShopItems();
        Transform shopItemContainer = shopUI.GetShopItemContainer();

        if (shopItemContainer == null || Inventory.Instance == null)
        {
            Debug.LogWarning("상점 컨테이너나 인벤토리가 없습니다.");
            return;
        }

        // 인벤토리에서 판매 가능한 모든 아이템을 찾아서 표시
        Dictionary<string, Item> uniqueItems = new Dictionary<string, Item>();

        // 인벤토리에서 판매 가능한 아이템들을 고유 ID로 구분하여 수집
        for (int i = 0; i < Inventory.Instance.InventorySize; i++)
        {
            Item inventoryItem = Inventory.Instance.InventoryItems[i];
            if (inventoryItem != null && inventoryItem.CanSell && !uniqueItems.ContainsKey(inventoryItem.ID))
            {
                uniqueItems.Add(inventoryItem.ID, inventoryItem);
            }
        }

        // 고유 아이템들을 판매 탭에 표시
        foreach (var item in uniqueItems.Values)
        {
            GameObject card = Instantiate(shopItemCardPrefab, shopItemContainer);
            if (card?.GetComponent<ShopItemCard>() is ShopItemCard itemCard)
            {
                itemCard.InitForSellOnly(item);
            }
        }
    }

    // 탭 변경 이벤트 처리 메서드
    public void OnTabChanged(ShopTab tab)
    {
        // 탭에 따라 UI 새로고침
        if (tab == ShopTab.Buy)
        {
            InitializeShopItems();
        }
        else if (tab == ShopTab.Sell)
        {
            InitializeSellTabItems();
        }
    }
    #endregion

    #region 장바구니 관리
    // 구매 장바구니에 아이템 추가
    public void AddToBuyCart(Item item, int quantity = 1)
    {
        if (item == null) return;

        // 같은 아이템이 이미 장바구니에 있는지 확인
        int existingIndex = buyCartItems.FindIndex(cartItem => cartItem.Item != null && cartItem.Item.ID == item.ID);

        if (existingIndex >= 0) // 장바구니에 이미 있는 경우
        {
            CartItem existingItem = buyCartItems[existingIndex];
            existingItem.Quantity = Mathf.Min(existingItem.Quantity + quantity, item.MaxStack);
            buyCartItems[existingIndex] = existingItem;
        }
        else // 새 아이템 추가
        {
            buyCartItems.Add(new CartItem(item, quantity));
        }

        UpdateBuyCartPanel();
    }

    // 판매 장바구니에 아이템 추가
    public void AddToSellCart(Item item, int slotIndex, int quantity = 1)
    {
        if (item == null || !IsItemInInventory(item, slotIndex)) return;

        // 인벤토리의 실제 아이템 가져오기
        Item inventoryItem = Inventory.Instance.InventoryItems[slotIndex];

        // 인벤토리 아이템과 장바구니에 이미 담긴 수량 확인
        int alreadyInCartQuantity = GetItemQuantityInSellCart(slotIndex);
        int availableQuantity = inventoryItem.Quantity - alreadyInCartQuantity;

        // 장바구니에 이미 모든 수량이 담겼으면 메시지 표시 후 종료
        if (availableQuantity <= 0)
        {
            shopUI.ShowMessage("이미 모든 수량이 장바구니에 담겨있습니다!", 1f);
            return;
        }

        // 수량 제한
        if (quantity > availableQuantity)
        {
            quantity = availableQuantity;
        }

        // 같은 슬롯 아이템이 이미 장바구니에 있는지 확인
        int existingIndex = sellCartItems.FindIndex(cartItem =>
            cartItem.InventoryItem != null && cartItem.SlotIndex == slotIndex);

        if (existingIndex >= 0) // 장바구니에 이미 있는 경우
        {
            CartItem existingItem = sellCartItems[existingIndex];
            existingItem.Quantity = Mathf.Min(existingItem.Quantity + quantity, inventoryItem.Quantity);
            sellCartItems[existingIndex] = existingItem;
        }
        else // 새 아이템 추가
        {
            sellCartItems.Add(new CartItem(inventoryItem, slotIndex, quantity));
        }

        UpdateSellCartPanel();
    }

    // 구매 장바구니에서 아이템 제거
    public void RemoveFromBuyCart(int cartIndex)
    {
        if (cartIndex >= 0 && cartIndex < buyCartItems.Count)
        {
            buyCartItems.RemoveAt(cartIndex);
            UpdateBuyCartPanel();
        }
    }

    // 판매 장바구니에서 아이템 제거
    public void RemoveFromSellCart(int cartIndex)
    {
        if (cartIndex >= 0 && cartIndex < sellCartItems.Count)
        {
            sellCartItems.RemoveAt(cartIndex);
            UpdateSellCartPanel();
        }
    }

    // 장바구니 업데이트 메서드
    private void UpdateBuyCartPanel()
    {
        StartCoroutine(UpdateCartPanelCoroutine(true));
    }

    private void UpdateSellCartPanel()
    {
        StartCoroutine(UpdateCartPanelCoroutine(false));
    }

    // 장바구니 업데이트 코루틴 (구매/판매 공통)
    private IEnumerator UpdateCartPanelCoroutine(bool isBuyCart)
    {
        List<GameObject> cartSlots = isBuyCart ? shopUI.GetBuyCartSlots() : shopUI.GetSellCartSlots();
        List<CartItem> cartItems = isBuyCart ? buyCartItems : sellCartItems;
        if (cartSlots == null || cartSlots.Count == 0) yield break;

        // 모든 슬롯 초기화
        foreach (GameObject slot in cartSlots)
            shopUI.ResetCartSlotUI(slot);

        // 슬롯 확대 필요시
        if (cartItems.Count > cartSlots.Count)
        {
            int targetSlotCount = Mathf.Max(cartSlots.Count * 2, cartItems.Count);
            for (int i = cartSlots.Count; i < targetSlotCount; i++)
                if (isBuyCart) shopUI.CreateBuyCartSlot(); else shopUI.CreateSellCartSlot();

            yield return null; // 한 프레임 대기
            cartSlots = isBuyCart ? shopUI.GetBuyCartSlots() : shopUI.GetSellCartSlots();
        }

        // 사용할 슬롯에 아이템 할당 부분 수정
        for (int i = 0; i < cartItems.Count && i < cartSlots.Count; i++)
        {
            int index = i;
            CartItem cartItem = cartItems[i];
            shopUI.SetupCartSlot(
                cartSlots[i],
                isBuyCart ? cartItem.Item.Icon : cartItem.InventoryItem.Icon,
                cartItem.Quantity,
                () =>
                {
                    if (isBuyCart)
                        RemoveFromBuyCart(index);
                    else
                        RemoveFromSellCart(index);
                },
                isBuyCart
            );
        }

        // 장바구니 요약 정보 업데이트
        UpdateCartSummary(cartItems, isBuyCart);
    }

    // 장바구니 요약 정보 업데이트
    private void UpdateCartSummary(List<CartItem> items, bool isBuyCart)
    {
        int totalItems = 0, totalPrice = 0;
        foreach (CartItem item in items)
        {
            totalItems += item.Quantity;
            totalPrice += item.GetTotalPrice();
        }

        if (isBuyCart)
            shopUI.UpdateBuyCartSummary(totalItems, totalPrice);
        else
            shopUI.UpdateSellCartSummary(totalItems, totalPrice);
    }

    // 모든 장바구니 비우기 (메시지 표시)
    public void ClearAllCarts()
    {
        ShopTab currentTab = shopUI.GetCurrentTab();

        if (currentTab == ShopTab.Buy)
        {
            // 구매 장바구니만 비우기
            buyCartItems.Clear();
            shopUI.ResetBuyCartSlots();
            UpdateBuyCartPanel();
            shopUI.ShowMessage("구매 장바구니를 비웠습니다.", 1f);
        }
        else if (currentTab == ShopTab.Sell)
        {
            // 판매 장바구니만 비우기
            sellCartItems.Clear();
            shopUI.ResetSellCartSlots();
            UpdateSellCartPanel();
            shopUI.ShowMessage("판매 장바구니를 비웠습니다.", 1f);
        }
    }

    // 모든 장바구니 비우기 (메시지 표시 X - 상점 첫 시작시)
    private void ClearAllCartsWithoutMessage()
    {
        buyCartItems.Clear();
        sellCartItems.Clear();
        shopUI.ResetAllCartSlots();
        UpdateBuyCartPanel();
        UpdateSellCartPanel();
    }

    // 판매 장바구니에 이미 담긴 아이템 수량 확인
    private int GetItemQuantityInSellCart(int slotIndex)
    {
        int quantity = 0;
        foreach (CartItem cartItem in sellCartItems)
        {
            if (cartItem.SlotIndex == slotIndex)
            {
                quantity += cartItem.Quantity;
            }
        }
        return quantity;
    }
    #endregion

    #region 계산 및 정보 처리 메서드
    // UI 업데이트
    public void UpdateShopUI()
    {
        shopUI.UpdatePlayerGold(GetPlayerGold());
        UpdateBuyCartPanel();
        UpdateSellCartPanel();
    }

    // 아이템 ID로 상점 아이템 찾기
    public Item FindShopItemById(string itemId)
    {
        if (currentShop?.ShopItems == null) return null;

        foreach (Item item in currentShop.ShopItems)
            if (item != null && item.ID == itemId)
                return item;
        return null;
    }

    // 플레이어 골드 가져오기
    private int GetPlayerGold()
    {
        return GoldManager.Instance.Gold;
    }

    // 아이템 구매 가능 여부 확인
    public bool CanBuyItem(Item item, int quantity = 1)
    {
        // 1. 빈 슬롯 수 확인
        int emptySlots = CountEmptyInventorySlots();

        // 2. 스택 불가능한 아이템
        if (!item.IsStackable)
        {
            return quantity <= emptySlots;
        }

        // 3. 스택 가능한 아이템
        // 3-1. 기존 스택에 채우고 남은 수량 계산
        int remainingQuantity = ReduceQuantityForStackableItems(item.ID, quantity);

        // 3-2. 남은 수량이 없으면 (기존 스택에 다 들어감) -> 구매 가능
        if (remainingQuantity <= 0)
        {
            return true;
        }

        // 3-3. 남은 아이템을 담기 위해 '새로 필요한 슬롯 수' 계산
        // (MaxStack이 0이나 음수가 되는 것을 방지)
        int maxStackPerSlot = Mathf.Max(1, item.MaxStack);
        int requiredNewSlots = Mathf.CeilToInt((float)remainingQuantity / maxStackPerSlot);

        // 3-4. 필요한 새 슬롯 수 vs 실제 빈 슬롯 수 비교
        return requiredNewSlots <= emptySlots;
    }

    // 비어있는 인벤토리 슬롯 개수 계산
    private int CountEmptyInventorySlots()
    {
        int count = 0;
        for (int i = 0; i < Inventory.Instance.InventorySize - 1; i++)
            if (Inventory.Instance.InventoryItems[i] == null)
                count++;
        return count;
    }

    // 스택 가능한 아이템의 필요 슬롯 수 감소 계산
    private int ReduceQuantityForStackableItems(string itemId, int quantity)
    {
        List<int> sameItemSlots = CheckItemSlotsInInventory(itemId);
        foreach (int slotIndex in sameItemSlots)
        {
            Item inventoryItem = Inventory.Instance.InventoryItems[slotIndex];
            if (inventoryItem.Quantity < inventoryItem.MaxStack)
            {
                int remainingSpace = inventoryItem.MaxStack - inventoryItem.Quantity;
                quantity -= remainingSpace;
                if (quantity <= 0) break;
            }
        }
        return quantity;
    }

    // 판매 가격 계산 
    public int CalculateSellPrice(Item item)
    {
        // 상점 아이템이 있는 경우 해당 정보 사용
        Item relatedShopItem = FindShopItemById(item.ID);
        if (relatedShopItem != null)
        {
            int basePrice = relatedShopItem.BasePrice;
            int ratio = relatedShopItem.SellPriceRatio;
            return (basePrice * ratio) / 100;
        }

        // 상점 아이템이 없는 경우 아이템의 기본 가격 사용
        return item.BasePrice / 2; // 아이템 기본 가격의 절반으로 판매 가격 설정
    }

    // 인벤토리에서 특정 아이템 ID를 가진 슬롯 찾기
    private List<int> CheckItemSlotsInInventory(string itemId)
    {
        List<int> slots = new List<int>();
        for (int i = 0; i < Inventory.Instance.InventorySize; i++)
        {
            Item item = Inventory.Instance.InventoryItems[i];
            if (item != null && item.ID == itemId)
                slots.Add(i);
        }
        return slots;
    }

    // 인벤토리에서 특정 아이템 ID를 가진 모든 슬롯 찾기
    public List<int> FindMatchingSlotsInInventory(string itemId)
    {
        List<int> matchingSlots = new List<int>();

        if (string.IsNullOrEmpty(itemId) || Inventory.Instance == null)
        {
            return matchingSlots;
        }

        for (int i = 0; i < Inventory.Instance.InventorySize; i++)
        {
            Item item = Inventory.Instance.InventoryItems[i];
            if (item != null && item.ID == itemId)
            {
                matchingSlots.Add(i);
            }
        }

        return matchingSlots;
    }

    // 인벤토리에 아이템이 있는지 확인
    private bool IsItemInInventory(Item item, int slotIndex)
    {
        if (item == null || Inventory.Instance == null) return false;

        // 슬롯 인덱스가 유효한지 확인
        if (slotIndex < 0 || slotIndex >= Inventory.Instance.InventorySize)
        {
            return false;
        }

        // 해당 슬롯에 아이템이 있는지 확인
        Item inventoryItem = Inventory.Instance.InventoryItems[slotIndex];

        if (inventoryItem == null)
        {
            return false;
        }

        // 아이템 ID가 일치하는지 확인
        if (inventoryItem.ID != item.ID)
        {
            return false;
        }

        return true;
    }
    #endregion

    #region 구매/판매 실행
    // 구매 실행 확인창 표시
    public void ExecutePurchase()
    {
        if (buyCartItems.Count == 0)
        {
            shopUI.ShowMessage("구매할 아이템이 없습니다!", 1f);
            return;
        }

        int totalPrice = 0;
        foreach (CartItem item in buyCartItems)
            totalPrice += item.GetTotalPrice();

        // 골드 확인
        if (GetPlayerGold() < totalPrice)
        {
            shopUI.ShowMessage("골드가 부족합니다!", 1f);
            return;
        }

        // 확인 대화상자 표시
        string message = $"총 {totalPrice} 골드를 지불하고 선택한 아이템을 구매하시겠습니까?";
        shopUI.ShowConfirmDialog(message, () => CompletePurchase(totalPrice));
    }

    // 구매 실행 처리
    private void CompletePurchase(int totalPrice)
    {
        // 인벤토리 공간 확인
        bool canFit = true;
        Dictionary<string, int> itemQuantities = new Dictionary<string, int>();

        foreach (CartItem cartItem in buyCartItems)
        {
            string itemId = cartItem.Item.ID;
            if (!itemQuantities.ContainsKey(itemId))
                itemQuantities[itemId] = 0;
            itemQuantities[itemId] += cartItem.Quantity;
        }

        foreach (var kvp in itemQuantities)
        {
            Item shopItem = FindShopItemById(kvp.Key);
            if (shopItem != null && !CanBuyItem(shopItem, kvp.Value))
            {
                canFit = false;
                break;
            }
        }

        if (!canFit)
        {
            shopUI.ShowMessage("인벤토리 공간이 부족합니다!", 1f);
            return;
        }

        // 골드 사용
        if (!GoldManager.Instance.UseGold(totalPrice))
        {
            shopUI.ShowMessage("골드 처리 중 오류가 발생했습니다!", 1f);
            return;
        }

        // 아이템 인벤토리에 추가
        foreach (CartItem item in buyCartItems)
            Inventory.Instance.AddItem(item.Item, item.Quantity);

        // 장바구니 비우기
        buyCartItems.Clear();
        shopUI.ResetBuyCartSlots();
        UpdateBuyCartPanel();

        // UI 업데이트
        UpdateShopUI();
        shopUI.ShowMessage($"{totalPrice} 골드를 지불하고 아이템을 구매했습니다!", 1f);
    }

    // 판매 실행 확인창 표시
    public void ExecuteSell()
    {
        if (sellCartItems.Count == 0)
        {
            shopUI.ShowMessage("판매할 아이템이 없습니다!", 1f);
            return;
        }

        int totalPrice = 0;
        foreach (CartItem item in sellCartItems)
            totalPrice += item.GetTotalPrice();

        // 확인 대화상자 표시
        string message = $"선택한 아이템을 판매하여 {totalPrice} 골드를 받으시겠습니까?";
        shopUI.ShowConfirmDialog(message, () => CompleteSell(totalPrice));
    }

    // 판매 실행 처리
    private void CompleteSell(int totalPrice)
    {
        // 판매 아이템을 슬롯 인덱스 기준으로 정렬 (큰 인덱스부터 처리하기 위해 역순 정렬)
        List<CartItem> sortedItems = new List<CartItem>(sellCartItems);
        sortedItems.Sort((a, b) => b.SlotIndex.CompareTo(a.SlotIndex));

        // 골드 추가
        GoldManager.Instance.AddGold(totalPrice);

        // 인벤토리에서 아이템 제거
        foreach (CartItem item in sortedItems)
        {
            Item inventoryItem = Inventory.Instance.InventoryItems[item.SlotIndex];

            if (inventoryItem.Quantity <= item.Quantity)
                Inventory.Instance.RemoveItem(item.SlotIndex);
            else
            {
                inventoryItem.Quantity -= item.Quantity;
                InventoryUI.Instance.DrawItem(inventoryItem, item.SlotIndex);
            }
        }

        // 장바구니 비우기
        sellCartItems.Clear();
        shopUI.ResetSellCartSlots();
        UpdateSellCartPanel();

        // UI 업데이트
        UpdateShopUI();

        // 판매 탭일 경우 아이템 목록 새로고침
        if (shopUI.GetCurrentTab() == ShopTab.Sell)
        {
            InitializeSellTabItems();
        }

        shopUI.ShowMessage($"아이템을 판매하여 {totalPrice} 골드를 얻었습니다!", 1f);
    }
    #endregion

    #region 수량 선택 및 아이템 클릭 처리
    // 수량 선택 창 표시 (구매용)
    public void ShowBuyQuantitySelector(Item item)
    {
        shopUI.ShowQuantitySelector(
            $"{item.Name}을 몇 개 담으시겠습니까?",
            1, item.MaxStack,
            (quantity) => AddToBuyCart(item, quantity)
        );
    }

    // 수량 선택 창 표시 (판매용)
    public void ShowSellQuantitySelector(Item item, int slotIndex, int maxAvailable = -1)
    {
        // 실제 가능한 최대 수량 계산
        int maxQuantity = maxAvailable > 0 ? maxAvailable : item.Quantity;

        shopUI.ShowQuantitySelector(
            $"{item.Name}을 몇 개 판매하시겠습니까?",
            1, maxQuantity,
            (quantity) => AddToSellCart(item, slotIndex, quantity)
        );
    }

    // 아이템 클릭 처리 함수
    public void OnItemClicked(object itemData, int slotIndex = -1, bool isRightClick = false, bool isShiftPressed = false)
    {
        ShopTab currentTab = shopUI.GetCurrentTab();

        // 판매 탭에서의 동작 처리
        if (currentTab == ShopTab.Sell && !isRightClick)
        {
            // 상점 아이템인 경우
            if (itemData is ShopItem shopItem)
            {
                AddItemToSellCartById(shopItem.ItemID, isShiftPressed);
                return;
            }
            // 모든 타입의 Item 또는 Item 서브클래스의 경우
            else if (itemData is Item item)
            {
                if (slotIndex >= 0)
                {
                    // 인벤토리에서 직접 선택한 경우 (슬롯 인덱스가 있음)
                    if (isShiftPressed)
                        ShowSellQuantitySelector(item, slotIndex);
                    else
                        AddToSellCart(item, slotIndex, 1);
                }
                else
                {
                    // 판매 탭에서 선택한 경우 (슬롯 인덱스가 없음)
                    AddItemToSellCartById(item.ID, isShiftPressed);
                }
                return;
            }
        }

        // 구매 탭에서의 동작 처리
        if (currentTab == ShopTab.Buy && !isRightClick)
        {
            if (itemData is Item buyShopItem)
            {
                if (isShiftPressed)
                    ShowBuyQuantitySelector(buyShopItem);
                else
                    AddToBuyCart(buyShopItem, 1);
                return;
            }
        }
    }

    // 인벤토리에서 동일한 아이템 ID를 가진 아이템을 찾아 판매 장바구니에 추가
    private void AddItemToSellCartById(string itemId, bool showQuantitySelector)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        // 인벤토리에서 동일한 아이템 ID를 가진 모든 슬롯 찾기
        List<int> matchingSlots = FindMatchingSlotsInInventory(itemId);

        if (matchingSlots.Count == 0)
        {
            shopUI.ShowMessage("이 아이템이 인벤토리에 없습니다.", 1f);
            return;
        }

        // 첫 번째 일치하는 슬롯 사용
        int slotIndex = matchingSlots[0];
        Item inventoryItem = Inventory.Instance.InventoryItems[slotIndex];

        // 장바구니에 이미 담긴 수량 확인
        int alreadyInCartQuantity = GetItemQuantityInSellCart(slotIndex);
        int availableQuantity = inventoryItem.Quantity - alreadyInCartQuantity;

        // 이미 모든 수량이 장바구니에 담겼있는 경우
        if (availableQuantity <= 0)
        {
            if (inventoryItem.IsStackable)
            {
                shopUI.ShowMessage("이미 모든 수량이 장바구니에 담겨있습니다!", 1f);
                return;
            }
            else
            {
                shopUI.ShowMessage("해당 아이템은 1개만 담을 수 있습니다!", 1f);
                return;
            }
        }

        if (showQuantitySelector && availableQuantity > 1 && inventoryItem.IsStackable)
            ShowSellQuantitySelector(inventoryItem, slotIndex, availableQuantity);
        else
            AddToSellCart(inventoryItem, slotIndex, 1);
    }
    #endregion
}