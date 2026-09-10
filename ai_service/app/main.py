from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse

from app.routers import health, narrate

app = FastAPI(title="textRPG AI 던전마스터", version="0.1.0")


@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    return JSONResponse(
        status_code=500, content={"detail": "서버 오류가 발생했습니다. 잠시 후 다시 시도해주세요."}
    )


app.include_router(health.router)
app.include_router(narrate.router, prefix="/narrate", tags=["Narrate"])
