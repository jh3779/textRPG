/*
 * Player.h
 *
 * 📚 학습 개념:
 * - 클래스(Class): 관련된 데이터(멤버 변수)와 함수(메서드)를 묶는 방법
 * - private/public: 접근 권한 제어 (캡슐화)
 * - 생성자(Constructor): 객체가 생성될 때 자동으로 실행되는 함수
 * - 인라인 함수: 작은 함수는 헤더에 구현 가능
 *
 * 📝 역할:
 * 게임의 플레이어 캐릭터 정보를 관리하는 클래스
 * 체력, 공격력, 경험치, 레벨 등 플레이어 스탯
 */

#ifndef PLAYER_H
#define PLAYER_H

#include <string>
#include <iostream>

class Player {
private:
    // 🔐 private: 클래스 내부에서만 접근 가능
    std::string name;           // 플레이어 이름
    int hp;                     // 현재 체력
    int maxHp;                  // 최대 체력
    int attack;                 // 공격력
    int defense;                // 방어력
    int level;                  // 현재 레벨
    int experience;             // 경험치
    int gold;                   // 소유 골드

public:
    // 🟢 public: 외부에서 접근 가능

    // 💡 생성자: 플레이어 객체 생성 시 초기화
    Player(const std::string& playerName);

    // 💡 Getter 함수: private 멤버 변수의 값을 읽음 (const = 이 함수는 데이터 수정 금지)
    std::string getName() const;
    int getHp() const;
    int getMaxHp() const;
    int getAttack() const;
    int getDefense() const;
    int getLevel() const;
    int getExperience() const;
    int getGold() const;

    // 💡 Setter 함수: private 멤버 변수의 값을 변경
    void setHp(int newHp);
    void setAttack(int newAttack);
    void addExperience(int exp);
    void addGold(int amount);

    // 💡 플레이어 상태를 화면에 표시하는 함수
    void displayStatus() const;

    // 💡 플레이어가 피해를 입음
    void takeDamage(int damage);

    // 💡 플레이어가 살아있는지 확인
    bool isAlive() const;

    // 💡 플레이어 레벨 업
    void levelUp();
};

#endif // PLAYER_H
