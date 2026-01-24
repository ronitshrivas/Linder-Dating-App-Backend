using System;

namespace AuthAPI.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty; // For cloud storage (Cloudinary)
        public bool IsMain { get; set; } = false;
        public int Order { get; set; } // Display order (1-6)
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User? User { get; set; }
    }

    public class PhotoUploadRequest
    {
        public string Base64Image { get; set; } = string.Empty;
        public int Order { get; set; } // 1-6
        public bool IsMain { get; set; } = false;
    }

    public class PhotoUploadResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Photo? Photo { get; set; }
    }
}