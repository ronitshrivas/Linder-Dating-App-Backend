using AuthAPI.Models;

namespace AuthAPI.Services
{
    public interface IPhotoService
    {
        Task<PhotoUploadResponse> UploadPhotoAsync(int userId, PhotoUploadRequest request);
        Task<List<Photo>> GetUserPhotosAsync(int userId);
        Task<bool> DeletePhotoAsync(int userId, int photoId);
        Task<bool> SetMainPhotoAsync(int userId, int photoId);
        Task<bool> ReorderPhotosAsync(int userId, List<int> photoIds);
    }
}