```

BenchmarkDotNet v0.13.12, macOS Ventura 13.3 (22E252) [Darwin 22.4.0]
Apple M2 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 9.0.101
  [Host]                              : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD DEBUG
  .NET 9.0                            : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD
  Orleans Service Discovery Benchmark : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD

Runtime=.NET 9.0  InvocationCount=1  UnrollFactor=1  

```
| Method                                          | Job                                 | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean            | Error         | StdDev      | Median          | Gen0      | Gen1      | Gen2      | Allocated   |
|------------------------------------------------ |------------------------------------ |--------------- |------------ |------------ |------------ |----------------:|--------------:|------------:|----------------:|----------:|----------:|----------:|------------:|
| &#39;MongoDB Service Discovery - Cluster Startup&#39;   | .NET 9.0                            | Default        | Default     | Default     | Default     |  7,158,086.9 μs |  18,543.87 μs | 17,345.9 μs |  7,166,405.0 μs | 5000.0000 | 2000.0000 | 2000.0000 | 17771.59 KB |
| &#39;ZooKeeper Service Discovery - Cluster Startup&#39; | .NET 9.0                            | Default        | Default     | Default     | Default     | 10,175,561.5 μs |  22,790.04 μs | 21,317.8 μs | 10,166,323.5 μs | 6000.0000 | 4000.0000 | 3000.0000 | 17772.84 KB |
| &#39;MongoDB Service Discovery - Grain Calls&#39;       | .NET 9.0                            | Default        | Default     | Default     | Default     |        558.7 μs |      58.69 μs |    171.2 μs |        533.8 μs |         - |         - |         - |    88.16 KB |
| &#39;ZooKeeper Service Discovery - Grain Calls&#39;     | .NET 9.0                            | Default        | Default     | Default     | Default     |        622.7 μs |      52.31 μs |    151.8 μs |        628.4 μs |         - |         - |         - |    85.02 KB |
| &#39;MongoDB Service Discovery - Silo Join/Leave&#39;   | .NET 9.0                            | Default        | Default     | Default     | Default     | 10,151,254.8 μs |  12,765.45 μs | 11,940.8 μs | 10,148,986.5 μs | 4000.0000 | 2000.0000 | 2000.0000 | 17667.66 KB |
| &#39;ZooKeeper Service Discovery - Silo Join/Leave&#39; | .NET 9.0                            | Default        | Default     | Default     | Default     | 10,190,087.2 μs |  39,450.41 μs | 34,971.8 μs | 10,177,667.7 μs | 5000.0000 | 3000.0000 | 3000.0000 | 17687.23 KB |
| &#39;MongoDB Service Discovery - Cluster Startup&#39;   | Orleans Service Discovery Benchmark | 5              | 1           | Throughput  | 2           |  7,155,135.2 μs |  40,527.50 μs | 10,524.9 μs |  7,158,009.2 μs | 4000.0000 | 1000.0000 | 1000.0000 | 17991.54 KB |
| &#39;ZooKeeper Service Discovery - Cluster Startup&#39; | Orleans Service Discovery Benchmark | 5              | 1           | Throughput  | 2           | 10,174,300.6 μs | 166,539.42 μs | 25,772.2 μs | 10,162,834.6 μs | 6000.0000 | 3000.0000 | 3000.0000 | 17912.09 KB |
| &#39;MongoDB Service Discovery - Grain Calls&#39;       | Orleans Service Discovery Benchmark | 5              | 1           | Throughput  | 2           |      1,335.2 μs |   3,111.08 μs |    807.9 μs |        893.0 μs |         - |         - |         - |    101.6 KB |
| &#39;ZooKeeper Service Discovery - Grain Calls&#39;     | Orleans Service Discovery Benchmark | 5              | 1           | Throughput  | 2           |      1,303.0 μs |   1,356.02 μs |    352.2 μs |      1,389.0 μs |         - |         - |         - |   102.95 KB |
| &#39;MongoDB Service Discovery - Silo Join/Leave&#39;   | Orleans Service Discovery Benchmark | 5              | 1           | Throughput  | 2           | 10,158,195.0 μs | 106,609.50 μs | 27,686.1 μs | 10,166,033.2 μs | 6000.0000 | 4000.0000 | 3000.0000 | 17791.39 KB |
| &#39;ZooKeeper Service Discovery - Silo Join/Leave&#39; | Orleans Service Discovery Benchmark | 5              | 1           | Throughput  | 2           | 10,190,046.5 μs |  19,706.42 μs |  5,117.7 μs | 10,190,198.8 μs | 4000.0000 | 2000.0000 | 1000.0000 | 17663.03 KB |
