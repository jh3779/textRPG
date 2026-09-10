/*
 * Quest.cs
 *
 * 📝 역할: include/Quest.h + src/Quest.cpp 그대로 포팅.
 * 상태 전이 NOT_STARTED → IN_PROGRESS → COMPLETED → REWARDED 정확히 유지
 * (05_state_machine.md STATE-103, FAILED는 콘솔 버전과 동일하게 자리만 유지하고 미사용).
 */

using System;

namespace TextRPG.GameLogic
{
    public class Quest
    {
        private readonly string questId;
        private readonly string title;
        private readonly string description;
        private readonly QuestType type;
        private QuestStatus status;
        private readonly int targetCount;
        private int currentCount;
        private readonly int rewardExp;
        private readonly int rewardGold;

        public Quest(string id, string title, string desc, QuestType questType,
            int target, int exp, int gold)
        {
            questId = id;
            this.title = title;
            description = desc;
            type = questType;
            status = QuestStatus.NOT_STARTED;
            targetCount = Math.Max(1, target);
            currentCount = 0;
            rewardExp = exp;
            rewardGold = gold;
        }

        public string GetQuestId() => questId;
        public string GetTitle() => title;
        public string GetDescription() => description;
        public QuestType GetQuestType() => type;
        public QuestStatus GetStatus() => status;
        public int GetTargetCount() => targetCount;
        public int GetCurrentCount() => currentCount;
        public int GetRewardExp() => rewardExp;
        public int GetRewardGold() => rewardGold;

        /// <summary>NOT_STARTED일 때만 IN_PROGRESS로 전이. src/Quest.cpp의 startQuest 그대로.</summary>
        public void StartQuest()
        {
            if (status == QuestStatus.NOT_STARTED)
            {
                status = QuestStatus.IN_PROGRESS;
            }
        }

        /// <summary>src/Quest.cpp의 updateProgress 그대로: IN_PROGRESS일 때만 진행, 목표 도달 시 COMPLETED.</summary>
        public void UpdateProgress(int amount = 1)
        {
            if (status != QuestStatus.IN_PROGRESS || amount <= 0)
            {
                return;
            }

            currentCount = Math.Min(targetCount, currentCount + amount);
            if (currentCount >= targetCount)
            {
                status = QuestStatus.COMPLETED;
            }
        }

        /// <summary>세이브 복원용. src/Quest.cpp의 loadState 그대로 — 제목/설명/보상은 건드리지 않는다.</summary>
        public void LoadState(QuestStatus savedStatus, int savedCurrentCount)
        {
            status = savedStatus;
            currentCount = Utils.Clamp(savedCurrentCount, 0, targetCount);
        }

        public bool IsCompleted() => status == QuestStatus.COMPLETED || status == QuestStatus.REWARDED;

        /// <summary>src/Quest.cpp의 getStatusAsString 그대로.</summary>
        public string GetStatusAsString()
        {
            switch (status)
            {
                case QuestStatus.NOT_STARTED:
                    return "시작 전";
                case QuestStatus.IN_PROGRESS:
                    return "진행 중";
                case QuestStatus.COMPLETED:
                    return "완료";
                case QuestStatus.FAILED:
                    return "실패";
                case QuestStatus.REWARDED:
                    return "보상 수령";
                default:
                    return "알 수 없음";
            }
        }
    }
}
