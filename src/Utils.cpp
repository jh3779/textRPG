#include "../include/Utils.h"

#include <chrono>
#include <limits>
#include <random>
#include <thread>

namespace {
    std::mt19937& randomEngine() {
        static std::mt19937 engine(std::random_device{}());
        return engine;
    }
}

namespace Utils {

    int getValidInput(int minValue, int maxValue) {
        int input = 0;

        while (true) {
            std::cout << "> ";

            if (std::cin >> input && input >= minValue && input <= maxValue) {
                std::cin.ignore(std::numeric_limits<std::streamsize>::max(), '\n');
                return input;
            }

            std::cout << "잘못된 입력입니다. "
                      << minValue << "~" << maxValue
                      << " 사이의 숫자를 입력하세요.\n";

            std::cin.clear();
            std::cin.ignore(std::numeric_limits<std::streamsize>::max(), '\n');
        }
    }

    int generateRandomNumber(int minValue, int maxValue) {
        if (minValue > maxValue) {
            std::swap(minValue, maxValue);
        }

        std::uniform_int_distribution<int> distribution(minValue, maxValue);
        return distribution(randomEngine());
    }

    void printSlowly(const std::string& text) {
        for (char ch : text) {
            std::cout << ch << std::flush;
            std::this_thread::sleep_for(std::chrono::milliseconds(8));
        }
    }

    void clearScreen() {
        std::cout << "\033[2J\033[H";
    }

    void pause(int milliseconds) {
        std::this_thread::sleep_for(std::chrono::milliseconds(milliseconds));
    }
}
