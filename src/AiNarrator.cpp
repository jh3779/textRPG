#include "AiNarrator.h"

#include <httplib.h>
#include <nlohmann/json.hpp>

using json = nlohmann::json;

AiNarrator::AiNarrator()
    : enabled(false), host("127.0.0.1"), port(8000), consecutiveFailures(0) {}

void AiNarrator::setEnabled(bool on) {
    enabled = on;
}

bool AiNarrator::isEnabled() const {
    return enabled;
}

bool AiNarrator::isDegraded() const {
    return enabled && consecutiveFailures >= kDegradedThreshold;
}

bool AiNarrator::checkHealth() {
    try {
        httplib::Client cli(host, port);
        cli.set_connection_timeout(2, 0);
        cli.set_read_timeout(2, 0);
        auto res = cli.Get("/health");
        bool ok = res && res->status == 200;
        if (ok) {
            consecutiveFailures = 0;
        }
        return ok;
    } catch (...) {
        return false;
    }
}

std::string AiNarrator::narrate(const std::string& eventType,
                                 const std::string& context,
                                 const std::string& fallback) {
    if (!enabled) {
        return fallback;
    }

    try {
        json body = {
            {"event_type", eventType},
            {"context", context},
        };

        httplib::Client cli(host, port);
        cli.set_connection_timeout(2, 0);
        cli.set_read_timeout(2, 0);

        auto res = cli.Post("/narrate/", body.dump(), "application/json");
        if (!res || res->status != 200) {
            consecutiveFailures++;
            return fallback;
        }

        json parsed = json::parse(res->body);
        if (!parsed.contains("narration") || !parsed["narration"].is_string()) {
            consecutiveFailures++;
            return fallback;
        }

        std::string narration = parsed["narration"].get<std::string>();
        if (narration.empty()) {
            consecutiveFailures++;
            return fallback;
        }

        consecutiveFailures = 0;
        return narration;
    } catch (...) {
        consecutiveFailures++;
        return fallback;
    }
}
