﻿using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Tests.Models.Aggregates;

public class TestAggregate1Id(string testAggregateId) : IAggregateId<TestAggregate1>
{
    public string Id => $"test-aggregate-1:{testAggregateId}";
}
