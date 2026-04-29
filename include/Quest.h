/*
 * Quest.h
 *
 * 📚 학습 개념:
 * - enum을 사용한 상태 관리
 * - 클래스를 통한 상태 추적
 *
 * 📝 역할:
 * 게임의 퀘스트(미션) 시스템을 관리하는 클래스
 * 퀘스트 진행, 보상 지급 등
 */

#ifndef QUEST_H
#define QUEST_H

#include <string>
#include <iostream>

 // 💡 enum: 퀘스트의 진행 상태
enum class QuestStatus {
    NOT_STARTED,    // 시작 전
    IN_PROGRESS,    // 진행 중
    COMPLETED,      // 완료
    FAILED,         // 실패
    REWARDED        // 보상 수령
};

// 💡 enum: 퀘스트 타입
enum class QuestType {
    KILL_ENEMY,     // 적 처치
    COLLECT_ITEM,   // 아이템 수집
    EXPLORE,        // 장소 탐험
    REACH_GOAL      // 목표 도달
};

class Quest {
private:
    std::string questId;        // 퀘스트 고유 ID
    std::string title;          // 퀘스트 제목
    std::string description;    // 퀘스트 설명
    QuestType type;             // 퀘스트 타입
    QuestStatus status;         // 현재 상태
    int targetCount;            // 목표 개수 (예: 몬스터 3마리 처치)
    int currentCount;           // 현재 진행 상황
    int rewardExp;              // 보상 경험치
    int rewardGold;             // 보상 골드

public:
    // 💡 생성자: �에스트 정보로 초기화
    Quest(const std::string& id, const std::string& title,
        const std::string& desc, QuestType questType,
        int target, int exp, int gold);

    // 💡 Getter 함수
    std::string getTitle() const;
    std::string getDescription() const;
    QuestStatus getStatus() const;
    int getTargetCount() const;
    int getCurrentCount() const;
    int getRewardExp() const;
    int getRewardGold() const;

    // 💡 퀘스트 시작
    void startQuest();

    // 💡 진행 상황 업데이트
    void updateProgress(int amount = 1);

    // 💡 진행도 표시 (예: 2/5)
    void displayProgress() const;

    // 💡 퀘스트 완료 여부 확인
    bool isCompleted() const;

    // 💡 퀘스트 정보 표시
    void displayQuestInfo() const;

    // 💡 퀘스트 상태를 문자열로 반환
    std::string getStatusAsString() const;
};

#endif // QUEST_H
