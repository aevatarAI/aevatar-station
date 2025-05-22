# Orleans Serializer Performance Benchmark

This project benchmarks the performance of Orleans Serializer compared to System.Text.Json and three MongoDB serializers for large, complex objects typically found in Orleans-based distributed systems.

## Overview

The benchmark tests serialization and deserialization performance using objects that mimic real-world Orleans PubSubGrainState documents, with varying sizes controlled by the number of consumer entries (500 to 16,000).

## Key Features

- Tests serialization/deserialization round-trip performance
- Compares five different serializers:
  - Orleans Serializer (default)
  - System.Text.Json
  - MongoDB BinaryGrainStateSerializer
  - MongoDB BsonGrainStateSerializer
  - MongoDB JsonGrainStateSerializer (Newtonsoft.Json-based)
- Measures performance across different object sizes (1.4MB to 14.6MB)
- Reports execution time, memory usage, and serialized payload size
- Analyzes CPU utilization and garbage collection impact
- Uses BenchmarkDotNet for precise measurements

## Running the Benchmark

```bash
# Run the benchmark in Release mode
dotnet run -c Release
```

## Understanding the Results

The benchmark provides several types of results:

1. **Manual Benchmark**: Shows serialization performance with different object sizes
2. **BenchmarkDotNet Results**: Provides detailed statistical analysis
3. **Visualization**: Open `performance_chart.html` in a browser to see graphical representation
4. **Detailed Report**: See `performance_report.md` for comprehensive analysis

## Sample Results

### Serializer Performance Comparison

```
--------------------------------------------------------------------------------------------
| Consumer Count | Size (MB) | Orleans | System.Text.Json | Binary | BSON | Newtonsoft.Json |
|---------------|-----------|---------|------------------|--------|------|-----------------|
|           500 |      1.40 |    13.0 |             12.0 |   13.0 | 23.0 |           124.0 |
|          1000 |      1.81 |     8.0 |             11.0 |   15.0 | 23.0 |           219.0 |
|          2000 |      2.67 |    16.0 |             29.0 |   16.0 | 63.0 |           407.0 |
|          4000 |      4.38 |    40.0 |             63.0 |   89.0 | 141.0 |           460.0 |
|          8000 |      7.77 |    22.0 |             46.0 |   27.0 | 81.0 |           670.0 |
|         16000 |     14.63 |    71.0 |             78.0 |   74.0 | 164.0 |          1642.0 |
--------------------------------------------------------------------------------------------
```

### Relative Performance (Orleans = 1.0)

```
---------------------------------------------------------------------------------
| Size (MB) | Orleans | System.Text.Json | Binary | BSON  | Newtonsoft.Json    |
|-----------|---------|------------------|--------|-------|-------------------|
|      1.40 |     1.0 |              0.9 |    1.0 |   1.8 |               9.5 |
|      1.81 |     1.0 |              1.4 |    1.9 |   2.9 |              27.4 |
|      2.67 |     1.0 |              1.8 |    1.0 |   3.9 |              25.4 |
|      4.38 |     1.0 |              1.6 |    2.2 |   3.5 |              11.5 |
|      7.77 |     1.0 |              2.1 |    1.2 |   3.7 |              30.5 |
|     14.63 |     1.0 |              1.1 |    1.0 |   2.3 |              23.1 |
---------------------------------------------------------------------------------
```

## Serializer Overview

The benchmark compares five different serialization approaches:

- **Orleans Serializer**: The default serializer for Orleans, optimized for binary serialization
- **System.Text.Json**: Microsoft's modern JSON serializer from the standard library
- **MongoDB BinaryGrainStateSerializer**: Uses Orleans serializer internally, wrapped in a BSON document
- **MongoDB BsonGrainStateSerializer**: Direct object-to-BSON mapping for better document query support
- **MongoDB JsonGrainStateSerializer**: Newtonsoft.Json-based, offers good compatibility with complex objects

## Advanced Metrics

The comprehensive analysis includes:

### Memory Efficiency

| Size (MB) | Orleans | System.Text.Json | Binary | BSON | Newtonsoft.Json |
|-----------|---------|------------------|--------|------|-----------------|
| 1.5MB     | 4.5     | 7.2              | 4.7    | 8.2  | 8.8             |
| 4MB       | 9.8     | 16.5             | 10.3   | 17.9 | 19.1            |
| 8MB       | 18.2    | 31.7             | 19.1   | 33.3 | 36.2            |
| 15MB      | 38.5    | 62.9             | 40.4   | 70.5 | 75.1            |

*Values shown are memory allocations in MB during serialization*

### Serialized Size Comparison

| Serializer           | Relative Size | Format                  |
|----------------------|---------------|-------------------------|
| Orleans              | 100% (base)   | Binary                  |
| Binary Serializer    | 103%          | BSON with binary payload|
| BSON Serializer      | 131%          | Native BSON             |
| System.Text.Json     | 172%          | JSON string             |
| Newtonsoft.Json      | 173%          | JSON converted to BSON  |

## Key Findings

- Orleans Serializer and MongoDB Binary Serializer offer the best overall performance
- Orleans produces binary payloads ~42% smaller than JSON format
- MongoDB Binary Serializer performs nearly as well as direct Orleans serialization
- BSON Serializer offers better document querying but with significant performance tradeoffs
- Newtonsoft.Json Serializer is dramatically slower, especially for large objects
- Orleans creates 38-43% less garbage during serialization operations
- Performance differences between Orleans and Binary serializers diminish for very large objects

## Dependencies

- .NET 9.0
- Orleans 9.0.1
- BenchmarkDotNet
- MongoDB.Driver and MongoDB.Bson
- Orleans.Providers.MongoDB
- Microsoft.Extensions.Options
- Newtonsoft.Json
- System.Text.Json

## Project Structure

- `Program.cs`: Main benchmark code
- `performance_report.md`: Detailed technical analysis with recommendations
- `performance_chart.html`: Interactive visualizations with multiple metrics
- `README.md`: This file 