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
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private bool IsAdmin() =>
            User.FindFirst("IsAdmin")?.Value == "True";

        // ===== DASHBOARD =====
        // GET: api/admin/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            if (!IsAdmin()) return Forbid();
            return Ok(await _adminService.GetDashboardStatsAsync());
        }

        // GET: api/admin/analytics
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            if (!IsAdmin()) return Forbid();
            return Ok(await _adminService.GetAdvancedAnalyticsAsync());
        }

        // ===== USER MANAGEMENT =====
        // GET: api/admin/users?page=1&pageSize=20&search=john&isBanned=false
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isBanned = null)
        {
            if (!IsAdmin()) return Forbid();
            return Ok(await _adminService.GetAllUsersAsync(page, pageSize, search, isBanned));
        }

        // GET: api/admin/users/{userId}
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserDetail(int userId)
        {
            if (!IsAdmin()) return Forbid();
            var user = await _adminService.GetUserDetailAsync(userId);
            if (user == null) return NotFound(new { message = "User not found" });
            return Ok(user);
        }

        // POST: api/admin/users/ban
        [HttpPost("users/ban")]
        public async Task<IActionResult> BanUser([FromBody] BanUserRequest request)
        {
            if (!IsAdmin()) return Forbid();
            var result = await _adminService.BanUserAsync(GetUserId(), request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST: api/admin/users/{userId}/unban
        [HttpPost("users/{userId}/unban")]
        public async Task<IActionResult> UnbanUser(int userId)
        {
            if (!IsAdmin()) return Forbid();
            var result = await _adminService.UnbanUserAsync(GetUserId(), userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // DELETE: api/admin/users/{userId}
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            if (!IsAdmin()) return Forbid();
            var result = await _adminService.DeleteUserAsync(GetUserId(), userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST: api/admin/users/{userId}/make-admin
        [HttpPost("users/{userId}/make-admin")]
        public async Task<IActionResult> MakeAdmin(int userId)
        {
            if (!IsAdmin()) return Forbid();
            var result = await _adminService.MakeAdminAsync(GetUserId(), userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST: api/admin/users/{userId}/remove-admin
        [HttpPost("users/{userId}/remove-admin")]
        public async Task<IActionResult> RemoveAdmin(int userId)
        {
            if (!IsAdmin()) return Forbid();
            var result = await _adminService.RemoveAdminAsync(GetUserId(), userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ===== BULK ACTIONS =====
        // POST: api/admin/users/bulk-ban
        [HttpPost("users/bulk-ban")]
        public async Task<IActionResult> BulkBanUsers([FromBody] BulkBanRequest request)
        {
            if (!IsAdmin()) return Forbid();
            var result = await _adminService.BulkBanUsersAsync(GetUserId(), request);
            return Ok(result);
        }

        // POST: api/admin/users/bulk-delete
        [HttpPost("users/bulk-delete")]
        public async Task<IActionResult> BulkDeleteUsers([FromBody] BulkDeleteRequest request)
        {
            if (!IsAdmin()) return Forbid();
            var result = await _adminService.BulkDeleteUsersAsync(GetUserId(), request);
            return Ok(result);
        }

        // ===== REPORTS =====
        // GET: api/admin/reports?page=1&pageSize=20&isResolved=false
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? isResolved = null)
        {
            if (!IsAdmin()) return Forbid();
            return Ok(await _adminService.GetReportsAsync(page, pageSize, isResolved));
        }

        // POST: api/admin/reports/resolve
        [HttpPost("reports/resolve")]
        public async Task<IActionResult> ResolveReport([FromBody] ResolveReportRequest request)
        {
            if (!IsAdmin()) return Forbid();
            var result = await _adminService.ResolveReportAsync(GetUserId(), request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ===== CONTENT CONTROL =====
        // GET: api/admin/messages?page=1&pageSize=20&userId=5
        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null)
        {
            if (!IsAdmin()) return Forbid();
            return Ok(await _adminService.GetAllMessagesAsync(page, pageSize, userId));
        }

        // DELETE: api/admin/messages/{messageId}
        [HttpDelete("messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            if (!IsAdmin()) return Forbid();
            var result = await _adminService.DeleteMessageAsync(GetUserId(), messageId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // DELETE: api/admin/photos/{photoId}
        [HttpDelete("photos/{photoId}")]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            if (!IsAdmin()) return Forbid();
            var result = await _adminService.DeletePhotoAsync(GetUserId(), photoId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ===== ACTIVITY LOG =====
        // GET: api/admin/logs?page=1&pageSize=20&adminId=1
        [HttpGet("logs")]
        public async Task<IActionResult> GetActivityLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? adminId = null)
        {
            if (!IsAdmin()) return Forbid();
            return Ok(await _adminService.GetActivityLogsAsync(page, pageSize, adminId));
        }

        // ===== APP SETTINGS =====
        // GET: api/admin/settings
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            if (!IsAdmin()) return Forbid();
            return Ok(await _adminService.GetAllSettingsAsync());
        }

        // PUT: api/admin/settings
        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
        {
            if (!IsAdmin()) return Forbid();
            var result = await _adminService.UpdateSettingsAsync(GetUserId(), request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ===== EXPORT =====
        // GET: api/admin/export/users
        [HttpGet("export/users")]
        public async Task<IActionResult> ExportUsers()
        {
            if (!IsAdmin()) return Forbid();
            var csvBytes = await _adminService.ExportUsersToCsvAsync();
            return File(csvBytes, "text/csv", $"users_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }
    }
}