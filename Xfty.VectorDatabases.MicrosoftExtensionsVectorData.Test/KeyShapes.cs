namespace Net.NowhereAtAll.Xfty.VectorDatabases.MicrosoftExtensionsVectorData.Test;

// Record shapes for MevdPersistenceGatewayInMemoryTest. Deliberately not
// `file` classes: the gateway names each collection after the record's
// Type.Name, and a file-local type's compiler-generated name (`<File>F1__...`)
// is no name to give a collection.

/// <summary>A non-nullable <c>string</c> key, left unset.</summary>
public sealed class StringKeyedChunk
{
    public string Id { get; set; } = null!;

    public float[]? Embedding { get; set; }
}

/// <summary>A key type the gateway cannot generate a value for.</summary>
public sealed class IntKeyedChunk
{
    public int? Id { get; set; }

    public float[]? Embedding { get; set; }
}

/// <summary>Two <c>float[]</c> properties: no single one to treat as the vector.</summary>
public sealed class TwoVectorChunk
{
    public Guid? Id { get; set; }

    public float[]? First { get; set; }

    public float[]? Second { get; set; }
}