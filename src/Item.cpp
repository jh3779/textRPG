#include "../include/Item.h"

Item::Item(const std::string& itemName, ItemType itemType, int itemValue,
           int itemPrice, const std::string& desc)
    : name(itemName),
      type(itemType),
      value(itemValue),
      price(itemPrice),
      description(desc) {
}

std::string Item::getName() const {
    return name;
}

ItemType Item::getType() const {
    return type;
}

int Item::getValue() const {
    return value;
}

int Item::getPrice() const {
    return price;
}

std::string Item::getDescription() const {
    return description;
}

void Item::displayInfo() const {
    std::cout << name << " [" << getTypeAsString() << "] "
              << "효과: " << value
              << " | 가격: " << price
              << " | " << description << "\n";
}

std::string Item::getTypeAsString() const {
    switch (type) {
        case ItemType::WEAPON:
            return "무기";
        case ItemType::ARMOR:
            return "방어구";
        case ItemType::POTION:
            return "포션";
        case ItemType::CONSUMABLE:
            return "소비 아이템";
        default:
            return "알 수 없음";
    }
}
