using ahis.template.application.Shared.Mediator;
using ahis.template.identity.Models.Entities;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Features.AuthenticationFeatures.Queries
{
    public class EncodeTokenQuery : IRequest<Result<string>>
    {
        public string UserId { get; set; }
        public string MadeUpUserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class EncodeTokenQueryHandler : IRequestHandler<EncodeTokenQuery, Result<string>>
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public EncodeTokenQueryHandler(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }
        public async Task<Result<string>> Handle(EncodeTokenQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return Result.Fail("User not found");
            }
                
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, request.MadeUpUserId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, request.Username ?? ""),
                new Claim(JwtRegisteredClaimNames.Email, request.Email ?? "")
            };

            var userClaims = await _userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey not configured")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddSeconds(int.Parse(_configuration["Jwt:AccessTokenExpirySeconds"] ?? "3600"));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "",
                audience: _configuration["Jwt:Audience"] ?? "",
                claims: claims,
                expires: expiry,
                signingCredentials: creds);

            return Result.Ok<string>(new JwtSecurityTokenHandler().WriteToken(token));
        }
    }
}
