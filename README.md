# Trading Data Aggregator

This project is a high-performance system designed to collect, process, and store market data from multiple exchanges in real-time. It is built using **.NET 9** and **.NET Aspire**, ensuring scalability, maintainability, and robust observability.

## 🛠 Tech Stack
* **Runtime:** .NET 9
* **Orchestration:** .NET Aspire (AppHost) for local development and service discovery
* **Database:** PostgreSQL with Entity Framework Core
* **Concurrency:** `System.Threading.Channels` for high-throughput producer-consumer patterns
* **Observability:** OpenTelemetry (Metrics, Logging, Tracing) integrated via .NET Aspire Dashboard

## 🏗 Architectural Overview
The solution follows **Clean Architecture** principles, separating concerns into distinct layers:

### 1. Domain Layer
* **Value Objects:** Uses `Symbol` for consistent asset representation and validation
* **Rules Engine:** An extensible `IAlertRule` system for real-time alerting based on price thresholds and volume spikes

### 2. Application Layer
* **Decoupled Data Flow:** Implements `IngestionChannel` (bounded) and `AlertChannel` (unbounded) to separate high-speed data ingestion from processing logic
* **Aggregators:** Stateful services for generating OHLC candles across multiple timeframes (1m, 5m, 1h)

### 3. Infrastructure Layer
* **Data Persistence:** Implements the Repository pattern with batch-processing support to handle 100+ ticks/sec efficiently
* **Pluggable Notifications:** Support for Console, File, and Email (stub) notification channels

### 4. Services (Workers)
* **Ingestion Workers:** Specialized background services for REST polling (`RestPollingWorker`) and WebSocket streaming (`WebSocketIngestionWorker`)
* **Processing Engine:** `TickProcessingService` orchestrates normalization, deduplication (via `IMemoryCache`), and batch DB writes

## 🚀 Key Features

### High-Load Performance
To meet the requirement of 100+ ticks/sec, the system uses:
* **Asynchronous Channels:** Non-blocking producers (API/Workers) and a single dedicated consumer (Processing Service)
* **DB Batching:** Data is buffered and written to the database in configurable batches, significantly reducing IO overhead
* **Efficient Deduplication:** Uses an memory cache with sliding expiration to filter out redundant data across different sources

### Graceful Shutdown
The system ensures data integrity during application termination:
1.  The `TickProcessingService` listens for cancellation signals
2.  It drains the remaining items in the `IngestionChannel`
3.  All buffered ticks and closed candles are flushed to the database before the service stops

### Monitoring & Observability
* **Performance Reports:** A dedicated `MonitoringController` calculates system lag and reports source health (online/offline)
* **Metrics:** Custom `TradingMetrics` expose real-time processing latency, throughput, and DB write duration

## 📖 How to Run
1.  **Prerequisites:** Install .NET 9 SDK and Docker Desktop.
2.  **Startup:** Set `AggregatorService.AppHost` as the startup project and run (F5).
3.  **Dashboards:** Use the .NET Aspire dashboard to monitor logs and metrics at the URL provided in the console.
