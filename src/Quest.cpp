#include "../include/Quest.h"

#include <algorithm>

Quest::Quest(const std::string& id, const std::string& title,
             const std::string& desc, QuestType questType,
             int target, int exp, int gold)
    : questId(id),
      title(title),
      description(desc),
      type(questType),
      status(QuestStatus::NOT_STARTED),
      targetCount(std::max(1, target)),
      currentCount(0),
      rewardExp(exp),
      rewardGold(gold) {
}

std::string Quest::getTitle() const {
    return title;
}

std::string Quest::getDescription() const {
    return description;
}

QuestStatus Quest::getStatus() const {
    return status;
}

int Quest::getTargetCount() const {
    return targetCount;
}

int Quest::getCurrentCount() const {
    return currentCount;
}

int Quest::getRewardExp() const {
    return rewardExp;
}

int Quest::getRewardGold() const {
    return rewardGold;
}

void Quest::startQuest() {
    if (status == QuestStatus::NOT_STARTED) {
        status = QuestStatus::IN_PROGRESS;
    }
}

void Quest::updateProgress(int amount) {
    if (status != QuestStatus::IN_PROGRESS || amount <= 0) {
        return;
    }

    currentCount = std::min(targetCount, currentCount + amount);
    if (currentCount >= targetCount) {
        status = QuestStatus::COMPLETED;
        std::cout << "\n퀘스트 완료: " << title << "\n";
    }
}

void Quest::displayProgress() const {
    std::cout << currentCount << "/" << targetCount;
}

bool Quest::isCompleted() const {
    return status == QuestStatus::COMPLETED || status == QuestStatus::REWARDED;
}

void Quest::displayQuestInfo() const {
    std::cout << title << " [" << getStatusAsString() << "]\n"
              << description << "\n"
              << "진행도: ";
    displayProgress();
    std::cout << " | 보상: 경험치 " << rewardExp << ", 골드 " << rewardGold << "\n";
}

std::string Quest::getStatusAsString() const {
    switch (status) {
        case QuestStatus::NOT_STARTED:
            return "시작 전";
        case QuestStatus::IN_PROGRESS:
            return "진행 중";
        case QuestStatus::COMPLETED:
            return "완료";
        case QuestStatus::FAILED:
            return "실패";
        case QuestStatus::REWARDED:
            return "보상 수령";
        default:
            return "알 수 없음";
    }
}
