namespace OmniHogar.Domain.Exceptions;

/// <summary>
/// The requested resource does not exist. <see cref="Message"/> is user-facing (Spanish) and
/// generic on purpose — the resource kind/key are kept as properties for logging, not leaked
/// to the client.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base("No se encontró el recurso solicitado.")
    {
        ResourceName = name;
        ResourceKey = key;
    }

    private NotFoundException(string name, object key, string message)
        : base(message)
    {
        ResourceName = name;
        ResourceKey = key;
    }

    public string ResourceName { get; }

    public object ResourceKey { get; }

    /// <summary>404 con mensaje específico de producto (HU-11 crit. 4, HU-10).</summary>
    public static NotFoundException Producto(object key) =>
        new("Product", key, "El producto solicitado no existe.");
}
