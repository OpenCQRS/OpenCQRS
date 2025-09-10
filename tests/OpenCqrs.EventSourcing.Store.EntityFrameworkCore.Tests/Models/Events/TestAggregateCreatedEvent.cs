﻿using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Events;

[EventType("TestAggregateCreated")]
public record TestAggregateCreatedEvent(string Id, string Name, string Description) : IEvent;
