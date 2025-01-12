using Microsoft.EntityFrameworkCore;
using Web.Models;

namespace Web.Repositories
{
    public class ChatRoomUserRepository : GenericRepository<ChatRoomUser>
    {
        public ChatRoomUserRepository(AppDbContext context) : base(context, context.ChatRoomUsers)
        {
        }

        public async Task<bool> IsUserInRoom(string userId, int chatRoomId)
        {
            return await _context.ChatRoomUsers
                .AnyAsync(x => x.UserId == userId && x.ChatRoomId == chatRoomId);
        }

        public async Task<List<ChatRoomUser>> GetRoomUsersAsync(int chatRoomId)
        {
            return await _context.ChatRoomUsers
                .Include(x => x.User)
                .Where(x => x.ChatRoomId == chatRoomId)
                .ToListAsync();
        }

        public async Task<bool> IsRoomAdmin(string userId, int roomId)
        {
            return await _context.ChatRoomUsers
                .AnyAsync(x => x.UserId == userId && 
                              x.ChatRoomId == roomId && 
                              x.IsRoomAdmin);
        }

        public async Task MakeRoomAdmin(string userId, int roomId)
        {
            var roomUser = await _context.ChatRoomUsers
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ChatRoomId == roomId);
            
            if (roomUser == null)
            {
                roomUser = new ChatRoomUser
                {
                    ChatRoomId = roomId,
                    UserId = userId,
                    JoinedAt = DateTime.Now,
                    IsRoomAdmin = true
                };
                await _context.ChatRoomUsers.AddAsync(roomUser);
            }
            else
            {
                roomUser.IsRoomAdmin = true;
                _context.ChatRoomUsers.Update(roomUser);
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task RemoveRoomAdmin(string userId, int roomId)
        {
            var roomUser = await _context.ChatRoomUsers
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ChatRoomId == roomId);
            
            if (roomUser != null)
            {
                roomUser.IsRoomAdmin = false;
                _context.ChatRoomUsers.Update(roomUser);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<ChatRoomUser>> GetRoomUsersWithDetailsAsync(int roomId)
        {
            var users = await _context.ChatRoomUsers
                .Include(x => x.User)
                .Where(x => x.ChatRoomId == roomId)
                .OrderBy(x => x.User.Name)
                .ToListAsync();

            // Debug için veritabanı değerlerini yazdıralım
            foreach (var user in users)
            {
                System.Diagnostics.Debug.WriteLine($"DB Check - UserId: {user.UserId}, RoomId: {user.ChatRoomId}, IsAdmin: {user.IsRoomAdmin}");
            }

            return users;
        }
    }
} 