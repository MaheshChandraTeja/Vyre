#include <catch2/catch_test_macros.hpp>

#include <cstring>
#include <string>
#include <string_view>

extern "C" {
  #include "wifi_api.h"
}

TEST_CASE("C ABI: GetVersion returns a string") {
  char* out = nullptr;
  const auto rc = vyre_wifi_get_version(&out);
  REQUIRE(rc == VYRE_WIFI_OK);
  REQUIRE(out != nullptr);

  std::string ver(out);
  vyre_wifi_free(out);

  REQUIRE(!ver.empty());
}

TEST_CASE("C ABI: AnalyzeJson returns report JSON (deterministic fields)") {
  const char* scan_json =
    "{"
      "\"report_id\":\"m2-fixture-001\","
      "\"generated_at_utc\":\"2026-02-01T12:00:00Z\","
      "\"captured_at_utc\":\"2026-02-01T11:59:00Z\","
      "\"platform\":\"windows\","
      "\"app_version\":\"0.1.0\","
      "\"access_points\":["
        "{"
          "\"bssid\":\"11:11:11:11:11:11\","
          "\"ssid\":\"HomeWiFi\","
          "\"band\":\"2.4ghz\","
          "\"channel\":6,"
          "\"rssi_dbm\":-43,"
          "\"security\":\"WPA2\""
        "},"
        "{"
          "\"bssid\":\"22:22:22:22:22:22\","
          "\"ssid\":\"CafeNet\","
          "\"band\":\"5ghz\","
          "\"channel\":36,"
          "\"rssi_dbm\":-61,"
          "\"security\":\"WPA2\""
        "}"
      "]"
    "}";

  char* out = nullptr;
  const auto rc = vyre_wifi_analyze_json(scan_json, &out);

  if (rc != VYRE_WIFI_OK) {
    FAIL(std::string("analyze failed: ") + vyre_wifi_last_error_message());
  }

  REQUIRE(out != nullptr);
  std::string report(out);
  vyre_wifi_free(out);

  REQUIRE(report.find("\"schema_version\":1") != std::string::npos);
  REQUIRE(report.find("\"report_id\":\"m2-fixture-001\"") != std::string::npos);
  REQUIRE(report.find("\"issues\"") != std::string::npos);
  REQUIRE(report.find("\"id\":\"report_generated\"") != std::string::npos);

  REQUIRE(report.find("\"id\":\"weak_signal\"") != std::string::npos);
}
