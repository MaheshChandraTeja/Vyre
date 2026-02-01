add_library(wifi-interop SHARED
  src/wifi_api.cpp
)

add_library(vyre::wifi-interop ALIAS wifi-interop)

target_include_directories(wifi-interop
  PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/include
)

target_link_libraries(wifi-interop PRIVATE vyre::wifi-core)

target_compile_definitions(wifi-interop PRIVATE WIFI_INTEROP_BUILD=1)

vyre_set_warnings(wifi-interop)

set_target_properties(wifi-interop PROPERTIES
  POSITION_INDEPENDENT_CODE ON
)

set_target_properties(wifi-interop PROPERTIES OUTPUT_NAME "wifi-interop")
