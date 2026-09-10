#include "../include/Map.h"

Map::Map()
    : currentLocationIndex(0),
      totalLocations(5) {
    locations[0] = {"던전 입구", "차가운 바람이 새어 나오는 던전 입구에 서 있습니다.", false, false};
    locations[1] = {"갈림길", "왼쪽은 희미한 빛, 오른쪽은 낮은 울음소리가 들립니다.", false, false};
    locations[2] = {"낡은 무기고", "먼지 쌓인 상자 사이에서 쓸 만한 장비를 찾을 수 있을 것 같습니다.", false, false};
    locations[3] = {"어두운 통로", "고블린의 발자국이 바닥에 남아 있습니다.", true, false};
    locations[4] = {"보스의 방", "던전의 주인이 이곳을 지키고 있습니다.", true, true};
}

Location Map::getCurrentLocation() const {
    return locations[currentLocationIndex];
}

std::string Map::getCurrentLocationName() const {
    return locations[currentLocationIndex].name;
}

void Map::displayCurrentLocation(AiNarrator* narrator) const {
    const Location& location = locations[currentLocationIndex];
    std::string description = location.description;

    if (narrator != nullptr && narrator->isEnabled()) {
        description = narrator->narrate(
            "location",
            location.name + ": " + location.description,
            location.description);
    }

    std::cout << "\n[" << location.name << "]\n"
              << description << "\n";
}

void Map::moveToLocation(int locationIndex) {
    if (locationIndex < 0 || locationIndex >= totalLocations) {
        std::cout << "그곳으로는 이동할 수 없습니다.\n";
        return;
    }

    currentLocationIndex = locationIndex;
}

bool Map::hasEnemyInCurrentLocation() const {
    return locations[currentLocationIndex].hasEnemy;
}

void Map::moveNext() {
    if (currentLocationIndex + 1 < totalLocations) {
        currentLocationIndex++;
    } else {
        std::cout << "더 이상 앞으로 갈 수 없습니다.\n";
    }
}

void Map::movePrevious() {
    if (currentLocationIndex > 0) {
        currentLocationIndex--;
    } else {
        std::cout << "던전 밖으로 나왔습니다.\n";
    }
}

void Map::displayMap() const {
    std::cout << "\n[지도]\n";
    for (int i = 0; i < totalLocations; ++i) {
        std::cout << (i == currentLocationIndex ? "> " : "  ")
                  << i + 1 << ". " << locations[i].name << "\n";
    }
}

int Map::getCurrentLocationIndex() const {
    return currentLocationIndex;
}
