TEST_CASE("Frequency to channel mapping") {
    REQUIRE(freq_to_channel(2412000) == 1);
    REQUIRE(freq_to_channel(5180000) == 36);
}
