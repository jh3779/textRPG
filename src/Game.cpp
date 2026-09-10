#include "../include/Game.h"
#include "../include/Utils.h"

#include <algorithm>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <limits>
#include <map>
#include <string>

namespace {
    const std::string SaveFilePath = "saves/save1.txt";

    void waitForEnter() {
        std::cout << "\nEnter 키를 누르면 계속합니다...";
        std::cin.ignore(std::numeric_limits<std::streamsize>::max(), '\n');
    }

    int readInt(const std::map<std::string, std::string>& data,
                const std::string& key,
                int fallback) {
        auto it = data.find(key);
        if (it == data.end()) {
            return fallback;
        }

        try {
            return std::stoi(it->second);
        } catch (...) {
            return fallback;
        }
    }

    bool readBool(const std::map<std::string, std::string>& data,
                  const std::string& key,
                  bool fallback) {
        return readInt(data, key, fallback ? 1 : 0) != 0;
    }

    std::string readStr(const std::map<std::string, std::string>& data,
                         const std::string& key,
                         const std::string& fallback) {
        auto it = data.find(key);
        return it == data.end() ? fallback : it->second;
    }
}

Game::Game()
    : player(new Player("모험가")),
      gameMap(new Map()),
      playerInventory(new Inventory(5)),
      currentState(GameState::MENU),
      isRunning(false),
      gameRound(0),
      armoryLooted(false),
      goblinDefeated(false) {
    playerInventory->addItem(
        Item("회복 물약", ItemType::POTION, 30, 50, "체력을 30 회복합니다.")
    );

    quests.emplace_back(
        "quest001",
        "던전 탈출",
        "보스의 방까지 도달해 던전의 주인을 쓰러뜨리세요.",
        QuestType::REACH_GOAL,
        1,
        100,
        80
    );
}

Game::~Game() {
    delete player;
    delete gameMap;
    delete playerInventory;
}

void Game::start() {
    isRunning = true;
    currentState = GameState::MENU;

    while (currentState == GameState::MENU && isRunning) {
        Utils::clearScreen();
        std::cout << "============================\n"
                  << "      Console Text RPG\n"
                  << "============================\n";

        displayMenu();
        int choice = Utils::getValidInput(1, 4);

        if (choice == 1) {
            currentState = GameState::PLAYING;
            for (Quest& quest : quests) {
                quest.startQuest();
            }
        } else if (choice == 2) {
            if (loadGame(SaveFilePath)) {
                std::cout << "\n저장된 게임을 불러왔습니다.\n";
                waitForEnter();
                currentState = GameState::PLAYING;
            } else {
                std::cout << "\n저장 파일이 없습니다. 새 게임을 시작하거나 종료하세요.\n";
                waitForEnter();
            }
        } else if (choice == 3) {
            if (aiNarrator.isEnabled()) {
                aiNarrator.setEnabled(false);
                std::cout << "\nAI 서술 모드를 껐습니다.\n";
            } else {
                std::cout << "\nAI 서술 서비스에 연결 중...\n";
                if (aiNarrator.checkHealth()) {
                    aiNarrator.setEnabled(true);
                    std::cout << "AI 서술 모드를 켰습니다.\n";
                } else {
                    std::cout << "AI 서술 서비스에 연결할 수 없습니다. "
                              << "ai_service/를 먼저 실행한 뒤 다시 시도하세요.\n";
                }
            }
            waitForEnter();
        } else {
            currentState = GameState::QUIT;
        }
    }

    run();
}

void Game::run() {
    while (isRunning) {
        switch (currentState) {
            case GameState::PLAYING:
                update();
                break;
            case GameState::GAME_OVER:
                handleGameOver();
                break;
            case GameState::QUIT:
                end();
                break;
            case GameState::MENU:
                currentState = GameState::PLAYING;
                break;
        }
    }
}

void Game::update() {
    if (!player->isAlive()) {
        currentState = GameState::GAME_OVER;
        return;
    }

    gameRound++;
    Utils::clearScreen();
    displayGameStatus();
    gameMap->displayCurrentLocation(&aiNarrator);
    handleLocationEvent();
}

void Game::displayMenu() const {
    std::cout << "\n1. 새 게임\n"
              << "2. 이어하기\n"
              << "3. AI 서술 모드 켜기/끄기 (현재: " << (aiNarrator.isEnabled() ? "ON" : "OFF") << ")\n"
              << "4. 종료\n";
}

void Game::displayGameStatus() const {
    std::cout << "[라운드 " << gameRound << "] "
              << player->getName()
              << " HP " << player->getHp() << "/" << player->getMaxHp()
              << " | ATK " << player->getAttack()
              << " | Gold " << player->getGold();

    if (aiNarrator.isEnabled()) {
        std::cout << " | AI " << (aiNarrator.isDegraded() ? "응답 없음(정적 텍스트로 진행 중)" : "ON");
    }

    std::cout << "\n";
}

void Game::handleLocationEvent() {
    int locationIndex = gameMap->getCurrentLocationIndex();

    if (locationIndex == 0) {
        std::cout << "\n1. 던전에 들어간다\n"
                  << "2. 상태를 확인한다\n"
                  << "3. 저장하고 종료\n";

        int choice = Utils::getValidInput(1, 3);
        if (choice == 1) {
            gameMap->moveToLocation(1);
        } else if (choice == 2) {
            player->displayStatus();
            playerInventory->displayItems();
            waitForEnter();
        } else {
            saveAndQuit();
        }
        return;
    }

    if (locationIndex == 1) {
        std::cout << "\n1. 왼쪽 빛을 따라간다\n"
                  << "2. 오른쪽 통로로 간다\n"
                  << "3. 저장하고 종료\n";

        int choice = Utils::getValidInput(1, 3);
        if (choice == 1) {
            gameMap->moveToLocation(2);
        } else if (choice == 2) {
            gameMap->moveToLocation(3);
        } else {
            saveAndQuit();
        }
        return;
    }

    if (locationIndex == 2) {
        std::cout << "\n상자에서 낡은 검과 물약을 발견했습니다.\n"
                  << "1. 장비를 챙긴다\n"
                  << "2. 지나간다\n"
                  << "3. 저장하고 종료\n";

        int choice = Utils::getValidInput(1, 3);
        if (choice == 1) {
            if (armoryLooted) {
                std::cout << "이미 챙길 만한 장비는 모두 가져갔습니다.\n";
                waitForEnter();
                gameMap->moveToLocation(3);
                return;
            }

            player->setAttack(player->getAttack() + 4);
            playerInventory->addItem(
                Item("작은 회복 물약", ItemType::POTION, 20, 30, "체력을 20 회복합니다.")
            );
            armoryLooted = true;
            std::cout << "공격력이 4 증가했습니다.\n";
            waitForEnter();
            gameMap->moveToLocation(3);
        } else if (choice == 2) {
            gameMap->moveToLocation(3);
        } else {
            saveAndQuit();
        }
        return;
    }

    if (locationIndex == 3) {
        if (goblinDefeated) {
            std::cout << "\n이미 고블린을 처치해 통로가 비어 있습니다.\n"
                      << "1. 보스의 방으로 간다\n"
                      << "2. 갈림길로 돌아간다\n"
                      << "3. 저장하고 종료\n";

            int choice = Utils::getValidInput(1, 3);
            if (choice == 1) {
                gameMap->moveToLocation(4);
            } else if (choice == 2) {
                gameMap->moveToLocation(1);
            } else {
                saveAndQuit();
            }
            return;
        }

        Enemy goblin("고블린", 30, 7, 1, 60, 25);
        BattleSystem battle(player, &goblin);
        BattleResult result = battle.startBattle(&aiNarrator);

        if (result == BattleResult::PLAYER_LOSE) {
            currentState = GameState::GAME_OVER;
        } else if (result == BattleResult::PLAYER_FLEE) {
            gameMap->moveToLocation(1);
        } else {
            goblinDefeated = true;
            gameMap->moveToLocation(4);
        }
        waitForEnter();
        return;
    }

    if (locationIndex == 4) {
        std::cout << "\n1. 보스에게 도전한다\n"
                  << "2. 갈림길로 물러난다\n"
                  << "3. 저장하고 종료\n";

        int choice = Utils::getValidInput(1, 3);
        if (choice == 2) {
            gameMap->moveToLocation(1);
            return;
        }
        if (choice == 3) {
            saveAndQuit();
            return;
        }

        Enemy guardian("던전 수호자", 55, 10, 3, 120, 70);
        BattleSystem battle(player, &guardian);
        BattleResult result = battle.startBattle(&aiNarrator);

        if (result == BattleResult::PLAYER_WIN) {
            for (Quest& quest : quests) {
                quest.updateProgress();
            }

            std::string victoryText = "던전 클리어!";
            if (aiNarrator.isEnabled()) {
                victoryText = aiNarrator.narrate(
                    "victory",
                    "플레이어가 던전 수호자를 물리치고 던전을 클리어함",
                    victoryText);
            }
            std::cout << "\n" << victoryText << "\n";
            if (!quests.empty() && quests.front().isCompleted()) {
                player->addExperience(quests.front().getRewardExp());
                player->addGold(quests.front().getRewardGold());
                std::cout << "퀘스트 보상으로 경험치 "
                          << quests.front().getRewardExp()
                          << ", 골드 " << quests.front().getRewardGold()
                          << " 획득.\n";
            }
            currentState = GameState::QUIT;
        } else if (result == BattleResult::PLAYER_FLEE) {
            gameMap->moveToLocation(3);
        } else {
            currentState = GameState::GAME_OVER;
        }
        waitForEnter();
    }
}

bool Game::saveGame(const std::string& filename) const {
    std::filesystem::path savePath(filename);
    if (savePath.has_parent_path()) {
        std::filesystem::create_directories(savePath.parent_path());
    }

    std::ofstream file(filename);
    if (!file) {
        return false;
    }

    file << "version=2\n"
         << "hp=" << player->getHp() << "\n"
         << "max_hp=" << player->getMaxHp() << "\n"
         << "attack=" << player->getAttack() << "\n"
         << "defense=" << player->getDefense() << "\n"
         << "level=" << player->getLevel() << "\n"
         << "experience=" << player->getExperience() << "\n"
         << "gold=" << player->getGold() << "\n"
         << "location=" << gameMap->getCurrentLocationIndex() << "\n"
         << "game_round=" << gameRound << "\n"
         << "armory_looted=" << (armoryLooted ? 1 : 0) << "\n"
         << "goblin_defeated=" << (goblinDefeated ? 1 : 0) << "\n";

    // 💡 인벤토리는 armoryLooted 같은 파생 플래그가 아니라 실제 아이템 목록을 그대로 저장한다
    // (플래그만 저장하면 새 아이템이 추가될 때마다 세이브 포맷을 또 손봐야 함)
    file << "item_count=" << playerInventory->getItemCount() << "\n";
    for (int i = 0; i < playerInventory->getItemCount(); ++i) {
        Item* item = playerInventory->getItem(i);
        std::string prefix = "item_" + std::to_string(i) + "_";
        file << prefix << "name=" << item->getName() << "\n"
             << prefix << "type=" << static_cast<int>(item->getType()) << "\n"
             << prefix << "value=" << item->getValue() << "\n"
             << prefix << "price=" << item->getPrice() << "\n"
             << prefix << "desc=" << item->getDescription() << "\n";
    }

    // 💡 퀘스트도 마찬가지로 상태(status)·진행도(currentCount)를 직접 저장한다.
    // 제목/설명/보상 같은 고정 스펙은 Game() 생성자가 매번 동일하게 만들어주므로 저장하지 않는다.
    file << "quest_count=" << quests.size() << "\n";
    for (std::size_t i = 0; i < quests.size(); ++i) {
        std::string prefix = "quest_" + std::to_string(i) + "_";
        file << prefix << "status=" << static_cast<int>(quests[i].getStatus()) << "\n"
             << prefix << "current=" << quests[i].getCurrentCount() << "\n";
    }

    return true;
}

bool Game::loadGame(const std::string& filename) {
    std::ifstream file(filename);
    if (!file) {
        return false;
    }

    std::map<std::string, std::string> data;
    std::string line;
    while (std::getline(file, line)) {
        if (line.empty() || line[0] == '#') {
            continue;
        }

        std::size_t delimiter = line.find('=');
        if (delimiter == std::string::npos) {
            continue;
        }

        data[line.substr(0, delimiter)] = line.substr(delimiter + 1);
    }

    int version = readInt(data, "version", 0);
    if (version != 2) {
        // 구버전(version=1) 세이브는 아이템/퀘스트 상태를 포함하지 않아 안전하게 복원할 수
        // 없으므로 거부한다 — 새 게임으로 다시 시작해야 함.
        return false;
    }

    player->loadState(
        readInt(data, "hp", 100),
        readInt(data, "max_hp", 100),
        readInt(data, "attack", 10),
        readInt(data, "defense", 3),
        readInt(data, "level", 1),
        readInt(data, "experience", 0),
        readInt(data, "gold", 0)
    );

    gameMap->moveToLocation(readInt(data, "location", 0));
    gameRound = readInt(data, "game_round", 0);
    armoryLooted = readBool(data, "armory_looted", false);
    goblinDefeated = readBool(data, "goblin_defeated", false);

    // 💡 저장된 아이템 목록을 그대로 복원 (파생 플래그로 유추하지 않음)
    playerInventory->clear();
    int itemCount = readInt(data, "item_count", 0);
    for (int i = 0; i < itemCount; ++i) {
        std::string prefix = "item_" + std::to_string(i) + "_";
        std::string name = readStr(data, prefix + "name", "아이템");
        int typeInt = std::clamp(readInt(data, prefix + "type", 0), 0, 3);
        int value = readInt(data, prefix + "value", 0);
        int price = readInt(data, prefix + "price", 0);
        std::string desc = readStr(data, prefix + "desc", "");
        playerInventory->addItem(
            Item(name, static_cast<ItemType>(typeInt), value, price, desc)
        );
    }

    // 💡 퀘스트도 저장된 status/currentCount로 정확히 복원 (startQuest()로 리셋하지 않음)
    int questCount = readInt(data, "quest_count", 0);
    for (std::size_t i = 0; i < quests.size() && static_cast<int>(i) < questCount; ++i) {
        std::string prefix = "quest_" + std::to_string(i) + "_";
        int statusInt = std::clamp(readInt(data, prefix + "status", 0), 0, 4);
        int currentCount = readInt(data, prefix + "current", 0);
        quests[i].loadState(static_cast<QuestStatus>(statusInt), currentCount);
    }

    return true;
}

void Game::saveAndQuit() {
    if (saveGame(SaveFilePath)) {
        std::cout << "\n게임을 저장했습니다: " << SaveFilePath << "\n";
    } else {
        std::cout << "\n저장에 실패했습니다.\n";
    }

    currentState = GameState::QUIT;
}

void Game::handleGameOver() {
    std::string defeatText = "체력이 0이 되어 모험이 끝났습니다.";
    if (aiNarrator.isEnabled()) {
        defeatText = aiNarrator.narrate(
            "defeat",
            "플레이어가 라운드 " + std::to_string(gameRound) + "에 체력이 0이 되어 패배함",
            defeatText);
    }
    std::cout << "\nGAME OVER\n" << defeatText << "\n";
    isRunning = false;
}

void Game::end() {
    std::cout << "\n최종 상태\n";
    player->displayStatus();
    isRunning = false;
}

Player* Game::getPlayer() const {
    return player;
}

Map* Game::getMap() const {
    return gameMap;
}

Inventory* Game::getInventory() const {
    return playerInventory;
}
