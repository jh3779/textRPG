# AI 던전마스터 서비스

textRPG 게임이 선택적으로 호출하는 로컬 FastAPI 사이드카. 위치/전투/승리/패배 서술을 매번 다르게 생성한다.
게임은 이 서비스가 꺼져 있어도 정적 텍스트로 정상 동작한다 — 이 서비스는 완전히 선택 사항이다.

## 설치

```bash
cd ai_service
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
cp .env.example .env   # .env에 OPENAI_API_KEY 입력
```

## 실행

```bash
uvicorn app.main:app --port 8000
```

게임 실행 후 메뉴에서 "AI 서술 모드"를 켜면 `GET /health`로 연결을 확인하고, 이후 위치 이동/전투 시작/승리/패배 시 `POST /narrate`를 호출해 서술을 받아온다.

## 엔드포인트

- `GET /health` → `{"status": "ok"}`
- `POST /narrate` — body: `{"event_type": "location|battle_start|victory|defeat", "context": "..."}` → `{"narration": "..."}`
