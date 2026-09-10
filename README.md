# Console Text RPG (C++)

간단한 텍스트 기반 콘솔 RPG입니다. 플레이어는 숫자 선택지를 통해 던전을 탐험하고, 전투와 이벤트를 거쳐 엔딩에 도달합니다.

## 주요 기능

* 선택지 기반 스토리 진행
* 1~3번 숫자 입력 검증
* 플레이어 스탯: HP, 공격력, 방어력, 레벨, 경험치, 골드
* 턴제 전투: 공격 또는 도망
* 짧은 던전 맵, 인벤토리, 퀘스트 보상
* 안전 지점 저장 및 이어하기
* 게임 오버 및 던전 클리어 엔딩
* (선택) AI 던전마스터: 위치/전투/승패 서술을 로컬 AI 서비스로 매번 다르게 생성

## 실행 방법

### CMake 사용

```bash
cmake -S . -B build
cmake --build build
./build/bin/game
```

CMake 최초 실행 시 AI 던전마스터 기능에 필요한 헤더 전용 라이브러리(cpp-httplib, nlohmann/json)를 `FetchContent`로 내려받으므로 인터넷 연결이 필요합니다(이후에는 캐시됨). 이 라이브러리들은 게임을 오프라인으로 빌드/실행하는 데는 영향이 없으며, AI 서술 모드를 켜지 않는 한 사용되지 않습니다.

### g++ 직접 빌드

`AiNarrator.cpp`가 cpp-httplib / nlohmann-json 헤더를 필요로 하므로, 먼저 `cmake -S . -B build`를 한 번 실행해 `build/_deps/`에 헤더를 받아둔 뒤 아래처럼 빌드하세요.

```bash
g++ -std=c++17 -Iinclude \
  -Ibuild/_deps/httplib-src -Ibuild/_deps/nlohmann_json-src/include \
  main.cpp src/Game.cpp src/Player.cpp src/Enemy.cpp src/Item.cpp src/Inventory.cpp \
  src/BattleSystem.cpp src/Map.cpp src/Quest.cpp src/Utils.cpp src/AiNarrator.cpp \
  -o game
./game
```

Windows PowerShell에서는 실행 파일 이름을 `game.exe`로 지정할 수 있습니다.

```powershell
g++ -std=c++17 -Iinclude -Ibuild/_deps/httplib-src -Ibuild/_deps/nlohmann_json-src/include main.cpp src/Game.cpp src/Player.cpp src/Enemy.cpp src/Item.cpp src/Inventory.cpp src/BattleSystem.cpp src/Map.cpp src/Quest.cpp src/Utils.cpp src/AiNarrator.cpp -o game.exe
./game.exe
```

### Java GUI 버전

상황 이미지, 상태 패널, 선택 버튼이 있는 GUI 버전도 실행할 수 있습니다.

```bash
javac java/TextRPGGui.java
java -cp java TextRPGGui
```

## 게임 진행 방식

1. 메인 메뉴에서 게임을 시작합니다.
2. 저장 파일이 있으면 `이어하기`로 이전 위치에서 재개할 수 있습니다.
3. 현재 위치 설명과 선택지가 출력됩니다.
4. 숫자를 입력해 이동, 상태 확인, 전투 행동을 선택합니다.
5. 전투에서는 공격하거나 도망칠 수 있습니다.
6. HP가 0이 되면 게임 오버, 보스를 처치하면 던전 클리어입니다.

## 저장 방식

게임 중 안전 지점에서 `저장하고 종료`를 선택하면 `saves/save1.txt`에 현재 상태가 저장됩니다. 전투 중에는 저장하지 않고, 위치 선택지에서만 저장할 수 있습니다.

## 선택 기능: AI 던전마스터

위치 설명, 전투 시작, 승리, 패배 서술을 로컬 AI 서비스가 매번 다르게 생성해줍니다. 서비스를 켜지 않아도 게임은 기존 정적 텍스트로 동일하게 동작합니다.

```bash
cd ai_service
python3 -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
cp .env.example .env   # OPENAI_API_KEY 입력
uvicorn app.main:app --port 8000
```

서비스를 켜둔 상태에서 게임 메인 메뉴의 `AI 서술 모드 켜기/끄기`를 선택하면 연결을 확인한 뒤 적용됩니다. 자세한 내용은 [`ai_service/README.md`](ai_service/README.md) 참고.

## 프로젝트 구조

```text
textRPG/
├── main.cpp
├── CMakeLists.txt
├── include/
│   ├── AiNarrator.h
│   ├── BattleSystem.h
│   ├── Enemy.h
│   ├── Game.h
│   ├── Inventory.h
│   ├── Item.h
│   ├── Map.h
│   ├── Player.h
│   ├── Quest.h
│   └── Utils.h
├── src/
│   ├── AiNarrator.cpp
│   ├── BattleSystem.cpp
│   ├── Enemy.cpp
│   ├── Game.cpp
│   ├── Inventory.cpp
│   ├── Item.cpp
│   ├── Map.cpp
│   ├── Player.cpp
│   ├── Quest.cpp
│   └── Utils.cpp
├── ai_service/            # 선택 기능: AI 던전마스터 (FastAPI)
├── data/
│   ├── enemies.txt
│   ├── items.txt
│   └── quests.txt
├── PRD.md
└── system_design.md
```

## 개발 환경 설정

이 저장소는 [pre-commit](https://pre-commit.com)으로 커밋 전 검증 훅을 강제합니다. 클론 후 한 번만 아래를 실행하면 활성화됩니다.

```bash
pip3 install --user pre-commit   # 또는: pipx install pre-commit
pre-commit install
```

훅이 막는 것:

* 머지 충돌 마커(`<<<<<<<`/`=======`/`>>>>>>>`)가 남은 채 커밋되는 것
* 대용량 파일 실수 추가(3MB 초과, 아트 에셋 경로는 예외)
* 대소문자만 다른 파일명 충돌, 개인키 파일 커밋
* Unity PlayMode/EditMode 테스트 실행 결과 XML(`scratch_*results*.xml`, `*playmode*results*.xml`, `*editmode*results*.xml`, `verify_playmode*.xml`, `verify_editmode*.xml`)이나 임시 검증 스크립트 산출물의 실수 커밋(DEC-133/DEC-142 사고 재발 방지 — 이런 파일은 `.gitignore`에도 등록돼 있어 기본적으로 `git add`에도 잡히지 않지만, `-f`로 강제 추가된 경우까지 이중으로 차단합니다)
* 후행 공백, 파일 끝 개행 누락, 혼용 줄바꿈(단, Unity가 자동 생성/재생성하는 `.meta`/`.asset`/`.unity`/`TextMesh Pro` 하위 파일과 `ProjectSettings/`, `Library/`, 아트 PNG는 검사 대상에서 제외)

`.pre-commit-config.yaml` 전체 훅 목록과 제외 규칙은 저장소 루트의 해당 파일을 참고하세요.

## 학습 포인트

* 조건문과 반복문을 이용한 선택지 처리
* 클래스와 객체를 이용한 상태 관리
* 헤더와 소스 파일 분리
* 참조와 포인터를 이용한 객체 간 상호작용
* 간단한 게임 루프와 전투 결과 처리
