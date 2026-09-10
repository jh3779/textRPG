/*
 * Utils.cs
 *
 * 📝 역할: include/Utils.h + src/Utils.cpp 중 게임 로직에 필요한 부분만 포팅한다.
 * (콘솔 입출력 함수인 getValidInput/printSlowly/clearScreen/pause는 UI 레이어의
 *  책임이므로 포팅하지 않는다 — Unity에서는 버튼 클릭이 입력을, TMP 텍스트가
 *  출력을 대신한다.)
 *
 * ⚠️ 중요: UnityEngine.Random.Range(int,int)는 상한이 배타적(exclusive)이라
 * C++의 std::uniform_int_distribution(min,max)(양쪽 포함, inclusive)와 분포가
 * 다르다. 수치·확률을 원본과 동일하게 유지하기 위해(00_project_brief.md 핵심 가치)
 * 이 클래스는 UnityEngine을 전혀 참조하지 않는 순수 C#으로 작성하고,
 * System.Random을 사용해 상한을 명시적으로 +1 보정한다.
 */

using System;

namespace TextRPG.GameLogic
{
    public static class Utils
    {
        // 💡 C++ 쪽 std::mt19937 static 엔진과 동일한 역할 — 프로세스 수명 동안 하나만 사용.
        private static readonly Random RandomEngine = new Random();

        /// <summary>
        /// minValue~maxValue 사이(양쪽 포함)의 정수 난수를 반환한다.
        /// src/Utils.cpp의 generateRandomNumber와 동일한 분포(양 끝 포함, min&gt;max면 swap).
        /// </summary>
        public static int GenerateRandomNumber(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                (minValue, maxValue) = (maxValue, minValue);
            }

            // System.Random.Next(min, max)는 max가 배타적이므로 +1 보정해 inclusive로 맞춘다.
            return RandomEngine.Next(minValue, maxValue + 1);
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
