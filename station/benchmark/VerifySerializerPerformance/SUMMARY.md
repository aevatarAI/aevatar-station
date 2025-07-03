# Orleans Serializer Performance Test Results

## Overview

This project compares the performance of Orleans serializer with System.Text.Json and other serializers when handling large objects. The tests measure serialization/deserialization time and output size efficiency.

## Test Environment

- Hardware: Apple M3 Pro, 12 logical and 12 physical cores
- OS: macOS 15.1.1 (24B91)
- .NET SDK: 9.0.201
- Runtime: .NET 9.0.3 (9.0.325.11113), Arm64 RyuJIT AdvSIMD

## Manual Test Results (Round-trip time in ms)

Test objects of different sizes were created by varying the number of consumer records:

| Consumer Count | Size (MB) | Orleans | System.Text.Json | Binary | BSON | Newtonsoft.Json |
|----------------|-----------|---------|------------------|--------|------|-----------------|
| 500            | 1.40      | 13.0    | 11.0             | 12.0   | 25.0 | 141.0           |
| 1000           | 1.81      | 10.0    | 11.0             | 18.0   | 25.0 | 203.0           |
| 2000           | 2.67      | 20.0    | 30.0             | 21.0   | 59.0 | 320.0           |
| 4000           | 4.38      | 31.0    | 50.0             | 41.0   | 125.0| 497.0           |
| 8000           | 7.77      | 80.0    | 88.0             | 80.0   | 138.0| 798.0           |
| 16000          | 14.63     | 83.0    | 108.0            | 138.0  | 188.0| 1720.0          |

## BenchmarkDotNet Results (Release Build)

Detailed benchmark results for ~2.7MB test objects (with 5 operations per test):

| Method                              | Mean      | Error     | StdDev    | Ratio |
|-------------------------------------|-----------|-----------|-----------|-------|
| OrleansSerializerRoundtrip          | 57.48 ms  | 1.801 ms  | 5.310 ms  | 1.00  |
| SystemTextJsonRoundtrip             | 53.36 ms  | 1.048 ms  | 1.943 ms  | 0.98  |
| BinaryGrainStateSerializerRoundtrip | 52.40 ms  | 1.012 ms  | 2.577 ms  | 0.92  |
| BsonGrainStateSerializerRoundtrip   | 86.11 ms  | 0.410 ms  | 0.384 ms  | 1.79  |
| JsonGrainStateSerializerRoundtrip   | 989.87 ms | 19.741 ms | 31.878 ms | 18.71 |

## Key Findings

1. **Orleans Serializer Performance**:
   - For smaller objects (~2MB), Orleans and System.Text.Json have similar performance
   - For medium-sized objects (4-8MB), Orleans shows better performance than System.Text.Json
   - For large objects (14MB+), Orleans significantly outperforms all other serializers
   - The BinaryGrainStateSerializer shows slightly better performance than Orleans for objects around 2-3MB

2. **MongoDB Serializers**:
   - BinaryGrainStateSerializer performs best, showing excellent performance close to or better than the Orleans native serializer
   - BsonGrainStateSerializer is about 1.8x slower than Orleans
   - JsonGrainStateSerializer (Newtonsoft.Json-based) is significantly slower (18.7x)

3. **Scaling with Size**:
   - Orleans serializer scaling with size is better than System.Text.Json for larger objects
   - Newtonsoft.Json performance degrades dramatically with larger object sizes
   - BSON shows poor scaling for medium and large objects

## Conclusion

For large object serialization in Orleans applications, the native Orleans serializer provides excellent performance, especially as object size increases. For objects around 2-3MB, System.Text.Json and BinaryGrainStateSerializer are slightly faster.

For MongoDB storage providers, the BinaryGrainStateSerializer is clearly the best option, offering performance that's even slightly better than the native Orleans serializer for smaller objects. BsonGrainStateSerializer is a viable but slower alternative at nearly 2x worse performance, while JsonGrainStateSerializer should be avoided for large objects due to its extremely poor performance (nearly 19x slower than Orleans serializer). 