using CollegeWebApp.BLL;
using CollegeWebApp.Models;
using CollegeWebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace CollegeWebApp.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly Repository _repository;

        private bool GetClaim()
        {
            var claim = User.FindFirst(ClaimsIdentity.DefaultNameClaimType);
            bool isPermit = bool.Parse(claim.Value.Split(';')[4]);
            return isPermit;
        }

        public ProfileController(Repository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            var profileInfo = HttpContext.Session.GetString("profileInfo")?.Split(';');
            string? pfp = HttpContext.Session.GetString("pfp");

            if (profileInfo == null)
            {
                return RedirectToAction("Login","Auth");
            }

            string? groupCode = null;

            if (!String.IsNullOrEmpty(profileInfo[3]))
            {
                groupCode = await _repository.GetGroupCodeByIdAsync(int.Parse(profileInfo[3]));
            }

            var profile = new
            {
                ProfilePic = pfp,
                FirstName = profileInfo[0],
                Surname = profileInfo[1],
                Patroymic = profileInfo[2],
                GroupCode = groupCode
            };

            ViewBag.IsPermit = GetClaim();

            return View(profile);
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Settings(IFormFile pic)
        {
            string base64Pic = Helpers.ConvertToBase64String(pic);

            var claim = User.FindFirst(ClaimsIdentity.DefaultNameClaimType);
            int userId = int.Parse(claim.Value.Split(';')[0]);

            _repository.UpdateProfilePicById(userId, base64Pic);
            HttpContext.Session.SetString("pfp", base64Pic);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var claim = User.FindFirst(ClaimsIdentity.DefaultNameClaimType);
                int userId = int.Parse(claim.Value.Split(';')[0]);
                _repository.ChangePasswordById(userId, model.NewPassword);
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> UsersList()
        {
            var list = await _repository.GetUsersBasicInfoAsync();
            ViewBag.Repos = _repository;
            if (GetClaim())
                return View(list);
            else
                return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (GetClaim())
            {
                _repository.DeleteUserById(id);
            }

            return RedirectToAction("UsersList");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _repository.GetUserByIdAsync(id);
            
            if (user.GroupId != null)
            {
                ViewBag.GroupCode = await _repository.GetGroupCodeByIdAsync(user.GroupId);
            }

            ViewBag.RoleName = await _repository.GetRoleByUserIdAsync(id);
            ViewBag.Roles = await _repository.GetRolesListAsync();
            ViewBag.Groups = await _repository.GetGroupsListAsync();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UserDTO user, string password, IFormFile pfp)
        {
            if (GetClaim())
            {
                if (!string.IsNullOrEmpty(password))
                {
                    user.PasswordHash = Helpers.GeneratePasswordHash(password);
                }
                if (pfp != null)
                {
                    user.ProfilePic = Helpers.ConvertToBase64String(pfp);
                }
                _repository.UpdateUser(user);
            }


            return RedirectToAction("UsersList");
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.GroupsList = await _repository.GetGroupsListAsync();
            ViewBag.RolesList = await _repository.GetRolesListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            UserDTO newUser = new UserDTO
            {
                Email = model.Email,
                PasswordHash = Helpers.GeneratePasswordHash(model.Password),
                FirstName = model.FirstName,
                Surname = model.Surname,
                Patronymic = model.Patronymic,
                ProfilePic = Helpers.ConvertToBase64String(model.ProfilePic),
                GroupId = model.SelectedGroupId,
                RoleId = model.SelectedRoleId
            };

            _repository.AddUser(newUser);

            return RedirectToAction("Index");
        }

    }
}
