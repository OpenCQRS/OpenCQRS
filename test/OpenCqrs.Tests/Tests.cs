using FluentAssertions;
using Xunit;

namespace OpenCqrs.Tests;

public class Tests
{
    [Fact]
    public void Test()
    {
        true.Should().BeTrue();
    }
}
