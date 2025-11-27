# Contributing to ShadowStrike

First off, thank you for considering contributing to ShadowStrike! It's people like you that make ShadowStrike such a great tool.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
- [Development Setup](#development-setup)
- [Pull Request Process](#pull-request-process)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Testing Guidelines](#testing-guidelines)

## Code of Conduct

This project and everyone participating in it is governed by our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the existing issues to avoid duplicates. When you create a bug report, include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples**
- **Describe the behavior you observed and what you expected**
- **Include screenshots if applicable**
- **Include your environment details** (OS, .NET version, etc.)

**Bug Report Template:**
```markdown
**Description:**
A clear description of the bug.

**Steps to Reproduce:**
1. Go to '...'
2. Click on '...'
3. See error

**Expected Behavior:**
What you expected to happen.

**Actual Behavior:**
What actually happened.

**Environment:**
- OS: Windows 10/11
- .NET Version: 8.0
- ShadowStrike Version: 2.0

**Screenshots:**
If applicable, add screenshots.
```

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion:

- **Use a clear and descriptive title**
- **Provide a detailed description of the proposed feature**
- **Explain why this enhancement would be useful**
- **List any alternatives you've considered**

### Pull Requests

We actively welcome your pull requests:

1. Fork the repo and create your branch from `main`
2. If you've added code that should be tested, add tests
3. Ensure the test suite passes
4. Make sure your code follows our coding standards
5. Issue the pull request

## Development Setup

### Prerequisites

- Windows 10/11 (64-bit)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- Git

### Setup Steps

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/ShadowStrike.git
cd ShadowStrike

# Add upstream remote
git remote add upstream https://github.com/MrShankarAryal/ShadowStrike.git

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the application
dotnet run --project ShadowStrike.UI/ShadowStrike.UI.csproj
```

### Project Structure

```
ShadowStrike/
├── ShadowStrike.Core/      # Core business logic
│   ├── OsintEngine.cs      # OSINT functionality
│   ├── HttpFlooder.cs      # DDoS modules
│   ├── SqlInjector.cs      # Injection testing
│   └── ...
├── ShadowStrike.UI/        # WPF user interface
│   ├── Views/              # XAML views
│   ├── MainWindow.xaml     # Main window
│   └── ...
└── ShadowStrike.sln        # Solution file
```

## Pull Request Process

1. **Create a Feature Branch**
   ```bash
   git checkout -b feature/amazing-feature
   ```

2. **Make Your Changes**
   - Write clean, readable code
   - Follow coding standards
   - Add comments where necessary
   - Update documentation

3. **Test Your Changes**
   ```bash
   dotnet build
   dotnet test
   ```

4. **Commit Your Changes**
   ```bash
   git add .
   git commit -m "Add amazing feature"
   ```

5. **Push to Your Fork**
   ```bash
   git push origin feature/amazing-feature
   ```

6. **Create Pull Request**
   - Go to the original repository
   - Click "New Pull Request"
   - Select your branch
   - Fill in the PR template
   - Submit for review

### Pull Request Template

```markdown
## Description
Brief description of changes.

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] All tests pass
- [ ] New tests added
- [ ] Manual testing completed

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Comments added to complex code
- [ ] Documentation updated
- [ ] No new warnings generated
```

## Coding Standards

### C# Style Guide

Follow the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):

```csharp
// Good
public class OsintEngine
{
    private readonly HttpClient _httpClient;
    
    public async Task<DnsRecord> AnalyzeDnsAsync(string domain)
    {
        // Implementation
    }
}

// Bad
public class osintengine
{
    private HttpClient httpClient;
    
    public DnsRecord analyze_dns(string domain)
    {
        // Implementation
    }
}
```

### Naming Conventions

- **Classes**: PascalCase (`OsintEngine`, `HttpFlooder`)
- **Methods**: PascalCase (`AnalyzeTarget`, `StartAttack`)
- **Properties**: PascalCase (`TargetUrl`, `ThreadCount`)
- **Private Fields**: _camelCase (`_httpClient`, `_isRunning`)
- **Local Variables**: camelCase (`targetUrl`, `responseTime`)
- **Constants**: UPPER_CASE (`MAX_THREADS`, `DEFAULT_TIMEOUT`)

### Code Organization

```csharp
// 1. Using statements
using System;
using System.Net.Http;

// 2. Namespace
namespace ShadowStrike.Core
{
    // 3. Class documentation
    /// <summary>
    /// Provides OSINT and reconnaissance functionality.
    /// </summary>
    public class OsintEngine
    {
        // 4. Private fields
        private readonly HttpClient _httpClient;
        
        // 5. Constructor
        public OsintEngine()
        {
            _httpClient = new HttpClient();
        }
        
        // 6. Public methods
        public async Task<string> AnalyzeAsync(string target)
        {
            // Implementation
        }
        
        // 7. Private methods
        private void ValidateInput(string input)
        {
            // Implementation
        }
    }
}
```

### XML Documentation

Add XML documentation to public APIs:

```csharp
/// <summary>
/// Analyzes the target domain for vulnerabilities.
/// </summary>
/// <param name="domain">The target domain to analyze.</param>
/// <returns>A task representing the analysis operation.</returns>
/// <exception cref="ArgumentNullException">Thrown when domain is null.</exception>
public async Task<AnalysisResult> AnalyzeDomainAsync(string domain)
{
    // Implementation
}
```

## Commit Guidelines

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation changes
- **style**: Code style changes (formatting, etc.)
- **refactor**: Code refactoring
- **test**: Adding or updating tests
- **chore**: Maintenance tasks

### Examples

```bash
# Good
feat(osint): add WHOIS lookup functionality
fix(ddos): resolve thread synchronization issue
docs(readme): update installation instructions

# Bad
update stuff
fixed bug
changes
```

## Testing Guidelines

### Unit Tests

Write unit tests for new functionality:

```csharp
[TestClass]
public class OsintEngineTests
{
    [TestMethod]
    public async Task AnalyzeDomain_ValidDomain_ReturnsResult()
    {
        // Arrange
        var engine = new OsintEngine();
        
        // Act
        var result = await engine.AnalyzeDomainAsync("example.com");
        
        // Assert
        Assert.IsNotNull(result);
    }
}
```

### Manual Testing

Before submitting a PR:

1. Build the solution without errors
2. Run all existing tests
3. Manually test your changes
4. Test on a clean Windows installation if possible
5. Verify no regressions in existing functionality

## Questions?

Feel free to:
- Open an issue for discussion
- Contact the maintainer: ShadowStrikeContact@shankararyal404.com.np
- Join discussions in existing issues

## Recognition

Contributors will be recognized in:
- README.md Contributors section
- Release notes
- Project documentation

Thank you for contributing to ShadowStrike!

