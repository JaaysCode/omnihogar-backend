using Microsoft.AspNetCore.Identity;

namespace OmniHogar.Infrastructure.Identity;

/// <summary>
/// OmniHogar's Identity user. Extend with profile fields (household, phone, etc.) as needed.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}
