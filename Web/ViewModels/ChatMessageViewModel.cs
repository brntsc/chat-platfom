using Web.Models;

namespace Web.ViewModels
{
    public class ChatMessageViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string? ImagePath { get; set; }
        public MessageType Type { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsCurrentUser { get; set; }
        public bool IsAdmin { get; set; }

        public UserDto Sender { get; set; }
    }
}