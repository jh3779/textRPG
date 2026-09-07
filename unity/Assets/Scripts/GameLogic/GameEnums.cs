/*
 * GameEnums.cs
 *
 * 콘솔 C++ 버전의 enum을 그대로 C#으로 포팅한다.
 * 이름·값 순서는 원본과 동일하게 유지한다 (수치/분기 정확성 요구, 00_project_brief.md 핵심 가치).
 *
 * - BattleResult, QuestStatus, QuestType, ItemType: include/BattleSystem.h, include/Quest.h,
 *   include/Item.h 그대로.
 * - GameState: 콘솔 버전의 include/Game.h (MENU/PLAYING/GAME_OVER/QUIT) 대신
 *   docs/05_state_machine.md STATE-101(Unity 버전으로 이미 재정의됨)을 따른다.
 *   TITLE·CLASS_SELECT·BATTLE·VICTORY가 추가/치환된 상태값이며, 이번 포팅 지시사항에
 *   명시적으로 포함된 변경이다.
 */

namespace TextRPG.GameLogic
{
    /// <summary>
    /// 게임 진행 상태. docs/05_state_machine.md STATE-101 그대로.
    /// </summary>
    public enum GameState
    {
        TITLE,
        CLASS_SELECT,
        PLAYING,
        BATTLE,
        GAME_OVER,
        VICTORY
    }

    /// <summary>
    /// 전투 결과. include/BattleSystem.h의 BattleResult 그대로(이름·값 순서 동일).
    /// </summary>
    public enum BattleResult
    {
        PLAYER_WIN,
        PLAYER_LOSE,
        PLAYER_FLEE,
        FLEE_FAILED
    }

    /// <summary>
    /// 퀘스트 상태. include/Quest.h의 QuestStatus 그대로.
    /// </summary>
    public enum QuestStatus
    {
        NOT_STARTED,
        IN_PROGRESS,
        COMPLETED,
        FAILED,
        REWARDED
    }

    /// <summary>
    /// 퀘스트 타입. include/Quest.h의 QuestType 그대로.
    /// </summary>
    public enum QuestType
    {
        KILL_ENEMY,
        COLLECT_ITEM,
        EXPLORE,
        REACH_GOAL
    }

    /// <summary>
    /// 아이템 타입. include/Item.h의 ItemType 그대로.
    /// </summary>
    public enum ItemType
    {
        WEAPON,
        ARMOR,
        POTION,
        CONSUMABLE
    }
}
