using UnityEngine;

[CreateAssetMenu(fileName = "New ShopItem", menuName = "Shop/Item")]
public class ShopItem : ScriptableObject
{
    [Header("Item Info")]
    [SerializeField] private Item baseItem;

    [Header("Price")]
    public int SellPriceRatio; // 판매 가격 비율 (% 단위 => 50 = 50%)


    public string ItemID => baseItem.ID;
    public string ItemName => baseItem.Name;
    public Sprite ItemIcon => baseItem.Icon;
    public string ItemDescription => baseItem.Description;
    public ItemType ItemType => baseItem.ItemType;
    public Item BaseItem => baseItem;
}
