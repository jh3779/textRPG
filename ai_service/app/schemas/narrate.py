from typing import Literal

from pydantic import BaseModel, Field

EventType = Literal["location", "battle_start", "victory", "defeat"]


class NarrateRequest(BaseModel):
    event_type: EventType = Field(description="서술 종류")
    context: str = Field(min_length=1, max_length=500, description="현재 상황 설명 (지역/적/상태 등)")


class NarrateResponse(BaseModel):
    narration: str
