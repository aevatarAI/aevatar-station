# LayeredLatencyBenchmark

A comprehensive performance benchmark tool for testing layered agent-to-agent communication latency and throughput in the Aevatar framework built on Microsoft Orleans.

## Overview

The LayeredLatencyBenchmark measures the performance characteristics of hierarchical agent communication patterns, where leader agents publish events to multiple sub-agents in a layered architecture. This benchmark is essential for understanding system scalability and latency characteristics under various load conditions.

## Architecture

### Agent Hierarchy
- **Leader Agents**: Top-level agents that publish events to their sub-agents
- **Sub-agents**: Receive and process events forwarded from leader agents
- **Layered Communication**: Events flow from leaders → sub-agents in a hierarchical pattern

### Key Components

| Component | Purpose |
|-----------|---------|
| `Program.cs` | Main entry point with CLI argument parsing and orchestration |
| `LayeredBenchmarkRunner.cs` | Core benchmark execution engine and metrics collection |
| `LayeredBenchmarkConfig.cs` | Configuration management and validation |
| `OrleansClient.cs` | Orleans cluster client for agent communication |
| `layered_agent_ids.json` | Persistent storage of agent IDs for consistent testing |

## Features

✅ **Scalability Testing**: Test with varying numbers of sub-agents (1 to 8192+)  
✅ **Latency Analysis**: Comprehensive latency metrics (avg, median, P95, P99)  
✅ **Throughput Measurement**: Events per second tracking and validation  
✅ **Warmup Phase**: Configurable warmup to ensure accurate measurements  
✅ **Debug Mode**: Single-event tracing for detailed analysis  
✅ **Persistent Results**: JSON output for analysis and comparison  
✅ **Graceful Cancellation**: Ctrl+C handling with completion waiting  

## Prerequisites

### Infrastructure Requirements
1. **Orleans Cluster**: Running Orleans silo with Aevatar framework
2. **MongoDB**: For Orleans persistence (configured via connection string)
3. **Kafka**: For Orleans streaming (optional, depending on configuration)

### Dependencies
- .NET 9.0
- Microsoft Orleans Client
- Aevatar Framework components
- CommandLineParser for CLI
- Microsoft Extensions Hosting/Logging

## Installation & Setup

### 1. Build the Project
```bash
cd /path/to/aevatar-station/station/benchmark/LayeredLatencyBenchmark
dotnet build
```

### 2. Configure Connection
Ensure your Orleans cluster is running and accessible. The client will connect using the configured Orleans client settings.

### 3. Verify Agent IDs (Optional)
The benchmark uses pre-generated agent IDs stored in `layered_agent_ids.json`. You can customize these IDs if needed.

## Usage

### Basic Usage
```bash
dotnet run
```

### Common Scenarios

#### Quick Performance Test
```bash
# Test with 1-32 sub-agents, 60-second duration
dotnet run -- --max-sub-agents 32 --duration 60
```

#### High-Scale Throughput Test
```bash
# Test up to 8192 sub-agents with higher event rate
dotnet run -- --max-sub-agents 8192 --events-per-second 50 --duration 120
```

#### Debug Mode (Detailed Tracing)
```bash
# Single event trace for debugging
dotnet run -- --debug
```

#### Custom Range Testing
```bash
# Test specific range with custom scaling
dotnet run -- --base-sub-agents 4 --max-sub-agents 1024 --scale-factor 4
```

#### Resume Testing from Specific Level
```bash
# Skip lower levels and start from 128 sub-agents
dotnet run -- --start-from-level 128 --max-sub-agents 2048
```

## Configuration Options

### Core Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `--leader-count` | 1 | Number of leader agents (fixed at 1 for layered architecture) |
| `--base-sub-agents` | 1 | Starting number of sub-agents |
| `--max-sub-agents` | 8192 | Maximum number of sub-agents to test |
| `--scale-factor` | 2 | Multiplication factor for progression (1→2→4→8...) |

### Test Control
| Parameter | Default | Description |
|-----------|---------|-------------|
| `--duration` | 60 | Test duration in seconds per concurrency level |
| `--warmup-duration` | 10 | Warmup phase duration in seconds |
| `--events-per-second` | 10 | Target events per second per leader |
| `--target-daily-events` | 10,000,000 | Target daily event processing capacity |

### Output & Debugging
| Parameter | Default | Description |
|-----------|---------|-------------|
| `--output-file` | layered-results.json | Results output file path |
| `--verbose` | false | Enable detailed logging |
| `--debug` | false | Debug mode: 1 leader + 1 sub-agent, single event |

### Advanced Control
| Parameter | Default | Description |
|-----------|---------|-------------|
| `--completion-timeout` | 60 | Max wait time for event processing completion (seconds) |
| `--completion-check-interval` | 1 | Interval between completion checks (seconds) |
| `--start-from-level` | - | Skip lower levels, start testing from this sub-agent count |
| `--stop-at-level` | 32 | Stop testing at this sub-agent count level |

## Understanding Results

### Metrics Collected
- **Latency**: Average, Median, Min, Max, P95, P99 (milliseconds)
- **Throughput**: Events sent/received per second
- **Completion**: Event processing completion rates
- **Success**: Test completion status per concurrency level

### Sample Output
```json
{
  "Config": { ... },
  "ConcurrencyResults": {
    "1": {
      "SubAgentCount": 1,
      "Success": true,
      "EventsSent": 600,
      "TotalEventsReceived": 600,
      "AverageLatencyMs": 12.5,
      "MedianLatencyMs": 11.2,
      "P95LatencyMs": 18.7,
      "P99LatencyMs": 24.1,
      "ActualEventsPerSecond": 10.0,
      "ThroughputAchieved": true
    }
  }
}
```

## Performance Characteristics

### Expected Scaling Patterns
- **Linear Scaling**: Sub-agent count should scale linearly with consistent per-agent latency
- **Throughput Limits**: System throughput plateaus indicate resource constraints
- **Latency Growth**: P99 latency typically increases with concurrency

### Typical Results Analysis
1. **1-10 Sub-agents**: Baseline latency measurements
2. **10-100 Sub-agents**: Linear scaling validation
3. **100-1000 Sub-agents**: Resource utilization limits
4. **1000+ Sub-agents**: System capacity boundaries

## Troubleshooting

### Common Issues

#### Connection Failures
```bash
# Verify Orleans cluster is running
# Check connection strings in configuration
```

#### Timeout Issues
```bash
# Increase completion timeout for large tests
dotnet run -- --completion-timeout 120 --max-sub-agents 1024
```

#### Performance Degradation
```bash
# Use debug mode to trace single event
dotnet run -- --debug

# Reduce event rate for large scale tests
dotnet run -- --events-per-second 5 --max-sub-agents 2048
```

#### Memory Issues
```bash
# Limit maximum sub-agents
dotnet run -- --max-sub-agents 512 --stop-at-level 512
```

### Debug Mode Analysis
Debug mode creates 1 leader + 1 sub-agent and sends a single event for detailed tracing:
```bash
dotnet run -- --debug --verbose
```

## Integration

### CI/CD Integration
```bash
# Automated performance regression testing
dotnet run -- --max-sub-agents 64 --duration 30 --output-file ci-results.json
```

### Monitoring Integration
The JSON output can be integrated with monitoring systems for continuous performance tracking.

## Contributing

When modifying the benchmark:
1. Maintain backward compatibility with existing result formats
2. Add new metrics as optional fields
3. Update this README with new configuration options
4. Test with both small and large-scale scenarios

## Related Documentation

- [Aevatar Framework Documentation](../../../framework/docs/)
- [Orleans Documentation](https://docs.microsoft.com/en-us/dotnet/orleans/)
- [Performance Tuning Guide](../../docs/)

## License

Part of the Aevatar Station project. See main project license for details. 