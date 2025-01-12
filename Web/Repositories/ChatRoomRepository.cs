using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Web.Models;


namespace Web.Repositories
{
    public class ChatRoomRepository : GenericRepository<ChatRoom>
    {
        public ChatRoomRepository(AppDbContext context) : base(context, context.ChatRooms)
        {
        }

        public async Task<List<ChatRoomViewModel>> GetAllWithDetailsAsync()
        {
            var rooms = await _context.ChatRooms
                .Include(x => x.CreatedBy)
                .Include(x => x.Users)
                .OrderByDescending(x => x.CreatedAt)
                .Select(r => new ChatRoomViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    ImagePath = r.ImagePath,
                    CreatedAt = r.CreatedAt,
                    CreatedBy = new UserDto
                    {
                        Id = r.CreatedBy.Id,
                        UserName = r.CreatedBy.UserName,
                        Name = r.CreatedBy.Name,
                        Surname = r.CreatedBy.Surname
                    }
                })
                .ToListAsync();
            return rooms;
        }

        public async Task<ChatRoom> GetByIdWithDetailsAsync(int id)
        {
            return await _context.ChatRooms
                .Include(x => x.CreatedBy)
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddMessageAsync(ChatMessage message)
        {
            await _context.ChatMessages.AddAsync(message);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ChatRoom>> GetRoomsByIds(List<int>? roomIds)
        {
            if (roomIds == null || !roomIds.Any())
                return new List<ChatRoom>();

            return await _context.ChatRooms
                .Where(r => roomIds.Contains(r.Id))
                .ToListAsync();
        }

        public async Task<int> GetCount(Expression<Func<ChatRoom, bool>> predicate = null)
        {
            if (predicate == null)
                return await _context.ChatRooms.CountAsync();
            return await _context.ChatRooms.CountAsync(predicate);
        }

        public async Task<List<ChatRoomViewModel>> GetRecentRooms(int count)
        {
            return await _context.ChatRooms
                .Include(x => x.CreatedBy)
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .Select(r => new ChatRoomViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt,
                    CreatedBy = new UserDto
                    {
                        Id = r.CreatedBy.Id,
                        UserName = r.CreatedBy.UserName,
                        Name = r.CreatedBy.Name,
                        Surname = r.CreatedBy.Surname
                    }
                })
                .ToListAsync();
        }
    }
} 