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

    /// <summary>404 con mensaje específico de empleado (HU-31).</summary>
    public static NotFoundException Empleado(object key) =>
        new("Employee", key, "El empleado solicitado no existe.");

    /// <summary>404 con mensaje específico de rol (HU-31).</summary>
    public static NotFoundException Rol(object key) =>
        new("Role", key, "El rol solicitado no existe.");

    /// <summary>404 cuando se opera sobre una línea que no está en el carrito (HU-05 crit. 2/3).</summary>
    public static NotFoundException ItemEnCarrito(object key) =>
        new("CartItem", key, "El producto no está en el carrito.");

    /// <summary>404 con mensaje específico de pedido (HU-08/HU-09 — checkout).</summary>
    public static NotFoundException Pedido(object key) =>
        new("Order", key, "El pedido solicitado no existe.");
}
