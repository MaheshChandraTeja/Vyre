add_library(vyre-core STATIC
    src/engine.cpp
)

add_library(vyre::core ALIAS vyre-core)

target_include_directories(vyre-core
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}/include
)

target_compile_features(vyre-core PUBLIC cxx_std_20)

target_compile_definitions(vyre-core
    PUBLIC
        VYRE_CORE_VERSION_MAJOR=${PROJECT_VERSION_MAJOR}
        VYRE_CORE_VERSION_MINOR=${PROJECT_VERSION_MINOR}
        VYRE_CORE_VERSION_PATCH=${PROJECT_VERSION_PATCH}
)
