using FluentAssertions;
using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Shared.Utils;
using Xunit;

namespace JonjubNet.Metrics.Shared.Tests.Utils
{
    public class CollectionPoolTests
    {
        [Fact]
        public void RentList_ShouldReturnList()
        {
            // Act
            var list = CollectionPool<MetricEvent>.RentList();

            // Assert
            list.Should().NotBeNull();
            list.Should().BeEmpty();
        }

        [Fact]
        public void ReturnList_ShouldClearAndReturnToList()
        {
            // Arrange
            var list = CollectionPool<MetricEvent>.RentList();
            list.Add(new MetricEvent { Name = "test", Type = MetricType.Counter, Value = 1.0 });

            // Act
            CollectionPool<MetricEvent>.ReturnList(list);
            var list2 = CollectionPool<MetricEvent>.RentList();

            // Assert
            list2.Should().BeEmpty();
        }

        [Fact]
        public void RentStringDictionary_ShouldReturnDictionary()
        {
            // Act
            var dict = CollectionPool<string>.RentStringDictionary();

            // Assert
            dict.Should().NotBeNull();
            dict.Should().BeEmpty();
        }

        [Fact]
        public void ReturnStringDictionary_ShouldClearAndReturnToPool()
        {
            // Arrange
            var dict = CollectionPool<string>.RentStringDictionary();
            dict["key"] = "value";

            // Act
            CollectionPool<string>.ReturnStringDictionary(dict);
            var dict2 = CollectionPool<string>.RentStringDictionary();

            // Assert
            dict2.Should().BeEmpty();
        }
    }
}
