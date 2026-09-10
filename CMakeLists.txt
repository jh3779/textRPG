cmake_minimum_required(VERSION 3.14)
project(TextRPG)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Include directories
include_directories(${PROJECT_SOURCE_DIR}/include)

# AI Dungeon Master (optional feature): fetch header-only HTTP + JSON libs
# used by AiNarrator to call the local ai_service/ FastAPI sidecar.
include(FetchContent)

FetchContent_Declare(
    httplib
    GIT_REPOSITORY https://github.com/yhirose/cpp-httplib.git
    GIT_TAG v0.18.3
)
set(HTTPLIB_REQUIRE_OPENSSL OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(httplib)

FetchContent_Declare(
    nlohmann_json
    GIT_REPOSITORY https://github.com/nlohmann/json.git
    GIT_TAG v3.11.3
)
FetchContent_MakeAvailable(nlohmann_json)

# Add source files
set(SOURCES
    main.cpp
    src/Game.cpp
    src/Player.cpp
    src/Enemy.cpp
    src/Item.cpp
    src/Inventory.cpp
    src/BattleSystem.cpp
    src/Map.cpp
    src/Quest.cpp
    src/Utils.cpp
    src/AiNarrator.cpp
)

# Add executable
add_executable(game ${SOURCES})

target_link_libraries(game PRIVATE httplib::httplib nlohmann_json::nlohmann_json)

# Set output directory
set_target_properties(game PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
)
