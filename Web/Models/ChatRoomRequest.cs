using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class ChatRoomRequest
    {
        public int Id { get; set; }
        
        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; }
        
        public string UserId { get; set; }
        public AppUser User { get; set; }
        
        public DateTime RequestDate { get; set; } = DateTime.Now;
        
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        
        public string? Message { get; set; } // Kullanıcının katılım isteği mesajı
    }

    public enum RequestStatus
    {
        Pending,    // Beklemede
        Approved,   // Onaylandı
        Rejected    // Reddedildi
    }
} 