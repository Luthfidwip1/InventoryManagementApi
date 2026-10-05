# Inventory Management API

Backend-only inventory management system built with ASP.NET Core, Entity Framework Core, PostgreSQL, .NET Worker Service, TCP sockets, and Docker.

The application manages product categories, products, stock transactions, low-stock detection, automatic restocking, and TCP communication between the API and Worker service.

The complete system can be executed locally with .NET or as a Dockerized multi-container application.

---

## Project Structure

```text
InventoryManagementApi/
├── compose.yaml
├── .dockerignore
├── README.md
├── InventoryManagementApi.slnx
│
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
│   ├── InventoryManagementApi.csproj
│   └── Dockerfile
│
└── InventoryManagementWorker/
    ├── Worker.cs
    ├── Program.cs
    ├── appsettings.json
    ├── InventoryManagementWorker.csproj
    └── Dockerfile
```

---

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
- Docker Compose
- DBeaver

---

# Database

The system uses exactly three application tables:

1. `Categories`
2. `Products`
3. `StockTransactions`

Entity Framework Core also creates:

```text
__EFMigrationsHistory
```

## Relationships

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

---

# Prerequisites

## Local Development

Required:

- .NET 10 SDK
- PostgreSQL
- Entity Framework Core CLI

Optional:

- OpenBSD Netcat for manual TCP testing
- DBeaver

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

## Docker Deployment

Required:

- Docker
- Docker Compose

Verify Docker:

```bash
docker --version
docker compose version
```

Verify the Docker daemon:

```bash
systemctl status docker
```

---

# Local PostgreSQL Setup

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

The application uses this connection string by default when running outside Docker:

```text
Host=localhost;Port=5432;Database=inventory_db;Username=inventory_user;Password=inventory_password
```

---

# Restore and Build

From the solution root:

```bash
dotnet restore
dotnet build
```

Or build the applications independently:

```bash
dotnet build InventoryManagementApi/InventoryManagementApi.csproj
dotnet build InventoryManagementWorker/InventoryManagementWorker.csproj
```

---

# EF Core Migrations

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

## Local Migration

For local PostgreSQL:

```bash
dotnet ef database update \
  --project InventoryManagementApi \
  --startup-project InventoryManagementApi
```

## Docker Migration

When the API container starts, migrations are applied automatically using:

```csharp
await dbContext.Database.MigrateAsync();
```

Therefore, manual `dotnet ef database update` is not required for a new Docker database.

---

# Run Locally

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

The API also starts the TCP server on port:

```text
5050
```

## Run the Worker

Open another terminal:

```bash
dotnet run --project InventoryManagementWorker
```

The Worker connects to:

```text
127.0.0.1:5050
```

when running locally.

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

---

# REST API Endpoints

## Categories

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/Categories` | Get all categories |
| GET | `/api/Categories/{id}` | Get category by ID |
| POST | `/api/Categories` | Create a category |
| PUT | `/api/Categories/{id}` | Update a category |
| DELETE | `/api/Categories/{id}` | Delete a category |

A category cannot be deleted while one or more products reference it.

---

## Products

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

---

## Stock Transactions

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

---

# Stock Business Rules

## Stock In

The quantity must be positive.

The quantity is added to the current stock.

## Stock Out

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

Example error:

```json
{
  "title": "Stock Transaction Error",
  "status": 400,
  "detail": "Insufficient stock."
}
```

## Adjustment

Adjustment can use a positive or negative quantity.

The operation is rejected if the resulting inventory would become negative.

---

# Low Stock

A Product is considered low-stock when:

```text
QuantityInStock <= ReorderLevel
```

Endpoint:

```text
GET /api/Products/low-stock
```

---

# TCP Protocol

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

---

# TCP Commands

## PING

Request:

```json
{"requestId":"manual-001","command":"PING"}
```

Response:

```json
{"requestId":"manual-001","status":"ok","data":"PONG","error":null}
```

---

## GET_LOW_STOCK

Request:

```json
{"requestId":"manual-002","command":"GET_LOW_STOCK"}
```

Returns products where:

```text
QuantityInStock <= ReorderLevel
```

---

## RESTOCK

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

---

## GET_SUMMARY

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

---

## Invalid Command

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

---

# Manual TCP Test with Netcat

When the API is running directly on the host:

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

Port `5050` is used internally between containers when running with Docker Compose and is not published to the host by default.

---

# Worker Example

Example successful cycle:

```text
===== Inventory Worker Cycle Started =====

Connected to TCP server api:5050.

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

When running outside Docker, the Worker uses:

```text
127.0.0.1:5050
```

When running in Docker, the Worker uses:

```text
api:5050
```

---

# Validation and Error Handling

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

---

# Worker Reliability

The Worker supports:

- Startup delay
- Configurable polling interval
- TCP connection timeout
- TCP request timeout
- Disconnection handling
- Reconnection on future polling cycles

---

# TCP Concurrency

The TCP server supports multiple connected clients.

Each client connection is handled independently.

The TCP protocol uses newline-delimited JSON framing and limits an individual input line to 8 KB.

---

# Dockerized Inventory Management System

The complete system can be executed with Docker Compose.

The Docker environment consists of three services:

```text
db
api
worker
```

Architecture:

```text
Browser / Swagger
       |
       | localhost:5064
       v
+-----------------------+
| inventory-api         |
| ASP.NET Core          |
| HTTP :8080            |
| TCP  :5050            |
+----------+------------+
           |
           | db:5432
           v
+-----------------------+
| inventory-db          |
| PostgreSQL 18         |
| PostgreSQL :5432      |
+-----------------------+
           ^
           |
           | persistent volume
           |
 inventory_postgres_data


+-----------------------+
| inventory-worker      |
| .NET Worker Service   |
+----------+------------+
           |
           | TCP api:5050
           v
     inventory-api
```

---

## Docker Files

```text
compose.yaml
.dockerignore

InventoryManagementApi/
└── Dockerfile

InventoryManagementWorker/
└── Dockerfile
```

---

## Docker Network Communication

Docker Compose creates an internal network where services can communicate using their service names.

API to PostgreSQL:

```text
Host=db
Port=5432
```

Worker to API TCP server:

```text
Host=api
Port=5050
```

The applications must not use `localhost` for communication between different containers.

---

## Docker Ports

| Service | Host | Container | Purpose |
|---|---:|---:|---|
| API | `5064` | `8080` | REST API / Swagger |
| PostgreSQL | `5433` | `5432` | DBeaver / host access |
| TCP Server | Internal only | `5050` | Worker → API TCP |

PostgreSQL uses host port `5433` so it can run alongside a local PostgreSQL instance using port `5432`.

---

## Validate Docker Compose

From the solution root:

```bash
docker compose config
```

List configured services:

```bash
docker compose config --services
```

Expected:

```text
db
api
worker
```

---

## Build Docker Images

```bash
docker compose build
```

The build creates:

```text
inventorymanagementapi-api
inventorymanagementapi-worker
```

PostgreSQL uses the official:

```text
postgres:18
```

image.

---

## Start the Complete System

Run:

```bash
docker compose up -d
```

Check the containers:

```bash
docker compose ps
```

Expected services:

```text
inventory-db
inventory-api
inventory-worker
```

The database should report:

```text
healthy
```

---

## Swagger with Docker

Swagger is available at:

```text
http://localhost:5064/swagger
```

Example endpoint:

```text
GET /api/Products?page=1&pageSize=10
```

Expected:

```text
200 OK
```

---

## PostgreSQL with Docker

The PostgreSQL container uses:

```text
Database : inventory_db
Username : inventory_user
Password : inventory_password
```

Inside the Docker network:

```text
Host : db
Port : 5432
```

From the host machine:

```text
Host : localhost
Port : 5433
```

---

# DBeaver Configuration

Create a PostgreSQL connection using:

```text
Host     : localhost
Port     : 5433
Database : inventory_db
Username : inventory_user
Password : inventory_password
```

The expected application tables are:

```text
Categories
Products
StockTransactions
__EFMigrationsHistory
```

Example verification query:

```sql
SELECT *
FROM "Products"
ORDER BY "Id";
```

Check migrations:

```sql
SELECT *
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId";
```

---

# Docker Worker Verification

Check Worker logs:

```bash
docker compose logs worker --tail=50
```

A healthy TCP cycle contains output similar to:

```text
Connecting to TCP server api:5050...
Connected to TCP server api:5050.
TCP server responded with PONG.
Low-stock products found: 0
INVENTORY SUMMARY ...
Inventory report written to /app/logs/inventory-report.log.
```

Filter important messages:

```bash
docker compose logs worker --tail=100 |
grep -E "PONG|Low-stock|Restocked|INVENTORY SUMMARY"
```

---

# Docker Automatic Restock Test

Stop the Worker temporarily:

```bash
docker compose stop worker
```

Create a Stock Out transaction that makes a product low-stock.

Example for Product `1001`:

```json
{
  "productId": 1001,
  "type": "Out",
  "quantity": 16,
  "note": "Docker worker automatic restock test"
}
```

If the original stock is `20`, the new quantity becomes:

```text
4
```

For:

```text
ReorderLevel = 5
```

the product is low-stock because:

```text
4 <= 5
```

Verify:

```text
GET /api/Products/low-stock
```

Start the Worker again:

```bash
docker compose start worker
```

The Worker calculates:

```text
Restock Quantity
= (2 × ReorderLevel) - QuantityInStock

= (2 × 5) - 4

= 6
```

The expected final stock is:

```text
10
```

Verify:

```text
GET /api/Products/1001
```

The expected result contains:

```json
{
  "quantityInStock": 10,
  "reorderLevel": 5
}
```

The low-stock endpoint should then return an empty collection if no other products are low-stock:

```json
[]
```

---

# Docker Persistence Test

The PostgreSQL database uses a named Docker volume:

```text
inventorymanagementapi_inventory_postgres_data
```

Check it using:

```bash
docker volume ls | grep inventory
```

To test persistence:

```bash
docker compose down
```

Then:

```bash
docker compose up -d
```

Verify the data again through Swagger or DBeaver.

Existing stock values and transaction history should remain available.

Do not use:

```bash
docker compose down -v
```

unless the database volume should intentionally be deleted.

The `-v` option removes the PostgreSQL volume and its stored database data.

---

# Docker Logs

API:

```bash
docker compose logs api --tail=50
```

Worker:

```bash
docker compose logs worker --tail=50
```

Database:

```bash
docker compose logs db --tail=50
```

Follow Worker logs continuously:

```bash
docker compose logs -f worker
```

---

# Docker Restart

Restart all services:

```bash
docker compose restart
```

Restart only the API:

```bash
docker compose restart api
```

Restart only the Worker:

```bash
docker compose restart worker
```

---

# Stop Docker System

Stop and remove the containers:

```bash
docker compose down
```

Database data remains because the named volume is preserved.

To start again:

```bash
docker compose up -d
```

---

# Docker Final Verification

Check running containers:

```bash
docker compose ps
```

Expected:

```text
inventory-db       Up (healthy)
inventory-api      Up
inventory-worker   Up
```

Check services:

```bash
docker compose config --services
```

Expected:

```text
db
api
worker
```

Check volume:

```bash
docker volume ls | grep inventory
```

Expected:

```text
inventorymanagementapi_inventory_postgres_data
```

Check Worker TCP:

```bash
docker compose logs worker --tail=100 |
grep -E "PONG|Low-stock|Restocked|INVENTORY SUMMARY"
```

---

# Tugas 10 Verification Summary

The Dockerized Inventory Management System has been verified with:

- PostgreSQL running in Docker
- ASP.NET Core API running in Docker
- .NET Worker Service running in Docker
- Three services managed by Docker Compose
- PostgreSQL health check
- Automatic EF Core migrations
- Swagger accessible through `localhost:5064`
- PostgreSQL accessible through `localhost:5433`
- DBeaver connected to Docker PostgreSQL
- API → PostgreSQL container communication
- Worker → API TCP container communication
- TCP `PING` / `PONG`
- Low-stock detection
- Automatic Worker restocking
- Docker volume persistence
- Container restart without database data loss

---

# Evidence

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
├── docker/
│   ├── docker-compose-ps.png
│   ├── swagger-docker.png
│   ├── worker-docker-log.png
│   ├── dbeaver-docker.png
│   └── persistence-test.png
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
- Docker Compose services running
- PostgreSQL Docker healthy
- Swagger running from Docker
- DBeaver connected to Docker PostgreSQL
- Worker TCP communication between containers
- Automatic restock from Worker
- Database persistence after Docker restart

---

# ER Diagram

The project contains three related application entities:

```text
Categories 1 ─────── N Products
Products   1 ─────── N StockTransactions
```

A graphical ER diagram should be included with the final submission.