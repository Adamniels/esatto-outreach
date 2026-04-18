.PHONY: build test coverage coverage-summary coverage-report db-up db-down migrate-dev run dev clean

# Backend Makefile

build:
	@echo "Building .NET Solution..."
	dotnet build

test:
	@echo "Running Unit Tests..."
	dotnet test

# Quick coverage — generates raw XML (used by VS Code Coverage Gutters extension)
coverage:
	@echo "Running Tests with XPlat Code Coverage..."
	dotnet test Esatto.Outreach.UnitTests --collect:"XPlat Code Coverage"

# Terminal summary — prints a coverage table directly in the terminal
coverage-summary:
	@echo "Running Tests with coverage summary..."
	dotnet test Esatto.Outreach.UnitTests -p:CollectCoverage=true

# Full coverage — generates HTML report and opens it in the browser
coverage-report:
	@echo "Running Tests and generating HTML Coverage Report..."
	dotnet test Esatto.Outreach.UnitTests --collect:"XPlat Code Coverage"
	~/.dotnet/tools/reportgenerator \
		"-reports:Esatto.Outreach.UnitTests/TestResults/*/coverage.cobertura.xml" \
		-targetdir:coveragereport \
		-reporttypes:Html
	@echo "Opening coverage report..."
	open coveragereport/index.html

db-up:
	@echo "Starting PostgreSQL database in Docker..."
	docker compose up -d

db-down:
	@echo "Stopping database..."
	docker compose down

migrate-dev:
	@echo "Applying EF Core migrations (Development)..."
	ASPNETCORE_ENVIRONMENT=Development DOTNET_ENVIRONMENT=Development dotnet ef database update \
		--project Esatto.Outreach.Infrastructure/Esatto.Outreach.Infrastructure.csproj \
		--startup-project Esatto.Outreach.Api/Esatto.Outreach.Api.csproj

run:
	@echo "Running the API..."
	DOTNET_ENVIRONMENT=Development ASPNETCORE_ENVIRONMENT=Development dotnet run --project Esatto.Outreach.Api/Esatto.Outreach.Api.csproj

dev:
	@echo "Running the API in development mode..."
	DOTNET_ENVIRONMENT=Development dotnet run --project Esatto.Outreach.Api/Esatto.Outreach.Api.csproj

clean:
	@echo "Cleaning bin and obj folders..."
	dotnet clean
