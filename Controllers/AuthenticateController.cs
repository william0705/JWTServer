using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JWTServer.Entities;
using JWTServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace JWTServer.Controllers
{
    [Route("auth")]
    [ApiController]
    [AllowAnonymous]
    public class AuthenticateController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public AuthenticateController(IConfiguration configuration,UserManager<User> userManager,RoleManager<Role> roleManager)
        {
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost("token",Name=nameof(GenerateToken))]
        public IActionResult GenerateToken(LoginUser loginUser)
        {
            if (!loginUser.User.Equals("william") || !loginUser.Password.Equals("123"))
            {
                return Unauthorized();
            }

            var tokenSection = _configuration.GetSection("Security:Token");
            var claims = new List<Claim>()//payload负载
            {
                new Claim(JwtRegisteredClaimNames.Iss, tokenSection["Issuer"]),
                new Claim(JwtRegisteredClaimNames.Aud,loginUser.User)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSection["Key"]));//密钥
            //对密钥使用算法加密
            var signCredential=new SigningCredentials(key,SecurityAlgorithms.HmacSha256);//签名

            var jwtToken = new JwtSecurityToken(tokenSection["Issuer"],tokenSection["Audience"],
                claims,null,DateTime.Now.AddMinutes(1), signCredential);
            return Ok(new
            {
                token=$"Bearer {new JwtSecurityTokenHandler().WriteToken(jwtToken)}",
                expriation=TimeZoneInfo.ConvertTimeFromUtc(jwtToken.ValidTo,TimeZoneInfo.Local)
            });
        }

        [HttpPost("register",Name = nameof(AddUserAsync))]
        public async Task<IActionResult> AddUserAsync(RegisterUser registerUser)
        {
            var user=new User()
            {
                UserName = registerUser.UserName,
                Email = registerUser.Email,
                BirthDate = registerUser.BirthDate
            };
            //password:Pwd123.
            //username:williamzhou
            var result = await _userManager.CreateAsync(user, registerUser.Password);
            if (result.Succeeded)
            {
                await AddUserToRoleAsync(user, "Administrator");
                return Ok();
            }

            ModelState.AddModelError("Error",result.Errors.FirstOrDefault()?.Description);
            return BadRequest(ModelState);
        }

        [HttpPost("token2",Name = nameof(GenerateTokenAsync))]
        public async Task<IActionResult> GenerateTokenAsync(LoginUser loginUser)
        {
            //验证用户信息
            var user = await _userManager.FindByNameAsync(loginUser.User);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, loginUser.Password);
            if (result != PasswordVerificationResult.Success)
            {
                return Unauthorized();
            }
            //生成token
            var userClaims =await _userManager.GetClaimsAsync(user);
            var roles =await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                userClaims.Add(new Claim(ClaimTypes.Role,role));
            }

            var claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Sub,user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email,user.Email)
            };
            claims.AddRange(userClaims);

            //从配置文件中获取
            var tokenSection = _configuration.GetSection("Security:Token");
            var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSection["Key"]));
            //对密钥使用算法加密
            var signCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwtToken = new JwtSecurityToken(tokenSection["Issuer"], tokenSection["Audience"],
                claims, null, DateTime.Now.AddMinutes(1), signCredentials);
            
            return Ok(new
            {
                token = $"Bearer {new JwtSecurityTokenHandler().WriteToken(jwtToken)}",
                expriation = TimeZoneInfo.ConvertTimeFromUtc(jwtToken.ValidTo, TimeZoneInfo.Local)
            });
        }

        private async Task AddUserToRoleAsync(User user,string roleName)
        {
            if(user==null||string.IsNullOrEmpty(roleName))
                return;
            var isRoleExist = await _roleManager.RoleExistsAsync(roleName);
            if (isRoleExist)
            {
                if (await _userManager.IsInRoleAsync(user, roleName))
                {
                    return;
                }
            }
            else
            {
                await _roleManager.CreateAsync(new Role() {Name = roleName});
            }
            await _userManager.AddToRoleAsync(user, roleName);
        }
    }
}
