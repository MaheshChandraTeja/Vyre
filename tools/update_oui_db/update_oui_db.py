#!/usr/bin/env python3
import argparse
import csv
import io
import pathlib
import re
import sys
import urllib.request

OUI_RE = re.compile(r"^[0-9A-Fa-f]{6}$")


def normalize_oui(value: str) -> str:
    hex_only = "".join(ch for ch in value if ch.isalnum())
    hex_only = hex_only.upper()
    if len(hex_only) < 6:
        return ""
    return hex_only[:6]


def parse_lines(text: str) -> dict[str, str]:
    entries: dict[str, str] = {}

    for raw_line in text.splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue

        if "," in line:
            left, right = line.split(",", 1)
            oui = normalize_oui(left.strip())
            vendor = right.strip()
            if OUI_RE.match(oui) and vendor:
                entries[oui] = vendor
            continue

        if "\t" in line:
            left, right = line.split("\t", 1)
            oui = normalize_oui(left.strip())
            vendor = right.strip()
            if OUI_RE.match(oui) and vendor:
                entries[oui] = vendor
            continue

        parts = line.split(None, 1)
        if len(parts) == 2:
            oui = normalize_oui(parts[0].strip())
            vendor = parts[1].strip()
            if OUI_RE.match(oui) and vendor:
                entries[oui] = vendor

    return entries


def load_source(path: str | None, url: str | None) -> str:
    if path:
        return pathlib.Path(path).read_text(encoding="utf-8")

    if url:
        with urllib.request.urlopen(url, timeout=30) as response:
            return response.read().decode("utf-8", errors="replace")

    raise ValueError("Provide either --input or --url")


def write_csv(entries: dict[str, str], output_path: pathlib.Path) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with output_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["# OUI", "Vendor"])
        for oui in sorted(entries.keys()):
            writer.writerow([oui, entries[oui]])


def main() -> int:
    parser = argparse.ArgumentParser(description="Normalize an OUI vendor source into Vyre CSV format.")
    parser.add_argument("--input", help="Path to a local OUI/vendor text source")
    parser.add_argument("--url", help="Optional URL to fetch OUI/vendor text from")
    parser.add_argument(
        "--output",
        required=True,
        help="Target CSV path, for example src/native/vyre-core/data/oui_db.csv",
    )
    args = parser.parse_args()

    try:
        text = load_source(args.input, args.url)
        entries = parse_lines(text)
        if not entries:
            print("No valid OUI entries were parsed.", file=sys.stderr)
            return 2

        output_path = pathlib.Path(args.output)
        write_csv(entries, output_path)
        print(f"Wrote {len(entries)} OUI entries to {output_path}")
        return 0
    except Exception as exc:
        print(f"Failed to update OUI database: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())