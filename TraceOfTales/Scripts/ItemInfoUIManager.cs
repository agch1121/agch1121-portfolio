using TMPro;
using UnityEngine;
using UnityEngine.UI; // LayoutRebuilder 사용을 위해 필요

public class ItemInfoUIManager : CustomSingletone<ItemInfoUIManager>
{
    [SerializeField] private GameObject itemInfoPanel;
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI requiredLevelText;

    // 보상 정보를 위한 추가 필드
    private bool isShowingReward = false;
    private int currentRewardAmount = 0;
    private RewardType currentRewardType = RewardType.Gold;

    // 보상 정보 설정
    public void SetRewardAmount(int amount, RewardType type)
    {
        isShowingReward = true;
        currentRewardAmount = amount;
        currentRewardType = type;

        // 보상 정보 업데이트
        UpdateRewardInfo();
    }

    // 보상 정보 업데이트
    private void UpdateRewardInfo()
    {
        string typeText = "";
        switch (currentRewardType)
        {
            case RewardType.Gold:
                typeText = "골드";
                break;
            case RewardType.Exp:
                typeText = "경험치";
                break;
            case RewardType.Item:
                typeText = "개";
                break;
        }

        priceText.text = $"보상: {currentRewardAmount} {typeText}";
        priceText.color = Color.green;  // 보상은 녹색으로 표시
    }

    // 일반 아이템 정보 표시
    public void ShowItemInfo(Item item, Vector3 position)
    {
        if (item == null || itemInfoPanel == null) return;

        itemInfoPanel.transform.position = position;
        itemImage.sprite = item.Icon;
        itemNameText.text = item.Name;
        itemDescriptionText.text = item.Description;

        // 보상 정보가 표시 중이 아니면 기본 가격 표시
        if (!isShowingReward)
        {
            priceText.text = "";
        }

        if (item.RequiredLevel > 1)
        {
            requiredLevelText.text = $"필요 레벨: {item.RequiredLevel}";
            // 레벨 요구사항 충족 여부에 따라 색상 변경
            requiredLevelText.color = LevelCheck(item.RequiredLevel) ? Color.white : Color.red;
        }
        else
        {
            requiredLevelText.text = "";
        }

        itemInfoPanel.SetActive(true);
        // FIX: 레이아웃 강제 업데이트 추가
        if (itemInfoPanel.TryGetComponent<RectTransform>(out RectTransform rectTransform))
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        // END FIX
    }

    // 구매 아이템 정보
    public void ShowBuyItemInfo(Item item, Vector3 position)
    {
        if (item == null) return;

        isShowingReward = false;  // 보상 모드 해제

        itemInfoPanel.transform.position = position;
        itemImage.sprite = item.Icon;
        itemNameText.text = item.Name;

        string itemDescription = item.Description;
        string statsText = GetEquipmentStatsText(item);
        
        // 아이템에 스텟 정보가 있을 경우 추가로 설명 텍스트에 첨부, 없으면 생략
        if (!string.IsNullOrEmpty(statsText))
        {
            if (itemDescription.Length < 1)
            {
                if (item is Item_Equipment_Armor armorEquipment)
                    itemDescription += "<color=yellow>[장비 스텟]</color>\n" + statsText;

                else if (item is Item_Equipment_Weapon weaponEquipment)
                    itemDescription += "<color=yellow>[무기 스텟]</color>\n" + statsText;
            }
            else
            {
                if (item is Item_Equipment_Armor armorEquipment)
                    itemDescription += "\n<color=yellow>[장비 스텟]</color>\n" + statsText;

                else if (item is Item_Equipment_Weapon weaponEquipment)
                    itemDescription += "\n<color=yellow>[무기 스텟]</color>\n" + statsText;
            }
        }

        itemDescriptionText.text = itemDescription;

        // 가격 표시
        priceText.text = $"가격: {item.BasePrice} 골드";
        priceText.color = Color.white;

        if (item.RequiredLevel > 1)
        {
            requiredLevelText.text = $"필요 레벨: {item.RequiredLevel}";
            // 레벨 요구사항 충족 여부에 따라 색상 변경
            requiredLevelText.color = LevelCheck(item.RequiredLevel) ? Color.white : Color.red;
        }
        else
        {
            requiredLevelText.text = "";
        }

        itemInfoPanel.SetActive(true);
        // 레이아웃 강제 업데이트 추가
        if (itemInfoPanel.TryGetComponent<RectTransform>(out RectTransform rectTransform))
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    // 판매 아이템 정보
    public void ShowSellItemInfo(Item item, Vector3 position, bool itemExists)
    {
        if (item == null) return;

        isShowingReward = false;  // 보상 모드 해제

        itemInfoPanel.transform.position = position;
        itemImage.sprite = item.Icon;
        itemNameText.text = item.Name;

        string itemDescription = item.Description;
        string statsText = GetEquipmentStatsText(item);

        // 아이템에 스텟 정보가 있을 경우 추가로 설명 텍스트에 첨부, 없으면 생략
        if (!string.IsNullOrEmpty(statsText))
        {
            if (itemDescription.Length < 1)
            {
                if (item is Item_Equipment_Armor armorEquipment)
                    itemDescription += "<color=yellow>[장비 스텟]</color>\n" + statsText;

                else if (item is Item_Equipment_Weapon weaponEquipment)
                    itemDescription += "<color=yellow>[무기 스텟]</color>\n" + statsText;
            }
            else
            {
                if (item is Item_Equipment_Armor armorEquipment)
                    itemDescription += "\n<color=yellow>[장비 스텟]</color>\n" + statsText;

                else if (item is Item_Equipment_Weapon weaponEquipment)
                    itemDescription += "\n<color=yellow>[무기 스텟]</color>\n" + statsText;
            }
        }

        itemDescriptionText.text = itemDescription;

        if (itemExists)
        {
            priceText.text = $"판매 가격: {ShopManager.Instance.CalculateSellPrice(item)} 골드";
            priceText.color = Color.white;
        }
        else
        {
            priceText.text = "판매불가";
            priceText.color = Color.red;
        }

        if (item.RequiredLevel > 1)
        {
            requiredLevelText.text = $"필요 레벨: {item.RequiredLevel}";
            // 레벨 요구사항 충족 여부에 따라 색상 변경
            requiredLevelText.color = LevelCheck(item.RequiredLevel) ? Color.white : Color.red;
        }
        else
        {
            requiredLevelText.text = "";
        }

        itemInfoPanel.SetActive(true);
        // FIX: 레이아웃 강제 업데이트 추가
        if (itemInfoPanel.TryGetComponent<RectTransform>(out RectTransform rectTransform))
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        // END FIX
    }

    // 장비 정보 표시 (스텟 포함)
    public void ShowEquipmentInfo(Item equipment, Vector3 position)
    {
        if (equipment == null || itemInfoPanel == null) return;

        itemInfoPanel.transform.position = position;
        itemImage.sprite = equipment.Icon;
        itemNameText.text = equipment.Name;

        // 장비 스텟 정보 생성
        string equipmentDescription = equipment.Description;
        string statsText = GetEquipmentStatsText(equipment);

        if (!string.IsNullOrEmpty(statsText))
        {
            if (equipment.Description == null)
            {
                if (equipment is Item_Equipment_Armor armorEquipment)
                    equipmentDescription += "<color=yellow>[장비 스텟]</color>\n" + statsText;

                else if (equipment is Item_Equipment_Weapon weaponEquipment)
                    equipmentDescription += "<color=yellow>[무기 스텟]</color>\n" + statsText;
            }
            else
            {
                if (equipment is Item_Equipment_Armor armorEquipment)
                    equipmentDescription += "\n<color=yellow>[장비 스텟]</color>\n" + statsText;

                else if (equipment is Item_Equipment_Weapon weaponEquipment)
                    equipmentDescription += "\n<color=yellow>[무기 스텟]</color>\n" + statsText;
            }
        }

        itemDescriptionText.text = equipmentDescription;

        // 보상 정보가 표시 중이 아니면 기본 가격 표시
        if (!isShowingReward)
        {
            priceText.text = "";
        }

        if (equipment.RequiredLevel >= 1)
        {
            requiredLevelText.text = $"필요 레벨: {equipment.RequiredLevel}";
            // 레벨 요구사항 충족 여부에 따라 색상 변경
            requiredLevelText.color = LevelCheck(equipment.RequiredLevel) ? Color.white : Color.red;
        }
        else
        {
            requiredLevelText.text = "";
        }

        itemInfoPanel.SetActive(true);
        // FIX: 레이아웃 강제 업데이트 추가 (가장 중요)
        if (itemInfoPanel.TryGetComponent<RectTransform>(out RectTransform rectTransform))
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        // END FIX
    }

    // 장비 스텟 텍스트 생성
    private string GetEquipmentStatsText(Item equipment)
    {
        string statsText = "";

        // 장비 아이템인지 확인
        if (equipment is Item_Equipment_Armor armorEquipment)
        {
            statsText = GetArmorStatsText(armorEquipment);
        }
        else if (equipment is Item_Equipment_Weapon weaponEquipment)
        {
            statsText = GetWeaponStatsText(weaponEquipment);
        }

        return statsText;
    }

    // 방어구 스텟 텍스트 생성
    private string GetArmorStatsText(Item_Equipment_Armor armor)
    {
        string statsText = "";

        // 방어력 (장비 주요 스텟)
        if (armor.armorBonus > 0)
            statsText += $"방어력: +{armor.armorBonus}\n";

        // HP/MP
        if (armor.hpBonus > 0)
            statsText += $"MaxHP: +{armor.hpBonus}\n";
        if (armor.mpBonus > 0)
            statsText += $"MaxMP: +{armor.mpBonus}\n";

        // 기본 능력치
        if (armor.strBonus > 0)
            statsText += $"힘: +{armor.strBonus}\n";
        if (armor.dexBonus > 0)
            statsText += $"민첩: +{armor.dexBonus}\n";
        if (armor.intBonus > 0)
            statsText += $"지능: +{armor.intBonus}\n";
        if (armor.vitBonus > 0)
            statsText += $"체력: +{armor.vitBonus}\n";

        // 회피율, 이동속도
        if (armor.evasionBonus > 0)
            statsText += $"회피율: +{armor.evasionBonus}%\n";
        if (armor.moveSpeedBonus > 0)
            statsText += $"이동속도: +{armor.moveSpeedBonus}\n";

        return statsText.TrimEnd('\n');
    }

    // 무기 스텟 텍스트 생성
    private string GetWeaponStatsText(Item_Equipment_Weapon weapon)
    {
        string statsText = "";

        // 무기 데미지 관련 (무기 주요 스탯)
        if (weapon.meleeDamageBonus > 0)
            statsText += $"근접 데미지: +{weapon.meleeDamageBonus}\n";
        if (weapon.magicDamageBonus > 0)
            statsText += $"마법 데미지: +{weapon.magicDamageBonus}\n";
        if (weapon.rangeDamageBonus > 0)
            statsText += $"원거리 데미지: +{weapon.rangeDamageBonus}\n";

        // 방어력
        if (weapon.armorBonus > 0)
            statsText += $"방어력: +{weapon.armorBonus}\n";

        // 기본 능력치
        if (weapon.strBonus > 0)
            statsText += $"힘: +{weapon.strBonus}\n";
        if (weapon.dexBonus > 0)
            statsText += $"민첩: +{weapon.dexBonus}\n";
        if (weapon.intBonus > 0)
            statsText += $"지능: +{weapon.intBonus}\n";
        if (weapon.vitBonus > 0)
            statsText += $"체력: +{weapon.vitBonus}\n";

        // 크리티컬 관련
        if (weapon.criticalChanceBonus > 0)
            statsText += $"크리티컬 확률: +{weapon.criticalChanceBonus}%\n";
        if (weapon.criticalDamageBonus > 0)
            statsText += $"크리티컬 데미지: +{weapon.criticalDamageBonus}%\n";

        // 회피율, 이동속도, 공격속도
        if (weapon.evasionBonus > 0)
            statsText += $"회피율: +{weapon.evasionBonus}%\n";
        if (weapon.moveSpeedBonus > 0)
            statsText += $"이동속도: +{weapon.moveSpeedBonus}\n";
        if (weapon.attackSpeedBonus > 0)
            statsText += $"공격속도: +{weapon.attackSpeedBonus}\n";

        return statsText.TrimEnd('\n');
    }

    public void HideItemInfo()
    {
        // null 체크 추가
        if (itemInfoPanel != null)
        {
            itemInfoPanel.SetActive(false);
        }
        isShowingReward = false;  // 보상 모드 초기화
    }

    public bool LevelCheck(int level)
    {
        int playerLevel = GameManager.Instance.Player.StatsManager.GetLevel();
        bool levelMatch;

        levelMatch = playerLevel >= level;

        return levelMatch;
    }
}