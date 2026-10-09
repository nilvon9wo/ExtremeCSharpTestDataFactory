using Google.Protobuf.Collections;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Net.NowhereAtAll.Xfty.VectorDatabases.Qdrant.Test;

/// <summary>
/// PREVIEW / proof-of-concept - see ../Xfty.VectorDatabases.Qdrant/README.md
/// for known assumptions and accepted risks.
///
/// Proves <see cref="QdrantPersistenceGateway"/> against a real Qdrant
/// container started with Docker via Testcontainers. Requires a running
/// Docker daemon; those tests skip (rather than fail) when one isn't
/// reachable. The record-shape guards reject before any network call, so
/// they run against a client that is never contacted.
/// </summary>
[Trait("Category", "Docker")]
public sealed class QdrantPersistenceGatewayTest(QdrantContainerFixture qdrant)
    : IClassFixture<QdrantContainerFixture>
{
    private const string DockerUnavailable =
        "Docker is not reachable from this machine - start Docker Desktop to run this tier.";

    private const string NeverContactedHost = "localhost";

    // The DocumentChunk collection is shared with the Supply test, whose provider
    // creates it at this size; Qdrant rejects a point of any other dimension.
    private const int DocumentChunkDimensions = 16;

    private readonly QdrantContainerFixture _qdrant = qdrant;

    [Fact]
    public async Task Supply_InNowMode_AgainstARealQdrantContainer_ActuallyInsertsARecord()
    {
        Assert.SkipUnless(this._qdrant.IsDockerAvailable, DockerUnavailable);

        // Arrange
        RecordProvider provider = new RecordProvider(typeof(DocumentChunk), new DemoProviderLookup())
            .SetInsertMode(InsertMode.Now)
            .SetPersistenceGateway(new QdrantPersistenceGateway(this._qdrant.Client));

        // Act
        DocumentChunk result = (DocumentChunk)await provider.Supply().ConfigureAwait(true);

        // Assert
        _ = Assert.NotNull(result.Id);
        Assert.NotNull(result.Embedding);
        Assert.Equal(DocumentChunkDimensions, result.Embedding!.Length);
    }

    [Fact]
    public async Task Insert_WhenTheIdIsAlreadySet_KeepsIt()
    {
        Assert.SkipUnless(this._qdrant.IsDockerAvailable, DockerUnavailable);

        // Arrange
        Guid presetId = Guid.NewGuid();
        DocumentChunk chunk = new()
        {
            Id = presetId,
            Content = "preset",
            Embedding = [.. Enumerable.Repeat(0.1f, DocumentChunkDimensions)],
        };
        QdrantPersistenceGateway gateway = new(this._qdrant.Client);

        // Act
        await gateway.Insert([chunk], Field.Of<DocumentChunk>(x => x.Id)).ConfigureAwait(true);

        // Assert
        Assert.Equal(presetId, chunk.Id);
        _ = Assert.Single(await this.Retrieve(nameof(DocumentChunk), presetId).ConfigureAwait(true));
    }

    [Fact]
    public async Task Insert_ForEverySupportedScalarType_WritesItIntoThePayload()
    {
        Assert.SkipUnless(this._qdrant.IsDockerAvailable, DockerUnavailable);

        // Arrange
        ScalarPayloadRecord record = NewScalarPayloadRecord();
        QdrantPersistenceGateway gateway = new(this._qdrant.Client);

        // Act
        await gateway.Insert([record], Field.Of<ScalarPayloadRecord>(x => x.Id)).ConfigureAwait(true);

        // Assert
        MapField<string, Value> payload = await this.PayloadOf(record.Id!.Value).ConfigureAwait(true);
        Assert.Equal("text", payload[nameof(ScalarPayloadRecord.Text)].StringValue);
        Assert.True(payload[nameof(ScalarPayloadRecord.Flag)].BoolValue);
        Assert.Equal(7, payload[nameof(ScalarPayloadRecord.Count)].IntegerValue);
        Assert.Equal(9_000_000_000L, payload[nameof(ScalarPayloadRecord.Total)].IntegerValue);
        Assert.Equal(0.5, payload[nameof(ScalarPayloadRecord.Ratio)].DoubleValue);
        Assert.Equal(0.25, payload[nameof(ScalarPayloadRecord.Score)].DoubleValue);
    }

    [Fact]
    public async Task Insert_WhenAPayloadValueIsNull_LeavesItOutOfThePayload()
    {
        Assert.SkipUnless(this._qdrant.IsDockerAvailable, DockerUnavailable);

        // Arrange
        ScalarPayloadRecord record = NewScalarPayloadRecord();
        QdrantPersistenceGateway gateway = new(this._qdrant.Client);

        // Sanity Check
        Assert.Null(record.Absent);

        // Act
        await gateway.Insert([record], Field.Of<ScalarPayloadRecord>(x => x.Id)).ConfigureAwait(true);

        // Assert
        MapField<string, Value> payload = await this.PayloadOf(record.Id!.Value).ConfigureAwait(true);
        Assert.False(payload.ContainsKey(nameof(ScalarPayloadRecord.Absent)));
    }

    [Fact]
    public async Task Insert_WhenAPayloadTypeIsUnsupported_Throws()
    {
        Assert.SkipUnless(this._qdrant.IsDockerAvailable, DockerUnavailable);

        // Arrange
        DatedPayloadRecord record = new() { Embedding = [0.1f, 0.2f], When = DateTime.UnixEpoch };
        QdrantPersistenceGateway gateway = new(this._qdrant.Client);

        // Act
        NotSupportedException thrown = await Assert.ThrowsAsync<NotSupportedException>(
            () => gateway.Insert([record], Field.Of<DatedPayloadRecord>(x => x.Id))
        ).ConfigureAwait(true);

        // Assert
        Assert.Contains(nameof(DatedPayloadRecord.When), thrown.Message);
    }

    [Fact]
    public async Task Insert_WhenTheIdFieldIsNotAGuid_Throws()
    {
        // Arrange
        using QdrantClient neverContacted = new(NeverContactedHost);
        QdrantPersistenceGateway gateway = new(neverContacted);
        StringKeyedRecord record = new() { Id = "not-a-guid", Embedding = [0.1f] };

        // Act
        NotSupportedException thrown = await Assert.ThrowsAsync<NotSupportedException>(
            () => gateway.Insert([record], Field.Of<StringKeyedRecord>(x => x.Id))
        ).ConfigureAwait(true);

        // Assert
        Assert.Contains("Guid-typed id field", thrown.Message);
    }

    [Fact]
    public async Task Insert_WhenTheRecordHasNoSingleVectorField_Throws()
    {
        // Arrange
        using QdrantClient neverContacted = new(NeverContactedHost);
        QdrantPersistenceGateway gateway = new(neverContacted);
        TwoVectorRecord record = new() { First = [0.1f], Second = [0.2f] };

        // Act
        NotSupportedException thrown = await Assert.ThrowsAsync<NotSupportedException>(
            () => gateway.Insert([record], Field.Of<TwoVectorRecord>(x => x.Id))
        ).ConfigureAwait(true);

        // Assert
        Assert.Contains("found 2", thrown.Message);
    }

    private static ScalarPayloadRecord NewScalarPayloadRecord() =>
        new()
        {
            Embedding = [0.1f, 0.2f],
            Text = "text",
            Flag = true,
            Count = 7,
            Total = 9_000_000_000L,
            Ratio = 0.5f,
            Score = 0.25,
            Absent = null,
        };

    private Task<IReadOnlyList<RetrievedPoint>> Retrieve(string collectionName, Guid id) =>
        this._qdrant.Client.RetrieveAsync(collectionName, id, withPayload: true);

    private async Task<MapField<string, Value>> PayloadOf(Guid id)
    {
        IReadOnlyList<RetrievedPoint> points = await this.Retrieve(nameof(ScalarPayloadRecord), id)
            .ConfigureAwait(false);
        return points.Single().Payload;
    }
}