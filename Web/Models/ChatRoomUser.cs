namespace Web.Models
{
    public class ChatRoomUser
    {
        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; }
        
        public string UserId { get; set; }
        public AppUser User { get; set; }
        
        public DateTime JoinedAt { get; set; } = DateTime.Now;
        public bool IsRoomAdmin { get; set; } = false;
    }
} 