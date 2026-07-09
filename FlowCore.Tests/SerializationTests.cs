using FluentAssertions;
using FlowCore.Core.Interfaces;
using FlowCore.Messaging;
using FlowCore.Tests.Helpers;
using Xunit;

namespace FlowCore.Tests;

public class SerializationTests
{
    private readonly IMessageSerializer _serializer = new SystemTextJsonSerializer();

    [Fact]
    public void Serialize_Generic_ShouldProduceBytes()
    {
        var command = new TestCommand("Hello");
        var bytes = _serializer.Serialize(command);

        bytes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Serialize_NonGeneric_ShouldProduceBytes()
    {
        var command = new TestCommand("World");
        var bytes = _serializer.Serialize(typeof(TestCommand), command);

        bytes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Deserialize_Generic_ShouldRestoreObject()
    {
        var original = new TestCommand("Roundtrip");
        var bytes = _serializer.Serialize(original);

        var deserialized = _serializer.Deserialize<TestCommand>(bytes);

        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("Roundtrip");
    }

    [Fact]
    public void Deserialize_NonGeneric_ShouldRestoreObject()
    {
        var original = new TestCommand("NonGeneric");
        var bytes = _serializer.Serialize(typeof(TestCommand), original);

        var deserialized = (TestCommand)_serializer.Deserialize(typeof(TestCommand), bytes);

        deserialized.Should().NotBeNull();
        deserialized.Name.Should().Be("NonGeneric");
    }

    [Fact]
    public void SerializeDeserialize_Query_ShouldPreserveData()
    {
        var original = new TestQuery("Q001");
        var bytes = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize<TestQuery>(bytes);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("Q001");
    }

    [Fact]
    public void SerializeDeserialize_Event_ShouldPreserveData()
    {
        var original = new TestEvent("event-data");
        var bytes = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize<TestEvent>(bytes);

        deserialized.Should().NotBeNull();
        deserialized!.Data.Should().Be("event-data");
    }

    [Fact]
    public void Serialize_NullObject_ShouldNotThrow()
    {
        var act = () => _serializer.Serialize(typeof(TestCommand), null!);
        act.Should().NotThrow();
    }

    [Fact]
    public void Deserialize_InvalidData_ShouldThrow()
    {
        var bytes = new byte[] { 0, 1, 2, 3 };

        var act = () => _serializer.Deserialize<TestCommand>(bytes);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void SerializeDeserialize_Record_ShouldPreserveCasing()
    {
        var original = new TestCachableQuery("CacheKey123");
        var bytes = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize<TestCachableQuery>(bytes);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("CacheKey123");
        deserialized.CacheKey.Should().Be("test:CacheKey123");
    }
}
