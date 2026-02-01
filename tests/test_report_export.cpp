#include <catch2/catch_test_macros.hpp>

#include <string_view>

#include "wifi/export.hpp"
#include "wifi/report.hpp"

using namespace vyre::wifi;

static Report fixture_report() {
  Report r;
  r.report_id = "fixture-001";
  r.generated_at_utc = "2026-02-01T12:00:00Z";
  r.platform = "windows";
  r.app_version = "0.1.0";

  r.scan.captured_at_utc = "2026-02-01T11:59:00Z";

  r.scan.access_points = {
    AccessPoint{
      .bssid = "22:22:22:22:22:22",
      .ssid = "CafeNet",
      .band = Band::GHz5,
      .channel = 36,
      .rssi_dbm = -61,
      .security = std::string("WPA2")
    },
    AccessPoint{
      .bssid = "11:11:11:11:11:11",
      .ssid = "HomeWiFi",
      .band = Band::GHz2_4,
      .channel = 6,
      .rssi_dbm = -43,
      .security = std::string("WPA2")
    }
  };

  r.issues = {
    Issue{.id="weak_signal", .severity=IssueSeverity::Warning, .message="Signal is weak on 5GHz band."},
    Issue{.id="channel_congestion", .severity=IssueSeverity::Info, .message="2.4GHz channel appears busy."}
  };

  r.recommendations = {
    Recommendation{.id="move_router", .priority=RecommendationPriority::High, .message="Move router to a central location."},
    Recommendation{.id="switch_channel", .priority=RecommendationPriority::Medium, .message="Try a less congested channel."}
  };

  return r;
}

TEST_CASE("JSON export is deterministic and matches contract") {
  const auto report = fixture_report();
  const auto json = vyre::wifi::exporter::to_json(report);

  const std::string expected =
    "{\"schema_version\":1,"
    "\"report_id\":\"fixture-001\","
    "\"generated_at_utc\":\"2026-02-01T12:00:00Z\","
    "\"platform\":\"windows\","
    "\"app_version\":\"0.1.0\","
    "\"scan\":{"
      "\"captured_at_utc\":\"2026-02-01T11:59:00Z\","
      "\"access_points\":["
        "{"
          "\"bssid\":\"22:22:22:22:22:22\","
          "\"ssid\":\"CafeNet\","
          "\"band\":\"5ghz\","
          "\"channel\":36,"
          "\"rssi_dbm\":-61,"
          "\"security\":\"WPA2\""
        "},"
        "{"
          "\"bssid\":\"11:11:11:11:11:11\","
          "\"ssid\":\"HomeWiFi\","
          "\"band\":\"2.4ghz\","
          "\"channel\":6,"
          "\"rssi_dbm\":-43,"
          "\"security\":\"WPA2\""
        "}"
      "]"
    "},"
    "\"issues\":["
      "{"
        "\"id\":\"channel_congestion\","
        "\"severity\":\"info\","
        "\"message\":\"2.4GHz channel appears busy.\""
      "},"
      "{"
        "\"id\":\"weak_signal\","
        "\"severity\":\"warning\","
        "\"message\":\"Signal is weak on 5GHz band.\""
      "}"
    "],"
    "\"recommendations\":["
      "{"
        "\"id\":\"move_router\","
        "\"priority\":\"high\","
        "\"message\":\"Move router to a central location.\""
      "},"
      "{"
        "\"id\":\"switch_channel\","
        "\"priority\":\"medium\","
        "\"message\":\"Try a less congested channel.\""
      "}"
    "]"
    "}";

  REQUIRE(json == expected);
}

TEST_CASE("CSV export is deterministic and matches contract") {
  const auto report = fixture_report();
  const auto csv = vyre::wifi::exporter::to_csv(report);

  const std::string expected =
    "# report\n"
    "schema_version,1\n"
    "report_id,fixture-001\n"
    "generated_at_utc,2026-02-01T12:00:00Z\n"
    "platform,windows\n"
    "app_version,0.1.0\n"
    "\n"
    "# scan\n"
    "captured_at_utc,2026-02-01T11:59:00Z\n"
    "\n"
    "# access_points\n"
    "bssid,ssid,band,channel,rssi_dbm,security\n"
    "22:22:22:22:22:22,CafeNet,5ghz,36,-61,WPA2\n"
    "11:11:11:11:11:11,HomeWiFi,2.4ghz,6,-43,WPA2\n"
    "\n"
    "# issues\n"
    "id,severity,message\n"
    "channel_congestion,info,2.4GHz channel appears busy.\n"
    "weak_signal,warning,Signal is weak on 5GHz band.\n"
    "\n"
    "# recommendations\n"
    "id,priority,message\n"
    "move_router,high,Move router to a central location.\n"
    "switch_channel,medium,Try a less congested channel.\n";

  REQUIRE(csv == expected);
}
