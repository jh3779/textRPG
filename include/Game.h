/*
 * Game.h
 *
 * 📚 학습 개념:
 * - 포인터(*): 메모리 주소를 저장
 * - 동적 메모리(new/delete): 프로그램 실행 중에 메모리 할당/해제
 * - 게임 루프: 게임의 전체 흐름 관리
 *
 * 📝 역할:
 * 게임 전체를 관리하는 클래스
 * 게임 시작, 진행, 종료 등의 전체 흐름 제어
 */

#ifndef GAME_H
#define GAME_H

#include "Player.h"
#include "Map.h"
#include "BattleSystem.h"
#include "Quest.h"
#include "Inventory.h"
#include "AiNarrator.h"
#include <string>
#include <vector>

 // 💡 enum: 게임 상태
enum class GameState {
    MENU,           // 메인 메뉴
    PLAYING,        // 게임 진행 중
    GAME_OVER,      // 게임 오버
    QUIT            // 게임 종료
};

class Game {
private:
    // 💡 포인터: 메모리 주소를 저장 (동적 할당)
    Player* player;             // 플레이어 객체의 주소
    Map* gameMap;               // 게임 맵의 주소
    Inventory* playerInventory; // 인벤토리의 주소

    std::vector<Quest> quests;  // 퀘스트 목록

    GameState currentState;     // 현재 게임 상태
    bool isRunning;             // 게임 실행 중 여부
    int gameRound;              // 현재 게임 라운드
    bool armoryLooted;          // 무기고 보상 획득 여부
    bool goblinDefeated;        // 고블린 처치 여부

    // 💡 선택 기능: 로컬 ai_service/에 연결해 위치/전투/승패 서술을 동적으로 받아옴
    // (서비스가 꺼져 있으면 항상 기존 정적 텍스트로 폴백)
    AiNarrator aiNarrator;

public:
    // 💡 생성자: 게임 초기화
    Game();

    // 💡 소멸자: 동적으로 할당된 메모리 해제
    // C++에서 메모리 관리의 중요한 부분!
    ~Game();

    // 💡 게임 시작
    void start();

    // 💡 게임 메인 루프
    // 게임이 종료될 때까지 반복 실행
    void run();

    // 💡 게임의 한 턴 진행
    void update();

    // 💡 메인 메뉴 표시
    void displayMenu() const;

    // 💡 게임 상태 표시
    void displayGameStatus() const;

    // 💡 현재 위치에서 이벤트 처리
    void handleLocationEvent();

    // 💡 저장/불러오기
    bool saveGame(const std::string& filename) const;
    bool loadGame(const std::string& filename);
    void saveAndQuit();

    // 💡 게임 오버 처리
    void handleGameOver();

    // 💡 게임 종료
    void end();

    // 💡 플레이어 정보 조회
    Player* getPlayer() const;
    Map* getMap() const;
    Inventory* getInventory() const;
};

#endif // GAME_H
