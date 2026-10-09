namespace Net.NowhereAtAll.Xfty.VectorDatabases.Qdrant.Test;

// Record shapes for QdrantPersistenceGatewayTest. Deliberately not `file`
// classes: the gateway names each Qdrant collection after the record's
// Type.Name, and a file-local type's compiler-generated name (`<File>F1__...`)
// is not a valid collection name.

/// <summary>One property of every payload type the gateway maps, plus one left null.</summary>
public sealed class ScalarPayloadRecord
{
    public Guid? Id { get; set; }

    public float[]? Embedding { get; set; }

    public string? Text { get; set; }

    public bool Flag { get; set; }

    public int Count { get; set; }

    public long Total { get; set; }

    public float Ratio { get; set; }

    public double Score { get; set; }

    public string? Absent { get; set; }
}

/// <summary>A payload property of a type the gateway does not map.</summary>
public sealed class DatedPayloadRecord
{
    public Guid? Id { get; set; }

    public float[]? Embedding { get; set; }

    public DateTime When { get; set; }
}

/// <summary>A string key, which Qdrant's connector rejects.</summary>
public sealed class StringKeyedRecord
{
    public string? Id { get; set; }

    public float[]? Embedding { get; set; }
}

/// <summary>Two <c>float[]</c> properties: no single one to treat as the vector.</summary>
public sealed class TwoVectorRecord
{
    public Guid? Id { get; set; }

    public float[]? First { get; set; }

    public float[]? Second { get; set; }
}