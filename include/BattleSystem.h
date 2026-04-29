/*
 * BattleSystem.h
 *
 * 📚 학습 개념:
 * - 참조(&): 변수의 원본을 가리키는 것 (복사가 아님)
 * - 클래스 간 상호작용: 다른 클래스의 객체를 사용
 *
 * 📝 역할:
 * 게임의 전투 시스템을 관리하는 클래스
 * 플레이어와 적 사이의 전투 진행, 결과 판정
 */

#ifndef BATTLESYSTEM_H
#define BATTLESYSTEM_H

#include "Player.h"
#include "Enemy.h"

 // 💡 enum: 전투 결과의 종류
enum class BattleResult {
    PLAYER_WIN,     // 플레이어 승리
    PLAYER_LOSE,    // 플레이어 패배
    PLAYER_FLEE,    // 플레이어 도망
    FLEE_FAILED     // 도망 실패
};

class BattleSystem {
private:
    // 💡 현재 진행 중인 전투의 플레이어, 적 참조
    Player* player;
    Enemy* enemy;
    int round;      // 현재 전투 라운드

public:
    // 💡 생성자: 플레이어와 적의 포인터를 받음
    // 포인터(*): 메모리 주소를 저장하는 변수
    BattleSystem(Player* p, Enemy* e);

    // 💡 전투 시작 (반복해서 라운드 진행)
    // 참조(&): 원본 객체를 직접 수정할 수 있음
    BattleResult startBattle();

    // 💡 한 라운드 진행
    void executeTurn(int playerAction);

    // 💡 플레이어의 공격
    void playerAttack();

    // 💡 적의 공격
    void enemyAttack();

    // 💡 플레이어 도망 시도
    bool attemptFlee();

    // 💡 현재 전투 상태 표시
    void displayBattleStatus() const;

    // 💡 플레이어 행동 선택 (입력)
    int getPlayerAction() const;

    // 💡 전투 종료 처리 (경험치, 골드 획득 등)
    void endBattle(BattleResult result);
};

#endif // BATTLESYSTEM_H
