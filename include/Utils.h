/*
 * Utils.h
 *
 * 📚 학습 개념:
 * - 헤더 가드(Header Guard): 같은 파일이 여러 번 포함되는 것을 방지
 * - 함수 선언(Function Declaration): 실제 구현은 cpp 파일에서
 * - namespace: 코드를 조직화하고 이름 충돌 방지
 *
 * 📝 역할:
 * 게임에서 반복적으로 사용되는 유틸리티 함수들을 모아둔 파일
 * 예: 입력 검증, 난수 생성, 문자열 처리 등
 */

#ifndef UTILS_H
#define UTILS_H

#include <iostream>
#include <string>
#include <cstdlib>
#include <ctime>
#include <cctype>

namespace Utils {

    // 💡 사용자 입력을 받고 숫자로 변환하는 함수
    // 매개변수: 최소값, 최대값
    // 반환값: 사용자가 입력한 숫자
    int getValidInput(int minValue, int maxValue);

    // 💡 일정 범위의 난수를 생성하는 함수 (전투에서 데미지 랜덤화)
    // 매개변수: 최소값, 최대값
    // 반환값: 최소값~최대값 사이의 난수
    int generateRandomNumber(int minValue, int maxValue);

    // 💡 텍스트를 화면에 천천히 출력 (게임 분위기 조성)
    // 매개변수: 출력할 텍스트
    void printSlowly(const std::string& text);

    // 💡 화면을 맑게 지우는 함수
    void clearScreen();

    // 💡 일시 정지 함수 (밀리초 단위)
    // 매개변수: 밀리초
    void pause(int milliseconds);
}

#endif // UTILS_H
