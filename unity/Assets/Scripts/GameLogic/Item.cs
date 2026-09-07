/*
 * Item.cs
 *
 * 📝 역할: include/Item.h + src/Item.cpp 그대로 포팅.
 * 콘솔 출력(displayInfo)은 콘솔 텍스트에 특화된 기능이라 포팅하지 않고,
 * 대신 UI가 각 필드를 그대로 읽어 화면에 표시한다.
 */

namespace TextRPG.GameLogic
{
    public class Item
    {
        private readonly string name;
        private readonly ItemType type;
        private readonly int value;
        private readonly int price;
        private readonly string description;

        public Item(string itemName, ItemType itemType, int itemValue, int itemPrice, string desc)
        {
            name = itemName;
            type = itemType;
            value = itemValue;
            price = itemPrice;
            description = desc;
        }

        public string GetName() => name;
        public ItemType GetItemType() => type;
        public int GetValue() => value;
        public int GetPrice() => price;
        public string GetDescription() => description;

        /// <summary>
        /// src/Item.cpp의 getTypeAsString() 그대로 — 화면 표시용 한글 라벨.
        /// </summary>
        public string GetTypeAsString()
        {
            switch (type)
            {
                case ItemType.WEAPON:
                    return "무기";
                case ItemType.ARMOR:
                    return "방어구";
                case ItemType.POTION:
                    return "포션";
                case ItemType.CONSUMABLE:
                    return "소비 아이템";
                default:
                    return "알 수 없음";
            }
        }
    }
}
