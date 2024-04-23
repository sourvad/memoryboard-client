using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Memoryboard
{
    class JwtTokenHandler
    {
        public static bool IsTokenExpired(string token)
        {
            JwtSecurityTokenHandler handler = new();

            if (handler.CanReadToken(token))
            {
                JwtSecurityToken jwtToken = handler.ReadJwtToken(token);
                Claim expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");

                if (expClaim != null && long.TryParse(expClaim.Value, out long exp))
                {
                    DateTime expiryDateTime = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                    return DateTime.UtcNow < expiryDateTime;
                }
            }

            return false;
        }
    }
}
