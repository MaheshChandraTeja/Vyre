#include "vyre/core/analysis.hpp"

#include <fstream>
#include <sstream>

namespace vyre::core::analysis
{
    namespace
    {
        std::string Trim(const std::string_view value)
        {
            std::size_t start = 0;
            std::size_t end = value.size();

            while (start < end && std::isspace(static_cast<unsigned char>(value[start])) != 0)
            {
                ++start;
            }

            while (end > start && std::isspace(static_cast<unsigned char>(value[end - 1])) != 0)
            {
                --end;
            }

            return std::string(value.substr(start, end - start));
        }
    }

    bool OuiVendorLookup::LoadFromCsvFile(const std::string& path)
    {
        std::ifstream input(path);
        if (!input.is_open())
        {
            return false;
        }

        std::ostringstream buffer;
        buffer << input.rdbuf();
        return LoadFromCsvText(buffer.str());
    }

    bool OuiVendorLookup::LoadFromCsvText(const std::string_view csv_text)
    {
        entries_.clear();

        std::istringstream input(std::string(csv_text));
        std::string line;
        bool any_loaded = false;

        while (std::getline(input, line))
        {
            if (line.empty())
            {
                continue;
            }

            if (line[0] == '#')
            {
                continue;
            }

            const auto separator = line.find(',');
            if (separator == std::string::npos)
            {
                continue;
            }

            const auto oui = NormalizeOui(Trim(std::string_view(line).substr(0, separator)));
            const auto vendor = Trim(std::string_view(line).substr(separator + 1));

            if (oui.size() != 6 || vendor.empty())
            {
                continue;
            }

            entries_[oui] = vendor;
            any_loaded = true;
        }

        return any_loaded;
    }

    std::string OuiVendorLookup::LookupVendor(const std::string_view bssid) const
    {
        const auto oui = NormalizeOui(bssid);
        if (oui.size() != 6)
        {
            return {};
        }

        const auto it = entries_.find(oui);
        if (it == entries_.end())
        {
            return {};
        }

        return it->second;
    }
}