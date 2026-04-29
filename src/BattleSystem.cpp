/*
 * BattleSystem.cpp
 *
 * 📚 학습 개념:
 * - 포인터 사용: 플레이어와 적의 참조 저장
 * - while 루프: 전투가 끝날 때까지 반복 (체력 체크)
 * - 상태 변환: enum을 반환값으로 사용
 *
 * 💡 팁:
 * - startBattle()는 전투가 끝날 때까지 계속 진행
 * - while (player->isAlive() && enemy->isAlive()) 형식으로 루프
 * - 포인터 접근: player->getHp() (->를 사용, .이 아님)
 */

#include "../include/BattleSystem.h"
#include "../include/Utils.h"
#include <iostream>
#include <cstdlib>
#include <ctime>

 // 💡 생성자 구현 예시:
 // BattleSystem::BattleSystem(Player* p, Enemy* e) {
 //     player = p;
 //     enemy = e;
 //     round = 0;
 // }

 // 💡 startBattle() 함수 구현 예시:
 // BattleResult BattleSystem::startBattle() {
 //     std::cout << "⚔️  전투 시작! " << enemy->getName() << "과 전투합니다!\n" << std::endl;
 //     
 //     while (player->isAlive() && enemy->isAlive()) {
 //         round++;
 //         displayBattleStatus();
 //         int action = getPlayerAction();
 //         
 //         if (action == 1) {
 //             executeTurn(action);
 //         } else if (action == 2) {
 //             if (attemptFlee()) {
 //                 return BattleResult::PLAYER_FLEE;
 //             }
 //         }
 //     }
 //     
 //     BattleResult result = player->isAlive() ? BattleResult::PLAYER_WIN : BattleResult::PLAYER_LOSE;
 //     endBattle(result);
 //     return result;
 // }

 // 👇 여기에 나머지 함수들의 구현 코드를 작성하세요
