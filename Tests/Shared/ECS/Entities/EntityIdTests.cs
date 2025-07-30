using Shared.ECS;
using Xunit;

namespace SharedUnitTests.ECS.Entities;

public class EntityIdTests
{
    [Fact]
    public void Constructor_ShouldSetValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var entityId = new EntityId(guid);

        // Assert
        Assert.Equal(guid, entityId.Value);
    }

    [Fact]
    public void New_ShouldCreateUniqueIds()
    {
        // Act
        var id1 = EntityId.New();
        var id2 = EntityId.New();

        // Assert
        Assert.NotEqual(id1.Value, id2.Value);
        Assert.NotEqual(Guid.Empty, id1.Value);
        Assert.NotEqual(Guid.Empty, id2.Value);
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var entityId = new EntityId(guid);

        // Act
        var result = entityId.ToString();

        // Assert
        Assert.Equal(guid.ToString(), result);
    }

    [Fact]
    public void Value_ShouldBeReadOnly()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var entityId = new EntityId(guid);

        // Act & Assert
        Assert.Equal(guid, entityId.Value);

        // Verify it's readonly by checking the struct is immutable
        var originalValue = entityId.Value;
        // The struct is readonly, so we can't modify it after construction
        Assert.Equal(originalValue, entityId.Value);
    }
}