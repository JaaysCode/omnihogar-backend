namespace OmniHogar.Domain.Constants;

/// <summary>Fixed identifiers for the permissions seeded by migrations.</summary>
public static class SeededPermissionIds
{
    public static readonly Guid UsuariosGestionar = new("11110000-0000-0000-0000-000000000001");
    public static readonly Guid ProductosGestionar = new("11110000-0000-0000-0000-000000000002");
    public static readonly Guid ProductosVerCatalogo = new("11110000-0000-0000-0000-000000000003");
    public static readonly Guid InventarioConsultar = new("11110000-0000-0000-0000-000000000004");
    public static readonly Guid InventarioAjustar = new("11110000-0000-0000-0000-000000000005");
    public static readonly Guid PedidosConsultar = new("11110000-0000-0000-0000-000000000006");
    public static readonly Guid PosRegistrarVenta = new("11110000-0000-0000-0000-000000000007");
}
