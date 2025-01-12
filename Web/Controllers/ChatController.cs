using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Web.Models;
using Web.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using Web.Hubs;

namespace Web.Controllers
{
    [Authorize(Roles = "User,Admin")]
    public class ChatController : Controller
    {
        private readonly ChatRoomRepository _chatRoomRepository;
        private readonly ChatRoomRequestRepository _chatRoomRequestRepository;
        private readonly ChatRoomUserRepository _chatRoomUserRepository;
        private readonly ChatMessageRepository _chatMessageRepository;
        private readonly INotyfService _notyfService;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(
            ChatRoomRepository chatRoomRepository,
            ChatRoomRequestRepository chatRoomRequestRepository,
            ChatRoomUserRepository chatRoomUserRepository,
            ChatMessageRepository chatMessageRepository,
            INotyfService notyfService,
            IWebHostEnvironment env,
            IHubContext<ChatHub> hubContext)
        {
            _chatRoomRepository = chatRoomRepository;
            _chatRoomRequestRepository = chatRoomRequestRepository;
            _chatRoomUserRepository = chatRoomUserRepository;
            _chatMessageRepository = chatMessageRepository;
            _notyfService = notyfService;
            _env = env;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Room(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var room = await _chatRoomRepository.GetByIdWithDetailsAsync(id);

            if (room == null)
            {
                _notyfService.Error("Sohbet odası bulunamadı!");
                return RedirectToAction("Index", "Home");
            }

            // Kullanıcının odaya katılma izni var mı kontrol et
            var isApproved = await _chatRoomRequestRepository.IsUserApproved(userId, id);
            if (!isApproved)
            {
                _notyfService.Warning("Bu odaya katılma izniniz yok!");
                return RedirectToAction("Index", "Home");
            }

            // Kullanıcı odaya daha önce eklenmemişse ekle
            var isInRoom = await _chatRoomUserRepository.IsUserInRoom(userId, id);
            if (!isInRoom)
            {
                var roomUser = new ChatRoomUser
                {
                    ChatRoomId = id,
                    UserId = userId,
                    JoinedAt = DateTime.Now
                };
                await _chatRoomUserRepository.AddAsync(roomUser);
            }

            // Önceki mesajları getir
            ViewBag.PreviousMessages = await _chatMessageRepository.GetPreviousMessages(id);
            ViewBag.IsRoomAdmin = await _chatRoomUserRepository.IsRoomAdmin(userId, id);
            ViewBag.AdminOnlyChat = room.AdminOnlyChat;
            
            return View(room);
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile image, int roomId)
        {
            if (image == null || image.Length == 0)
                return Json(new { success = false, message = "Lütfen bir resim seçin!" });

            // Resim boyutu kontrolü (örn: 5MB)
            if (image.Length > 5 * 1024 * 1024)
                return Json(new { success = false, message = "Resim boyutu 5MB'dan küçük olmalıdır!" });

            // Sadece resim dosyaları
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(image.ContentType.ToLower()))
                return Json(new { success = false, message = "Sadece resim dosyaları yüklenebilir!" });

            try
            {
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                var uploadsFolder = Path.Combine(_env.WebRootPath, "chat-images");
                
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                    
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }
                
                return Json(new { 
                    success = true, 
                    imagePath = "/chat-images/" + uniqueFileName 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Resim yüklenirken bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleAdminOnlyChat(int roomId, bool enabled)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = await _chatRoomUserRepository.IsRoomAdmin(userId, roomId);

                if (!isAdmin)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok!" });
                }

                var room = await _chatRoomRepository.GetByIdAsync(roomId);
                room.AdminOnlyChat = enabled;
                await _chatRoomRepository.UpdateAsync(room);

                // SignalR ile tüm kullanıcılara bildir
                await _hubContext.Clients.Group($"Room_{roomId}").SendAsync("AdminOnlyChatToggled", enabled);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
} 