using AuthAPI.Data;
using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthAPI.Services
{
    public class PhotoService : IPhotoService
    {
        private readonly AppDbContext _context;

        public PhotoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PhotoUploadResponse> UploadPhotoAsync(int userId, PhotoUploadRequest request)
        {
            // Validate user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new PhotoUploadResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Check photo limit (max 6)
            var existingPhotos = await _context.Photos
                .Where(p => p.UserId == userId)
                .CountAsync();

            if (existingPhotos >= 6)
            {
                return new PhotoUploadResponse
                {
                    Success = false,
                    Message = "Maximum 6 photos allowed"
                };
            }

            // In production, you would upload to cloud storage (Cloudinary, AWS S3, etc.)
            // For now, we'll simulate it
            var photoUrl = await SavePhotoToStorage(request.Base64Image);

            var photo = new Photo
            {
                UserId = userId,
                Url = photoUrl,
                PublicId = Guid.NewGuid().ToString(),
                IsMain = request.IsMain || existingPhotos == 0, // First photo is always main
                Order = request.Order > 0 ? request.Order : existingPhotos + 1,
                UploadedAt = DateTime.UtcNow
            };

            // If setting as main, unset other main photos
            if (photo.IsMain)
            {
                var otherPhotos = await _context.Photos
                    .Where(p => p.UserId == userId && p.IsMain)
                    .ToListAsync();

                foreach (var p in otherPhotos)
                {
                    p.IsMain = false;
                }
            }

            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            return new PhotoUploadResponse
            {
                Success = true,
                Message = "Photo uploaded successfully",
                Photo = photo
            };
        }

        public async Task<List<Photo>> GetUserPhotosAsync(int userId)
        {
            return await _context.Photos
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.Order)
                .ToListAsync();
        }

        public async Task<bool> DeletePhotoAsync(int userId, int photoId)
        {
            var photo = await _context.Photos
                .FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId);

            if (photo == null)
                return false;

            // Don't allow deleting if it's the last photo
            var photoCount = await _context.Photos.CountAsync(p => p.UserId == userId);
            if (photoCount <= 1)
                return false;

            // Delete from cloud storage (implement this)
            await DeletePhotoFromStorage(photo.PublicId);

            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetMainPhotoAsync(int userId, int photoId)
        {
            var photo = await _context.Photos
                .FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId);

            if (photo == null)
                return false;

            // Unset all other main photos
            var otherPhotos = await _context.Photos
                .Where(p => p.UserId == userId && p.IsMain)
                .ToListAsync();

            foreach (var p in otherPhotos)
            {
                p.IsMain = false;
            }

            photo.IsMain = true;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ReorderPhotosAsync(int userId, List<int> photoIds)
        {
            var photos = await _context.Photos
                .Where(p => p.UserId == userId)
                .ToListAsync();

            for (int i = 0; i < photoIds.Count; i++)
            {
                var photo = photos.FirstOrDefault(p => p.Id == photoIds[i]);
                if (photo != null)
                {
                    photo.Order = i + 1;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // Simulated cloud storage methods (implement with Cloudinary/AWS S3 in production)
        private async Task<string> SavePhotoToStorage(string base64Image)
        {
            // In production: Upload to Cloudinary/AWS S3
            // For now, return a placeholder URL
            await Task.Delay(100); // Simulate upload delay
            return $"https://storage.linder.com/photos/{Guid.NewGuid()}.jpg";
        }

        private async Task DeletePhotoFromStorage(string publicId)
        {
            // In production: Delete from Cloudinary/AWS S3
            await Task.Delay(50);
        }
    }
}