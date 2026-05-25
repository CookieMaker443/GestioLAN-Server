using System.Security.Claims;

namespace GestioLan.API.Utils.Helpers;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Username =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("username")  // adatta al tuo claim
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
        ?? "anonymous";

    public bool IsAdmin =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("isAdmin") == "true";
}