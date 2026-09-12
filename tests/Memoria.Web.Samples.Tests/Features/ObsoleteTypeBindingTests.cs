using System;
using FluentAssertions;
using FluentAssertions.Execution;
using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Dcb.Extensions;
using Memoria.EventSourcing.Domain;
using Memoria.EventSourcing.Extensions;
using Memoria.Web.Samples.Dcb.Aggregates;
using Memoria.Web.Samples.Dcb.Events;
using Memoria.Web.Samples.Dcb.Projections;
using Memoria.Web.Samples.Streamed.Aggregates;
using Memoria.Web.Samples.Streamed.Events;
using Memoria.Web.Samples.Streamed.Projections;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// The types under test are obsolete on purpose. Naming them is the point.
#pragma warning disable CS0618

namespace Memoria.Web.Samples.Tests.Features;

/// <summary>
/// The retired parts of the sample domain stay bound.
/// </summary>
/// <remarks>
/// A type marked obsolete is one nothing should write through any more, not one the store has
/// forgotten: the events it left in the log and the snapshots written by its shape are still there,
/// and reading them back needs the binding. So the same scans that bind the live types must bind
/// these — which is also what lets the web tool register them from the uploaded assembly.
/// </remarks>
public class ObsoleteTypeBindingTests
{
    [Fact]
    public void Binds_the_retired_streamed_types_beside_the_live_ones()
    {
        new ServiceCollection().AddMemoriaEventSourcing(typeof(Program));

        using var scope = new AssertionScope();

        TypeBindings.EventTypeBindings.Should()
            .Contain(TypeBindings.GetTypeBindingKey("LoyaltyPointsEarned", 1), typeof(LoyaltyPointsEarnedEvent));
        TypeBindings.AggregateTypeBindings.Should()
            .Contain(TypeBindings.GetTypeBindingKey("LoyaltyBalance", 1), typeof(LoyaltyBalance));
        TypeBindings.ProjectionTypeBindings.Should()
            .Contain(TypeBindings.GetTypeBindingKey("LoyaltyStatement", 1), typeof(LoyaltyStatement));
    }

    [Fact]
    public void Binds_the_retired_dcb_types_beside_the_live_ones()
    {
        new ServiceCollection().AddMemoriaDcb(typeof(Program));

        using var scope = new AssertionScope();

        TypeBindings.EventTypeBindings.Should()
            .Contain(TypeBindings.GetTypeBindingKey("SupplierRated", 1), typeof(SupplierRatedEvent));
        DcbTypeBindings.AggregateTypeBindings.Should()
            .Contain(TypeBindings.GetTypeBindingKey("SupplierRating", 1), typeof(SupplierRating));
        DcbTypeBindings.ProjectionTypeBindings.Should()
            .Contain(TypeBindings.GetTypeBindingKey("SupplierScorecard", 1), typeof(SupplierScorecard));
    }

    /// <summary>
    /// What makes them retired rather than merely old: the attribute the compiler reads, on the
    /// model, the event, and the identifier that addresses the model — so nothing new can reach
    /// any of them without being told.
    /// </summary>
    [Theory]
    [InlineData(typeof(LoyaltyPointsEarnedEvent))]
    [InlineData(typeof(LoyaltyBalance))]
    [InlineData(typeof(LoyaltyBalanceId))]
    [InlineData(typeof(LoyaltyStatement))]
    [InlineData(typeof(LoyaltyStatementId))]
    [InlineData(typeof(SupplierRatedEvent))]
    [InlineData(typeof(SupplierRating))]
    [InlineData(typeof(SupplierRatingId))]
    [InlineData(typeof(SupplierScorecard))]
    [InlineData(typeof(SupplierScorecardId))]
    public void Marks_every_retired_type_obsolete(Type type) =>
        type.Should().BeDecoratedWith<ObsoleteAttribute>(
            attribute => !attribute.IsError, "a retired type warns rather than refuses, so the history it wrote can still be read");
}
