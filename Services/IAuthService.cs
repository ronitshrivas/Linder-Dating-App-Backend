using System;
using AuthAPI.Models;

namespace AuthAPI.Services
{
    public interface IAuthService
    {
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);
        Task<CompleteProfileResponse> CompleteProfileAsync(int userId, CompleteProfileRequest request);
        Task<ProfileStatusDto> GetProfileStatusAsync(int userId);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}

