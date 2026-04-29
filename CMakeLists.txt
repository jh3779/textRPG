cmake_minimum_required(VERSION 3.10)
project(TextRPG)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Include directories
include_directories(${PROJECT_SOURCE_DIR}/include)

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
)

# Add executable
add_executable(game ${SOURCES})

# Set output directory
set_target_properties(game PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
)
