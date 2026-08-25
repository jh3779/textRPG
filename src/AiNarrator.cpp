#include "AiNarrator.h"

#include <httplib.h>
#include <nlohmann/json.hpp>

using json = nlohmann::json;

AiNarrator::AiNarrator() : enabled(false), host("127.0.0.1"), port(8000) {}

void AiNarrator::setEnabled(bool on) {
    enabled = on;
}

bool AiNarrator::isEnabled() const {
    return enabled;
}

bool AiNarrator::checkHealth() {
    try {
        httplib::Client cli(host, port);
        cli.set_connection_timeout(2, 0);
        cli.set_read_timeout(2, 0);
        auto res = cli.Get("/health");
        return res && res->status == 200;
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
            return fallback;
        }

        json parsed = json::parse(res->body);
        if (!parsed.contains("narration") || !parsed["narration"].is_string()) {
            return fallback;
        }

        std::string narration = parsed["narration"].get<std::string>();
        return narration.empty() ? fallback : narration;
    } catch (...) {
        return fallback;
    }
}
