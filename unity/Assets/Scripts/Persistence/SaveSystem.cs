/*
 * SaveSystem.cs
 *
 * 📝 역할: docs/design-system/unity-mapping.html M-05.
 * Application.persistentDataPath/save1.json에 JsonUtility로 저장/로드한다.
 * GameSession(순수 C#)을 UnityEngine.Application/System.IO와 연결하는 어댑터 역할만
 * 담당하고, 게임 규칙 자체는 건드리지 않는다(M-04 로직/뷰 분리 원칙).
 *
 * src/Game.cpp의 saveGame/loadGame과 동일한 검증 규칙:
 * version != 2 인 세이브는 거부한다(구버전 호환 안 함).
 */

using System.Collections.Generic;
using System.IO;
using TextRPG.GameLogic;
using UnityEngine;

namespace TextRPG.Persistence
{
    public static class SaveSystem
    {
        private const string SaveFileName = "save1.json";

        public static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }

        public static bool SaveExists()
        {
            return File.Exists(GetSavePath());
        }

        /// <summary>src/Game.cpp saveGame()과 동일한 필드를 JSON으로 기록한다.</summary>
        public static bool Save(GameSession session)
        {
            if (session?.Player == null || session.Map == null || session.Inventory == null)
            {
                return false;
            }

            var data = new SaveGameData
            {
                version = 2,
                class_id = session.Player.ClassId,
                hp = session.Player.GetHp(),
                max_hp = session.Player.GetMaxHp(),
                attack = session.Player.GetAttack(),
                defense = session.Player.GetDefense(),
                level = session.Player.GetLevel(),
                experience = session.Player.GetExperience(),
                gold = session.Player.GetGold(),
                mana = session.Player.GetMana(),
                max_mana = session.Player.GetMaxMana(),
                attack_speed = session.Player.GetAttackSpeed(),
                has_double_attack = session.Player.HasDoubleAttack ? 1 : 0,
                equipped_weapon = session.Player.EquippedWeaponName,
                equipped_armor = session.Player.EquippedArmorName,
                location = session.Map.GetCurrentLocationIndex(),
                game_round = session.GameRound,
                armory_looted = session.ArmoryLooted ? 1 : 0,
                goblin_defeated = session.GoblinDefeated ? 1 : 0,
            };

            for (int i = 0; i < session.Inventory.GetItemCount(); i++)
            {
                var item = session.Inventory.GetItem(i);
                data.items.Add(new ItemSaveData
                {
                    name = item.GetName(),
                    type = (int)item.GetItemType(),
                    value = item.GetValue(),
                    price = item.GetPrice(),
                    desc = item.GetDescription()
                });
            }

            foreach (var quest in session.Quests)
            {
                data.quests.Add(new QuestSaveData
                {
                    status = (int)quest.GetStatus(),
                    current = quest.GetCurrentCount()
                });
            }

            try
            {
                string json = JsonUtility.ToJson(data, true);
                string path = GetSavePath();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(path, json);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] 저장 실패: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// src/Game.cpp loadGame()과 동일하게 세이브를 읽어 GameSession에 반영한다.
        /// version != 2 이면 false를 반환하고 세션은 건드리지 않는다.
        /// </summary>
        public static bool Load(GameSession session)
        {
            string path = GetSavePath();
            if (!File.Exists(path))
            {
                return false;
            }

            SaveGameData data;
            try
            {
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<SaveGameData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] 로드 실패: {e.Message}");
                return false;
            }

            if (data == null || data.version != 2)
            {
                return false;
            }

            var characterClass = CharacterClassDatabase.Get(data.class_id);
            var player = characterClass != null
                ? new Player(GameSession.PlayerDisplayName, characterClass)
                : new Player(GameSession.PlayerDisplayName);
            player.LoadState(data.hp, data.max_hp, data.attack, data.defense,
                data.level, data.experience, data.gold, data.class_id,
                data.mana, data.max_mana,
                data.attack_speed, data.has_double_attack != 0,
                data.equipped_weapon, data.equipped_armor);

            var map = new Map();
            map.MoveToLocation(data.location);

            var inventory = new Inventory(5);
            foreach (var savedItem in data.items)
            {
                int clampedType = Utils.Clamp(savedItem.type, 0, 3);
                inventory.AddItem(new Item(savedItem.name, (ItemType)clampedType,
                    savedItem.value, savedItem.price, savedItem.desc));
            }

            var quests = new List<Quest>
            {
                new Quest("quest001", "던전 탈출", "보스의 방까지 도달해 던전의 주인을 쓰러뜨리세요.",
                    QuestType.REACH_GOAL, 1, 100, 80)
            };
            for (int i = 0; i < quests.Count && i < data.quests.Count; i++)
            {
                int clampedStatus = Utils.Clamp(data.quests[i].status, 0, 4);
                quests[i].LoadState((QuestStatus)clampedStatus, data.quests[i].current);
            }

            session.ResumeFromLoadedState(player, map, inventory, quests,
                data.game_round, data.armory_looted != 0, data.goblin_defeated != 0);
            return true;
        }
    }
}
