using Web.Models;
using System.Security.Claims;
using Web.ViewModels;

namespace Web.Helpers
{
    public static class MessageHelper
    {
        public static string CreateMessageHtml(ChatMessageViewModel message, string currentUserId)
        {
            var isOutgoing = message.Sender.Id == currentUserId;
            var messageClass = isOutgoing ? "message outgoing" : "message";
            var isAdmin = message.IsAdmin;

            string content = "";
            switch (message.Type)
            {
                case MessageType.Text:
                    content = $"<div class='message-content {(isAdmin ? "admin-message" : "")}'>{message.Content}</div>";
                    break;
                case MessageType.Image:
                    content = $@"<div class='message-content {(isAdmin ? "admin-message" : "")}'>
                                  <img src='{message.ImagePath}' class='message-image' onclick='showImage(""{message.ImagePath}"")'>
                               </div>";
                    break;
                case MessageType.Emoji:
                    content = $"<div class='message-content message-emoji {(isAdmin ? "admin-message" : "")}'>{message.Content}</div>";
                    break;
                case MessageType.System:
                    return $"<div class='system-message'>{message.Content}</div>";
            }

            return $@"<div class='{messageClass}'>
                        <img src='{(message.Sender.ProfileImage ?? "/images/default-avatar.png")}' 
                             class='sender-avatar' alt='{message.Sender.UserName}'>
                        {content}
                        <div class='message-info'>
                            {message.Sender.Name} {(isAdmin ? "👑" : "")} · {message.SentAt:HH:mm}
                        </div>
                    </div>";
        }
    }
} 