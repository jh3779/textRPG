import os

from dotenv import load_dotenv

load_dotenv()

from functools import lru_cache

from langchain_openai import ChatOpenAI


@lru_cache()
def get_llm() -> ChatOpenAI:
    return ChatOpenAI(model=os.getenv("OPENAI_MODEL", "gpt-4o-mini"), temperature=0.9)
