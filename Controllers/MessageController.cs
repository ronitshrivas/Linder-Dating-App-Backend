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
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        // GET: api/message/conversations
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var conversations = await _messageService.GetConversationsAsync(userId);
            return Ok(conversations);
        }

        // GET: api/message/conversation/{otherUserId}
        [HttpGet("conversation/{otherUserId}")]
        public async Task<IActionResult> GetMessages(int otherUserId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var messages = await _messageService.GetMessagesAsync(userId, otherUserId);
            return Ok(messages);
        }

        // POST: api/message/send
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var message = await _messageService.SendMessageAsync(userId, request);

            if (message != null)
                return Ok(message);

            return BadRequest(new { message = "Failed to send message" });
        }

        // PUT: api/message/mark-read/{messageId}
        [HttpPut("mark-read/{messageId}")]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _messageService.MarkAsReadAsync(userId, messageId);

            if (success)
                return Ok(new { message = "Marked as read" });

            return BadRequest(new { message = "Failed to mark as read" });
        }

        // PUT: api/message/mark-all-read/{otherUserId}
        [HttpPut("mark-all-read/{otherUserId}")]
        public async Task<IActionResult> MarkAllAsRead(int otherUserId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _messageService.MarkAllAsReadAsync(userId, otherUserId);

            if (success)
                return Ok(new { message = "All messages marked as read" });

            return BadRequest(new { message = "Failed to mark messages as read" });
        }

        // DELETE: api/message/{messageId}
        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _messageService.DeleteMessageAsync(userId, messageId);

            if (success)
                return Ok(new { message = "Message deleted" });

            return BadRequest(new { message = "Failed to delete message" });
        }
    }
}