```

BenchmarkDotNet v0.13.12, macOS 15.1.1 (24B91) [Darwin 24.1.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 9.0.201
  [Host]     : .NET 9.0.3 (9.0.325.11113), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 9.0.3 (9.0.325.11113), Arm64 RyuJIT AdvSIMD


```
| Method                              | OperationCount | Mean      | Error     | StdDev    | Ratio | RatioSD |
|------------------------------------ |--------------- |----------:|----------:|----------:|------:|--------:|
| OrleansSerializerRoundtrip          | 5              |  57.48 ms |  1.801 ms |  5.310 ms |  1.00 |    0.00 |
| SystemTextJsonRoundtrip             | 5              |  53.36 ms |  1.048 ms |  1.943 ms |  0.98 |    0.13 |
| BinaryGrainStateSerializerRoundtrip | 5              |  52.40 ms |  1.012 ms |  2.577 ms |  0.92 |    0.09 |
| BsonGrainStateSerializerRoundtrip   | 5              |  86.11 ms |  0.410 ms |  0.384 ms |  1.79 |    0.10 |
| JsonGrainStateSerializerRoundtrip   | 5              | 989.87 ms | 19.741 ms | 31.878 ms | 18.71 |    2.54 |
