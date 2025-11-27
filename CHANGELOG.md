# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.0] - 2025-11-27

### Added
- **Automated CI/CD Pipeline**: GitHub Actions workflow for automatic building and releasing
- **Professional Release Process**: Automated installer and portable ZIP creation on version tags
- **Desktop Icon Support**: Fixed installer to properly display eagle logo on all shortcuts
- **Release Documentation**: Added RELEASE_GUIDE.md for automated release workflow

### Changed
- **Installation Process**: Modernized to use GitHub Releases instead of manual building
- **Documentation**: Updated README.md and INSTALLER_GUIDE.md to prioritize pre-built releases
- **Installer Configuration**: Enhanced setup.iss with current v2.0 features and fixed encoding issues

### Fixed
- **Installer Compilation**: Resolved emoji encoding errors in Inno Setup script
- **Shortcut Icons**: Fixed desktop and Start Menu shortcuts to display eagle logo properly
- **Feature Descriptions**: Updated installer to accurately reflect all current modules (Ransomware, Terminal, etc.)

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
