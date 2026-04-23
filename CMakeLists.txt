cmake_minimum_required(VERSION 3.26)

project(Vyre
    VERSION 0.1.0
    DESCRIPTION "Vyre native engine and interop"
    LANGUAGES CXX
)

option(VYRE_BUILD_TESTS "Build native test targets" ON)
option(VYRE_BUILD_INTEROP "Build the C ABI interop layer" ON)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/artifacts/lib")
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/artifacts/bin")
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/artifacts/bin")

if(MSVC)
    add_compile_options(/permissive- /W4 /EHsc /Zc:__cplusplus)
    add_compile_definitions(_CRT_SECURE_NO_WARNINGS)
else()
    add_compile_options(-Wall -Wextra -Wpedantic -Wconversion -Wshadow -Wnon-virtual-dtor)
endif()

add_subdirectory(src/native/vyre-core)

if(VYRE_BUILD_INTEROP)
    add_subdirectory(src/native/vyre-interop)
endif()

if(VYRE_BUILD_TESTS)
    include(CTest)
    enable_testing()
    add_subdirectory(src/native/tests)
endif()
