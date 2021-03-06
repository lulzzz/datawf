﻿using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
    [ResponseCache(CacheProfileName = "Never")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    [Auth]
    public class AuthorizeController : ControllerBase
    {
        private readonly IOptions<JwtAuth> jwtAuth;
        private readonly DBTable<User> users;

        public AuthorizeController(IOptions<JwtAuth> jwtAuth)
        {
            this.jwtAuth = jwtAuth ?? throw new ArgumentNullException(nameof(jwtAuth));
            users = DBTable.GetTable<User>();
        }

        public User CurrentUser => User.GetCommonUser();

        [AllowAnonymous]
        [HttpPost("LoginIn/")]
        public async Task<ActionResult<TokenModel>> LoginIn([FromBody]LoginModel login)
        {
            var user = (User)null;
            try
            {
                user = await GetUser(login);
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
                return BadRequest("Invalid email or password.");
            }
            try
            {
                user.AccessToken = CreateAccessToken(user);
                if (login.Online)
                {
                    user.RefreshToken = CreateRefreshToken(user);
                }
                else
                {
                    user.RefreshToken = null;
                }
                await user.Save(user);
                return new TokenModel
                {
                    Email = user.EMail,
                    AccessToken = user.AccessToken,
                    RefreshToken = user.RefreshToken
                };
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [AllowAnonymous]
        [HttpPost("ReLogin/")]
        public async Task<ActionResult<TokenModel>> ReLogin([FromBody]TokenModel token)
        {
            var user = await GetUser(token);
            if (user.RefreshToken == null || token.RefreshToken == null)
            {
                return BadRequest("Refresh token was not found.");
            }
            else if (user.RefreshToken != token.RefreshToken)
            {
                return BadRequest("Refresh token is invalid.");
            }
            try
            {
                token.AccessToken =
                    user.AccessToken = CreateAccessToken(user);
                await user.Save(user);
                return token;
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost("LoginOut/")]
        public async Task<ActionResult<TokenModel>> LoginOut([FromBody]TokenModel token)
        {
            var user = await GetUser(token);
            if (user != CurrentUser)
            {
                return BadRequest("Invalid Arguments!");
            }
            try
            {
                token.AccessToken =
                    token.RefreshToken =
                user.AccessToken =
                    user.RefreshToken = null;
                await user.Save(CurrentUser);
                return token;
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost("ResetPassword/")]
        public async Task<ActionResult<bool>> ResetPassword([FromBody]LoginModel login)
        {
            try
            {
                var user = DataWF.Module.Common.User.GetByEmail(login.Email) ?? DataWF.Module.Common.User.GetByLogin(login.Email);
                if (user == null)
                {
                    return NotFound();
                }

                if (user != CurrentUser)
                {
                    if (!user.Access.GetFlag(AccessType.Admin, CurrentUser)
                        && !user.Table.Access.GetFlag(AccessType.Admin, CurrentUser))
                    {
                        return Forbid();
                    }
                }

                DataWF.Module.Common.User.ChangePassword(user, login.Password);
                await user.Save(CurrentUser);
                return true;
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        private BadRequestObjectResult BadRequest(Exception ex)
        {
            Helper.OnException(ex);
            return BadRequest(ex.Message);
        }

        [HttpGet()]
        public ActionResult<User> Get()
        {
            return CurrentUser;
        }

        [HttpGet("Logs/")]
        public ActionResult<Stream> GetLogs()
        {
            var stream = new MemoryStream();
            Helper.Logs.Save(stream);
            stream.Position = 0;
            return File(stream, System.Net.Mime.MediaTypeNames.Application.Octet, $"ServerLogs{ DateTime.Now.ToString("yyMMddHHmmss")}.xml");
        }

        private string CreateRefreshToken(User user)
        {
            return Helper.GetSha256(user.EMail + Guid.NewGuid().ToString());
        }

        private string CreateAccessToken(User user)
        {
            var identity = GetIdentity(user);
            var now = DateTime.UtcNow;

            var jwt = new JwtSecurityToken(
                    issuer: jwtAuth.Value.ValidIssuer,
                    audience: jwtAuth.Value.ValidAudience,
                    notBefore: now,
                    expires: now.AddMinutes(jwtAuth.Value.LifeTime),
                    claims: identity.Claims,
                    signingCredentials: jwtAuth.Value.SigningCredentials);
            var jwthandler = new JwtSecurityTokenHandler();
            return jwthandler.WriteToken(jwt);
        }

        private Task<User> GetUser(LoginModel login)
        {
            return DataWF.Module.Common.User.StartSession(new NetworkCredential(login.Email, login.Password));
        }

        private Task<User> GetUser(TokenModel token)
        {
            return DataWF.Module.Common.User.StartSession(token.Email);
        }

        private ClaimsIdentity GetIdentity(User user)
        {
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(
                identity: user,
                claims: GetClaims(user),
                authenticationType: JwtBearerDefaults.AuthenticationScheme,
                nameType: JwtRegisteredClaimNames.NameId,
                roleType: "");
            return claimsIdentity;
        }

        private IEnumerable<Claim> GetClaims(User person)
        {
            yield return new Claim(ClaimTypes.Name, person.EMail);
            yield return new Claim(ClaimTypes.Email, person.EMail);
        }
    }
}
