.PHONY: build test coverage db-up db-down run clean

# Backend Makefile

build:
	@echo "Building .NET Solution..."
	dotnet build

test:
	@echo "Running Unit Tests..."
	dotnet test

coverage:
	@echo "Running Tests with XPlat Code Coverage..."
	dotnet test Esatto.Outreach.UnitTests --collect:"XPlat Code Coverage"

db-up:
	@echo "Starting PostgreSQL database in Docker..."
	docker compose up -d

db-down:
	@echo "Stopping database..."
	docker compose down

run:
	@echo "Running the API..."
	dotnet run --project Esatto.Outreach.Api/Esatto.Outreach.Api.csproj

dev:
	@echo "Running the API in development mode..."
	DOTNET_ENVIRONMENT=Development dotnet run --project Esatto.Outreach.Api/Esatto.Outreach.Api.csproj

clean:
	@echo "Cleaning bin and obj folders..."
	dotnet clean
