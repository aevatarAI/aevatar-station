# BroadcastLatencyBenchmark

A comprehensive benchmark tool for measuring broadcast latency in agent-to-agent communication scenarios within the Aevatar system.

## Overview

This benchmark tool tests broadcast scenarios where publisher agents send events to multiple subscriber agents, measuring the latency from event creation to event processing. It combines patterns from the VerifyDbIssue545 project (for agent creation and broadcast communication) with metrics collection capabilities from the LatencyBenchmark project.

## Features

### Core Functionality
- **Broadcast Communication**: Publishers send events to all subscribers using `BroadCastEventWithDistributionAsync`
- **Latency Measurement**: Comprehensive latency metrics including min, max, average, median, P95, P99, and standard deviation
- **Scalable Testing**: Support for multiple publishers and subscribers
- **Persistent Agent IDs**: Option to reuse agent IDs across test runs
- **Debug Mode**: Send single events to trace communication flow

### Agent Architecture
- **BroadcastScheduleAgent**: Publisher agents that send broadcast events (placed on "Scheduler" silos)
- **BroadcastUserAgent**: Subscriber agents that receive and process events (placed on "User" silos)
- **Event-driven Architecture**: Uses Orleans event handling with automatic state persistence

### Metrics and Reporting
- **Real-time Metrics**: Live tracking of events sent and processed
- **Latency Analysis**: Detailed latency statistics across all subscribers
- **JSON Reports**: Comprehensive benchmark results saved to configurable output files
- **Success Rate Tracking**: Measures percentage of successfully processed events

## Usage

### Basic Usage
```bash
# Run with default settings (1 publisher, 10 subscribers, 5 seconds, stored IDs)
dotnet run

# Run with custom parameters
dotnet run -- --subscriber-count 100 --publisher-count 2 --duration 30

# Run in debug mode
dotnet run -- --debug --subscriber-count 5

# Disable stored agent IDs (create new ones each time)
dotnet run -- --use-stored-ids=false --subscriber-count 50
```

### Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--subscriber-count` | `-s` | Number of subscriber agents | 10 |
| `--publisher-count` | `-p` | Number of publisher agents | 1 |
| `--duration` | `-d` | Test duration in seconds | 5 |
| `--events-per-second` | `-r` | Events per second per publisher | 1 |
| `--output-file` | `-o` | Output file for results | `broadcast-latency-results.json` |
| `--verbose` | `-v` | Enable verbose logging | false |
| `--warmup-duration` | `-w` | Warmup duration in seconds | 10 |
| `--use-stored-ids` | | Use stored agent IDs | true |
| `--debug` | | Debug mode (single event) | false |
| `--event-number` | | Number value in broadcast events | 100 |
| `--completion-timeout` | | Max wait time for completion | 60 |
| `--completion-check-interval` | | Check interval for completion | 1 |

### Example Commands

```bash
# High-scale broadcast test
dotnet run -- --subscriber-count 1000 --publisher-count 5 --duration 120 --events-per-second 2

# Debug communication flow
dotnet run -- --debug --verbose --subscriber-count 3

# Performance test with stored IDs (default behavior)
dotnet run -- --subscriber-count 500 --duration 300 --events-per-second 5

# Quick test with custom event number
dotnet run -- --subscriber-count 20 --duration 10 --event-number 250
```

## Architecture

### Agent Types

#### BroadcastScheduleAgent (Publisher)
- **Role**: Sends broadcast events to all subscribers
- **Placement**: "Scheduler" silos
- **Key Methods**:
  - `BroadcastEventAsync()`: Sends events using broadcast distribution
  - `GetEventsSentAsync()`: Returns count of events sent
  - `ResetMetricsAsync()`: Resets internal metrics

#### BroadcastUserAgent (Subscriber)
- **Role**: Receives and processes broadcast events
- **Placement**: "User" silos
- **Key Methods**:
  - `ActivateAsync()`: Activates the agent
  - `GetCount()`: Returns current event count (similar to VerifyDbIssue545)
  - `GetLatencyMetricsAsync()`: Returns detailed latency metrics
  - `OnBroadcastTestEvent()`: Event handler for processing events

### Event Flow
1. **Initialization**: Create and activate publisher and subscriber agents
2. **Broadcast**: Publishers send events using `BroadCastEventWithDistributionAsync`
3. **Processing**: Subscribers receive events via `OnBroadcastTestEvent` handler
4. **Measurement**: Latency calculated from event creation to processing timestamps
5. **Aggregation**: Metrics collected from all subscribers and aggregated

### State Management
- **Persistent State**: Agent states are persisted using Orleans log consistency
- **Event Sourcing**: State changes tracked through event sourcing pattern
- **Metrics Storage**: Latency measurements stored in concurrent collections

## Metrics

### Latency Metrics
- **Minimum Latency**: Fastest event processing time
- **Maximum Latency**: Slowest event processing time
- **Average Latency**: Mean processing time across all events
- **Median Latency**: 50th percentile processing time
- **P95 Latency**: 95th percentile processing time
- **P99 Latency**: 99th percentile processing time
- **Standard Deviation**: Measure of latency variance

### Throughput Metrics
- **Events Sent**: Total events sent by all publishers
- **Events Processed**: Total events processed by all subscribers
- **Success Rate**: Percentage of events successfully processed
- **Broadcast Fan-out**: Number of subscribers per published event

### Publisher Metrics
- **Events Per Second**: Configured rate per publisher
- **Total Events Sent**: Cumulative count per publisher
- **Publisher Agent ID**: Unique identifier for each publisher

### Subscriber Metrics
- **Events Processed**: Count of events processed per subscriber
- **Individual Latency**: Per-subscriber latency measurements
- **Event Counts**: Running totals (similar to VerifyDbIssue545)

## Configuration

### Orleans Configuration
- **MongoDB Clustering**: Uses MongoDB for cluster membership
- **Kafka Streaming**: Kafka-based event streaming for high-throughput scenarios
- **Silo Placement**: Publishers on "Scheduler" silos, subscribers on "User" silos
- **Storage Providers**: Persistent storage for agent state

### Benchmark Configuration
- **Scalability**: Support for hundreds of publishers and thousands of subscribers
- **Flexibility**: Configurable duration, event rates, and timeouts
- **Persistence**: Optional agent ID storage for consistent testing

## Output

### JSON Report Structure
```json
{
  "Configuration": {
    "SubscriberCount": 10,
    "PublisherCount": 1,
    "Duration": 60,
    "EventsPerSecond": 1,
    "EventNumber": 100
  },
  "Results": [{
    "PublisherCount": 1,
    "SubscriberCount": 10,
    "TotalEventsSent": 60,
    "TotalEventsProcessed": 600,
    "MinLatencyMs": 2.5,
    "MaxLatencyMs": 45.2,
    "AverageLatencyMs": 12.8,
    "P95LatencyMs": 28.5,
    "P99LatencyMs": 42.1,
    "Success": true
  }],
  "GeneratedAt": "2024-01-15T10:30:00Z"
}
```

### Console Output
- **Real-time Progress**: Live updates during benchmark execution
- **Configuration Summary**: Display of all settings before execution
- **Results Summary**: Key metrics displayed at completion
- **Error Reporting**: Detailed error messages for troubleshooting

## Comparison with Related Projects

### vs. VerifyDbIssue545
- **Similarities**: Uses same broadcast pattern (`BroadCastEventWithDistributionAsync`), agent activation, and event counting
- **Differences**: Adds comprehensive latency measurement, metrics collection, and scalable testing framework

### vs. LatencyBenchmark
- **Similarities**: Latency measurement, metrics collection, and reporting capabilities
- **Differences**: Focuses on broadcast (1-to-many) rather than point-to-point (1-to-1) communication

## Prerequisites

- .NET 8.0 or later
- MongoDB (for Orleans clustering)
- Apache Kafka (for event streaming)
- Orleans Silo cluster running with "Scheduler" and "User" silos

## Build and Run

```bash
# Navigate to project directory
cd station/benchmark/BroadcastLatencyBenchmark

# Build the project
dotnet build

# Run with default settings
dotnet run

# Run with custom parameters
dotnet run -- --subscriber-count 50 --duration 30 --verbose
```

## Troubleshooting

### Common Issues

1. **Orleans Connection Issues**
   - Ensure MongoDB is running on `localhost:27017`
   - Verify Orleans cluster is running with correct silo types

2. **Kafka Connection Issues**
   - Ensure Kafka is running on `localhost:9092`
   - Check topic creation permissions

3. **Agent Activation Failures**
   - Verify silo placement configuration
   - Check storage provider configuration

4. **Timeout Issues**
   - Increase `--completion-timeout` for large-scale tests
   - Adjust `--completion-check-interval` for more frequent checks

### Debug Mode
Use `--debug` flag to send only one event and trace the communication flow:
```bash
dotnet run -- --debug --verbose --subscriber-count 3
```

## Contributing

When extending this benchmark:
1. Follow the existing pattern of separating concerns (config, runner, publisher, agents)
2. Maintain compatibility with Orleans event sourcing patterns
3. Add comprehensive logging for debugging
4. Include metrics for new features
5. Update documentation and examples

## License

This project follows the same license as the parent Aevatar project. 