using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shop", menuName = "Shop/Definition")]
public class ShopDefinition : ScriptableObject
{
    [Header("Shop Info")]
    public string ShopID;
    public string ShopName;
    public ShopType ShopTypes;

    [Header("Items")]
    public List<Item> ShopItems = new List<Item>();

    // 상점 타입 - 아이템 필터링에 사용
    public enum ShopType
    {
        General,    // 잡화
        Weapon,     // 무기
        Armor,      // 방어구
        Potion,     // 물약
        Accessory   // 악세서리
    }
}