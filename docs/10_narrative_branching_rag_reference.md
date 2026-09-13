# 서사 분기 판단 참고자료 — narrative_qa(rag-project) 기법 조사

작성일: 2026-09-13
상태: **조사자료 (설계 미착수)** — textRPG에 분기형 서사 시스템을 실제로 도입하기로
결정된 것은 아니다. 나중에 "유저 입력 텍스트를 보고 어떤 서사 분기로 보낼지"를
설계할 때 바로 참고할 수 있도록 남겨두는 문서.

원본: `~/Developer/rag-project`(별도 저장소, LG CNS AI 캠퍼스 팀 프로젝트)의
`narrative_qa/` 모듈. textRPG와 무관한 프로젝트이며, 코드를 그대로 가져다 쓸 수
없다(라이선스·의존성·언어(Python) 자체가 다름) — 여기 정리하는 건 **설계 패턴과
아이디어**이지 이식 가능한 코드가 아니다. 이 문서 하나로 원본을 대체하지 않으며,
실제 참고가 필요하면 원본 저장소를 직접 열어봐야 한다(경로: 위 참고).

## 0. narrative_qa가 뭔지 (배경)

셜록 홈즈 같은 미스터리 소설을 읽어나가는 독자에게, "지금까지 읽은 지점(K)"보다
미래의 사실은 절대 노출하지 않으면서 지식그래프 기반으로 질문에 답하는 시스템.
2026-09-10 금융 도메인으로 피벗하며 "현재 작업 대상 아님"으로 동결됐지만, 핵심
메커니즘(`visible_at(K)`)은 Neo4j로 검증까지 끝난 상태로 레포에 남아있다.

## 1. 입력 텍스트 → 분기 타입 분류 (가장 직접 적용 가능)

**근거**: `narrative_qa/qa/chain.py:39-41`(`_analyze()`), `narrative_qa/qa/schema.py:11-15`
(`QueryAnalysis`), 프롬프트는 `narrative_qa/qa/prompts.py:6-13`(`ANALYZE_SYS`).

```python
def _analyze(question: str) -> QueryAnalysis:
    r = complete(ANALYZE_SYS, question, tier="mini", schema=QueryAnalysis)
    return r.parsed or QueryAnalysis(question_type="fact", target_entities=[], needs_graph=True)
```

자유 형식 텍스트(독자 질문)를 LLM에 넣고 **구조화된 스키마**로 강제 반환받아
이후 처리 경로를 가른다:
- `question_type: fact|retcon|relationship|irony|community|meta` — 6가지 분기
  타입, 각각의 판단 기준이 프롬프트에 자연어로 명시돼 있음(예: "retcon = 앞에서
  한 방식으로 설명됐다가 뒤에서 정정될 수 있는 것").
- `target_entities: list[str]` — 질문이 지목하는 인물/장소/조직 이름 추출.
- `needs_graph: bool` — 아래 2번 참고.

**같은 패턴이 다른 프로젝트에도 반복됨**: `disclosure_lit/factcheck/claim.py:16-18`
(`extract_claim`, 시그니처만 있고 미구현이지만 "루머 텍스트 → 구조화 클레임" 동일
패턴) — 두 프로젝트가 독립적으로 수렴한 설계라는 뜻.

**textRPG 적용 아이디어**: "자유 텍스트 입력 → LLM 분류 → 구조화 스키마 → 그
값으로 switch/if 분기" 3단 구조 자체를 그대로 템플릿으로 가져다 쓸 수 있다. 유저가
자유 서술로 행동/대사/스토리를 입력하면, 그 입력을 이런 식의 스키마(예:
`action_type: attack|dialogue|explore|item_use|...`, `target_entities`,
`needs_world_state: bool`)로 먼저 분류한 뒤 하위 게임 로직으로 라우팅.

## 2. "이 분기는 깊은 추론이 필요한가" 게이트

**근거**: `QueryAnalysis.needs_graph`(`narrative_qa/qa/schema.py:14`).

단순 문서검색으로 충분한 분기와, 그래프 다단계 추론(관계·타임라인)이 필요한
분기를 분류 단계에서 미리 나눈다. relationship/irony/community 타입과 다단계
추론이 필요한 fact 질문만 `needs_graph=true`로 표시된다(프롬프트 마지막 줄).

**textRPG 적용 아이디어**: 유저 입력이 단순 키워드성 분기(고정 응답표로 충분)인지,
기존 세계관·인물 관계·퀘스트 상태를 실제로 조회해야 하는 분기인지 먼저 이진
플래그로 걸러내면, 매 분기마다 무겁게(그래프/DB 조회) 처리하지 않고 비용을
아낄 수 있다.

## 3. Bi-temporal 그래프 — MVP를 넘어설 때 가장 흥미로운 확장 포인트

**근거**: `narrative_qa/retrieval/visibility.py:1-19`(모듈 docstring),
`narrative_qa/graph/schema.py:14-38`(엔티티: Character/Location/Organization/Item,
관계: AllyOf/EnemyOf/Suspects 등).

각 사실(그래프 엣지)에 `valid_at`(성립 시점)·`invalid_at`(반박·정정된 시점)을
붙여 "그때는 맞았지만 지금은 틀린 사실"을 시간축으로 관리한다(엣지를 지우지
않고 무효화 시점만 기록 — retcon 처리). 현재 narrative_qa는 이걸 **선형 축
하나(K = 독자가 읽은 위치)**에만 쓴다.

**MVP 외 확장 아이디어(가장 핵심)**: 이 구조를 선형이 아니라 **분기형**으로
일반화하면 — 즉 `invalid_at`을 "시점"이 아니라 "특정 유저 선택 이후"로
재정의하면 — 플레이어가 특정 선택을 한 순간 이전 사실이 무효화되고 새 사실이
유효해지는 걸 그래프 레벨에서 표현할 수 있다. **지금 코드는 이걸 하지 않는다
(단일 시간축뿐)** — 실제로 분기형으로 쓰려면 별도 설계·구현이 필요하다. 다만
엔티티/관계 스키마 자체(Character/Location/Organization/Item +
AllyOf/EnemyOf/Suspects)는 분기별 세계관 상태 추적에 그대로 재사용 가능한
형태다.

## 4. 생성 후 검증(guard) — 분기 결과의 안전판

**근거**: `narrative_qa/qa/chain.py:65-79`(`_guard()`),
`narrative_qa/eval/leak_detect.py`(정규식·별칭·패러프레이즈 기반 결정론적 탐지),
`narrative_qa/eval/judges.py`(LLM judge). "둘 중 하나라도 걸리면 leak"으로 이중
검증.

LLM이 생성한 답이 "아직 안 밝혀진 사실"을 누설했는지 별도 LLM judge로 재검사하고,
걸리면 자동으로 안전한 수정본(`revised_answer`)을 만든다.

**textRPG 적용 아이디어**: 유저 입력 기반으로 서사 분기를 자동 생성하는 시스템을
만든다면, "이 분기가 아직 노출되면 안 되는 세계관 정보(미공개 퀘스트 결말,
숨겨진 관계 등)를 실수로 흘리지 않았는가"를 결정론적 검사 + LLM judge 이중으로
자동 검증하는 데 그대로 적용 가능.

## 5. 종합 판단

| 번호 | 메커니즘 | 재사용 난이도 | 적용 시나리오 |
|---|---|---|---|
| 1 | 텍스트→분류 스키마 | 낮음 (패턴만 가져오면 됨) | 유저 자유 입력을 게임 분기 타입으로 즉시 라우팅 |
| 2 | needs_graph 게이트 | 낮음 | 분기 처리 비용(단순 응답 vs 세계관 조회) 사전 분리 |
| 3 | Bi-temporal 그래프 | 높음 (선형→분기형 재설계 필요) | 진짜 분기형 서사 엔진의 핵심 데이터 모델 |
| 4 | 생성 후 guard 검증 | 중간 (LLM judge 프롬프트 이식) | AI 생성 분기 텍스트의 스포일러/일관성 자동 검증 |

가장 적은 수정으로 "유저 입력 → 분기 판단"에 쓸 수 있는 건 **1·2번**이고,
진짜 분기형 서사 엔진을 노린다면 **3번(bi-temporal invalid_at의 분기 재해석)**이
핵심 설계 포인트가 된다.

## 6. 이 문서의 한계

- 원본(`~/Developer/rag-project`)의 특정 커밋 시점 스냅샷 조사다. 원본이 계속
  개발 중이므로 코드가 이 문서 작성 시점과 달라졌을 수 있다 — 실제 적용 직전에
  원본을 다시 확인할 것.
- narrative_qa 자체가 이미 "동결된 이전 프로젝트"라 이 패턴들이 실전에서 얼마나
  잘 작동하는지(정확도·비용·레이턴시)는 원본 프로젝트에서도 별도 평가(`eval/`)
  결과를 확인하지 않고는 보증할 수 없다.
- textRPG에 실제로 적용할지, 어떤 형태로 적용할지는 별도 설계 논의가 필요하다 —
  이 문서는 "무엇이 가능한가"의 조사자료이지 "무엇을 할 것인가"의 결정문서가
  아니다.

## 7. 다음 단계 (실제 적용 시)

1. textRPG에 유저 자유 텍스트 입력이 실제로 필요한 기능(예: 커스텀 행동 입력,
   대화 선택 등)이 확정되면, 1·2번 패턴부터 프로토타입으로 시작.
2. 분기형 서사가 실제 요구사항이 되면 `docs/06_open_questions.md`에 OQ로 먼저
   등록하고, 3번(bi-temporal 재해석) 설계를 별도 문서로 구체화한 뒤 착수.
3. AI로 분기 텍스트를 생성하는 기능이 들어가면 4번(guard 검증)을 처음부터 함께
   설계 — 나중에 붙이면 리팩터링 비용이 커진다.
