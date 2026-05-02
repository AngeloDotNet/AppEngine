# AppEngine

## 🏷️ Introduction

AppEngine is a lightweight development engine for .NET web applications, designed to simplify and provide essential tools for developing web apps. It offers a collection of utilities, extensions, and validation mechanisms to streamline the development process and enhance productivity.

## 📁 Project Structure

```
AppEngine/
├── .github/                                    # GitHub configuration files
│   ├── instructions/                           # Instructions for contributing and using the project
│   └── workflows/                              # GitHub Actions workflows for CI/CD
│
├── src/
│   ├── AppEngine.Caching/                      # Caching utilities and implementations
│   │   ├──  DependencyInjection/               # Dependency injection configurations for caching
│   │   │    └── ServiceCollectionExtensions/   # Extension methods for IServiceCollection related to caching
│   │   │
│   │   ├──  Options/                           # Caching options and configuration classes
│   │   └──  Settings/                          # Caching settings and related classes
│   │   
│   └── Directory.Build.props                   # Shared MSBuild properties
│
├── .editorconfig                               # Code style and formatting rules
├── .gitignore                                  # Git ignore file
├── LICENSE                                     # License information
├── README.md                                   # Project documentation
└── AppEngine.slnx                              # Solution file for the entire project
```

## 🛠️ Installation

### Prerequisites

- .NET 10.0 SDK (latest version)

### Setup

The libraries are available on [Baget](http://nuget.aepserver.it), just search for _TinyAppEngine.Caching_ in the Package Manager GUI or run the following command in the .NET CLI:

```shell
dotnet add package TinyAppEngine.Caching
```

> [!NOTE]
> It is necessary to configure nuget.config in order to be able to install nuget packages from Baget without problems, below is an example of configuration

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <add key="Baget" value="http://nuget.aepserver.it/v3/index.json" allowInsecureConnections="true" />
    </packageSources>

    <!-- Opzionale: limitare quali pacchetti vengono risolti da quale sorgente -->
    <packageSourceMapping>
        <packageSource key="nuget.org">
            <package pattern="*" />
        </packageSource>
        <packageSource key="Baget">
            <package pattern="TinyAppEngine*" /><!-- Tutti i pacchetti che iniziano con TinyAppEngine -->
        </packageSource>
    </packageSourceMapping>

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