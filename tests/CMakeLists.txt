include(FetchContent)

FetchContent_Declare(
  Catch2
  GIT_REPOSITORY https://github.com/catchorg/Catch2.git
  GIT_TAG v3.6.0
)
FetchContent_MakeAvailable(Catch2)

add_executable(wifi-core-tests
  test_core.cpp
  test_report_export.cpp
  test_interop_boundary.cpp
)

target_link_libraries(wifi-core-tests PRIVATE Catch2::Catch2WithMain vyre::wifi-core vyre::wifi-interop)
vyre_set_warnings(wifi-core-tests)

add_test(NAME wifi-core-tests COMMAND wifi-core-tests)
