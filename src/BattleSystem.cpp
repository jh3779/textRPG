#include "../include/BattleSystem.h"
#include "../include/Utils.h"

#include <algorithm>
#include <iostream>

BattleSystem::BattleSystem(Player* p, Enemy* e)
    : player(p),
      enemy(e),
      round(0) {
}

BattleResult BattleSystem::startBattle(AiNarrator* narrator) {
    std::string intro = "전투 시작! " + enemy->getName() + " 등장!";
    if (narrator != nullptr && narrator->isEnabled()) {
        intro = narrator->narrate(
            "battle_start",
            enemy->getName() + "와(과)의 전투가 시작됨. 플레이어 HP "
                + std::to_string(player->getHp()) + "/" + std::to_string(player->getMaxHp()),
            intro);
    }
    std::cout << "\n" << intro << "\n";

    while (player->isAlive() && enemy->isAlive()) {
        round++;
        displayBattleStatus();

        int action = getPlayerAction();
        if (action == 2) {
            if (attemptFlee()) {
                BattleResult result = BattleResult::PLAYER_FLEE;
                endBattle(result);
                return result;
            }

            std::cout << "도망에 실패했습니다!\n";
            enemyAttack();
            continue;
        }

        executeTurn(action);
    }

    BattleResult result = player->isAlive()
        ? BattleResult::PLAYER_WIN
        : BattleResult::PLAYER_LOSE;

    endBattle(result);
    return result;
}

void BattleSystem::executeTurn(int playerAction) {
    if (playerAction != 1) {
        return;
    }

    playerAttack();
    if (enemy->isAlive()) {
        enemyAttack();
    }
}

void BattleSystem::playerAttack() {
    int damage = player->getAttack() + Utils::generateRandomNumber(0, 3);
    std::cout << "\n" << player->getName() << "의 공격!\n";
    enemy->takeDamage(damage);
}

void BattleSystem::enemyAttack() {
    if (!enemy->isAlive()) {
        return;
    }

    int damage = enemy->getAttack() + Utils::generateRandomNumber(0, 2);
    std::cout << enemy->getName() << "의 반격!\n";
    player->takeDamage(damage);
}

bool BattleSystem::attemptFlee() {
    return Utils::generateRandomNumber(1, 100) <= 55;
}

void BattleSystem::displayBattleStatus() const {
    std::cout << "\n--- 전투 " << round << "턴 ---\n";
    std::cout << player->getName()
              << " HP: " << player->getHp() << "/" << player->getMaxHp() << "\n";
    enemy->displayStatus();
}

int BattleSystem::getPlayerAction() const {
    std::cout << "\n1. 공격한다\n"
              << "2. 도망친다\n";
    return Utils::getValidInput(1, 2);
}

void BattleSystem::endBattle(BattleResult result) {
    switch (result) {
        case BattleResult::PLAYER_WIN:
            std::cout << "\n승리했습니다!\n"
                      << "경험치 " << enemy->getExperienceReward()
                      << ", 골드 " << enemy->getGoldReward()
                      << " 획득.\n";
            player->addExperience(enemy->getExperienceReward());
            player->addGold(enemy->getGoldReward());
            break;
        case BattleResult::PLAYER_LOSE:
            std::cout << "\n전투에서 쓰러졌습니다.\n";
            break;
        case BattleResult::PLAYER_FLEE:
            std::cout << "\n전투에서 벗어났습니다.\n";
            break;
        case BattleResult::FLEE_FAILED:
            std::cout << "\n도망에 실패했습니다.\n";
            break;
    }
}
