using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Web.Models
{
    public class ChatRoom
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public string? ImagePath { get; set; } // Oda resmi için
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public string CreatedById { get; set; }
        public AppUser CreatedBy { get; set; }

        // İlişkiler
        public ICollection<ChatMessage> Messages { get; set; }
        public ICollection<ChatRoomUser> Users { get; set; }

        // Form için gerekli, veritabanına kaydedilmeyecek
        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public bool AdminOnlyChat { get; set; } = false;
    }
} 