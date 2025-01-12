using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Web.Models;
using Microsoft.AspNetCore.Identity;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Web.Repositories;

using Web.ViewModels;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly INotyfService _notyfService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<AppUser> _userManager;
        private readonly ChatRoomRequestRepository _chatRoomRequestRepository;
        private readonly ChatRoomRepository _chatRoomRepository;
        private readonly ChatRoomUserRepository _chatRoomUserRepository;

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context,
            INotyfService notyfService,
            IWebHostEnvironment webHostEnvironment,
            UserManager<AppUser> userManager,
            ChatRoomRequestRepository chatRoomRequestRepository,
            ChatRoomRepository chatRoomRepository,
            ChatRoomUserRepository chatRoomUserRepository
            )
        {
            _logger = logger;
            _context = context;
            _notyfService = notyfService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _chatRoomRequestRepository = chatRoomRequestRepository;
            _chatRoomRepository = chatRoomRepository;
            _chatRoomUserRepository = chatRoomUserRepository;
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await _chatRoomRepository.GetAllWithDetailsAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var roomStatuses = new Dictionary<int, (bool isApproved, bool hasPending)>();

            foreach (var room in rooms)
            {
                if (User.Identity.IsAuthenticated)
                {
                    var isAdmin = await _chatRoomUserRepository.IsRoomAdmin(userId, room.Id);
                    var isApproved = await _chatRoomRequestRepository.IsUserApproved(userId, room.Id);
                    var hasPending = await _chatRoomRequestRepository.HasPendingRequest(userId, room.Id);
                    
                    // Eğer admin ise veya onaylanmışsa true döndür
                    roomStatuses[room.Id] = (isApproved || isAdmin, hasPending);
                }
                else
                {
                    roomStatuses[room.Id] = (false, false);
                }
            }

            ViewBag.RoomStatuses = roomStatuses;
            return View(rooms);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> JoinRequest(int roomId, string message)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Lütfen önce giriş yapın!" });
            }
           
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Daha önce istek gönderilmiş mi kontrol et
            var hasPendingRequest = await _chatRoomRequestRepository.HasPendingRequest(userId, roomId);
            if (hasPendingRequest)
            {
                return Json(new { success = false, message = "Zaten bekleyen bir isteğiniz var!" });
            }

            // Zaten onaylanmış mı kontrol et
            var isApproved = await _chatRoomRequestRepository.IsUserApproved(userId, roomId);
            if (isApproved)
            {
                return Json(new { success = false, message = "Bu odaya zaten katıldınız!" });
            }

            var request = new ChatRoomRequest
            {
                ChatRoomId = roomId,
                UserId = userId,
                Message = message
            };

            await _chatRoomRequestRepository.AddAsync(request);
            
            return Json(new { success = true, message = "Katılım isteğiniz gönderildi. Onay bekliyor." });
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            
            if (user == null)
                return NotFound();

            var model = new ProfileViewModel
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                UserName = user.UserName,
                ProfileImage = user.ProfileImage
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateProfileImage(IFormFile image)
        {
            if (image == null)
                return Json(new { success = false, message = "Lütfen bir resim seçin!" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            
            if (user == null)
                return Json(new { success = false, message = "Kullanıcı bulunamadı!" });

            // Eski resmi sil
            if (!string.IsNullOrEmpty(user.ProfileImage))
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfileImage.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                    System.IO.File.Delete(oldImagePath);
            }

            // Yeni resmi kaydet
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "profile-images");
            
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
        
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }
            
            user.ProfileImage = "/profile-images/" + uniqueFileName;
            await _userManager.UpdateAsync(user);
            
            return Json(new { 
                success = true, 
                message = "Profil resmi güncellendi!", 
                imageUrl = user.ProfileImage 
            });
        }

        [HttpGet]
        public async Task<IActionResult> CheckRoomStatus(int roomId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { 
                    isApproved = false, 
                    hasPendingRequest = false 
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isApproved = await _chatRoomRequestRepository.IsUserApproved(userId, roomId);
            var hasPendingRequest = await _chatRoomRequestRepository.HasPendingRequest(userId, roomId);

            return Json(new { 
                isApproved = isApproved,
                hasPendingRequest = hasPendingRequest
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetChatRooms()
        {
            var rooms = await _chatRoomRepository.GetAllWithDetailsAsync();
            return Json(new { data = rooms });
        }
    }
}
