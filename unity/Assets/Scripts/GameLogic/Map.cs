/*
 * Map.cs
 *
 * 📝 역할: include/Map.h + src/Map.cpp 그대로 포팅.
 * DEC-101: 5개 고정 지역 그대로 — 지역 추가 금지.
 * 콘솔 버전은 AiNarrator로 위치 서술을 동적으로 받아올 수 있었으나(narrator 파라미터),
 * 이번 포팅 범위에는 AiNarrator가 포함되지 않아(요청 범위 밖) 정적 description만 사용한다.
 */

namespace TextRPG.GameLogic
{
    public struct Location
    {
        public string Name;
        public string Description;
        public bool HasEnemy;
        public bool IsDeadEnd;

        public Location(string name, string description, bool hasEnemy, bool isDeadEnd)
        {
            Name = name;
            Description = description;
            HasEnemy = hasEnemy;
            IsDeadEnd = isDeadEnd;
        }
    }

    public class Map
    {
        // 💡 src/Map.cpp 생성자와 동일한 순서·이름·설명·플래그.
        private readonly Location[] locations = new Location[5];
        private int currentLocationIndex;
        private readonly int totalLocations;

        public Map()
        {
            totalLocations = 5;
            currentLocationIndex = 0;

            locations[0] = new Location("던전 입구", "차가운 바람이 새어 나오는 던전 입구에 서 있습니다.", false, false);
            locations[1] = new Location("갈림길", "왼쪽은 희미한 빛, 오른쪽은 낮은 울음소리가 들립니다.", false, false);
            locations[2] = new Location("낡은 무기고", "먼지 쌓인 상자 사이에서 쓸 만한 장비를 찾을 수 있을 것 같습니다.", false, false);
            locations[3] = new Location("어두운 통로", "고블린의 발자국이 바닥에 남아 있습니다.", true, false);
            locations[4] = new Location("보스의 방", "던전의 주인이 이곳을 지키고 있습니다.", true, true);
        }

        public Location GetCurrentLocation() => locations[currentLocationIndex];

        public string GetCurrentLocationName() => locations[currentLocationIndex].Name;

        /// <summary>지정한 인덱스로 이동. 범위를 벗어나면 아무 일도 일어나지 않는다(src/Map.cpp moveToLocation 그대로).</summary>
        public void MoveToLocation(int locationIndex)
        {
            if (locationIndex < 0 || locationIndex >= totalLocations)
            {
                return;
            }

            currentLocationIndex = locationIndex;
        }

        public bool HasEnemyInCurrentLocation() => locations[currentLocationIndex].HasEnemy;

        public void MoveNext()
        {
            if (currentLocationIndex + 1 < totalLocations)
            {
                currentLocationIndex++;
            }
        }

        public void MovePrevious()
        {
            if (currentLocationIndex > 0)
            {
                currentLocationIndex--;
            }
        }

        public int GetCurrentLocationIndex() => currentLocationIndex;

        public int GetTotalLocations() => totalLocations;

        public Location GetLocationAt(int index) => locations[index];
    }
}
