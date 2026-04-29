/*
 * Inventory.cpp
 *
 * 📚 학습 개념:
 * - std::vector 사용: push_back(), erase(), size() 등의 메서드
 * - vector 반복: for (int i = 0; i < items.size(); i++) 방식
 * - 포인터 반환: 조건에 따라 nullptr이나 원소의 주소 반환
 *
 * 💡 팁:
 * - push_back(): 벡터의 끝에 새 요소 추가
 * - erase(): 특정 인덱스의 요소 삭제
 * - size(): 현재 벡터의 크기 반환
 * - nullptr: C++의 null 포인터 (유효하지 않은 주소)
 */

#include "../include/Inventory.h"

 // 💡 생성자 구현 예시:
 // Inventory::Inventory(int maxCapacity) {
 //     capacity = maxCapacity;
 //     // items는 std::vector이므로 자동으로 빈 벡터로 초기화됨
 // }

 // 💡 addItem() 함수 구현 예시:
 // bool Inventory::addItem(const Item& item) {
 //     if (items.size() < capacity) {
 //         items.push_back(item);
 //         return true;
 //     }
 //     return false;
 // }

 // 💡 getItemCount() 함수 구현 예시:
 // int Inventory::getItemCount() const {
 //     return items.size();
 // }

 // 👇 여기에 나머지 함수들의 구현 코드를 작성하세요
