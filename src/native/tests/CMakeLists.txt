add_executable(vyre-native-smoke-tests
    engine_smoke_tests.cpp
)

target_link_libraries(vyre-native-smoke-tests
    PRIVATE
        vyre::core
        vyre::interop
)

if(UNIX)
    set_target_properties(vyre-native-smoke-tests PROPERTIES BUILD_RPATH "$ORIGIN")
endif()

add_test(NAME vyre-native-smoke-tests COMMAND vyre-native-smoke-tests)
