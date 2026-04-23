# AppEngine

A lightweight development engine for .NET web applications, designed to simplify and provide essential tools for developing web apps.

## 🏷️ Introduction

AppEngine is a lightweight development engine for .NET web applications, designed to simplify and provide essential tools for developing web apps. It offers a collection of utilities, extensions, and validation mechanisms to streamline the development process and enhance productivity.

## 🛠️ Installation

### Prerequisites

- .NET 10.0 SDK (latest version)

## 📁 Project Structure

```
AppEngine/
├── .github/                        # GitHub configuration files
│   ├── instructions/               # Instructions for contributing and using the project
│   │   └── copilot.instructions.md # Instructions for using GitHub Copilot with the project
│   │
│   ├── workflows/                  # GitHub Actions workflows for CI/CD
│   │   ├── linter.yml              # Workflow for code linting and formatting checks
│   │   ├── publish.yml             # Workflow for publishing the project to Baget
│   │   └── publish_tools.yml       # Workflow for publishing the tools package to Baget
│   │
│   └── dependabot.yml              # Dependabot configuration for automated dependency updates
│   
├── src/
│   ├── AppEngine/                  # Core functionalities and utilities
│   │   ├──  Enums/                 # Enumeration types used across the project
│   │   ├──  Extensions/            # Extension methods for various classes and types
│   │   ├──  FluentValidation/      # Fluent validation rules and validators
│   │   ├──  Routing/               # Routing configurations and related classes
│   │   ├──  Serialization/         # Custom serializers and deserializers
│   │   └──  Validation/            # Validation logic and classes
│   │
│   ├── AppEngine.Tools/            # Command-line tools and utilities
│   │   ├──  OperationResult/       # Classes related to operation results and responses
│   │   └──  SimpleAuth/            # Simple authentication utilities and classes
│   │
│   └── Directory.Build.props       # Shared MSBuild properties
│
├── .editorconfig                   # Code style and formatting rules
├── .gitignore                      # Git ignore file
├── LICENSE                         # License information
├── README.md                       # Project documentation
└── AppEngine.slnx                  # Solution file for the entire project
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