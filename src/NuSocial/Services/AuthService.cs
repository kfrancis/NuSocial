using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.Services
{
    public interface IAuthService
    {
        string AccessToken { get; }
        public bool IsAuthenticated { get; set; }
        bool IsRefreshTokenExpired { get; }
        string Username { get; }

        Task LoginUserAsync(string username, string? password);

        Task LogoutAsync();

        Task<string> RefreshTokenAsync();
    }
}
