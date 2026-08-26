namespace OmniHogar.Application.Common.Interfaces;

/// <summary>
/// Abstraction over password hashing, keeping Application ignorant of the
/// concrete ASP.NET Core Identity hashing implementation (Infrastructure-only concern).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
}
