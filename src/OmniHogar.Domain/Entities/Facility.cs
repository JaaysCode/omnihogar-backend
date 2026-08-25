using OmniHogar.Domain.Enums;

namespace OmniHogar.Domain.Entities;

/// <summary>Physical location: warehouse or point of sale (facilities).</summary>
public class Facility
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public FacilityType Type { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
