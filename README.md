# Vyre  
### Cross-Platform Wi‑Fi Analyzer & Diagnostics Suite

![Build](https://img.shields.io/badge/build-CI%20green-brightgreen)
![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20Android%20%7C%20iOS-blue)
![Linux](https://img.shields.io/badge/Linux-core%20%2B%20CLI-informational)
![Language](https://img.shields.io/badge/C%2B%2B20-%2300599C?logo=c%2B%2B&logoColor=white)
![UI](https://img.shields.io/badge/.NET%20MAUI-UI-purple)
![Local First](https://img.shields.io/badge/local--first-no%20cloud-black)
![License](https://img.shields.io/badge/license-Proprietary-lightgrey)

**Vyre** is a sleek, modern, cross-platform Wi‑Fi analysis application built for engineers, researchers, and curious humans who want **clarity instead of noise**. 🛜✨

It combines a **high‑performance C++ networking engine** with a **.NET MAUI user interface**, delivering actionable wireless insights across **Windows, Android, and iOS**, with **Linux supported via the shared core and CLI**.

No fake theatrics.  
No bloated dashboards.  
No "it works because the spinner says so" nonsense.  
Just clean data, real diagnostics, and a UI that stays out of your way.

---

## ✨ Features

### 📡 Wi‑Fi Discovery & Analysis
- Scan nearby access points with rich metadata  
- SSID, BSSID, RSSI, channel, band, security, vendor (OUI)  
- Cross‑platform normalization for consistent results  

### 🧠 Intelligent Insights
- Channel congestion detection (2.4 / 5 / 6 GHz aware)
- Weak signal and stability warnings
- Security audits (Open, WEP, WPA2, WPA3)
- Evidence‑based detection of suspicious SSID clones

### 📊 Reports That Matter
- Export results as **JSON**, **CSV**, or **HTML**
- Offline‑ready HTML reports with bundled assets
- Scan history and comparison (new / removed / changed networks)

### 📱 Per-App Network Usage
- Android per-app usage via OS-supported APIs
- Windows usage data where platform support allows it
- iOS limitation handling without pretending Apple gave us magic keys
- Filters for Wi‑Fi / mobile / all and 24h / 7d / 30d views

### 🖥️ Modern Cross‑Platform UI
- Built with **.NET MAUI**
- Clean MVVM architecture
- Fully async — the UI never blocks
- Native look and feel per platform

### ⚙️ Performance‑First Core
- C++20 engine designed for speed and portability
- Stable C ABI boundary for platform interop
- Architecture ready for future packet capture and monitor‑mode extensions

---

**Design principles**
- UI and engine are strictly separated  
- Platform quirks are isolated, not leaked upward  
- Shared logic lives once, not three times  
- Reports are deterministic and testable  
- Unsupported OS features degrade clearly instead of lying with confidence  

---

## 🧩 Core Modules

| Module | Role | Notes |
|---|---|---|
| `wifi-core` | C++ Core Engine | Scanning where allowed, normalization, analysis, report generation, storage |
| `wifi-interop` | Interop Layer | Stable **C ABI** boundary for P/Invoke |
| `wifi-ui-maui` | MAUI App | UI, state, navigation, permissions, platform glue |
| `linux-ui` | Linux UI / CLI | Separate front-end later via Qt/Avalonia, or CLI-first support |
| `tools` | Support tooling | OUI database updates, fixtures, test helpers, benchmarks |

---

## 🖥️ Platform Support

| Platform | Status | Notes |
|--------|------|------|
| Windows | ✅ Full | Native WLAN API |
| Android | ✅ Partial | OS‑permitted scanning only |
| iOS | ⚠️ Limited | Diagnostics & visibility within Apple restrictions |
| Linux | 🧠 Core | CLI support, desktop UI planned |

> Packet capture and monitor mode are **desktop‑only and hardware‑dependent** by design.

---

## 🧭 App Screens

| Screen | Purpose |
|---|---|
| **Scan** | Nearby access points, filters, refresh, signal metadata |
| **Insights** | Ranked issues and practical recommendations |
| **Reports** | History, export/share, and scan comparisons |
| **Usage** | Per-app network usage where the OS allows it |
| **Settings** | Interface selection, scan interval, privacy toggles |
| **Doctor** | Capabilities, permissions, limitations, and platform sanity checks |

---

## 🚀 Getting Started

### Windows
```bash
Vyre.exe
```

### Android / iOS
Install the app and grant required network permissions.

### Linux (Core / CLI)
```bash
./vyre-cli scan --analyze --html report/
```

---

## 🔐 Security & Privacy

- No telemetry  
- No cloud dependency  
- No background uploads  
- All analysis runs locally and deterministically  

Your networks stay yours. Radical concept, apparently. 🔒

---

## 🛠️ Tech Stack

- **C++20** — core analysis engine  
- **.NET MAUI** — cross‑platform UI  
- **C ABI + P/Invoke** — stable native bridge  
- **libpcap / Npcap** — optional desktop capture  
- **SQLite / JSON** — local storage  
- **CMake + CI** — reproducible builds  

---

**Platform behavior**
- **Android:** real per-app usage via `NetworkStatsManager`, mapped from UID to installed apps
- **Windows:** usage data where supported, clearly labeled as Windows-specific sourcing
- **iOS:** no fake “other apps usage” screen; shows a limitation card and app-owned diagnostics/history instead

---

## 🔬 Research Motivation

Vyre is motivated by a practical research question:

> **How can we construct accurate, explainable wireless diagnostics when measurement capabilities are heterogeneous, incomplete, and constrained by modern operating systems?**

Contemporary platforms, especially mobile OSes, restrict low‑level network visibility. That creates a real gap between the phenomena of interest — interference, misconfiguration, insecure deployments, roaming instability — and what can be directly observed. Vyre is designed as a systems‑oriented study in **cross‑platform observability**, where the central challenge is to derive high‑value inferences from **partial and non‑uniform measurements** while remaining transparent about uncertainty.

The project provides a foundation for investigating:
- **Normalization under heterogeneity:** mapping platform‑specific measurements into a unified, versioned representation
- **Explainable heuristic inference:** producing diagnostics with explicit evidence and traceable decision rules rather than opaque scoring
- **Robust degradation:** formalizing graceful fallback behavior when measurements are unavailable, without silently biasing conclusions
- **Reproducibility and evaluation:** enabling deterministic exports that support offline analysis, longitudinal comparison, and benchmarking

Beyond its utility as an end‑user tool, Vyre is structured to support future extensions including management‑frame analysis, capture‑assisted validation on desktop platforms, and quantitative evaluation of diagnostic accuracy under varying observability constraints.

---

## 🧭 Roadmap

- Desktop packet capture (managed mode)
- Linux desktop UI
- Monitor mode & 802.11 frame decoding (Linux‑first)
- Advanced spectrum analytics
- Research‑grade reporting profiles
- Benchmarks for scan latency, report generation, and analyzer stability

---

## 🏢 About

**Built by**  
**Mahesh Chandra Teja Garnepudi**  
**Sagarika Srivastava**

**Organization**  
**Kairais Tech**  
https://www.kairais.com

---

## 📄 License

Vyre is proprietary software.  
Third‑party dependencies are licensed under their respective terms and used in compliance.

---

## 💡 Philosophy

Wireless analysis tools should be:
- Accurate  
- Explainable  
- Respectful of OS boundaries  
- Pleasant to use  

Vyre exists because too many tools fail at at least two of those, then somehow still ship with a settings page nobody asked for.

---

**Vyre** — clarity in the airwaves. 🛜
