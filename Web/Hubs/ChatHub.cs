using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Web.Models;
using Web.Repositories;
using Web.ViewModels;

namespace Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly INotyfService _notyfService;
        private readonly ChatRoomRepository _chatRoomRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly ChatRoomUserRepository _chatRoomUserRepository;

        public static HashSet<string> OnlineUsers = new HashSet<string>();

        public ChatHub(
            INotyfService notyfService,
            ChatRoomRepository chatRoomRepository,
            UserManager<AppUser> userManager,
            ChatRoomUserRepository chatRoomUserRepository)
        {
            _notyfService = notyfService;
            _chatRoomRepository = chatRoomRepository;
            _userManager = userManager;
            _chatRoomUserRepository = chatRoomUserRepository;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                OnlineUsers.Add(userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                OnlineUsers.Remove(userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinRoom(int roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{roomId}");
            
            var user = await _userManager.FindByIdAsync(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
            await Clients.Group($"Room_{roomId}").SendAsync("ReceiveSystemMessage", 
                $"{user.UserName} sohbete katıldı.");
        }

        public async Task LeaveRoom(int roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Room_{roomId}");
            
            var user = await _userManager.FindByIdAsync(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
            await Clients.Group($"Room_{roomId}").SendAsync("ReceiveSystemMessage", 
                $"{user.UserName} sohbetten ayrıldı.");
        }

        public async Task SendMessage(int roomId, string message)
        {
            try
            {
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);
                
                // Odanın admin-only durumunu kontrol et
                var room = await _chatRoomRepository.GetByIdAsync(roomId);
                var isRoomAdmin = await _chatRoomUserRepository.IsRoomAdmin(userId, roomId);
                
                if (room.AdminOnlyChat && !isRoomAdmin)
                {
                    throw new HubException("Bu kanalda sadece yetkililer mesaj gönderebilir!");
                }

                // Kullanıcının oda yöneticisi olup olmadığını kontrol et
                var isSystemAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                var chatMessage = new ChatMessage
                {
                    Content = message,
                    Type = MessageType.Text,
                    SentAt = DateTime.Now,
                    SenderId = userId,
                    ChatRoomId = roomId
                };
                await _chatRoomRepository.AddMessageAsync(chatMessage);

                var messageDto = new ChatMessageViewModel
                {
                    Content = message,
                    Type = MessageType.Text,
                    SentAt = DateTime.Now,
                    Sender = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Name = user.Name,
                        Surname = user.Surname,
                        ProfileImage = user.ProfileImage
                    },
                    IsAdmin = isSystemAdmin || isRoomAdmin // Sistem admin veya oda admini
                };

                await Clients.Group($"Room_{roomId}").SendAsync("ReceiveMessage", messageDto);
            }
            catch (Exception ex)
            {
                throw new HubException($"Mesaj gönderilirken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task SendImage(int roomId, string imagePath)
        {
            try 
            {
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception("Kullanıcı kimliği bulunamadı");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    throw new Exception("Kullanıcı bulunamadı");
                }

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                // Önce mesajı veritabanına kaydet
                var chatMessage = new ChatMessage
                {
                    ImagePath = imagePath,
                    Type = MessageType.Image,
                    SentAt = DateTime.Now,
                    SenderId = userId,
                    ChatRoomId = roomId,
                    Content = null
                };

                await _chatRoomRepository.AddMessageAsync(chatMessage);

                // Sonra DTO oluştur ve gönder
                var messageDto = new ChatMessageViewModel
                {
                    Id = chatMessage.Id,
                    ImagePath = imagePath,
                    Type = MessageType.Image,
                    SentAt = DateTime.Now,
                    IsAdmin = isAdmin,
                    Sender = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Name = user.Name,
                        Surname = user.Surname,
                        ProfileImage = user.ProfileImage
                    }
                };

                await Clients.Group($"Room_{roomId}").SendAsync("ReceiveMessage", messageDto);
            }
            catch (Exception ex)
            {
                // Hatayı logla
                System.Diagnostics.Debug.WriteLine($"SendImage Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Hatayı istemciye gönder
                throw new HubException($"Resim gönderilirken bir hata oluştu: {ex.Message}");
            }
        }
    }
} 