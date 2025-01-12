using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        
        public string? Content { get; set; }
        public string? ImagePath { get; set; }
        public MessageType Type { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;
        
        // Gönderen kullanıcı
        public string SenderId { get; set; }
        public virtual AppUser Sender { get; set; }
        
        // Hangi odaya ait
        public int ChatRoomId { get; set; }
        public virtual ChatRoom ChatRoom { get; set; }
    }

    public enum MessageType
    {
        Text = 0,
        Image = 1,
        Emoji = 2,
        System = 3
    }
} 