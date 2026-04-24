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

if (WIN32)
    set(VYRE_ENABLE_PCAP_DEFAULT OFF)
else()
    set(VYRE_ENABLE_PCAP_DEFAULT ON)
endif()

option(VYRE_ENABLE_PCAP "Enable libpcap/Npcap capture support" ${VYRE_ENABLE_PCAP_DEFAULT})

if (VYRE_ENABLE_PCAP)
    find_path(VYRE_PCAP_INCLUDE_DIR pcap.h)
    find_library(VYRE_PCAP_LIBRARY NAMES pcap wpcap)

    if (NOT VYRE_PCAP_INCLUDE_DIR OR NOT VYRE_PCAP_LIBRARY)
        message(WARNING "libpcap/Npcap was requested but pcap.h or the pcap library was not found. Building capture stubs.")
        set(VYRE_ENABLE_PCAP OFF)
    endif()
endif()

if (VYRE_ENABLE_PCAP)
    target_include_directories(vyre-core
        PRIVATE
            ${VYRE_PCAP_INCLUDE_DIR}
    )

    target_compile_definitions(vyre-core
        PUBLIC
            VYRE_HAS_PCAP=1
    )

    target_link_libraries(vyre-core
        PUBLIC
            ${VYRE_PCAP_LIBRARY}
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
