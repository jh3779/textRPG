/*
 * Game.cpp
 *
 * 📚 학습 개념:
 * - 동적 메모리 할당(new): 프로그램 실행 중 메모리 생성
 * - 동적 메모리 해제(delete): 사용이 끝난 메모리 정리 (중요!)
 * - 소멸자(Destructor): 객체가 삭제될 때 자동으로 실행
 * - 게임 루프: 게임이 끝날 때까지 반복 실행
 *
 * 💡 팁:
 * - new로 생성한 모든 메모리는 delete로 해제해야 함 (메모리 누수 방지)
 * - ~ClassName()이 소멸자 형식
 * - run() 함수에서 while(isRunning) 루프 사용
 */

#include "../include/Game.h"
#include "../include/Utils.h"
#include <iostream>

 // 💡 생성자 구현 예시:
 // Game::Game() {
 //     // new로 동적 메모리 할당
 //     player = new Player("영웅");
 //     gameMap = new Map();
 //     playerInventory = new Inventory();
 //     
 //     currentState = GameState::MENU;
 //     isRunning = false;
 //     gameRound = 0;
 // }

 // 💡 소멸자 구현 예시:
 // Game::~Game() {
 //     // new로 할당한 메모리는 반드시 delete로 해제!
 //     // 메모리 누수를 방지하기 위해 중요함
 //     if (player != nullptr) delete player;
 //     if (gameMap != nullptr) delete gameMap;
 //     if (playerInventory != nullptr) delete playerInventory;
 // }

 // 💡 run() 함수 구현 예시:
 // void Game::run() {
 //     while (isRunning) {
 //         update();  // 게임 한 턴 진행
 //     }
 // }

 // 💡 update() 함수 구현 예시:
 // void Game::update() {
 //     displayGameStatus();
 //     // 플레이어 입력 받기
 //     // 게임 상태 업데이트
 //     // 이벤트 처리
 // }

 // 👇 여기에 나머지 함수들의 구현 코드를 작성하세요
