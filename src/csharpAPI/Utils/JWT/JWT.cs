using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace csharpAPI.Utils.JWT;
public class JWT
{
    private readonly IConfiguration _conf;

    public JWT(IConfiguration configuration)
    {
        _conf = configuration;
    }

    public string GenerateToken(string username)
    {
        var jwtSecret = _conf["JWT-Settings:key"];
        var keyBytes = Encoding.ASCII.GetBytes(jwtSecret);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}