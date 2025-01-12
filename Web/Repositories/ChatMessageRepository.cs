using Microsoft.EntityFrameworkCore;
using Web.Models;
using Web.ViewModels;

namespace Web.Repositories
{
    public class ChatMessageRepository : GenericRepository<ChatMessage>
    {
        public ChatMessageRepository(AppDbContext context) : base(context, context.ChatMessages)
        {
        }

        public async Task<List<ChatMessageViewModel>> GetPreviousMessages(int roomId, int count = 50)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.ChatRoomId == roomId)
                .OrderByDescending(m => m.SentAt)
                .Take(count)
                .Include(m => m.Sender)
                .Include(m => m.ChatRoom)
                    .ThenInclude(r => r.Users)
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatMessageViewModel
                {
                    Id = m.Id,
                    Content = m.Content,
                    ImagePath = m.ImagePath,
                    Type = m.Type,
                    SentAt = m.SentAt,
                    Sender = new UserDto
                    {
                        Id = m.Sender.Id,
                        UserName = m.Sender.UserName,
                        Name = m.Sender.Name,
                        Surname = m.Sender.Surname,
                        ProfileImage = m.Sender.ProfileImage
                    },
                    IsAdmin = m.ChatRoom.Users.Any(u => u.UserId == m.SenderId && u.IsRoomAdmin)
                })
                .ToListAsync();

            return messages;
        }

        public async Task<int> GetCount()
        {
            return await _context.ChatMessages.CountAsync();
        }

        public async Task<List<ChatMessageViewModel>> GetRecentMessages(int count)
        {
            return await _context.ChatMessages
                .Include(x => x.Sender)
                .Include(x => x.ChatRoom)
                .OrderByDescending(x => x.SentAt)
                .Take(count)
                .Select(m => new ChatMessageViewModel
                {
                    Id = m.Id,
                    Content = m.Content,
                    ImagePath = m.ImagePath,
                    Type = m.Type,
                    SentAt = m.SentAt,
                    Sender = new UserDto
                    {
                        Id = m.Sender.Id,
                        UserName = m.Sender.UserName,
                        Name = m.Sender.Name,
                        Surname = m.Sender.Surname,
                        ProfileImage = m.Sender.ProfileImage
                    },
                   
                })
                .ToListAsync();
        }
    }
} 