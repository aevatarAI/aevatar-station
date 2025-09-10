# Orleans Service Discovery Benchmark

This benchmark project compares the performance of MongoDB and ZooKeeper as service discovery providers for Microsoft Orleans.

## Overview

The benchmark measures:
- **Cluster Startup Time**: Time taken to start an Orleans cluster with 3 silos
- **Grain Call Performance**: Throughput and latency of grain method calls
- **Silo Join/Leave Operations**: Time to add and remove silos from the cluster
- **Resource Usage**: Memory consumption during operations

## Test Scenarios

### MongoDB Service Discovery
- Uses `Orleans.Providers.MongoDB` package
- MongoDB single document strategy
- In-memory MongoDB via EphemeralMongo for consistent testing
- Connection pooling optimizations

### ZooKeeper Service Discovery  
- Uses `Microsoft.Orleans.Clustering.ZooKeeper` package
- ZooKeeper ensemble for cluster coordination
- Docker-based ZooKeeper for consistent testing
- Session timeout and operation timeout configurations

## Prerequisites

1. **.NET 9.0 SDK** installed
2. **Docker** installed and running (for ZooKeeper)
3. **MongoDB** (automatically handled via EphemeralMongo)

## Running the Benchmark

```bash
# From the project root
cd benchmark/OrleansServiceDiscoveryBenchmark

# Run the benchmark
dotnet run -c Release
```

## Results

The benchmark generates detailed results including:
- Performance metrics (mean, median, standard deviation)
- Memory allocation profiles
- HTML and Markdown reports
- JSON data for further analysis

Results are saved to `BenchmarkDotNet.Artifacts/results/` directory.

## Configuration

### MongoDB Configuration
- Database: `OrleansServiceDiscoveryBenchmark`
- Strategy: Single Document
- Connection pooling: 10-100 connections
- Collection prefix: `OrleansCluster`

### ZooKeeper Configuration
- Connection: `localhost:2181`
- Root path: `/orleans/servicediscovery/benchmark`
- Session timeout: 30 seconds
- Operation timeout: 30 seconds

## Key Metrics

The benchmark focuses on these performance indicators:

1. **Startup Latency**: How quickly can a cluster be formed?
2. **Call Throughput**: How many grain calls per second?
3. **Membership Changes**: How quickly can silos join/leave?
4. **Memory Efficiency**: How much memory is consumed?
5. **Network Usage**: Bandwidth consumption for coordination

## Expected Results

Generally, you might expect:
- **ZooKeeper**: Lower latency for membership changes, better consistency
- **MongoDB**: Higher throughput for read-heavy workloads, easier operations

Actual results will depend on your specific environment and use case.

## Troubleshooting

### ZooKeeper Issues
```bash
# Manually start ZooKeeper via Docker
docker run -d --name orleans-zookeeper -p 2181:2181 zookeeper:3.7

# Check ZooKeeper status
docker logs orleans-zookeeper
```

### MongoDB Issues
- EphemeralMongo automatically handles MongoDB instance
- Check console output for MongoDB connection issues
- Ensure no other MongoDB instances conflict on port 27017

## Architecture

```
┌─────────────────┐    ┌─────────────────┐
│   MongoDB       │    │   ZooKeeper     │
│   Cluster       │    │   Ensemble      │
│   Coordination  │    │   Coordination  │
└─────────────────┘    └─────────────────┘
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌─────────────────┐
│ Orleans Cluster │    │ Orleans Cluster │
│ (3 Silos)       │    │ (3 Silos)       │
│ MongoDB Provider│    │ ZooKeeper Provider
└─────────────────┘    └─────────────────┘
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌─────────────────┐
│ Benchmark       │    │ Benchmark       │
│ Test Grains     │    │ Test Grains     │
└─────────────────┘    └─────────────────┘
```

## Contributing

To add new test scenarios:
1. Add benchmark methods to `ServiceDiscoveryBenchmark` class
2. Use `[Benchmark]` attribute
3. Follow BenchmarkDotNet naming conventions
4. Update this README with new test descriptions 