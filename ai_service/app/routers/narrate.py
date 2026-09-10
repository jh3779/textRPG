from fastapi import APIRouter, Depends
from langchain_core.prompts import ChatPromptTemplate
from langchain_openai import ChatOpenAI
from pydantic import BaseModel, Field

from app.dependencies import get_llm
from app.schemas.narrate import NarrateRequest, NarrateResponse

router = APIRouter()

SYSTEM_PROMPT = (
    "당신은 던전 판타지 콘솔 게임의 서술자입니다. "
    "주어진 상황(event_type, context)을 한국어 1~3문장으로 분위기 있게 묘사하세요. "
    "과장하지 말고 게임 텍스트로 어울리는 간결한 톤을 유지하세요."
)

_PROMPT = ChatPromptTemplate.from_messages([
    ("system", SYSTEM_PROMPT),
    ("human", "event_type: {event_type}\ncontext: {context}"),
])


class Narration(BaseModel):
    text: str = Field(max_length=400, description="1~3문장 한국어 서술")


@router.post("/", response_model=NarrateResponse)
async def narrate_endpoint(
    request: NarrateRequest, llm: ChatOpenAI = Depends(get_llm)
) -> NarrateResponse:
    structured_llm = llm.with_structured_output(Narration)
    chain = _PROMPT | structured_llm
    result = await chain.ainvoke(
        {"event_type": request.event_type, "context": request.context}
    )
    return NarrateResponse(narration=result.text)
