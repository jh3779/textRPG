/*
 * Enemy.h
 *
 * 📚 학습 개념:
 * - 클래스 설계: 플레이어와 유사한 구조
 * - const 멤버 함수: 객체의 상태를 변경하지 않는 함수
 *
 * 📝 역할:
 * 게임에 등장하는 적 캐릭터를 관리하는 클래스
 * 몬스터 유형별로 다른 스탯 정보 저장
 */

#ifndef ENEMY_H
#define ENEMY_H

#include <string>
#include <iostream>

class Enemy {
private:
    std::string name;           // 적 이름 (예: "고블린", "오크")
    int hp;                     // 현재 체력
    int maxHp;                  // 최대 체력
    int attack;                 // 공격력
    int defense;                // 방어력
    int experienceReward;       // 처치 시 얻는 경험치
    int goldReward;             // 처치 시 얻는 골드

public:
    // 💡 생성자: 적의 기본 정보로 초기화
    Enemy(const std::string& enemyName, int hp, int atk, int def, int exp, int gold);

    // 💡 Getter 함수
    std::string getName() const;
    int getHp() const;
    int getMaxHp() const;
    int getAttack() const;
    int getDefense() const;
    int getExperienceReward() const;
    int getGoldReward() const;

    // 💡 Setter 함수
    void setHp(int newHp);

    // 💡 적의 상태 표시
    void displayStatus() const;

    // 💡 적이 피해를 입음
    void takeDamage(int damage);

    // 💡 적이 살아있는지 확인
    bool isAlive() const;
};

#endif // ENEMY_H
