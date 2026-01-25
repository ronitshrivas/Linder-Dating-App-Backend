using AuthAPI.Data;
using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AuthAPI.Services
{
    public interface IMessageService
    {
        Task<List<ConversationDto>> GetConversationsAsync(int userId);
        Task<List<MessageDto>> GetMessagesAsync(int userId, int otherUserId);
        Task<MessageDto?> SendMessageAsync(int userId, SendMessageRequest request);
        Task<bool> MarkAsReadAsync(int userId, int messageId);
        Task<bool> MarkAllAsReadAsync(int userId, int otherUserId);
        Task<bool> DeleteMessageAsync(int userId, int messageId);
    }

    public class MessageService : IMessageService
    {
        private readonly AppDbContext _context;

        public MessageService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ConversationDto>> GetConversationsAsync(int userId)
        {
            // Get all users this user has exchanged messages with
            var conversationUserIds = await _context.Messages
                .Where(m => (m.SenderId == userId || m.ReceiverId == userId) && !m.IsDeleted)
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var conversations = new List<ConversationDto>();

            foreach (var otherUserId in conversationUserIds)
            {
                var otherUser = await _context.Users.FindAsync(otherUserId);
                if (otherUser == null) continue;

                var lastMessage = await _context.Messages
                    .Where(m => !m.IsDeleted &&
                               ((m.SenderId == userId && m.ReceiverId == otherUserId) ||
                                (m.SenderId == otherUserId && m.ReceiverId == userId)))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.Messages
                    .CountAsync(m => m.SenderId == otherUserId &&
                                    m.ReceiverId == userId &&
                                    !m.IsRead &&
                                    !m.IsDeleted);

                conversations.Add(new ConversationDto
                {
                    OtherUser = MapToUserDto(otherUser),
                    LastMessage = lastMessage != null ? MapToMessageDto(lastMessage) : null,
                    UnreadCount = unreadCount
                });
            }

            return conversations.OrderByDescending(c => c.LastMessage?.SentAt ?? DateTime.MinValue).ToList();
        }

        public async Task<List<MessageDto>> GetMessagesAsync(int userId, int otherUserId)
        {
            var messages = await _context.Messages
                .Where(m => !m.IsDeleted &&
                           ((m.SenderId == userId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == userId)))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return messages.Select(MapToMessageDto).ToList();
        }

        public async Task<MessageDto?> SendMessageAsync(int userId, SendMessageRequest request)
        {
            // Check if users are matched
            var isMatched = await _context.Matches
                .AnyAsync(m => m.UserId == userId &&
                              m.TargetUserId == request.ReceiverId &&
                              m.IsMatch);

            if (!isMatched)
                return null;

            var message = new Message
            {
                SenderId = userId,
                ReceiverId = request.ReceiverId,
                Content = request.Content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return MapToMessageDto(message);
        }

        public async Task<bool> MarkAsReadAsync(int userId, int messageId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == userId);

            if (message == null)
                return false;

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId, int otherUserId)
        {
            var messages = await _context.Messages
                .Where(m => m.SenderId == otherUserId && m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMessageAsync(int userId, int messageId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);

            if (message == null)
                return false;

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            return true;
        }

        private MessageDto MapToMessageDto(Message message)
        {
            return new MessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                SentAt = message.SentAt,
                IsRead = message.IsRead,
                ReadAt = message.ReadAt
            };
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Age = user.Age,
                Gender = user.Gender,
                MaxDistance = user.MaxDistance,
                City = user.City,
                State = user.State,
                ProfilePhotos = JsonSerializer.Deserialize<List<string>>(user.ProfilePhotos) ?? new List<string>(),
                Hobbies = JsonSerializer.Deserialize<List<string>>(user.Hobbies) ?? new List<string>(),
                Interests = JsonSerializer.Deserialize<List<string>>(user.Interests) ?? new List<string>(),
                ZodiacSign = user.ZodiacSign,
                SunSign = user.SunSign,
                MoonSign = user.MoonSign,
                RashiSign = user.RashiSign,
                Nakshatra = user.Nakshatra,
                ChineseZodiac = user.ChineseZodiac,
                Bio = user.Bio,
                Occupation = user.Occupation,
                Education = user.Education,
                Height = user.Height,
                IsProfileComplete = user.IsProfileComplete,
                CreatedAt = user.CreatedAt
            };
        }
    }
}