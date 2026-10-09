using Microsoft.SemanticKernel.Connectors.InMemory;

namespace Net.NowhereAtAll.Xfty.VectorDatabases.MicrosoftExtensionsVectorData.Test;

/// <summary>
/// The gateway's own key and record-shape handling, against the in-memory
/// <see cref="Microsoft.Extensions.VectorData.VectorStore"/> - no Docker. A
/// string key is something Qdrant (MevdPersistenceGatewayTest's store)
/// rejects outright, so these paths need a store that accepts it.
/// </summary>
public sealed class MevdPersistenceGatewayInMemoryTest
{
    private readonly MevdPersistenceGateway _gateway = new(new InMemoryVectorStore());

    [Fact]
    public async Task Insert_WhenAStringIdIsUnset_GeneratesOne()
    {
        // Arrange
        StringKeyedChunk chunk = new() { Embedding = [0.1f, 0.2f] };

        // Sanity Check
        Assert.Null(chunk.Id);

        // Act
        await this._gateway.Insert([chunk], Field.Of<StringKeyedChunk>(x => x.Id)).ConfigureAwait(true);

        // Assert
        Assert.True(Guid.TryParse(chunk.Id, out _));
    }

    [Fact]
    public async Task Insert_WhenTheIdIsAlreadySet_KeepsIt()
    {
        // Arrange
        Guid presetId = Guid.NewGuid();
        DocumentChunk chunk = new() { Id = presetId, Embedding = [0.1f, 0.2f] };

        // Act
        await this._gateway.Insert([chunk], Field.Of<DocumentChunk>(x => x.Id)).ConfigureAwait(true);

        // Assert
        Assert.Equal(presetId, chunk.Id);
    }

    [Fact]
    public async Task Insert_WhenAnUnsetIdIsNeitherGuidNorString_Throws()
    {
        // Arrange
        IntKeyedChunk chunk = new() { Embedding = [0.1f, 0.2f] };

        // Act
        NotSupportedException thrown = await Assert.ThrowsAsync<NotSupportedException>(
            () => this._gateway.Insert([chunk], Field.Of<IntKeyedChunk>(x => x.Id))
        ).ConfigureAwait(true);

        // Assert
        Assert.Contains("Int32", thrown.Message);
    }

    [Fact]
    public async Task Insert_WhenTheRecordHasNoSingleVectorField_Throws()
    {
        // Arrange
        TwoVectorChunk chunk = new() { First = [0.1f], Second = [0.2f] };

        // Act
        NotSupportedException thrown = await Assert.ThrowsAsync<NotSupportedException>(
            () => this._gateway.Insert([chunk], Field.Of<TwoVectorChunk>(x => x.Id))
        ).ConfigureAwait(true);

        // Assert
        Assert.Contains("found 2", thrown.Message);
    }
}