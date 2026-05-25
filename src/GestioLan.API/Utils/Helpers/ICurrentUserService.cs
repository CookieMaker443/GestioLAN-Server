using System.Security.Claims;

namespace GestioLan.API.Utils.Helpers;

public interface ICurrentUserService
{
    string Username { get; }
    bool IsAdmin { get; }
}