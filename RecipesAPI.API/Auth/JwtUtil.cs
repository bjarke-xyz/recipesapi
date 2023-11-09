using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Auth;

public class JwtUtil
{
    private readonly string firebaseAppId;
    private readonly ICacheProvider? cache;
    private readonly HttpClient httpClient = new HttpClient();

    public JwtUtil(string firebaseAppId, ICacheProvider? cache)
    {
        this.firebaseAppId = firebaseAppId;
        this.cache = cache;
    }

    public string GetAuthority() => $"https://securetoken.google.com/{firebaseAppId}";
    public TokenValidationParameters GetTokenValidationParameters()
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{firebaseAppId}",
            ValidateAudience = true,
            ValidAudience = firebaseAppId,
            ValidateLifetime = true,
        };
        return tokenValidationParameters;
    }

    public async Task<(bool isValid, SecurityToken token, ClaimsPrincipal claims)> ValidateToken(string authToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = GetTokenValidationParameters();
        var signingKeys = await GetGoogleSigningKeys();
        if (signingKeys == null)
        {
            throw new Exception("Failed to get google signing keys");
        }
        validationParameters.IssuerSigningKeys = signingKeys;
        var claims = tokenHandler.ValidateToken(authToken, validationParameters, out var validatedToken);
        return (true, validatedToken, claims);
    }

    private async Task<SecurityKey[]?> GetGoogleSigningKeys()
    {
        ArgumentNullException.ThrowIfNull(cache);
        var cacheKey = "GOOGLESIGNINGKEYS";
        var x509Data = await cache.Get<GoogleSigningKeys>(cacheKey);
        if (x509Data == null)
        {
            var response = await httpClient.GetAsync("https://www.googleapis.com/robot/v1/metadata/x509/securetoken@system.gserviceaccount.com");
            if (!response.IsSuccessStatusCode) return null;
            var _x509Data = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            if (_x509Data == null) return null;
            x509Data = new GoogleSigningKeys
            {
                SigningKeys = _x509Data,
            };
            await cache.Put(cacheKey, x509Data, TimeSpan.FromDays(7));
        }
        var keys = x509Data.SigningKeys.Values.Select(CreateSecurityKeyFromPublicKey).ToArray();
        return keys;
    }

    private static SecurityKey CreateSecurityKeyFromPublicKey(string data)
    {
        return new X509SecurityKey(new X509Certificate2(Encoding.UTF8.GetBytes(data)));
    }

    public static string? GetUserId(ClaimsPrincipal claims)
    {
        return claims.FindFirstValue(ClaimTypes.NameIdentifier);
    }

}

public class GoogleSigningKeys
{
    public Dictionary<string, string> SigningKeys { get; set; } = default!;
}