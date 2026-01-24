using AuthAPI.Models;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PhotoController : ControllerBase
    {
        private readonly IPhotoService _photoService;

        public PhotoController(IPhotoService photoService)
        {
            _photoService = photoService;
        }

        // POST: api/photo/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadPhoto([FromBody] PhotoUploadRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _photoService.UploadPhotoAsync(userId, request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        // GET: api/photo/my-photos
        [HttpGet("my-photos")]
        public async Task<IActionResult> GetMyPhotos()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var photos = await _photoService.GetUserPhotosAsync(userId);
            return Ok(photos);
        }

        // DELETE: api/photo/{photoId}
        [HttpDelete("{photoId}")]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _photoService.DeletePhotoAsync(userId, photoId);

            if (success)
                return Ok(new { message = "Photo deleted successfully" });

            return BadRequest(new { message = "Failed to delete photo" });
        }

        // PUT: api/photo/{photoId}/set-main
        [HttpPut("{photoId}/set-main")]
        public async Task<IActionResult> SetMainPhoto(int photoId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _photoService.SetMainPhotoAsync(userId, photoId);

            if (success)
                return Ok(new { message = "Main photo updated" });

            return BadRequest(new { message = "Failed to update main photo" });
        }

        // PUT: api/photo/reorder
        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderPhotos([FromBody] List<int> photoIds)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _photoService.ReorderPhotosAsync(userId, photoIds);

            if (success)
                return Ok(new { message = "Photos reordered successfully" });

            return BadRequest(new { message = "Failed to reorder photos" });
        }
    }
}