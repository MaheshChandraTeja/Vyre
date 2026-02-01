add_library(wifi-core
  src/wifi_core.cpp
  src/export/json.cpp
  src/export/csv.cpp
)

add_library(vyre::wifi-core ALIAS wifi-core)

target_include_directories(wifi-core
  PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/include
    ${CMAKE_CURRENT_BINARY_DIR}/generated
)

configure_file(
  ${CMAKE_CURRENT_SOURCE_DIR}/include/wifi_core/version.h.in
  ${CMAKE_CURRENT_BINARY_DIR}/generated/wifi_core/version.h
  @ONLY
)

vyre_set_warnings(wifi-core)

set_target_properties(wifi-core PROPERTIES
  POSITION_INDEPENDENT_CODE ON
)
