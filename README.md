# Inventory Management API

Backend-only inventory management system built with ASP.NET Core, Entity Framework Core, PostgreSQL, and a .NET Worker Service.

The application manages product categories, products, stock transactions, low-stock detection, automatic restocking, and TCP communication between the API and Worker service.

## Project Structure

```text
InventoryManagementApi/
├── InventoryManagementApi/
│   ├── Controllers/
│   ├── Data/
│   │   ├── Entities/
│   │   └── Migrations/
│   ├── DTOs/
│   ├── Enums/
│   ├── Exceptions/
│   ├── Services/
│   ├── Tcp/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Dockerfile
│   └── docker-compose.yml
│
├── InventoryManagementWorker/
│   ├── Worker.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── README.md
└── InventoryManagementApi.slnx
```

## Technologies

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Npgsql
- Swagger / OpenAPI
- TCP sockets
- .NET Worker Service
- Docker

## Database

The system uses exactly three application tables:

1. `Categories`
2. `Products`
3. `StockTransactions`

Entity Framework Core also creates:

```text
__EFMigrationsHistory
```

### Relationships

```text
Categories
    1
    |
    | N
Products
    1
    |
    | N
StockTransactions
```

A Category can contain many Products.

A Product belongs to one Category and can have many StockTransactions.

## Prerequisites

Required:

- .NET 10 SDK
- PostgreSQL
- Entity Framework Core CLI

Optional:

- OpenBSD Netcat for manual TCP testing
- Docker

For Arch Linux / CachyOS:

```bash
sudo pacman -S postgresql openbsd-netcat
```

Install EF Core CLI:

```bash
dotnet tool install --global dotnet-ef
```

If the global .NET tool directory is not available in `PATH`:

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

## PostgreSQL Setup

Start PostgreSQL:

```bash
sudo systemctl enable --now postgresql
```

Create the database user and database:

```bash
sudo -iu postgres psql
```

Run:

```sql
CREATE USER inventory_user WITH PASSWORD 'inventory_password';
CREATE DATABASE inventory_db OWNER inventory_user;
GRANT ALL PRIVILEGES ON DATABASE inventory_db TO inventory_user;
```

Exit PostgreSQL:

```sql
\q
```

The application uses this connection string by default:

```text
Host=localhost;Port=5432;Database=inventory_db;Username=inventory_user;Password=inventory_password
```

## Restore and Build

From the solution root:

```bash
dotnet restore
dotnet build
```

## Apply EF Core Migrations

Run:

```bash
dotnet ef database update \
  --project InventoryManagementApi \
  --startup-project InventoryManagementApi
```

Current migrations:

```text
InitialCreate
Task3DataModel
Task3SeedData
FinalizeTugas9
```

The seed data contains:

- 3 categories
- 10 products
- 5 stock transactions

## Run the API

From the solution root:

```bash
dotnet run --project InventoryManagementApi
```

Development URL:

```text
http://localhost:5064
```

Swagger UI:

```text
http://localhost:5064/swagger
```

The API also starts the TCP server on:

```text
127.0.0.1:5050
```

## Run the Worker

Open another terminal:

```bash
dotnet run --project InventoryManagementWorker
```

The Worker periodically performs this TCP cycle:

```text
PING
GET_LOW_STOCK
RESTOCK
GET_SUMMARY
```

For every low-stock product:

```text
Restock Quantity =
(2 × ReorderLevel) - QuantityInStock
```

The Worker writes summary reports to:

```text
InventoryManagementWorker/logs/inventory-report.log
```

## REST API Endpoints

### Categories

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/Categories` | Get all categories |
| GET | `/api/Categories/{id}` | Get category by ID |
| POST | `/api/Categories` | Create a category |
| PUT | `/api/Categories/{id}` | Update a category |
| DELETE | `/api/Categories/{id}` | Delete a category |

A category cannot be deleted while one or more products reference it.

### Products

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/Products` | Get products |
| GET | `/api/Products/{id}` | Get product by ID |
| POST | `/api/Products` | Create a product |
| PUT | `/api/Products/{id}` | Update a product |
| DELETE | `/api/Products/{id}` | Delete a product |
| GET | `/api/Products/low-stock` | Get low-stock products |
| GET | `/api/Products/{id}/transactions` | Get transactions for a product |

`GET /api/Products` supports:

```text
search
categoryId
page
pageSize
```

Example:

```text
GET /api/Products?search=mouse&categoryId=1001&page=1&pageSize=10
```

A newly created Product starts with:

```text
QuantityInStock = 0
```

Stock changes are recorded through StockTransactions.

A Product cannot be deleted if it already has stock transactions.

### Stock Transactions

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/StockTransactions` | Get stock transactions |
| GET | `/api/StockTransactions/{id}` | Get transaction by ID |
| GET | `/api/StockTransactions/product/{productId}` | Get transactions by product |
| POST | `/api/StockTransactions` | Create an API stock transaction |
| POST | `/api/StockTransactions/worker` | Create a Worker stock transaction |

Transaction types:

```text
1 = In
2 = Out
3 = Adjustment
```

Transaction sources:

```text
1 = Api
2 = Worker
```

## Stock Business Rules

### Stock In

The quantity must be positive.

The quantity is added to the current stock.

### Stock Out

The quantity must be positive.

The transaction is rejected when the requested quantity is greater than the available stock.

Example:

```json
{
  "productId": 1001,
  "type": 2,
  "quantity": 999,
  "note": "Insufficient stock test"
}
```

Expected response:

```text
400 Bad Request
```

with:

```json
{
  "title": "Stock Transaction Error",
  "status": 400,
  "detail": "Insufficient stock."
}
```

### Adjustment

Adjustment can use a positive or negative quantity.

The operation is rejected if the resulting inventory would become negative.

## Low Stock

A Product is considered low-stock when:

```text
QuantityInStock <= ReorderLevel
```

Endpoint:

```text
GET /api/Products/low-stock
```

## TCP Protocol

TCP communication uses newline-delimited JSON.

Each request is sent as one JSON object followed by a newline.

General request:

```json
{
  "requestId": "request-001",
  "command": "PING",
  "data": null
}
```

General success response:

```json
{
  "requestId": "request-001",
  "status": "ok",
  "data": {},
  "error": null
}
```

General error response:

```json
{
  "requestId": "request-001",
  "status": "error",
  "data": null,
  "error": {
    "code": "ERROR_CODE",
    "message": "Error message"
  }
}
```

## TCP Commands

### PING

Request:

```json
{"requestId":"manual-001","command":"PING"}
```

Response:

```json
{"requestId":"manual-001","status":"ok","data":"PONG","error":null}
```

### GET_LOW_STOCK

Request:

```json
{"requestId":"manual-002","command":"GET_LOW_STOCK"}
```

Returns products where:

```text
QuantityInStock <= ReorderLevel
```

### RESTOCK

Request:

```json
{
  "requestId": "manual-003",
  "command": "RESTOCK",
  "data": {
    "productId": 1001,
    "quantity": 6
  }
}
```

RESTOCK records the stock transaction as:

```text
Type = In
Source = Worker
```

### GET_SUMMARY

Request:

```json
{"requestId":"manual-004","command":"GET_SUMMARY"}
```

The returned summary contains:

```text
totalProducts
totalUnits
lowStockCount
stockValue
```

### Invalid Command

Request:

```json
{"requestId":"manual-error","command":"INVALID_COMMAND"}
```

Example response:

```json
{
  "requestId": "manual-error",
  "status": "error",
  "data": null,
  "error": {
    "code": "UNKNOWN_COMMAND",
    "message": "Unknown command: INVALID_COMMAND"
  }
}
```

## Manual TCP Test with Netcat

Connect:

```bash
nc 127.0.0.1 5050
```

Example commands:

```json
{"requestId":"manual-001","command":"PING"}
{"requestId":"manual-002","command":"GET_LOW_STOCK"}
{"requestId":"manual-003","command":"GET_SUMMARY"}
{"requestId":"manual-004","command":"INVALID_COMMAND"}
```

## Worker Example

Example successful cycle:

```text
===== Inventory Worker Cycle Started =====

Connected to TCP server 127.0.0.1:5050.

TCP server responded with PONG.

Low-stock products found: 1

Restocked product 1001 (Wireless Mouse) by 6 units.

INVENTORY SUMMARY |
Total Products: 10 |
Total Units: 289 |
Low Stock Products: 0 |
Stock Value: 30750000.00 |
Restocked Products This Cycle: 1 |
Units Added This Cycle: 6

===== Inventory Worker Cycle Finished =====
```

## Validation and Error Handling

The application validates:

- Required request fields
- Unique category names
- Unique product SKUs
- Existing category references
- Non-negative UnitPrice
- Non-negative ReorderLevel
- Non-negative stock
- Non-zero stock transaction quantity
- Positive Stock In quantity
- Positive Stock Out quantity
- Sufficient stock for Stock Out
- Adjustment cannot make stock negative
- Product pagination
- TCP JSON format
- TCP request ID
- TCP command
- RESTOCK quantity
- RESTOCK product

HTTP errors use ASP.NET Core `ProblemDetails`.

TCP errors use structured JSON responses.

## Worker Reliability

The Worker supports:

- Startup delay
- Configurable polling interval
- TCP connection timeout
- TCP request timeout
- Disconnection handling
- Reconnection on future polling cycles

## TCP Concurrency

The TCP server supports multiple connected clients.

Each client connection is handled independently.

The TCP protocol uses newline-delimited JSON framing and limits an individual input line to 8 KB.

## Docker

Docker support is included as an optional bonus.

Files:

```text
InventoryManagementApi/Dockerfile
InventoryManagementApi/docker-compose.yml
```

## Evidence

Recommended submission evidence structure:

```text
evidence/
├── api/
│   ├── categories/
│   ├── products/
│   └── stock-transactions/
├── worker/
│   └── full-cycle.png
├── tcp/
│   └── manual-tcp-test.png
└── er-diagram/
    └── inventory-er-diagram.png
```

Evidence should include:

- Category CRUD
- Product CRUD
- Low-stock lookup
- Stock In
- Stock Out
- Failed Stock Out because of insufficient stock
- Adjustment
- Worker full cycle
- Manual TCP test
- TCP error response

## ER Diagram

The project contains three related application entities:

```text
Categories 1 ─────── N Products
Products   1 ─────── N StockTransactions
```

A graphical ER diagram should be included with the final submission.