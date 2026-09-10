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

        // 신규 필드(DEC-123, Unity 한정) — 마나는 HP처럼 전투 간 이월되는 지속 자원이라 저장이 필요하다.
        // 구버전(이 필드가 없는 version=2) 세이브를 읽으면 JsonUtility가 0/0으로 채우는데,
        // Player.LoadState()가 max_mana<=0이면 클래스 기반 기본값을 그대로 유지하도록 방어해 둠.
        public int mana;
        public int max_mana;

        // 신규 필드(DEC-129, Unity 한정) — 무기/방어구를 장착·교체할 수 있게 되면서 공격속도·쌍검
        // 패시브·현재 장착 중인 무기/방어구 이름도 저장이 필요해졌다. 구버전 세이브는 이 필드들이
        // 전부 기본값(0/false/빈 문자열)으로 채워지는데, Player.LoadState()가 equipped_weapon이
        // 비어 있으면 캐릭터 생성 시 채워둔 기본 무기를 그대로 유지하고, equipped_armor가 비어
        // 있으면(구버전이든 실제로 맨몸이든) 방어구 없음으로 처리하도록 방어해 둠.
        public int attack_speed;
        public int has_double_attack; // 0/1
        public string equipped_weapon;
        public string equipped_armor;

        public int location;
        public int game_round;
        public int armory_looted;   // 0/1 (원본과 동일하게 bool 대신 int로 저장)
        public int goblin_defeated; // 0/1

        public List<ItemSaveData> items = new List<ItemSaveData>();
        public List<QuestSaveData> quests = new List<QuestSaveData>();
    }
}
