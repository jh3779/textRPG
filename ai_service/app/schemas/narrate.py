from pydantic import BaseModel, Field


class NarrateRequest(BaseModel):
    event_type: str = Field(description="location | battle_start | victory | defeat")
    context: str = Field(min_length=1, max_length=500, description="현재 상황 설명 (지역/적/상태 등)")


class NarrateResponse(BaseModel):
    narration: str
