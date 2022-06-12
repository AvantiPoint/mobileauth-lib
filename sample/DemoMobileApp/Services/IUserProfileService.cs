using Refit;

namespace DemoMobileApp.Services;

public interface IUserProfileService
{
    [Headers("Authorization: Bearer")]
    [Get("/profile")]
    Task<ApiResponse<Dictionary<string, string>>> GetProfileClaims();
}
