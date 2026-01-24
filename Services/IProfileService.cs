using AuthAPI.Models;

namespace AuthAPI.Services
{
    public interface IProfileService
    {
        Task<UserDto?> GetProfileAsync(int userId);
        Task<AuthResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
        Task<bool> DeleteAccountAsync(int userId);
    }
}