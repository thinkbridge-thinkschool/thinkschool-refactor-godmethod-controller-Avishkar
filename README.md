# ASP.NET Core 10 God-Method Refactoring

![CI](https://github.com/thinkbridge-thinkschool/thinkschool-refactor-godmethod-controller-Avishkar/actions/workflows/dotnet.yml/badge.svg)

This repository demonstrates the step-by-step refactoring of a legacy, high-smell "God-Method" `OrderController.cs` in an ASP.NET Core 10 Web API into a clean, layered (Controller -> Service -> Repository) architecture.

## Repository Contents

- **[prompt.md](prompt.md):** The original prompt used to instruct the AI to generate the deliberately bad legacy controller code.
- **[REFACTOR_NOTES.md](REFACTOR_NOTES.md):** A detailed review documenting 11 distinct code smells, their consequences, and the intended architectural fixes.
- **[LegacyOrderApi/](LegacyOrderApi/):** The refactored Web API codebase using Clean Architecture & dependency injection.
- **[LegacyOrderApi.Tests/](LegacyOrderApi.Tests/):** Robust unit tests for the Service layer (using Moq) and end-to-end integration tests (using `WebApplicationFactory`).

---

## How to Run & Test the Application

### 1. Prerequisite
Ensure you have the .NET 10 SDK installed on your system.

### 2. Run the Application
Navigate to the API project folder and run the dev server:
```bash
cd LegacyOrderApi
dotnet run
```
The application will boot up and seed an in-memory database with a test user (`test@example.com` with a balance of $5000) and sample products. It will output the listening URL (e.g., `http://localhost:5284`).

### 3. Run Automated Tests
Navigate to the test project directory and run:
```bash
cd LegacyOrderApi.Tests
dotnet test
```

### 4. Manually Test the POST Endpoint
While the API is running, you can hit the POST `/api/order` endpoint from a Bash terminal:
```bash
curl -X POST http://localhost:5284/api/order \
-H "Content-Type: application/json" \
-d '{
    "Email": "test@example.com",
    "ShippingAddress": "123 Main St",
    "Items": [
        {
            "ProductId": 1,
            "Quantity": 2
        }
    ]
}'
```
