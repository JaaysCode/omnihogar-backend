namespace OmniHogar.Domain.Entities;

/// <summary>Key/value configuration entry (system_parameters).</summary>
public class SystemParameter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
