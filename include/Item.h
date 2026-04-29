/*
 * Item.h
 *
 * 📚 학습 개념:
 * - enum: 미리 정의된 값들의 목록 (아이템 타입 분류)
 * - 클래스를 통한 객체 표현
 *
 * 📝 역할:
 * 게임에서 사용할 수 있는 아이템들을 정의하고 관리
 * 무기, 방어구, 포션 등 다양한 아이템 타입
 */

#ifndef ITEM_H
#define ITEM_H

#include <string>
#include <iostream>

 // 💡 enum: 여러 개의 상수 값을 한 번에 정의
 // 아이템의 종류를 분류하기 위해 사용
enum class ItemType {
    WEAPON,      // 무기
    ARMOR,       // 방어구
    POTION,      // 포션
    CONSUMABLE   // 소비 아이템
};

class Item {
private:
    std::string name;           // 아이템 이름
    ItemType type;              // 아이템 종류
    int value;                  // 아이템의 능력치 (데미지, 방어력, 회복량 등)
    int price;                  // 판매 가격
    std::string description;    // 아이템 설명

public:
    // 💡 생성자: 아이템 정보로 초기화
    Item(const std::string& itemName, ItemType itemType, int itemValue,
        int itemPrice, const std::string& desc);

    // 💡 Getter 함수
    std::string getName() const;
    ItemType getType() const;
    int getValue() const;
    int getPrice() const;
    std::string getDescription() const;

    // 💡 아이템 정보 표시
    void displayInfo() const;

    // 💡 아이템 종류를 문자열로 변환 (화면 출력용)
    std::string getTypeAsString() const;
};

#endif // ITEM_H
