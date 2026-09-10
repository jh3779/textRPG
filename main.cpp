/*
 * main.cpp
 *
 * 📚 학습 개념:
 * - 프로젝트의 시작점: main() 함수가 프로그램 실행을 시작
 * - #include: 다른 파일에서 선언한 클래스/함수 사용
 * - 프로그램의 흐름: 게임 객체 생성 → 게임 시작
 *
 * 💡 C++ 프로그램의 기본 구조:
 * 1. 필요한 헤더 파일 #include
 * 2. main() 함수 구현
 * 3. return 0 (정상 종료)
 */

#include <iostream>
#include "include/Game.h"
#include "include/Utils.h"

using namespace std;

// 💡 main() 함수: 프로그램이 여기서 시작됨
// 반환값: 정수 (0 = 정상 종료, 1 = 오류)
int main() {
    try {
        // 💡 게임 객체 생성
        // 생성자에서 모든 게임 요소가 초기화됨
        Game game;

        // 💡 게임 시작
        // start() 함수에서 초기 설정과 메인 루프 실행
        game.start();

        // 💡 게임이 끝나고 여기에 도달
        cout << "\n게임을 종료합니다. 지금까지 플레이해주셔서 감사합니다!\n" << endl;

    }
    catch (const exception& e) {
        // 💡 예외 처리: 프로그램 실행 중 오류가 발생했을 때
        cerr << "오류가 발생했습니다: " << e.what() << endl;
        return 1;  // 오류로 종료
    }

    return 0;  // 정상 종료
}
