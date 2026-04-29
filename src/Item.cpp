/*
 * Item.cpp
 *
 * 📚 학습 개념:
 * - enum 값 사용: ItemType::WEAPON처럼 접근
 * - switch 문: 여러 경우를 나누어 처리
 *
 * 💡 팁:
 * - getTypeAsString() 함수에서는 enum을 문자열로 변환
 * - switch 문 예시:
 *   switch(type) {
 *       case ItemType::WEAPON: return "무기";
 *       case ItemType::ARMOR: return "방어구";
 *       ...
 *   }
 */

#include "../include/Item.h"

 // 💡 생성자 구현 예시:
 // Item::Item(const std::string& itemName, ItemType itemType, int itemValue, 
 //            int itemPrice, const std::string& desc) {
 //     name = itemName;
 //     type = itemType;
 //     value = itemValue;
 //     price = itemPrice;
 //     description = desc;
 // }

 // 💡 getTypeAsString() 함수 구현 예시:
 // std::string Item::getTypeAsString() const {
 //     switch(type) {
 //         case ItemType::WEAPON:
 //             return "무기";
 //         case ItemType::ARMOR:
 //             return "방어구";
 //         ...
 //     }
 // }

 // 👇 여기에 나머지 함수들의 구현 코드를 작성하세요
