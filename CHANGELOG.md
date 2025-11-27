# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2025-11-26

### Added
- **New UI**: Complete rewrite using WPF and Material Design.
- **OSINT Engine**: Comprehensive target analysis, DNS enumeration, and technology detection.
- **DDoS Modules**:
    - HTTP Flood (Layer 7) with multi-threading support.
    - SYN Flood (Layer 4) using raw sockets.
    - UDP Flood (Layer 4) for bandwidth exhaustion.
- **Injection Testing**:
    - SQL Injection scanner (Error-based, Blind, Union-based).
    - File Upload vulnerability tester.
- **Logging**: Automatic JSON logging for all scans and attacks.
- **History**: Ability to view and reload past scan reports.
- **Documentation**: Comprehensive README, installation guide, and contributing guidelines.

### Changed
- **Architecture**: Migrated from Python to .NET 8 (C#).
- **Performance**: Significantly improved performance and stability with multi-threading.
- **Installation**: Simplified installation process with a single executable or portable zip.

### Removed
- Legacy Python scripts and dependencies.

## [1.0.0] - 2024-01-01

### Added
- Initial release of ShadowStrike (Python version).
- Basic port scanner.
- Simple HTTP flooder.
- Command-line interface.
