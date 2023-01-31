namespace AvantiPoint.MobileAuth.Authentication;

public interface ITokenOptions
{
    string? JwtKey { get; }
    bool OverrideTokenExpiration { get; }
    TimeSpan DefaultExpiration { get; }
}