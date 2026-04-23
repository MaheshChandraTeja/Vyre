set(VYRE_INTEROP_SOURCES
    src/vyre_api.cpp
)

if(APPLE AND CMAKE_SYSTEM_NAME STREQUAL "iOS")
    add_library(vyre-interop STATIC ${VYRE_INTEROP_SOURCES})
else()
    add_library(vyre-interop SHARED ${VYRE_INTEROP_SOURCES})
endif()

add_library(vyre::interop ALIAS vyre-interop)

target_include_directories(vyre-interop
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}/include
)

target_link_libraries(vyre-interop
    PRIVATE
        vyre::core
)

target_compile_features(vyre-interop PRIVATE cxx_std_20)
set_target_properties(vyre-interop PROPERTIES OUTPUT_NAME "vyre-interop")
target_compile_definitions(vyre-interop PRIVATE VYRE_INTEROP_BUILDING)
