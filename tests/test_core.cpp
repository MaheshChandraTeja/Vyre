#include <catch2/catch_test_macros.hpp>
#include <string_view>

#include "wifi_core/wifi_core.h"

TEST_CASE("wifi-core version exists") {
  REQUIRE(vyre::wifi::version() != nullptr);
  REQUIRE(std::string_view(vyre::wifi::version()).size() > 0);
}

TEST_CASE("wifi-core hello matches canonical misery") {
  REQUIRE(std::string_view(vyre::wifi::hello()) == "Hello, cross-platform misery");
}
