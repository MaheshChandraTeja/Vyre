# Vyre  
### Cross-Platform Wi-Fi Analyzer & Diagnostics Suite

![Build](https://img.shields.io/badge/build-CI%20green-brightgreen)
![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20Android%20%7C%20iOS-blue)
![Linux](https://img.shields.io/badge/Linux-core%20%2B%20CLI-informational)
![Language](https://img.shields.io/badge/C%2B%2B20-%2300599C?logo=c%2B%2B&logoColor=white)
![UI](https://img.shields.io/badge/.NET%20MAUI-UI-purple)
![License](https://img.shields.io/badge/license-Proprietary-lightgrey)

**Vyre** is a sleek, modern, cross-platform Wi‑Fi analysis application built for engineers and researchers who want **clarity instead of noise**.

It combines a **high‑performance C++ networking engine** with a **.NET MAUI user interface**, delivering actionable wireless insights across **Windows, Android, and iOS**, with **Linux supported via the shared core and CLI**.

No fake theatrics.  
No bloated dashboards.  
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

## 🧱 Architecture Overview

```
Vyre
├── wifi-core        # C++20 analysis & diagnostics engine
│   ├── domain       # Core data models
│   ├── analysis     # Heuristics & insights
│   ├── export       # JSON / CSV / HTML
│   └── platform     # OS-specific scanners
│
├── wifi-interop     # Stable C ABI for interop
│
├── wifi-ui-maui     # .NET MAUI application
│   ├── Views        # UI screens
│   ├── ViewModels   # MVVM logic
│   └── Platforms    # Android / iOS / Windows glue
│
└── tools            # OUI updater, fixtures, helpers
```

**Design principles**
- UI and engine are strictly separated  
- Platform quirks are isolated, not leaked upward  
- Shared logic lives once, not three times  

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

Your networks stay yours.

---

## 🛠️ Tech Stack

- **C++20** — core analysis engine  
- **.NET MAUI** — cross‑platform UI  
- **libpcap / Npcap** — optional desktop capture  
- **SQLite / JSON** — local storage  
- **CMake + CI** — reproducible builds  

---

## 🔬 Research Motivation

Vyre is motivated by a practical research question:

> **How can we construct accurate, explainable wireless diagnostics when measurement capabilities are heterogeneous, incomplete, and constrained by modern operating systems?**

Contemporary platforms (especially mobile OSes) restrict low‑level network visibility, creating an inherent gap between the phenomena of interest (e.g., interference, misconfiguration, insecure deployments, roaming instability) and what can be directly observed. Vyre is designed as a systems‑oriented study in **cross‑platform observability**, where the central challenge is to derive high‑value inferences from **partial and non‑uniform measurements** while remaining transparent about uncertainty.

The project provides a foundation for investigating:
- **Normalization under heterogeneity:** mapping platform‑specific measurements (signal indicators, security descriptors, channel/frequency reporting) into a unified, versioned representation.
- **Explainable heuristic inference:** producing diagnostics with explicit evidence and traceable decision rules rather than opaque scoring.
- **Robust degradation:** formalizing “graceful fallback” behavior when specific measurements are unavailable, without silently biasing conclusions.
- **Reproducibility and evaluation:** enabling deterministic exports (JSON/CSV/HTML) that support offline analysis, longitudinal comparisons, and benchmarking against controlled testbeds.

Beyond its utility as an end‑user tool, Vyre is structured to support future research extensions including management‑frame analysis, capture‑assisted validation on desktop platforms, and quantitative evaluation of diagnostic accuracy under varying observability constraints.

---

## 🧭 Roadmap

- Desktop packet capture (managed mode)
- Linux desktop UI
- Monitor mode & 802.11 frame decoding (Linux‑first)
- Advanced spectrum analytics
- Research‑grade reporting profiles

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

Vyre exists because too many tools fail at at least two of those.

---

**Vyre** — clarity in the airwaves.
