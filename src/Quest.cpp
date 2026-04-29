/*
 * Quest.cpp
 *
 * 📚 학습 개념:
 * - enum 값 사용 및 비교
 * - 진행 상황 추적: currentCount와 targetCount
 * - 조건 검사: isCompleted()에서 진행도 확인
 *
 * 💡 팁:
 * - status는 NOT_STARTED로 시작
 * - updateProgress(1) 호출 시마다 currentCount 증가
 * - currentCount >= targetCount일 때 완료 판정
 */

#include "../include/Quest.h"

 // 💡 생성자 구현 예시:
 // Quest::Quest(const std::string& id, const std::string& title, 
 //              const std::string& desc, QuestType questType,
 //              int target, int exp, int gold) {
 //     questId = id;
 //     this->title = title;
 //     description = desc;
 //     type = questType;
 //     targetCount = target;
 //     currentCount = 0;
 //     rewardExp = exp;
 //     rewardGold = gold;
 //     status = QuestStatus::NOT_STARTED;
 // }

 // 💡 updateProgress() 함수 구현 예시:
 // void Quest::updateProgress(int amount) {
 //     if (status == QuestStatus::IN_PROGRESS) {
 //         currentCount += amount;
 //         if (currentCount >= targetCount) {
 //             status = QuestStatus::COMPLETED;
 //             std::cout << "🎉 퀘스트 '" << title << "'를 완료했습니다!" << std::endl;
 //         }
 //     }
 // }

 // 👇 여기에 나머지 함수들의 구현 코드를 작성하세요
