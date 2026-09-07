# textRPG Unity DS — 디자인 시스템 문서

textRPG를 Unity 데스크톱 TRPG로 재설계하기 위한 와이어프레임·디자인 시스템 문서 세트. `design-interview` → `design-system-docs` 스킬로 생성.

> **v0.2 (2026-09-04):** 레이아웃을 보드게임(태블탑) 형식으로 재작업 — 나무 테이블 배경, 양피지 카드, 던전 보드 타일 맵, 캐릭터 시트(전신 실루엣). 자세한 배경은 `../07_visual_style.md` "재질" 절 참조.
> **v0.3 (2026-09-04):** 탐색 화면에 지역 배경 씬(Scene), 전투 화면에 플레이어·몬스터 대립 구도(Versus) 추가. `../06_open_questions.md`에 AI 아트 자동 생성(세계관 업로드 시 직업·배경·몬스터 일러스트 생성) 방향을 10-star 비전으로 기록.

## 보는 법

브라우저로 `design-system.html`을 로컬에서 열어 시작(상단 네비로 5개 페이지 이동, 우측 상단 "◐ 테마"로 라이트/다크 전환).

```
open docs/design-system/design-system.html
```

CDN 폰트(Pretendard·Roboto Mono·Cinzel·Material Symbols)를 쓰므로 인터넷 연결이 필요합니다. Artifact 등 CSP가 있는 환경에 올릴 땐 `styles.css`의 `@import` 폰트를 시스템 폰트 스택으로 교체하세요.

## 페이지 구성

| 페이지 | 내용 |
|---|---|
| [design-system.html](design-system.html) | 원칙 7가지(v0.2에서 "재질" 추가), ColorScheme "Gilded Dungeon", 타이포그래피, 간격/모양/고도, 모션 |
| [components.html](components.html) | 재질(나무·양피지)·초상화 프레임·HP바·상태바·타로 선택지 카드·캐릭터 시트·보드 타일 맵·주사위 배지·아이템/퀘스트 카드 + 제외 목록 |
| [patterns.html](patterns.html) | 전투 결과·인벤토리·퀘스트·직업 미선택 상태 패턴 + 접근성 표 |
| [unity-mapping.html](unity-mapping.html) | 토큰→Unity 애셋, 컴포넌트→프리팹(타로카드·캐릭터시트·보드·재질 스프라이트 포함), 화면→씬/패널, 상태→C#, 저장 방식 + Conflict List |
| [wireframes.html](wireframes.html) | 8개 화면(SCR-001~008) 보드게임 레이아웃(나무 테이블+양피지 카드+던전 보드) 목업 + 전체 흐름도 |

## 정본 (Source of Truth)

이 문서는 시각 토큰·컴포넌트·상태 표현·화면 흐름·구현 매핑만 소유한다. 아래 정본 문서와 어긋나면 정본이 우선:

- [../00_project_brief.md](../00_project_brief.md) — 프로젝트 목표·비전
- [../01_requirements.md](../01_requirements.md) — MoSCoW 범위
- [../02_ui_flow.md](../02_ui_flow.md), [../03_screen_contract.md](../03_screen_contract.md) — 화면 흐름·계약
- [../04_data_model.md](../04_data_model.md), [../05_state_machine.md](../05_state_machine.md) — 데이터·상태
- [../06_open_questions.md](../06_open_questions.md) — 미결정 사항(OQ-101~104)·결정 로그(DEC-101~107)
- [../07_visual_style.md](../07_visual_style.md) — 비주얼 스타일 확정본(다크 판타지 태블탑/보드게임 UI)

## 미결정 (구현 전 확인 필요)

`unity-mapping.html` § Conflict List 참조 — Unity 버전/UI 시스템(OQ-101), 세이브 덮어쓰기 확인 모달(OQ-102), 직업 정확한 구성(OQ-103), 아트 자산 확보 방법(OQ-104).
