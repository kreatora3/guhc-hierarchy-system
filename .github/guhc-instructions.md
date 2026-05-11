# Copilot Instructions – Senior .NET Developer Interview Project

## Project Context

You are assisting in the implementation of a **Hierarchy Accounts System** for **Grand Unified Holding Corp. (GUHC)**.

The system manages account hierarchies in a strict tree structure:

- Global account (root)
- Regional branches
- Country offices
- Local resellers

The existing process uses Excel and email and suffers from:
- No single view of hierarchy
- Manual hierarchy changes
- Invalid structures and cycles
- Difficult subtree management

The goal is to build a clean, production-quality .NET solution demonstrating:
- SOLID principles
- Clean Architecture
- RESTful API design
- EF Core + SQL Server modeling
- Validation of hierarchical business rules
- Unit testing
- Proper project organization

---

# Technical Stack

## Required Technologies

- .NET 8
- ASP.NET Core Web API
- EF Core
- SQL Server
- Swagger / OpenAPI
- xUnit + FluentAssertions
- Git

## Solution Structure

Prefer the following structure:

```text
src/
 +-- Api/
 +-- Application/
 +-- Domain/
 +-- Infrastructure/
 +-- ConsoleClient/
tests/
 +-- UnitTests/
 +-- IntegrationTests/