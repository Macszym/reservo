# Reservo — Resource Reservation System

Web application for managing reservations of firm resources — conference rooms, IT equipment, vehicles, and tools. Built as a course project in ASP.NET Core 9.0 MVC with SQLite.

## Authors

Co-authored with **Mateusz Sobiech** and **Maciej Szymański**.

## What it does

Reservo addresses the everyday problem of sharing resources inside a company:

- **Resource booking** — employees can reserve resources they need
- **Availability control** — the system automatically detects scheduling conflicts
- **Resource organisation** — resources grouped into categories (rooms, equipment, vehicles)
- **User management** — admins control access and roles

## Features

### For users
- Browse available resources with category filtering
- Create reservations (resource, time window, purpose)
- View own reservation history
- Check resource availability on a weekly calendar

### For administrators
- User management — add users, assign roles
- Resource management — add, edit, delete resources
- Category management — colour-coded categories
- Reports and statistics — resource utilisation, active users
- System calendar — overview of all reservations

### REST API
- Programmatic access to all data
- API-key authorisation for external apps
- Auto-generated Swagger / OpenAPI documentation

## Database schema

SQLite database with six tables:

**Core**
1. `users` — users with roles and API keys
2. `categories` — resource categories with colours
3. `resources` — bookable resources
4. `reservations` — reservation records with time windows and statuses

**Legacy compatibility**
5. `loginy` — legacy login table
6. `dane` — legacy sample data

**Relations**
- Resource belongs to a category (`resources` → `categories`)
- Reservation belongs to a user (`reservations` → `users`)
- Reservation references a resource (`reservations` → `resources`)

## Setup

### Requirements
- .NET 9.0 SDK or newer
- Windows, macOS, or Linux

### Steps

```bash
git clone https://github.com/Macszym/reservo.git
cd reservo/MVC_Wynajem
dotnet restore
dotnet run
```

Open the app at <http://localhost:5106> or browse the API docs at <http://localhost:5106/api-docs>.

## First run

The app seeds itself on first launch:

- An administrator account (login `admin`, password `admin` — **demo default; change immediately or replace the seeder before any real deployment**)
- A regular user account (login `user`, password `user`)
- Sample categories (conference rooms, IT equipment, vehicles, tools)
- Sample resources (e.g., room A1, Sony projector, company car)

## REST API

All requests require an authorisation header:

```
X-API-Key: <user-api-key>
```

### Main endpoints

- `GET /api/Resources` — list resources
- `POST /api/Resources` — add a resource (admin)
- `GET /api/Reservations` — list reservations
- `POST /api/Reservations` — create a reservation
- `GET /api/Resources/{id}/availability` — check availability of a resource

### Trying it out

Either run the bundled console demo:

```bash
cd ../ApiDemo
dotnet run
```

…or hit the Swagger UI at <http://localhost:5106/api-docs>.

## Tech stack

- **ASP.NET Core 9.0 MVC** — web framework
- **Entity Framework Core** — ORM
- **SQLite** — database
- **Bootstrap 5** — UI
- **Swagger / OpenAPI** — API documentation

## Project structure

```
MVC_Wynajem/
├── Controllers/    # MVC + API controllers
├── Models/         # data models and DTOs
├── Views/          # Razor views
├── Attributes/     # authorisation attributes
├── Migrations/     # EF Core migrations
└── wwwroot/        # static assets

ApiDemo/            # console demo client for the REST API
```

## A note

This is a course project — a reasonable starting point for the domain, but production deployment would need at minimum: stronger password hashing (Argon2 / bcrypt), token rotation, rate limiting, an audit log, and the seed admin replaced by a proper setup wizard.
