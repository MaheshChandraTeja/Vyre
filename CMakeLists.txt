cmake_minimum_required(VERSION 3.24)

project(Vyre
  VERSION 0.1.0
  DESCRIPTION "Vyre - wifi-core + interop + MAUI UI"
  LANGUAGES CXX
)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

include(CTest)

# Keep output predictable
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)

list(APPEND CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/cmake")
include(CompilerWarnings)
include(Sanitizers)
include(StaticAnalysis)

option(VYRE_ENABLE_SANITIZERS "Enable ASan/UBSan where supported" OFF)
option(VYRE_ENABLE_TIDY "Enable clang-tidy (if available)" OFF)
option(VYRE_BUILD_SHARED "Build shared libs (useful for interop)" ON)

add_subdirectory(wifi-core)
add_subdirectory(wifi-interop)
add_subdirectory(tests)

if(VYRE_ENABLE_SANITIZERS)
  vyre_enable_sanitizers()
endif()

if(VYRE_ENABLE_TIDY)
  vyre_enable_clang_tidy()
endif()
