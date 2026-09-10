namespace OmniHogar.Domain.Constants;

/// <summary>
/// Fixed identifiers for the roles seeded by migrations. Referenced from EF configurations,
/// migrations, and handlers (e.g. assigning <see cref="Cliente"/> on customer registration)
/// so the GUIDs never drift.
/// </summary>
public static class SeededRoleIds
{
    public static readonly Guid Administrador = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid JefeDeBodega = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AsesorDeTienda = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid CoordinadorDeDespacho = new("44444444-4444-4444-4444-444444444444");
    public static readonly Guid Cliente = new("55555555-5555-5555-5555-555555555555");
}
