/*
 * AiNarrator.h
 *
 * 📝 역할:
 * 로컬에서 실행 중인 ai_service/ FastAPI 서버에 상황을 보내
 * 매번 다른 서술 문장을 받아오는 선택적(optional) 기능.
 * 서비스가 꺼져 있거나 응답이 없어도 게임이 멈추지 않도록
 * 모든 실패는 fallback 문자열로 흡수한다.
 */

#ifndef AINARRATOR_H
#define AINARRATOR_H

#include <string>

class AiNarrator {
private:
    // 💡 연속 실패 횟수가 이 값 이상이면 isDegraded()가 true를 반환한다.
    // (한 번의 일시적 실패로 "응답 없음" 표시가 깜빡이지 않도록 여유를 둠)
    static const int kDegradedThreshold = 2;

    bool enabled;
    std::string host;
    int port;
    int consecutiveFailures;

public:
    AiNarrator();

    // 💡 AI 서술 모드 켜기/끄기
    void setEnabled(bool on);
    bool isEnabled() const;

    // 💡 최근 호출이 계속 실패해 사실상 정적 텍스트로만 동작 중인지 여부.
    // isEnabled()는 true인데 이게 true면, 사용자에게는 켜져 보이지만 실제로는 응답을 못 받는 상태.
    bool isDegraded() const;

    // 💡 서비스 연결 확인 (GET /health). 토글을 켤 때 호출. 성공 시 실패 카운트를 초기화한다.
    bool checkHealth();

    // 💡 서술 요청. 비활성화 상태이거나 실패하면 즉시 fallback 반환 (never throws)
    std::string narrate(const std::string& eventType,
                         const std::string& context,
                         const std::string& fallback);
};

#endif // AINARRATOR_H
