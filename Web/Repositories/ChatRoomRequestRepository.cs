using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Web.Models;


namespace Web.Repositories
{
    public class ChatRoomRequestRepository : GenericRepository<ChatRoomRequest>
    {
        public ChatRoomRequestRepository(AppDbContext context) : base(context, context.ChatRoomRequests)
        {
        }

        public async Task<List<ChatRoomRequestDto>> GetPendingRequestsAsync()
        {
            return await _context.ChatRoomRequests
                .Include(r => r.User)
                .Include(r => r.ChatRoom)
                .Where(r => r.Status == RequestStatus.Pending)
                .OrderByDescending(r => r.RequestDate)
                .Select(r => new ChatRoomRequestDto
                {
                    Id = r.Id,
                    Message = r.Message,
                    RequestDate = r.RequestDate,
                    Status = r.Status,
                    User = new UserDto
                    {
                        Id = r.User.Id,
                        UserName = r.User.UserName,
                        Name = r.User.Name,
                        Surname = r.User.Surname
                    },
                    ChatRoom = new ChatRoomViewModel
                    {
                        Id = r.ChatRoom.Id,
                        Name = r.ChatRoom.Name,
                        Description = r.ChatRoom.Description,
                        IsActive = r.ChatRoom.IsActive
                    }
                })
                .ToListAsync();
        }

        public async Task<bool> HasPendingRequest(string userId, int chatRoomId)
        {
            return await _context.ChatRoomRequests
                .AnyAsync(r => r.UserId == userId && 
                              r.ChatRoomId == chatRoomId && 
                              r.Status == RequestStatus.Pending);
        }

        public async Task<bool> IsUserApproved(string userId, int chatRoomId)
        {
            return await _context.ChatRoomRequests
                .AnyAsync(r => r.UserId == userId && 
                              r.ChatRoomId == chatRoomId && 
                              r.Status == RequestStatus.Approved);
        }

        public async Task<ChatRoomRequest> GetPendingRequest(string userId, int roomId)
        {
            return await _context.ChatRoomRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && 
                                         r.ChatRoomId == roomId && 
                                         r.Status == RequestStatus.Pending);
        }

        public async Task<int> GetCount(Expression<Func<ChatRoomRequest, bool>> predicate = null)
        {
            if (predicate == null)
                return await _context.ChatRoomRequests.CountAsync();
            return await _context.ChatRoomRequests.CountAsync(predicate);
        }
    }
} 