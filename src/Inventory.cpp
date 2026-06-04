#include "../include/Inventory.h"

#include <iostream>

Inventory::Inventory(int maxCapacity)
    : capacity(maxCapacity) {
}

bool Inventory::addItem(const Item& item) {
    if (isFull()) {
        std::cout << "인벤토리가 가득 찼습니다.\n";
        return false;
    }

    items.push_back(item);
    return true;
}

bool Inventory::removeItem(int index) {
    if (index < 0 || index >= static_cast<int>(items.size())) {
        return false;
    }

    items.erase(items.begin() + index);
    return true;
}

Item* Inventory::getItem(int index) {
    if (index < 0 || index >= static_cast<int>(items.size())) {
        return nullptr;
    }

    return &items[index];
}

void Inventory::displayItems() const {
    std::cout << "\n[인벤토리] " << items.size() << "/" << capacity << "\n";

    if (items.empty()) {
        std::cout << "비어 있습니다.\n";
        return;
    }

    for (int i = 0; i < static_cast<int>(items.size()); ++i) {
        std::cout << i + 1 << ". ";
        items[i].displayInfo();
    }
}

int Inventory::getItemCount() const {
    return static_cast<int>(items.size());
}

int Inventory::getCapacity() const {
    return capacity;
}

bool Inventory::isFull() const {
    return static_cast<int>(items.size()) >= capacity;
}

void Inventory::clear() {
    items.clear();
}
