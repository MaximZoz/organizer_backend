using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Claim.Data;
using Claim.Data.Entities;
using Enum;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Model;
using Model.BindingModel;
using Models;
using Models.BindingModel;
using Models.DTO;
using Newtonsoft.Json;
using Stripe;

namespace dotnetClaimAuthorization.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private RoleManager<IdentityRole> _roleManager;
        private readonly JWTConfig _jWTConfig;
        private readonly AppDBContext _context;

        public UserController(ILogger<UserController> logger, UserManager<AppUser> userManager,
            SignInManager<AppUser> signManager, IOptions<JWTConfig> jwtConfig, RoleManager<IdentityRole> roleManager,
            AppDBContext context)
        {
            _userManager = userManager;
            _signInManager = signManager;
            _roleManager = roleManager;
            _logger = logger;
            _jWTConfig = jwtConfig.Value;
            _context = context;
        }

        [HttpPost("RegisterUser")]
        public async Task<object> RegisterUser([FromBody] AddUpdateRegisterUserBindingModel model)
        {
            try
            {
                if (model.Roles == null)
                {
                    return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error,
                        "Roles are missing", null));
                }

                foreach (var role in model.Roles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error,
                            "Role does not exist",
                            null));
                    }
                }


                var user = new AppUser()
                {
                    FullName = model.FullName, Email = model.Email, UserName = model.Email,
                    DateCreated = DateTime.UtcNow, DateModified = DateTime.UtcNow
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var tempUser = await _userManager.FindByEmailAsync(model.Email);
                    foreach (var role in model.Roles)
                    {
                        await _userManager.AddToRoleAsync(tempUser, role);
                    }

                    return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.OK,
                        "Вы зарегистрированы", null));
                }

                return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error, "",
                    result.Errors.Select(x => x.Description).ToArray()));
            }
            catch (Exception ex)
            {
                return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error, ex.Message,
                    null));
            }
        }

        ///<summary>
        ///Get All User from database   
        ///</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllUser")]
        public async Task<object> GetAllUser()
        {
            try
            {
                List<UserDTO> allUserDTO = new List<UserDTO>();
                var users = _userManager.Users.ToList();
                foreach (var user in users)
                {
                    var roles = (await _userManager.GetRolesAsync(user)).ToList();

                    allUserDTO.Add(new UserDTO(user.FullName, user.Id, user.Email, user.UserName, user.DateCreated,
                        roles, ""));
                }

                return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.OK, "", allUserDTO));
            }
            catch (Exception ex)
            {
                return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error, ex.Message,
                    null));
            }
        }

        [Authorize(Roles = "User,Admin")]
        [HttpGet("GetUserList")]
        public async Task<object> GetUserList()
        {
            try
            {
                List<UserDTO> allUserDTO = new List<UserDTO>();
                var users = _userManager.Users.ToList();
                foreach (var user in users)
                {
                    var role = (await _userManager.GetRolesAsync(user)).ToList();
                    if (role.Any(x => x == "User"))
                    {
                        var quantityNotes = _context.Tasks.Where(t => t.UserId.ToString() == user.Id).ToList().Count
                            .ToString();
                        allUserDTO.Add(new UserDTO(user.FullName, user.Id, user.Email, user.UserName, user.DateCreated,
                            role, quantityNotes));
                    }
                }

                return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.OK, "", allUserDTO));
            }
            catch (Exception ex)
            {
                return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error, ex.Message,
                    null));
            }
        }


        ///<summary>
        ///To login into App  
        ///</summary>
        ///<param name="model"></param>
        //
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<object> Login([FromBody] LoginBindingModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);
                    if (result.Succeeded)
                    {
                        var appUser = await _userManager.FindByEmailAsync(model.Email);
                        var roles = (await _userManager.GetRolesAsync(appUser)).ToList();

                        var user = new UserDTO(appUser.FullName, appUser.Id, appUser.Email, appUser.UserName,
                            appUser.DateCreated,
                            roles, "");
                        user.Token = GenerateToken(appUser, roles);

                        return await System.Threading.Tasks.Task.FromResult(
                            new ResponseModel(ResponseCode.OK, $"Добро пожаловать, {appUser.FullName}!", user));
                    }

                    if (result.IsLockedOut)
                    {
                        return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error,
                            "Ваша учетная запись заблокирована администратором", null));
                    }
                }

                return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error,
                    "Неверная почта или пароль", null));
            }
            catch (Exception ex)
            {
                return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error, ex.Message,
                    null));
            }
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("AddRole")]
        public async Task<object> AddRole([FromBody] AddRoleBindingModel model)
        {
            try
            {
                if (model == null || model.Role == "")
                {
                    return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error,
                        "parameters are missing", null));
                }

                if (await _roleManager.RoleExistsAsync(model.Role))
                {
                    return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.OK,
                        "Role already exist", null));
                }

                var role = new IdentityRole();
                role.Name = model.Role;
                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.OK,
                        "Role added successfully", null));
                }

                return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error,
                    "Что-то пошло не так. Пожалуйста, повторите попытку позже", null));
            }
            catch (Exception ex)
            {
                return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error, ex.Message,
                    null));
            }
        }

        [HttpGet("GetRoles")]
        public async Task<object> GetRoles()
        {
            try
            {
                var roles = _roleManager.Roles.Select(x => x.Name).ToList();
                var user = new IdentityRole {Name = "Admin"};
                var admin = new IdentityRole {Name = "User"};
                if (string.IsNullOrEmpty(roles.Find(el => el == "User")))
                {
                    await _roleManager.CreateAsync(user);
                }

                if (string.IsNullOrEmpty(roles.Find(el => el == "Admin")))
                {
                    await _roleManager.CreateAsync(admin);
                }

                return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.OK, "", roles));
            }
            catch (Exception ex)
            {
                return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error, ex.Message,
                    null));
            }
        }

        private string GenerateToken(AppUser user, List<string> roles)
        {
            var claims = new List<System.Security.Claims.Claim>()
            {
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.NameId, user.Id),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Email, user.Email),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            foreach (var role in roles)
            {
                claims.Add(new System.Security.Claims.Claim(ClaimTypes.Role, role));
            }

            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jWTConfig.Key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(12),
                SigningCredentials =
                    new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Audience = _jWTConfig.Audience,
                Issuer = _jWTConfig.Issuer
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }

        ///<summary>
        ///To login into App  
        ///</summary>
        ///<param name="model"></param>
        //
        [AllowAnonymous]
        [HttpPost("Tasks")]
        public async Task<object> Task([FromBody] TodoItemTask model)

        {
            var strDate = model.Date.ToShortDateString();
            model.Date = DateTime.Parse(strDate);
            _context.Tasks.Add(model);
            await _context.SaveChangesAsync();
            return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.Error, "good",
                model));
        }

        [AllowAnonymous]
        [HttpGet("Tasks/{userId}/{date}")]
        public async Task<object> GetTask(Guid userId, string date)

        {
            var model = _context.Tasks.Where(t => t.UserId == userId).Where(t => t.Date.Equals(DateTime.Parse(date)));
            return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.OK, "good",
                model));
        }

        [AllowAnonymous]
        [HttpDelete("Tasks/{id}")]
        public async Task<object> DeleteTask(Guid id)

        {
            var task = new TodoItemTask() {Id = id};
            _context.Tasks.Attach(task);
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.OK, "good", null
            ));
        }

        [AllowAnonymous]
        [HttpGet("GetTaskMonth/{userId}/{date}")]
        public async Task<object> GetTaskMonth(Guid userId, string date)

        {
            var startDate = new DateTime(DateTime.Parse(date).Year, DateTime.Parse(date).Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var model = _context.Tasks.Where(t => t.UserId == userId)
                .Where(t => t.Date >= startDate)
                .Where(t => t.Date <= endDate);

            return await System.Threading.Tasks.Task.FromResult(new ResponseModel(ResponseCode.OK, "good",
                model));
        }
    }
}