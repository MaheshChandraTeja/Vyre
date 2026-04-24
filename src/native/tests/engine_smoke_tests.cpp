#include "vyre/core/engine.hpp"
#include "vyre/interop/vyre_api.h"

#include <cassert>
#include <cstdint>
#include <cstring>
#include <iostream>
#include <memory>
#include <string>

namespace {

std::string TakeOwnership(char* value) {
    assert(value != nullptr);
    std::string managed(value);
    vyre_free_string(value);
    return managed;
}

} // namespace

int main() {
    const auto info = vyre::core::Engine::GetBuildInfo();
    assert(info.product_name == "Vyre");
    assert(!info.version.empty());
    assert(!info.compiler.empty());
    assert(!info.platform.empty());

    char* version_value = nullptr;
    assert(vyre_get_version(&version_value) == VYRE_STATUS_OK);
    const std::string version = TakeOwnership(version_value);
    assert(version == "0.1.0");

    std::int64_t scan_handle = 0;
    assert(vyre_scan_start(&scan_handle) == VYRE_STATUS_OK);
    assert(scan_handle > 0);

    vyre_scan_results_t results{};
    assert(vyre_scan_get_results(scan_handle, &results) == VYRE_STATUS_OK);
    assert(results.count == 3);
    assert(results.items != nullptr);
    assert(std::strcmp(results.items[0].ssid, "Kairais-5G") == 0);

    const std::string scan_json =
        "["
        "{\"bssid\":\"34:12:98:AB:CD:EF\",\"ssid\":\"Kairais-5G\",\"channel\":36,\"rssiDbm\":-42,\"frequencyMhz\":5180,\"security\":\"WPA3\",\"hidden\":false},"
        "{\"bssid\":\"14:23:45:67:89:10\",\"ssid\":\"Office-IoT\",\"channel\":6,\"rssiDbm\":-67,\"frequencyMhz\":2437,\"security\":\"WPA2\",\"hidden\":false}"
        "]";

    char* report_value = nullptr;
    assert(vyre_analyze_json(scan_json.c_str(), &report_value) == VYRE_STATUS_OK);
    const std::string report = TakeOwnership(report_value);
    assert(report.find("\"schema\":\"vyre.report.v1\"") != std::string::npos);
    assert(report.find("\"accessPoints\":[") != std::string::npos);
    assert(report.find("\"bssid\":\"34:12:98:AB:CD:EF\"") != std::string::npos);
    assert(report.find("\"bssid\":\"14:23:45:67:89:10\"") != std::string::npos);

    assert(vyre_scan_free_results(&results) == VYRE_STATUS_OK);
    assert(results.count == 0);
    assert(results.items == nullptr);

    assert(vyre_scan_stop(scan_handle) == VYRE_STATUS_OK);

    char* error_text = nullptr;
    assert(vyre_get_error_string(VYRE_STATUS_OK, &error_text) == VYRE_STATUS_OK);
    const std::string ok_text = TakeOwnership(error_text);
    assert(ok_text == "No error.");

    std::cout << "Native smoke tests passed. Version: " << version
              << " | Report: " << report << std::endl;
    return 0;
}
