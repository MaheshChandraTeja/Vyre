cmake_minimum_required(VERSION 3.26)

add_library(vyre-interop SHARED
    src/vyre_api.cpp
)

add_library(vyre::interop ALIAS vyre-interop)

target_include_directories(vyre-interop
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}/include
)

target_compile_features(vyre-interop PUBLIC cxx_std_20)

target_compile_definitions(vyre-interop
    PRIVATE
        VYRE_INTEROP_BUILDING=1
)

target_link_libraries(vyre-interop
    PUBLIC
        vyre-core
)

set_target_properties(vyre-interop PROPERTIES
    OUTPUT_NAME "vyre-interop"
)
