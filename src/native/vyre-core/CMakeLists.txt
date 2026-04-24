cmake_minimum_required(VERSION 3.26)

add_library(vyre-core STATIC
    src/engine.cpp
    src/scanner_factory.cpp
    src/analysis/normalize.cpp
    src/analysis/oui_lookup.cpp
    src/analysis/analyzer_v1.cpp
    src/storage/report_store.cpp
    src/export/html_report.cpp
    src/capture/pcap_capture.cpp
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
        VYRE_CORE_OUI_DB_RELATIVE_PATH="data/oui_db.csv"
)

option(VYRE_ENABLE_PCAP "Enable libpcap/Npcap capture support" ON)

if (VYRE_ENABLE_PCAP)
    target_compile_definitions(vyre-core
        PUBLIC
            VYRE_HAS_PCAP=1
    )

    target_link_libraries(vyre-core
        PUBLIC
            pcap
    )
endif()

if (WIN32)
    target_sources(vyre-core
        PRIVATE
            src/platform/windows/scanner_wlanapi.cpp
    )

    target_link_libraries(vyre-core
        PUBLIC
            wlanapi
            ole32
    )

    target_compile_definitions(vyre-core
        PUBLIC
            VYRE_PLATFORM_WINDOWS=1
    )
endif()