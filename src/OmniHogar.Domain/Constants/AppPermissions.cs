namespace OmniHogar.Domain.Constants;

/// <summary>
/// The permission catalogue. Each string is both the <c>permissions.name</c> seeded value,
/// the <c>"permission"</c> JWT claim value, and the authorization policy name
/// (<c>[Authorize(Policy = AppPermissions.X)]</c>). Kept in one place so the API, the token,
/// and the frontend all agree.
/// </summary>
public static class AppPermissions
{
    public const string UsuariosGestionar = "usuarios.gestionar";
    public const string ProductosGestionar = "productos.gestionar";
    public const string ProductosVerCatalogo = "productos.ver_catalogo";
    public const string InventarioConsultar = "inventario.consultar";
    public const string InventarioAjustar = "inventario.ajustar";
    public const string PedidosConsultar = "pedidos.consultar";
    public const string PosRegistrarVenta = "pos.registrar_venta";

    public static readonly IReadOnlyList<string> All =
    [
        UsuariosGestionar,
        ProductosGestionar,
        ProductosVerCatalogo,
        InventarioConsultar,
        InventarioAjustar,
        PedidosConsultar,
        PosRegistrarVenta,
    ];
}
