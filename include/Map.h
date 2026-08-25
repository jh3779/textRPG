/*
 * Map.h
 *
 * 📚 학습 개념:
 * - struct vs class: struct는 기본이 public, class는 기본이 private
 * - 중첩 구조: 구조체나 클래스 안에 다른 타입 포함
 *
 * 📝 역할:
 * 게임의 맵/위치를 관리하는 클래스
 * 각 지역에서 발생할 수 있는 이벤트 정의
 */

#ifndef MAP_H
#define MAP_H

#include <string>
#include <iostream>

#include "AiNarrator.h"

 // 💡 struct: 데이터를 묶는 방법 (기본적으로 public)
 // 간단한 데이터 구조에는 struct 사용
struct Location {
    std::string name;           // 지역 이름
    std::string description;    // 지역 설명
    bool hasEnemy;              // 적이 있는지 여부
    bool isDeadEnd;             // 막힌 길인지 여부
};

class Map {
private:
    // 💡 배열: 고정 크기의 여러 데이터 저장
    // 10개의 지역을 저장
    Location locations[10];
    int currentLocationIndex;   // 현재 플레이어 위치
    int totalLocations;         // 총 지역 개수

public:
    // 💡 생성자: 게임 맵 초기화
    Map();

    // 💡 현재 위치의 지역 정보 반환
    Location getCurrentLocation() const;

    // 💡 현재 위치 이름 반환
    std::string getCurrentLocationName() const;

    // 💡 현재 위치 설명 표시
    // narrator가 주어지고 AI 서술 모드가 켜져 있으면 매번 다른 서술을 받아온다.
    void displayCurrentLocation(AiNarrator* narrator = nullptr) const;

    // 💡 특정 위치로 이동
    void moveToLocation(int locationIndex);

    // 💡 현재 위치에 적이 있는지 확인
    bool hasEnemyInCurrentLocation() const;

    // 💡 다음 위치로 이동
    void moveNext();

    // 💡 이전 위치로 이동
    void movePrevious();

    // 💡 모든 위치 목록 표시
    void displayMap() const;

    // 💡 현재 위치 인덱스 반환
    int getCurrentLocationIndex() const;
};

#endif // MAP_H
