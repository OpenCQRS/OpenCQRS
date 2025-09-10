﻿using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Events;

[EventType("TestAggregateUpdated")]
public record TestAggregateUpdatedEvent(string Id, string Name, string Description) : IEvent;
