using System;
using System.Collections.Generic;
using System.Text.Json;
using Shouldly;
using Xunit;
using Aevatar.Core.Interception.Configurations;
using Aevatar.Dto;

namespace Aevatar.Application.Tests.Dto;

/// <summary>
/// Comprehensive unit tests for all tracing-related DTOs.
/// Tests cover property assignments, serialization, deserialization, and edge cases.
/// </summary>
public class TracingDtoTests
{
    #region TraceConfigDto Tests

    [Fact]
    public void TraceConfigDto_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var dto = new TraceConfigDto();

        // Assert
        dto.IsEnabled.ShouldBeFalse();
        dto.TrackedIds.ShouldNotBeNull();
        dto.TrackedIds.ShouldBeEmpty();
        dto.Configuration.ShouldBeNull();
    }

    [Fact]
    public void TraceConfigDto_PropertyAssignments_ShouldWorkCorrectly()
    {
        // Arrange
        var config = new TraceConfig();
        var trackedIds = new HashSet<string> { "trace1", "trace2", "trace3" };

        // Act
        var dto = new TraceConfigDto
        {
            IsEnabled = true,
            TrackedIds = trackedIds,
            Configuration = config
        };

        // Assert
        dto.IsEnabled.ShouldBeTrue();
        dto.TrackedIds.ShouldBe(trackedIds);
        dto.Configuration.ShouldBe(config);
    }

    [Fact]
    public void TraceConfigDto_JsonSerialization_ShouldWorkCorrectly()
    {
        // Arrange
        var dto = new TraceConfigDto
        {
            IsEnabled = true,
            TrackedIds = new HashSet<string> { "trace1", "trace2" },
            Configuration = new TraceConfig()
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserializedDto = JsonSerializer.Deserialize<TraceConfigDto>(json);

        // Assert
        deserializedDto.ShouldNotBeNull();
        deserializedDto.IsEnabled.ShouldBe(dto.IsEnabled);
        deserializedDto.TrackedIds.Count.ShouldBe(2);
        deserializedDto.TrackedIds.ShouldContain("trace1");
        deserializedDto.TrackedIds.ShouldContain("trace2");
    }

    [Fact]
    public void TraceConfigDto_WithEmptyTrackedIds_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new TraceConfigDto
        {
            IsEnabled = false,
            TrackedIds = new HashSet<string>(),
            Configuration = null
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserializedDto = JsonSerializer.Deserialize<TraceConfigDto>(json);

        // Assert
        deserializedDto.ShouldNotBeNull();
        deserializedDto.IsEnabled.ShouldBeFalse();
        deserializedDto.TrackedIds.ShouldBeEmpty();
        deserializedDto.Configuration.ShouldBeNull();
    }

    #endregion

    #region UpdateTraceConfigRequest Tests

    [Fact]
    public void UpdateTraceConfigRequest_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new UpdateTraceConfigRequest();

        // Assert
        request.IsEnabled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateTraceConfigRequest_PropertyAssignment_ShouldWorkCorrectly(bool isEnabled)
    {
        // Act
        var request = new UpdateTraceConfigRequest { IsEnabled = isEnabled };

        // Assert
        request.IsEnabled.ShouldBe(isEnabled);
    }

    [Fact]
    public void UpdateTraceConfigRequest_JsonSerialization_ShouldWorkCorrectly()
    {
        // Arrange
        var request = new UpdateTraceConfigRequest { IsEnabled = true };

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserializedRequest = JsonSerializer.Deserialize<UpdateTraceConfigRequest>(json);

        // Assert
        deserializedRequest.ShouldNotBeNull();
        deserializedRequest.IsEnabled.ShouldBeTrue();
    }

    #endregion

    #region TracingStatusDto Tests

    [Fact]
    public void TracingStatusDto_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var dto = new TracingStatusDto();

        // Assert
        dto.IsEnabled.ShouldBeFalse();
        dto.TrackedTraceCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, 5)]
    [InlineData(false, 0)]
    [InlineData(false, 10)]
    public void TracingStatusDto_PropertyAssignments_ShouldWorkCorrectly(bool isEnabled, int trackedCount)
    {
        // Act
        var dto = new TracingStatusDto
        {
            IsEnabled = isEnabled,
            TrackedTraceCount = trackedCount
        };

        // Assert
        dto.IsEnabled.ShouldBe(isEnabled);
        dto.TrackedTraceCount.ShouldBe(trackedCount);
    }

    [Fact]
    public void TracingStatusDto_JsonSerialization_ShouldWorkCorrectly()
    {
        // Arrange
        var dto = new TracingStatusDto
        {
            IsEnabled = true,
            TrackedTraceCount = 42
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserializedDto = JsonSerializer.Deserialize<TracingStatusDto>(json);

        // Assert
        deserializedDto.ShouldNotBeNull();
        deserializedDto.IsEnabled.ShouldBeTrue();
        deserializedDto.TrackedTraceCount.ShouldBe(42);
    }

    #endregion

    #region TraceDto Tests

    [Fact]
    public void TraceDto_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var dto = new TraceDto();

        // Assert
        dto.TraceId.ShouldBe(string.Empty);
        dto.IsEnabled.ShouldBeFalse();
    }

    [Theory]
    [InlineData("trace-123", true)]
    [InlineData("trace-456", false)]
    [InlineData("", true)]
    public void TraceDto_PropertyAssignments_ShouldWorkCorrectly(string traceId, bool isEnabled)
    {
        // Act
        var dto = new TraceDto
        {
            TraceId = traceId,
            IsEnabled = isEnabled
        };

        // Assert
        dto.TraceId.ShouldBe(traceId);
        dto.IsEnabled.ShouldBe(isEnabled);
    }

    [Fact]
    public void TraceDto_JsonSerialization_ShouldWorkCorrectly()
    {
        // Arrange
        var dto = new TraceDto
        {
            TraceId = "test-trace-id",
            IsEnabled = true
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserializedDto = JsonSerializer.Deserialize<TraceDto>(json);

        // Assert
        deserializedDto.ShouldNotBeNull();
        deserializedDto.TraceId.ShouldBe("test-trace-id");
        deserializedDto.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void TraceDto_WithSpecialCharacters_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new TraceDto
        {
            TraceId = "trace-with-special@chars#123!_",
            IsEnabled = false
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserializedDto = JsonSerializer.Deserialize<TraceDto>(json);

        // Assert
        deserializedDto.ShouldNotBeNull();
        deserializedDto.TraceId.ShouldBe("trace-with-special@chars#123!_");
        deserializedDto.IsEnabled.ShouldBeFalse();
    }

    #endregion

    #region UpdateTraceRequest Tests

    [Fact]
    public void UpdateTraceRequest_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new UpdateTraceRequest();

        // Assert
        request.IsEnabled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateTraceRequest_PropertyAssignment_ShouldWorkCorrectly(bool isEnabled)
    {
        // Act
        var request = new UpdateTraceRequest { IsEnabled = isEnabled };

        // Assert
        request.IsEnabled.ShouldBe(isEnabled);
    }

    [Fact]
    public void UpdateTraceRequest_JsonSerialization_ShouldWorkCorrectly()
    {
        // Arrange
        var request = new UpdateTraceRequest { IsEnabled = false };

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserializedRequest = JsonSerializer.Deserialize<UpdateTraceRequest>(json);

        // Assert
        deserializedRequest.ShouldNotBeNull();
        deserializedRequest.IsEnabled.ShouldBeFalse();
    }

    #endregion

    #region TrackedTracesDto Tests

    [Fact]
    public void TrackedTracesDto_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var dto = new TrackedTracesDto();

        // Assert
        dto.TraceIds.ShouldNotBeNull();
        dto.TraceIds.ShouldBeEmpty();
        dto.Count.ShouldBe(0);
    }

    [Fact]
    public void TrackedTracesDto_PropertyAssignments_ShouldWorkCorrectly()
    {
        // Arrange
        var traceIds = new List<string> { "trace1", "trace2", "trace3" };

        // Act
        var dto = new TrackedTracesDto
        {
            TraceIds = traceIds,
            Count = 3
        };

        // Assert
        dto.TraceIds.ShouldBe(traceIds);
        dto.Count.ShouldBe(3);
    }

    [Fact]
    public void TrackedTracesDto_JsonSerialization_ShouldWorkCorrectly()
    {
        // Arrange
        var dto = new TrackedTracesDto
        {
            TraceIds = new List<string> { "trace1", "trace2", "trace3" },
            Count = 3
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserializedDto = JsonSerializer.Deserialize<TrackedTracesDto>(json);

        // Assert
        deserializedDto.ShouldNotBeNull();
        deserializedDto.TraceIds.Count.ShouldBe(3);
        deserializedDto.TraceIds.ShouldContain("trace1");
        deserializedDto.TraceIds.ShouldContain("trace2");
        deserializedDto.TraceIds.ShouldContain("trace3");
        deserializedDto.Count.ShouldBe(3);
    }

    [Fact]
    public void TrackedTracesDto_WithEmptyList_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new TrackedTracesDto
        {
            TraceIds = new List<string>(),
            Count = 0
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserializedDto = JsonSerializer.Deserialize<TrackedTracesDto>(json);

        // Assert
        deserializedDto.ShouldNotBeNull();
        deserializedDto.TraceIds.ShouldBeEmpty();
        deserializedDto.Count.ShouldBe(0);
    }

    #endregion

    #region TrackedTraceDto Tests

    [Fact]
    public void TrackedTraceDto_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var dto = new TrackedTraceDto();

        // Assert
        dto.TraceId.ShouldBe(string.Empty);
        dto.IsEnabled.ShouldBeFalse();
        dto.AddedAt.ShouldBe(default(DateTime));
    }

    [Fact]
    public void TrackedTraceDto_PropertyAssignments_ShouldWorkCorrectly()
    {
        // Arrange
        var addedAt = DateTime.UtcNow;

        // Act
        var dto = new TrackedTraceDto
        {
            TraceId = "test-trace",
            IsEnabled = true,
            AddedAt = addedAt
        };

        // Assert
        dto.TraceId.ShouldBe("test-trace");
        dto.IsEnabled.ShouldBeTrue();
        dto.AddedAt.ShouldBe(addedAt);
    }

    [Fact]
    public void TrackedTraceDto_JsonSerialization_ShouldWorkCorrectly()
    {
        // Arrange
        var addedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var dto = new TrackedTraceDto
        {
            TraceId = "serialization-test-trace",
            IsEnabled = true,
            AddedAt = addedAt
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserializedDto = JsonSerializer.Deserialize<TrackedTraceDto>(json);

        // Assert
        deserializedDto.ShouldNotBeNull();
        deserializedDto.TraceId.ShouldBe("serialization-test-trace");
        deserializedDto.IsEnabled.ShouldBeTrue();
        deserializedDto.AddedAt.ShouldBe(addedAt);
    }

    [Fact]
    public void TrackedTraceDto_WithMinMaxDateTime_ShouldSerializeCorrectly()
    {
        // Arrange & Act for Min DateTime
        var minDto = new TrackedTraceDto
        {
            TraceId = "min-datetime-trace",
            IsEnabled = false,
            AddedAt = DateTime.MinValue
        };

        var minJson = JsonSerializer.Serialize(minDto);
        var deserializedMinDto = JsonSerializer.Deserialize<TrackedTraceDto>(minJson);

        // Assert for Min DateTime
        deserializedMinDto.ShouldNotBeNull();
        deserializedMinDto.AddedAt.ShouldBe(DateTime.MinValue);

        // Arrange & Act for Max DateTime
        var maxDto = new TrackedTraceDto
        {
            TraceId = "max-datetime-trace",
            IsEnabled = true,
            AddedAt = DateTime.MaxValue
        };

        var maxJson = JsonSerializer.Serialize(maxDto);
        var deserializedMaxDto = JsonSerializer.Deserialize<TrackedTraceDto>(maxJson);

        // Assert for Max DateTime
        deserializedMaxDto.ShouldNotBeNull();
        deserializedMaxDto.AddedAt.ShouldBe(DateTime.MaxValue);
    }

    #endregion

    #region AddTrackedTraceRequest Tests

    [Fact]
    public void AddTrackedTraceRequest_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new AddTrackedTraceRequest();

        // Assert
        request.TraceId.ShouldBe(string.Empty);
        request.IsEnabled.ShouldBeTrue(); // Default is true according to the DTO
    }

    [Theory]
    [InlineData("trace-123", true)]
    [InlineData("trace-456", false)]
    [InlineData("", true)]
    public void AddTrackedTraceRequest_PropertyAssignments_ShouldWorkCorrectly(string traceId, bool isEnabled)
    {
        // Act
        var request = new AddTrackedTraceRequest
        {
            TraceId = traceId,
            IsEnabled = isEnabled
        };

        // Assert
        request.TraceId.ShouldBe(traceId);
        request.IsEnabled.ShouldBe(isEnabled);
    }

    [Fact]
    public void AddTrackedTraceRequest_JsonSerialization_ShouldWorkCorrectly()
    {
        // Arrange
        var request = new AddTrackedTraceRequest
        {
            TraceId = "json-test-trace",
            IsEnabled = false
        };

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserializedRequest = JsonSerializer.Deserialize<AddTrackedTraceRequest>(json);

        // Assert
        deserializedRequest.ShouldNotBeNull();
        deserializedRequest.TraceId.ShouldBe("json-test-trace");
        deserializedRequest.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void AddTrackedTraceRequest_WithVeryLongTraceId_ShouldSerializeCorrectly()
    {
        // Arrange
        var longTraceId = new string('a', 1000);
        var request = new AddTrackedTraceRequest
        {
            TraceId = longTraceId,
            IsEnabled = true
        };

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserializedRequest = JsonSerializer.Deserialize<AddTrackedTraceRequest>(json);

        // Assert
        deserializedRequest.ShouldNotBeNull();
        deserializedRequest.TraceId.ShouldBe(longTraceId);
        deserializedRequest.TraceId.Length.ShouldBe(1000);
        deserializedRequest.IsEnabled.ShouldBeTrue();
    }

    #endregion

    #region Cross-DTO Integration Tests

    [Fact]
    public void AllTracingDtos_JsonSerializationRoundTrip_ShouldPreserveData()
    {
        // Arrange
        var traceConfigDto = new TraceConfigDto
        {
            IsEnabled = true,
            TrackedIds = new HashSet<string> { "trace1", "trace2" },
            Configuration = new TraceConfig()
        };

        var tracingStatusDto = new TracingStatusDto
        {
            IsEnabled = true,
            TrackedTraceCount = 2
        };

        var trackedTracesDto = new TrackedTracesDto
        {
            TraceIds = new List<string> { "trace1", "trace2" },
            Count = 2
        };

        // Act & Assert for TraceConfigDto
        var traceConfigJson = JsonSerializer.Serialize(traceConfigDto);
        var deserializedTraceConfig = JsonSerializer.Deserialize<TraceConfigDto>(traceConfigJson);
        deserializedTraceConfig.ShouldNotBeNull();
        deserializedTraceConfig.TrackedIds.Count.ShouldBe(2);

        // Act & Assert for TracingStatusDto
        var tracingStatusJson = JsonSerializer.Serialize(tracingStatusDto);
        var deserializedTracingStatus = JsonSerializer.Deserialize<TracingStatusDto>(tracingStatusJson);
        deserializedTracingStatus.ShouldNotBeNull();
        deserializedTracingStatus.TrackedTraceCount.ShouldBe(2);

        // Act & Assert for TrackedTracesDto
        var trackedTracesJson = JsonSerializer.Serialize(trackedTracesDto);
        var deserializedTrackedTraces = JsonSerializer.Deserialize<TrackedTracesDto>(trackedTracesJson);
        deserializedTrackedTraces.ShouldNotBeNull();
        deserializedTrackedTraces.Count.ShouldBe(2);
    }

    #endregion
}
