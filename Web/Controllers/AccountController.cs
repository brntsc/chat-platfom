using System.Data;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Web.ViewModels;
using System.Security.Claims;
using Web.Repositories;


namespace Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly INotyfService _notfyservice;
        private readonly IWebHostEnvironment _env;
        private readonly ChatRoomRepository _chatRoomRepository;
        private readonly ChatRoomUserRepository _chatRoomUserRepository;
        private readonly ChatRoomRequestRepository _chatRoomRequestRepository;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, INotyfService notfyservice, IWebHostEnvironment env, ChatRoomRepository chatRoomRepository, ChatRoomUserRepository chatRoomUserRepository, ChatRoomRequestRepository chatRoomRequestRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _notfyservice = notfyservice;
            _env = env;
            _chatRoomRepository = chatRoomRepository;
            _chatRoomUserRepository = chatRoomUserRepository;
            _chatRoomRequestRepository = chatRoomRequestRepository;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email veya şifre hatalı!");
                _notfyservice.Warning("Email veya şifre hatalı!");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Admin"))
                {
                    _notfyservice.Success("Giriş Başarılı");
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    _notfyservice.Success("Giriş Başarılı");
                    return RedirectToAction("Index", "Home");// düzenlenecek
                }
            
            }

            ModelState.AddModelError(string.Empty, "Email veya şifre hatalı!");
            _notfyservice.Warning("Email veya şifre hatalı!");
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            
            var usernames = model.Name.Trim() + model.Surname.Trim();
            
            var user = new AppUser
            {
                Email = model.Email,
                Surname = model.Surname,
                UserName = usernames,
                Name = model.Name,
                ProfileImage = null
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");

                // Genel Sohbet odasına otomatik ekle
                var generalRoom = await _chatRoomRepository.GetByIdAsync(1); // Genel Sohbet ID'si
                if (generalRoom != null)
                {
                    // Odaya ekle
                    var roomUser = new ChatRoomUser
                    {
                        ChatRoomId = generalRoom.Id,
                        UserId = user.Id,
                        JoinedAt = DateTime.Now
                    };
                    await _chatRoomUserRepository.AddAsync(roomUser);

                    // Otomatik onaylı istek oluştur
                    var request = new ChatRoomRequest
                    {
                        ChatRoomId = generalRoom.Id,
                        UserId = user.Id,
                        RequestDate = DateTime.Now,
                        Status = RequestStatus.Approved,
                        Message = "Otomatik katılım"
                    };
                    await _chatRoomRequestRepository.AddAsync(request);
                }

                await _signInManager.SignInAsync(user, isPersistent: true);
                _notfyservice.Success("Kayıt işlemi başarılı!");
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                _notfyservice.Warning(error.Description);
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _notfyservice.Success("Sistem Başarıyla Çıkıldı");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult GetAvatar(string name)
        {
            var avatarUrl = AvatarHelper.GetInitialsAvatar(name);
            return Content(avatarUrl, "image/svg+xml");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfileImage(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                    return Json(new { success = false, message = "Lütfen bir resim seçin!" });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);

                // Eski resmi sil
                if (!string.IsNullOrEmpty(user.ProfileImage))
                {
                    var oldImagePath = Path.Combine(_env.WebRootPath, user.ProfileImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }

                // Yeni resmi kaydet
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                var uploadsFolder = Path.Combine(_env.WebRootPath, "profile-images");
                
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                    
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                // Kullanıcı bilgisini güncelle
                user.ProfileImage = "/profile-images/" + uniqueFileName;
                await _userManager.UpdateAsync(user);

                // Claims'i güncelle
                var claims = await _userManager.GetClaimsAsync(user);
                var profileImageClaim = claims.FirstOrDefault(c => c.Type == "ProfileImage");
                
                if (profileImageClaim != null)
                {
                    await _userManager.RemoveClaimAsync(user, profileImageClaim);
                }
                
                await _userManager.AddClaimAsync(user, new Claim("ProfileImage", user.ProfileImage));

                // Oturumu yenile
                await _signInManager.RefreshSignInAsync(user);

                return Json(new { success = true, imagePath = user.ProfileImage });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
} 