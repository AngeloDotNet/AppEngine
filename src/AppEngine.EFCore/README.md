# AppEngine EFCore

## 🏷️ Introduction

AppEngine EFCore is a lightweight development engine for .NET web applications, designed to simplify and provide essential tools for developing web apps. It offers a collection of utilities, extensions, and validation mechanisms to streamline the development process and enhance productivity.

## 🛠️ Installation

### Prerequisites

- .NET 10.0 SDK (latest version)

### Setup

The libraries are available on [Baget](http://nuget.aepserver.it), just search for _TinyAppEngine.EFCore_ in the Package Manager GUI or run the following command in the .NET CLI:

```shell
dotnet add package TinyAppEngine.EFCore
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

    <activePackageSource>
        <add key="All" value="(Aggregate source)" />
    </activePackageSource>

    <config>
        <!-- esempio: personalizzare la cartella dei pacchetti (opzionale) -->
        <!-- <add key="globalPackagesFolder" value="%USERPROFILE%\.nuget\packages" /> -->
    </config>
</configuration>
```

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.