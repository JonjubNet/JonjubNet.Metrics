using FluentAssertions;
using JonjubNet.Metrics.Shared.Utils;
using System.Text.Json;
using Xunit;

namespace JonjubNet.Metrics.Shared.Tests.Utils
{
    public class JsonSerializerOptionsCacheTests
    {
        [Fact]
        public void Default_ShouldReturnCachedOptions()
        {
            // Act
            var options1 = JsonSerializerOptionsCache.Default;
            var options2 = JsonSerializerOptionsCache.Default;

            // Assert
            options1.Should().BeSameAs(options2);
            options1.PropertyNamingPolicy.Should().BeOfType<JsonCamelCaseNamingPolicy>();
        }

        [Fact]
        public void GetOrCreate_WithSameKey_ShouldReturnSameInstance()
        {
            // Act
            var options1 = JsonSerializerOptionsCache.GetOrCreate("test");
            var options2 = JsonSerializerOptionsCache.GetOrCreate("test");

            // Assert
            options1.Should().BeSameAs(options2);
        }

        [Fact]
        public void GetOrCreate_WithDifferentKeys_ShouldReturnDifferentInstances()
        {
            // Act
            var options1 = JsonSerializerOptionsCache.GetOrCreate("key1");
            var options2 = JsonSerializerOptionsCache.GetOrCreate("key2");

            // Assert
            options1.Should().NotBeSameAs(options2);
        }

        [Fact]
        public void Indented_ShouldHaveWriteIndentedTrue()
        {
            // Act
            var options = JsonSerializerOptionsCache.Indented;

            // Assert
            options.WriteIndented.Should().BeTrue();
        }
    }
}
