# art-assets/

textRPG Unity 버전 MVP 아트(DEC-112, `docs/06_open_questions.md`). 2026-09-04 사용자가 다운로드 폴더에 업로드 → Claude가 확인·크롭·정리 완료(DEC-113, DEC-114).

## 현재 상태 — 1차 업로드 완료 (20장)

| 카테고리 | 파일 | 비고 |
|---|---|---|
| 배경 씬 | `scene_던전입구.png`, `scene_갈림길.png`, `scene_무기고.png`, `scene_어두운통로.png`, `scene_보스의방.png` | 5장 — 원본이 2×2 콜라주 1장 + 단독 1장으로 와서 Pillow로 분리 |
| 직업 캐릭터 | `char_전사.png`, `char_도적.png`, `char_마법사.png` | 3장 — 개별 디자인시트에서 메인 전신 figure만 크롭(우측 detail 인셋 제외) |
| 몬스터 — 고블린 | `enemy_고블린_약소형.png`, `_날렵형.png`, `_거대형.png`, `_주술사형.png` | 4장 — **랜덤 조우 풀**로 전부 사용(DEC-114). 대표 단일 파일 없음 |
| 몬스터 — 던전 수호자 | `enemy_던전수호자.png`(=균형형, MVP 보스) + `_균형형.png`/`_중장형.png`/`_기동형.png`/`_마도형.png`(보존용 4종) | 균형형만 게임에 연결, 나머지는 스토리 확장 대비 보관(OQ-108) |
| 재질 텍스처 | `material_나무.png`, `material_양피지.png` | 2장 — seamless, 그대로 사용 가능 |
| 아이템 아이콘 | `item_회복물약.png`, `item_작은회복물약.png` | 2장 — 투명 배경, 그대로 사용 가능 |

## 알려진 한계
- 자동 크롭(좌표 기반)이라 인물·몬스터 이미지는 **진짜 투명 배경이 아님**(어두운 단색조 배경 그대로). UI 톤(`#0f0d1a`)과는 크게 안 어긋나지만, 픽셀 퍼펙트한 투명 PNG가 필요하면 별도 배경 제거 작업이 필요(OQ-106 후속).
- 직업 캐릭터 3장은 전부 남성 버전 원본에서 크롭함 — 원본에는 여성 변형 버전도 있었음(사용 안 함, 참고만).
- 고블린/던전 수호자 크롭 좌표는 시각적으로 눈대중 조정한 값이라 완벽한 프레이밍은 아닐 수 있음.

## 미결정
- OQ-107: 고블린 4변종이 스탯도 다른지(현재는 ASM-104로 "그림만 다름" 가정)
- OQ-108: 던전 수호자 보존 3변종을 실제로 어디에 쓸지

## 현재 상태 — 10차 작업 완료 (40장, DEC-134)

2026-09-09 09:53~10:23, ChatGPT Image 배치 생성(10분 간격×4장×10회) 40장을 사용자가 다운로드
폴더에 업로드 → 실제 다운로드 순서(타임스탬프)가 사용자 매니페스트 차수 순서와 정확히
일치하지 않을 수 있다는 전제 하에, 40장 전부를 직접 열어 그림 내용을 확인하고 매니페스트에
매칭함(파일명/타임스탬프 순서를 신뢰하지 않음). 9차(던전 보드 노드)에서는 실제로 배치 내
순서가 매니페스트 순서와 달랐음(전투 노드와 일반 노드가 뒤바뀜) — 내용 기준으로 바로잡음.

| 카테고리 | 파일 | 비고 |
|---|---|---|
| 무기 아이콘 — 기본 | `item_전사장검.png`, `item_전사대검.png`, `item_도적단검2자루.png`, `item_독아단검.png` | 4장 — 투명 배경, 그대로 사용 가능 |
| 무기·방어구 — 마법/상위 | `item_마법사지팡이.png`, `item_수정지팡이.png`, `item_가죽갑옷.png`, `item_강화판금갑옷.png` | 4장 — 등급 차이가 실루엣으로 구분됨 |
| 마나 아이템 | `item_마나결정.png`, `item_마나포션.png` | 2장 |
| 깃펜 필기 애니메이션 | `vfx_quill_idle.png`, `vfx_quill_writing_01~04.png`, `vfx_quill_writing_end.png` | 6장 — **주의**: 6장이 서로 거의 동일한 포즈/구도라 육안으로는 프레임 순서를 확정하기 어려움. 타임스탬프 생성 순서대로 idle→01→02→03→04→end 배정했으나, 실제 애니메이션 프레임 순서와 다를 수 있음(애매, 확신 못함) |
| 잉크 VFX — 방울/번짐 | `vfx_ink_drop.png`, `vfx_ink_splatter_01~03.png` | 4장 — 번짐 강도(작음→강함) 순으로 확실하게 구분됨 |
| 필기 보조 VFX | `vfx_ink_line.png`, `vfx_ink_endmark.png`, `vfx_quill_shadow.png`, `vfx_ink_dry.png` | 4장 |
| 마스크 (합성용) | `mask_character_softedge.png`, `mask_enemy_softedge.png`, `mask_ground_fade.png`, `mask_portrait_vignette.png` | 4장 — 흰색 실루엣/그라디언트. character(세로로 긴 인물형)와 enemy(더 넓은 어깨형)는 형태 차이로 구분했으나 완전히 확신하지는 못함(애매) |
| 필리그리 장식 | `ui_filigree_top_left.png`, `ui_filigree_top_right.png`, `ui_filigree_bottom_left.png`, `ui_filigree_bottom_right.png` | 4장 — 금박 넝쿨 무늬 굵은 쪽(모서리) 위치로 방향 확정, 확실 |
| 던전 보드 — 기본 | `board_dungeon_route.png`(SA-CHO GILDED DUNGEON 전체 경로), `board_node_normal.png`(횃불 복도), `board_node_battle.png`(교차 검+방패+피), `board_node_boss.png`(악마 해골 왕좌방) | 4장 — 확실. 원본 배치 내 순서가 매니페스트 순서와 달라 내용으로 재배정함. **`board_dungeon_route.png`는 제목·5개 노드 라벨·태그라인이 하드코딩된 완성형 목업이고 알파 채널도 없는 불투명 RGB라 그대로 동적 배경으로 재사용 불가 — 크롭/재생성 필요(차단 조건, OQ-110 참조)** |
| 던전 보드 — 상태 | `token_current_location.png`(받침대 위 별 장식 토큰), `board_node_cleared.png`(월계관+체크마크), `board_node_locked.png`(자물쇠+사슬), `board_route_completed.png`(양끝 원형 장식이 있는 금색 연결 바) | 4장 — 확실 |

### 알려진 한계 (10차)
- 깃펜 필기 프레임 6장(`vfx_quill_idle`~`vfx_quill_writing_end`)은 육안 구분이 사실상 불가능할 정도로 유사함 — Unity 반영 전에 실제 애니메이션 순서 재검토 필요(다음 단계 과제로 남김).
- 마스크 4장 중 character/enemy 두 장의 실루엣 형태 차이가 미묘해 완전한 확신은 없음.
- 이번 작업 범위는 `art-assets/` 정리까지이며, `unity/Assets/Art/` 복사나 셰이더/UI/씬 연결은 하지 않음(다음 단계).
