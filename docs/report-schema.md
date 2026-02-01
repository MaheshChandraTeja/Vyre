# Vyre WiFi Report Schema (v1)

This document defines the stable, versioned contract used for exporting WiFi scan reports across platforms.

## Versioning

- Every exported report MUST include `schema_version`.
- Schema version changes MUST be backwards compatible unless the major schema version is bumped.
- Current: `schema_version = 1`.

---

## JSON Export (v1)

### Top-level object

| Field | Type | Required | Notes |
|------|------|----------|------|
| schema_version | number | yes | currently `1` |
| report_id | string | yes | stable id provided by caller |
| generated_at_utc | string | yes | timestamp string (UTC) |
| platform | string | yes | e.g. `windows`, `android`, `ios`, `linux` |
| app_version | string | yes | app build version |
| scan | object | yes | scan payload |
| issues | array | yes | sorted by `id` |
| recommendations | array | yes | sorted by `id` |

### scan object

| Field | Type | Required | Notes |
|------|------|----------|------|
| captured_at_utc | string | yes | timestamp string (UTC) |
| access_points | array | yes | sorted by (ssid, bssid) |

### access_points array element

| Field | Type | Required | Notes |
|------|------|----------|------|
| bssid | string | yes | MAC-like string |
| ssid | string | yes | can be empty |
| band | string | yes | `unknown`, `2.4ghz`, `5ghz`, `6ghz` |
| channel | number/null | yes | nullable |
| rssi_dbm | number/null | yes | nullable |
| security | string/null | yes | nullable |

### issues array element

| Field | Type | Required |
|------|------|----------|
| id | string | yes |
| severity | string | yes (`info`, `warning`, `critical`) |
| message | string | yes |

### recommendations array element

| Field | Type | Required |
|------|------|----------|
| id | string | yes |
| priority | string | yes (`low`, `medium`, `high`) |
| message | string | yes |

---

## CSV Export (v1)

CSV output is a single text document with sections:

1. `# report` (key/value pairs)
2. `# scan` (key/value pairs)
3. `# access_points` (table)
4. `# issues` (table)
5. `# recommendations` (table)

### Determinism rules

- `access_points` sorted by (ssid, bssid)
- `issues` sorted by id
- `recommendations` sorted by id

This ensures stable exports for snapshot testing, CI validation, and long-term compatibility.
