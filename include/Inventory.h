/*
 * Inventory.h
 *
 * 📚 학습 개념:
 * - vector: 동적 배열 (크기가 변할 수 있는 배열)
 * - #include: 다른 헤더 파일의 기능 사용
 * - 컨테이너 조작: push_back, erase 등
 *
 * 📝 역할:
 * 플레이어가 소유한 아이템들을 관리하는 클래스
 * 아이템 추가, 제거, 조회 기능
 */

#ifndef INVENTORY_H
#define INVENTORY_H

#include <vector>
#include "Item.h"

class Inventory {
private:
    // 💡 vector: 동적 배열 (벡터) - 필요에 따라 크기가 자동으로 조정됨
    std::vector<Item> items;    // 소유한 아이템 목록
    int capacity;               // 인벤토리 최대 용량

public:
    // 💡 생성자: 초기 용량을 받음
    Inventory(int maxCapacity = 20);

    // 💡 아이템 추가
    // 성공하면 true, 용량 초과하면 false 반환
    bool addItem(const Item& item);

    // 💡 특정 인덱스의 아이템 제거
    // 반환값: 제거 성공 여부
    bool removeItem(int index);

    // 💡 인덱스로 아이템 조회
    Item* getItem(int index);

    // 💡 모든 아이템 출력
    void displayItems() const;

    // 💡 현재 인벤토리에 있는 아이템 개수
    int getItemCount() const;

    // 💡 인벤토리 용량 반환
    int getCapacity() const;

    // 💡 인벤토리가 가득 찼는지 확인
    bool isFull() const;

    // 💡 인벤토리 비우기
    void clear();
};

#endif // INVENTORY_H
