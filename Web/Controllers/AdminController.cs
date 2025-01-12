using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using System.Linq.Expressions;

using Web.Models;
using Web.Repositories;
using Microsoft.AspNetCore.Identity;
using Web.ViewModels;
using Microsoft.AspNetCore.SignalR;
using Web.Hubs;


namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ChatRoomRepository _chatRoomRepository;
        private readonly ChatRoomRequestRepository _chatRoomRequestRepository;
        private readonly ChatRoomUserRepository _chatRoomUserRepository;
        private readonly ChatMessageRepository _chatMessageRepository;
        private readonly INotyfService _notyfService;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;

        public AdminController(ChatRoomRepository chatRoomRepository, 
            ChatRoomRequestRepository chatRoomRequestRepository,
            ChatRoomUserRepository chatRoomUserRepository,
            INotyfService notyfService, 
            IWebHostEnvironment env,
            UserManager<AppUser> userManager,
            IHubContext<ChatHub> hubContext,
            ChatMessageRepository chatMessageRepository
            )
        {
            _chatRoomRepository = chatRoomRepository;
            _chatRoomRequestRepository = chatRoomRequestRepository;
            _chatRoomUserRepository = chatRoomUserRepository;
            _notyfService = notyfService;
            _env = env;
            _userManager = userManager;
            _hubContext = hubContext;
            _chatMessageRepository = chatMessageRepository;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalRooms = await _chatRoomRepository.GetCount(),
                ActiveRooms = await _chatRoomRepository.GetCount(r => r.IsActive),
                PendingRequests = await _chatRoomRequestRepository.GetCount(r => r.Status == RequestStatus.Pending),
                TotalMessages = await _chatMessageRepository.GetCount(),
                TotalUsers = await _userManager.Users.CountAsync(),
                OnlineUsers = ChatHub.OnlineUsers.Count,
                RecentRooms = await _chatRoomRepository.GetRecentRooms(5),
                RecentMessages = await _chatMessageRepository.GetRecentMessages(5)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> GetRoomList()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetChatRooms()
        {
            var rooms = await _chatRoomRepository.GetAllWithDetailsAsync();
            return Json(new { data = rooms });
        }

        [HttpGet]
        public IActionResult CreateChatRoom()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateChatRoom(ChatRoom model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                _notyfService.Error("Lütfen önce giriş yapın!");
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _notyfService.Error("Kullanıcı bilgisi bulunamadı!");
                return RedirectToAction("Login", "Account");
            }

            model.CreatedById = userId;
            
            if (model.ImageFile != null)
            {
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                var uploadsFolder = Path.Combine(_env.WebRootPath, "chat-rooms");
                
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                    
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }
                
                model.ImagePath = "/chat-rooms/" + uniqueFileName;
            }

            try
            {
                await _chatRoomRepository.AddAsync(model);
                _notyfService.Success("Sohbet odası başarıyla oluşturuldu!");
                return RedirectToAction("GetRoomList");
            }
            catch (Exception ex)
            {
                _notyfService.Error("Bir hata oluştu: " + ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteChatRoom(int id)
        {
            var room = await _chatRoomRepository.GetByIdAsync(id);
            if (room == null)
                return Json(new { success = false });

            if (!string.IsNullOrEmpty(room.ImagePath))
            {
                var imagePath = Path.Combine(_env.WebRootPath, room.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }

            await _chatRoomRepository.DeleteAsync(id);
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> EditChatRoom(int id)
        {
            var room = await _chatRoomRepository.GetByIdWithDetailsAsync(id);
            if (room == null)
                return NotFound();
        
            return Json(new { success = true, data = room });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateChatRoom(int id, string name, string description, bool isActive, IFormFile? image)
        {
            var room = await _chatRoomRepository.GetByIdAsync(id);
            if (room == null)
                return Json(new { success = false, message = "Oda bulunamadı!" });

            room.Name = name;
            room.Description = description;
            room.IsActive = isActive;

            if (image != null)
            {
                // Eski resmi sil
                if (!string.IsNullOrEmpty(room.ImagePath))
                {
                    var oldImagePath = Path.Combine(_env.WebRootPath, room.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }

                // Yeni resmi kaydet
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                var uploadsFolder = Path.Combine(_env.WebRootPath, "chat-rooms");
                
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                    
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }
                
                room.ImagePath = "/chat-rooms/" + uniqueFileName;
            }

            try
            {
                await _chatRoomRepository.UpdateAsync(room);
                return Json(new { success = true, message = "Oda başarıyla güncellendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Güncelleme sırasında bir hata oluştu: " + ex.Message });
            }
        }

        public IActionResult RoomRequests()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _chatRoomRequestRepository.GetPendingRequestsAsync();
            return Json(new { data = requests });
        }

        [HttpPost]
        public async Task<IActionResult> HandleRequest(int requestId, bool isApproved)
        {
            var request = await _chatRoomRequestRepository.GetByIdAsync(requestId);
            if (request == null)
                return Json(new { success = false, message = "İstek bulunamadı!" });

            request.Status = isApproved ? RequestStatus.Approved : RequestStatus.Rejected;
            
            if (isApproved)
            {
                // Kullanıcıyı odaya ekle
                var roomUser = new ChatRoomUser
                {
                    ChatRoomId = request.ChatRoomId,
                    UserId = request.UserId,
                    JoinedAt = DateTime.Now
                };
                
                await _chatRoomUserRepository.AddAsync(roomUser);
            }

            await _chatRoomRequestRepository.UpdateAsync(request);
            
            return Json(new { 
                success = true, 
                message = isApproved ? "İstek onaylandı!" : "İstek reddedildi!" 
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom(ChatRoom model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Mevcut admin kullanıcısının ID'sini al
                    var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    
                    // Resim yükleme işlemi
                    if (model.ImageFile != null)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                        var uploadsFolder = Path.Combine(_env.WebRootPath, "chat-rooms");
                        
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);
                        
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(fileStream);
                        }
                        
                        model.ImagePath = "/chat-rooms/" + uniqueFileName;
                    }

                    // Oda bilgilerini ayarla
                    model.CreatedById = adminId; // Admin ID'sini ata
                    model.CreatedAt = DateTime.Now;
                    model.IsActive = true;

                    await _chatRoomRepository.AddAsync(model);
                    _notyfService.Success("Sohbet odası başarıyla oluşturuldu!");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _notyfService.Error("Sohbet odası oluşturulurken bir hata oluştu: " + ex.Message);
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleRoomAdmin(string userId, int roomId)
        {
            try
            {
                // Önce kullanıcının odada olup olmadığını kontrol et
                var isInRoom = await _chatRoomUserRepository.IsUserInRoom(userId, roomId);
                if (!isInRoom)
                {
                    // Kullanıcı odada değilse, önce odaya ekle
                    var roomUser = new ChatRoomUser
                    {
                        ChatRoomId = roomId,
                        UserId = userId,
                        JoinedAt = DateTime.Now
                    };
                    await _chatRoomUserRepository.AddAsync(roomUser);

                    // Varsa bekleyen isteği onayla, yoksa yeni onaylı istek oluştur
                    var pendingRequest = await _chatRoomRequestRepository.GetPendingRequest(userId, roomId);
                    if (pendingRequest != null)
                    {
                        pendingRequest.Status = RequestStatus.Approved;
                        await _chatRoomRequestRepository.UpdateAsync(pendingRequest);
                    }
                    else
                    {
                        // Hiç istek yoksa otomatik onaylı istek oluştur
                        var request = new ChatRoomRequest
                        {
                            ChatRoomId = roomId,
                            UserId = userId,
                            RequestDate = DateTime.Now,
                            Status = RequestStatus.Approved,
                            Message = "Admin tarafından otomatik onaylandı"
                        };
                        await _chatRoomRequestRepository.AddAsync(request);
                    }
                }

                var isAdmin = await _chatRoomUserRepository.IsRoomAdmin(userId, roomId);
                
                if (isAdmin)
                {
                    await _chatRoomUserRepository.RemoveRoomAdmin(userId, roomId);
                    return Json(new { success = true, message = "Yönetici yetkisi kaldırıldı." });
                }
                else
                {
                    await _chatRoomUserRepository.MakeRoomAdmin(userId, roomId);
                    return Json(new { success = true, message = "Yönetici yetkisi verildi." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomUsers(int roomId)
        {
            try 
            {
                var allUsers = await _userManager.Users.ToListAsync();
                var roomUsers = await _chatRoomUserRepository.GetRoomUsersWithDetailsAsync(roomId);

                var result = allUsers.Select(u => {
                    var roomUser = roomUsers.FirstOrDefault(ru => ru.UserId == u.Id);
                    var isAdmin = roomUser?.IsRoomAdmin ?? false;
                    
                    System.Diagnostics.Debug.WriteLine($"Processing User: {u.UserName}");
                    System.Diagnostics.Debug.WriteLine($"Found RoomUser: {roomUser != null}");
                    System.Diagnostics.Debug.WriteLine($"IsAdmin Value: {isAdmin}");
                    
                    return new {
                        id = u.Id,
                        name = u.Name,
                        surname = u.Surname,
                        userName = u.UserName,
                        isRoomAdmin = isAdmin
                    };
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetRoomUsers Error: {ex.Message}");
                return Json(new { error = true, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendAdminMessage([FromForm] AdminMessageViewModel model)
        {
            try
            {
                var rooms = model.SendToAllRooms 
                    ? await _chatRoomRepository.GetAllAsync()
                    : await _chatRoomRepository.GetRoomsByIds(model.SelectedRoomIds);

                if (!rooms.Any())
                {
                    return Json(new { success = false, message = "Seçili oda bulunamadı!" });
                }

                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var adminUser = await _userManager.FindByIdAsync(adminId);

                foreach (var room in rooms)
                {
                    var message = new ChatMessage
                    {
                        Content = $"Admin Mesajı: {model.Message}",
                        Type = MessageType.System,
                        SentAt = DateTime.Now,
                        SenderId = adminId,
                        ChatRoomId = room.Id
                    };

                    await _chatRoomRepository.AddMessageAsync(message);
                    
                    await _hubContext.Clients.Group($"Room_{room.Id}")
                        .SendAsync("ReceiveSystemMessage", $"Admin Mesajı: {model.Message}");
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
