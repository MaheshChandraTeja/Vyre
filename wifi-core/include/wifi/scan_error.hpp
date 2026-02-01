#pragma once
#include <stdexcept>
#include <string>

namespace wifi {

class ScanError : public std::runtime_error {
public:
    explicit ScanError(const std::string& msg)
        : std::runtime_error(msg) {}
};

}
