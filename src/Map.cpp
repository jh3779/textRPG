/*
 * Map.cpp
 *
 * 📚 학습 개념:
 * - 배열 초기화: 여러 struct 값을 한번에 설정
 * - 배열 인덱싱: locations[i]로 각 지역 접근
 *
 * 💡 팁:
 * - 생성자에서 locations[0], locations[1] 등을 초기화
 * - currentLocationIndex는 0부터 시작 (첫 지역)
 * - 각 지역에 고유한 이름, 설명, 적 유무 설정
 */

#include "../include/Map.h"

 // 💡 생성자 구현 예시:
 // Map::Map() {
 //     // 0번 지역: 던전 입구
 //     locations[0] = {"던전 입구", "어두운 던전의 입구에 서있다.", false, false};
 //     
 //     // 1번 지역: 첫 번째 방
 //     locations[1] = {"첫 번째 방", "손가락만한 귀뚜라미들이 우글거린다.", true, false};
 //     
 //     // ... 나머지 지역들 설정 ...
 //     
 //     currentLocationIndex = 0;
 //     totalLocations = 10;  // 총 10개 지역
 // }

 // 💡 moveToLocation() 함수 구현 예시:
 // void Map::moveToLocation(int locationIndex) {
 //     if (locationIndex >= 0 && locationIndex < totalLocations) {
 //         currentLocationIndex = locationIndex;
 //         displayCurrentLocation();
 //     } else {
 //         std::cout << "이동할 수 없습니다!" << std::endl;
 //     }
 // }

 // 👇 여기에 나머지 함수들의 구현 코드를 작성하세요
