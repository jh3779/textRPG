/*
 * SaveData.cs
 *
 * 📝 역할: docs/design-system/unity-mapping.html M-05 그대로.
 * 콘솔 버전 saves/save1.txt(version=2, src/Game.cpp saveGame/loadGame이 정본)의 필드를
 * 그대로 JSON 필드로 옮기고 class_id만 추가한다. UnityEngine.JsonUtility로 직렬화하기
 * 위해 [Serializable] + public 필드로 구성한다(프로퍼티는 JsonUtility가 못 읽음).
 */

using System;
using System.Collections.Generic;

namespace TextRPG.Persistence
{
    [Serializable]
    public class ItemSaveData
    {
        public string name;
        public int type;   // GameLogic.ItemType의 정수값 (원본 static_cast<int>(item->getType())와 동일)
        public int value;
        public int price;
        public string desc;
    }

    [Serializable]
    public class QuestSaveData
    {
        public int status;  // GameLogic.QuestStatus의 정수값
        public int current;
    }

    [Serializable]
    public class SaveGameData
    {
        public int version = 2; // 콘솔 버전과 동일하게 2 고정. 1 이하는 읽을 때 거부(구버전 세이브 비호환).

        public string class_id; // 신규 필드 — 04_data_model.md ENT-102 / M-05

        public int hp;
        public int max_hp;
        public int attack;
        public int defense;
        public int level;
        public int experience;
        public int gold;

        public int location;
        public int game_round;
        public int armory_looted;   // 0/1 (원본과 동일하게 bool 대신 int로 저장)
        public int goblin_defeated; // 0/1

        public List<ItemSaveData> items = new List<ItemSaveData>();
        public List<QuestSaveData> quests = new List<QuestSaveData>();
    }
}
