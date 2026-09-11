# Third-Party Notices

이 문서는 프로젝트에 포함된 서드파티(제3자) 에셋 중 저작자 표시(attribution)가
필요한 라이선스로 배포된 항목을 기록한다. 각 항목은 최소 조건(저작자·라이선스명·
링크·수정 여부)을 명시해야 하며, 아직 실제 게임 빌드/인게임 크레딧 화면에
연결되지 않은 항목은 그 사실을 `상태` 필드에 명시한다.

---

## R01 — Quill (깃펜) 실루엣

- **에셋**: `unity/Assets/Art/UI/Quill/quill_base.png`,
  `unity/Assets/Art/UI/Quill/quill_shadow.png`
- **원본**: Game-icons.net "Quill" 아이콘
- **저작자**: Lorc
- **라이선스**: CC BY 3.0
- **라이선스 링크**: https://creativecommons.org/licenses/by/3.0/
- **원본 링크**: https://game-icons.net/1x1/lorc/quill.html
- **수정 사실**: 재채색(단색 실루엣 → 깃털/축 그라디언트 틴트 또는 그림자용
  단색 블러) 및 변형(업스케일, 세로 압축 등). 실루엣 형태 자체는 변경하지
  않음. (세부 내용은 `unity/Assets/Art/UI/Quill/quill_pivot_meta.json`의
  `_sources` 항목 참고)
- **상태**: **출시 전 반드시 반영 필요.** 현재 이 저작자 표시는 이 문서와
  `quill_pivot_meta.json`(Unity 임포트 대상 아닌 순수 메타 JSON)에만
  존재하며, 실제 게임 빌드나 플레이어가 보는 화면(인게임 크레딧 등)에
  노출되는 경로가 없다. 출시 전에 인게임 크레딧/설정 화면 등 플레이어가
  실제로 확인 가능한 위치에 이 표시를 반영해야 한다.

---

## 항목 추가 시 규칙

새로운 서드파티 에셋(이미지·폰트·사운드 등)을 CC BY류 저작자 표시 필수
라이선스로 도입할 때는 이 문서에 위와 동일한 형식으로 항목을 추가한다.
