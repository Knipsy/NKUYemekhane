﻿using System.Threading.Tasks;
using API.Dtos;
using API.Errors;
using API.Extensions;
using AutoMapper;
using Core.Entities.Identity;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


namespace API.Controllers
{
 
    public class AccountController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;


        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService, IMapper mapper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var user = await _userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);


            return new UserDto
            {
                Email = user.Email,
                Token = _tokenService.CreateToken(user),
                SchoolNumber = user.SchoolNumber
            };
        }

        [HttpGet("emailexists")]
        public async Task<ActionResult<bool>> CheckEmailExistAsync([FromQuery] string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user==null)
            {
                return Unauthorized(new ApiResponse(401,"Kullanıcı Bulunamadı. Kayıt olarak giriş yapabilirsiniz."));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
            {
                return Unauthorized(new ApiResponse(401,"Bilgilerinizi kontrol ederek tekrar deneyin."));
            }
            return new UserDto
            {
                Email = user.Email,
                SchoolNumber = user.SchoolNumber,
                Token = _tokenService.CreateToken(user)
            };
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (CheckEmailExistAsync(registerDto.Email).Result.Value)
            {
                return new BadRequestObjectResult(new ApiValidationErrorResponse { Errors = new[] { "Email adresi kullanılıyor." } });
            }

            var user = new AppUser
            {
                SchoolNumber = registerDto.SchoolNumber,
                Email = registerDto.Email,
                UserName = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse(400));
            }
            return new UserDto
            {
                SchoolNumber = user.SchoolNumber,
                Token = _tokenService.CreateToken(user),
                Email = user.Email
            };
        }

        [Authorize]
        [HttpGet("school")]
        public async Task<ActionResult<SchoolDto>> GetUserSchool()
        {
            var user = await _userManager.FindByUserByClaimsPrincipleWithSchoolAsync(HttpContext.User);
            var data= _mapper.Map<School, SchoolDto>(user.School);
            var keko= new SchoolDto
            {
                SchoolNameId = user.School.SchoolNameId,
                DinnerTimeId = user.School.DinnerTimeId
            };

            return Ok(keko);

        }

        //[HttpPost("createrole")]
        //public async Task<ActionResult> CreateRole(RoleDto role)
        //{
        //    var res =await _roleManager.CreateAsync(new IdentityRole(role.Name));

        //    if (res.Succeeded)
        //    {
        //        return Ok();
        //    }
        //    else
        //    {
        //        return new BadRequestObjectResult(new ApiResponse(500,$"{res.Errors.ToString()}"));
        //    }
        //}
    }
}
