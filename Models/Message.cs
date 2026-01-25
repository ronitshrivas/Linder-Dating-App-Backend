using System;

namespace AuthAPI.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public User? Sender { get; set; }
        public User? Receiver { get; set; }
    }

    public class SendMessageRequest
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class ConversationDto
    {
        public UserDto OtherUser { get; set; } = new UserDto();
        public MessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
}