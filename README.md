# ClinicSimulator

ClinicSimulator is a .NET application leveraging Semantic Kernel to simulate clinic operations, including receptionist interactions, appointment management, and patient handling.

## Overview

The project uses AI agents to simulate realistic interactions within a clinic environment. It is built with:
- **.NET 8**
- **Semantic Kernel** for AI orchestration.
- **C#** as the primary language.

## Structure

- `src/ClinicSimulator.AI`: Contains the AI logic, Agents, and Plugins (e.g., `ClinicInfoPlugin`, `AppointmentPlugin`).
- `src/ClinicSimulator.Console`: The console application entry point for running the simulator.
- `src/ClinicSimulator.Core`: Core logic and data models.
- `src/ClinicSimulator.Tests`: Unit and integration tests.

## Getting Started

1.  Clone the repository.
2.  Configure your AI service credentials (e.g., OpenAI, Azure OpenAI) in `appsettings.json` or user secrets.
3.  Run the `ClinicSimulator.Console` project.

## Configuration

Ensure you have the necessary API keys configured for Semantic Kernel to function correctly.

## License

[License Information]
