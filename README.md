# AppEngine

A lightweight development engine for .NET web applications, designed to simplify and provide essential tools for developing web apps.

## 🏷️ Introduction

AppEngine is a lightweight development engine for .NET web applications, designed to simplify and provide essential tools for developing web apps. It offers a collection of utilities, extensions, and validation mechanisms to streamline the development process and enhance productivity.

## 📁 Project Structure

```
AppEngine/
├── .github/                                    # GitHub configuration files
│   ├── instructions/                           # Instructions for contributing and using the project
│   │   └── copilot.instructions.md             # Instructions for using GitHub Copilot with the project
│   │
│   ├── workflows/                              # GitHub Actions workflows for CI/CD
│   │   ├── linter.yml                          # Workflow for code linting and formatting checks
│   │   ├── publish.yml                         # Workflow for publishing the project to Baget
│   │   └── publish_tools.yml                   # Workflow for publishing the tools package to Baget
│   │
│   └── dependabot.yml                          # Dependabot configuration for automated dependency updates
│
├── src/
│   ├── AppEngine/                              # Core functionalities and utilities
│   │   ├──  Enums/                             # Enumeration types used across the project
│   │   ├──  Extensions/                        # Extension methods for various classes and types
│   │   ├──  FluentValidation/                  # Fluent validation rules and validators
│   │   ├──  Logging/                           # Logging utilities and configurations
│   │   ├──  Middleware/                        # Custom middleware components
│   │   ├──  Routing/                           # Routing configurations and related classes
│   │   ├──  Settings/                          # Configuration classes and settings management
│   │   └──  Validation/                        # Validation logic and classes
│   │
│   ├── AppEngine.Caching/                      # Caching utilities and implementations
│   │   ├──  DependencyInjection/               # Dependency injection configurations for caching
│   │   │    └── ServiceCollectionExtensions/   # Extension methods for IServiceCollection related to caching
│   │   │
│   │   ├──  Options/                           # Caching options and configuration classes
│   │   └──  Settings/                          # Caching settings and related classes
│   │
│   ├── AppEngine.Tools/                        # Command-line tools and utilities
│   │   ├──  ExceptionHandlers/                 # Custom exception handlers and related classes
│   │   ├──  Extensions/                        # Extension methods specific to the tools
│   │   ├──  OpenApi/                           # OpenAPI related utilities and classes
│   │   │    ├── Filters/                       # OpenAPI filters and related classes
│   │   │    ├── Helpers/                       # OpenAPI helper classes and utilities
│   │   │    ├── Options/                       # OpenAPI options and configuration classes
│   │   │    └── SimpleAuthentication/          # OpenAPI extensions and utilities for simple authentication
│   │   │
│   │   ├──  OperationResult/                   # Classes related to operation results and responses
│   │   │    └── AspNetCore.Http/               # Operation result classes specific to ASP.NET Core HTTP responses
│   │   │
│   │   ├──  Serialization/                     # Serialization utilities and classes
│   │   ├──  SimpleAuthentication/              # Simple authentication utilities and classes
│   │   │    ├── Abstractions/                  # Abstractions and interfaces for authentication
│   │   │    └── JWTBearer/                     # JWT Bearer authentication related classes and utilities
│   │   │
│   │   └──  TimeZoneService/                   # Time zone related utilities and services
│   │        └── Interfaces/                    # Interfaces for time zone services
│   │   
│   └── Directory.Build.props                   # Shared MSBuild properties
│
├── .editorconfig                       # Code style and formatting rules
├── .gitignore                          # Git ignore file
├── LICENSE                             # License information
├── README.md                           # Project documentation
└── AppEngine.slnx                      # Solution file for the entire project
```

## 🛠️ Installation

### Prerequisites

- .NET 10.0 SDK (latest version)

### Setup

The libraries are available on [Baget](http://nuget.aepserver.it), just search for _TinyAppEngine_ or _TinyAppEngine.Tools_ in the Package Manager GUI or run the following command in the .NET CLI:

```shell
dotnet add package TinyAppEngine

dotnet add package TinyAppEngine.Tools

dotnet add package TinyAppEngine.Caching
```

> [!NOTE]
> It is necessary to configure nuget.config in order to be able to install nuget packages from Baget without problems, below is an example of configuration

```xml
﻿<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <add key="Baget" value="http://nuget.aepserver.it/v3/index.json" allowInsecureConnections="true" />
    </packageSources>

    <!-- Opzionale: limitare quali pacchetti vengono risolti da quale sorgente -->
    <packageSourceMapping>
        <packageSource key="Baget">
            <package pattern="TinyAppEngine*" /><!-- Tutti i pacchetti che iniziano con TinyAppEngine -->
        </packageSource>
    </packageSourceMapping>

    <!-- Altre impostazioni utili -->
    <config>
        <!-- esempio: personalizzare la cartella dei pacchetti (opzionale) -->
        <!-- <add key="globalPackagesFolder" value="%USERPROFILE%\.nuget\packages" /> -->
    </config>
</configuration>
```

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

<!--
## ⭐ Give a Star

Don't forget that if you find this project helpful, please give it a ⭐ on GitHub to show your support and help others discover it.

## 🤝 Contributing

The project is constantly evolving. Contributions are always welcome. Feel free to report issues and submit pull requests to the repository, following the steps below:

1. Fork the repository
2. Create a feature branch (starting from the develop branch)
3. Make your changes
4. Submit a pull requests (targeting develop)
-->