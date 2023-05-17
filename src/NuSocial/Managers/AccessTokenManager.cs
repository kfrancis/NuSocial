using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NuSocial.Managers
{
    public interface IAccessTokenManager
    {
        string GetAccessToken();

        Task<AuthenticateResult> LoginAsync(string username, string? password);

        Task<string> RefreshTokenAsync();

        void Logout();
        void SetApiClient(INostrService apiClient);

        bool IsUserLoggedIn { get; }

        bool IsRefreshTokenExpired { get; }

        AuthenticateResult? AuthenticateResult { get; set; }
        AuthenticateRequest AuthenticateModel { get; set; }

        DateTime AccessTokenRetrieveTime { get; set; }
    }

    public class AccessTokenManager : IAccessTokenManager, ISingletonDependency
    {
        public bool IsUserLoggedIn => false;

        public bool IsRefreshTokenExpired => throw new NotImplementedException();

        public AuthenticateResult? AuthenticateResult { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public AuthenticateRequest AuthenticateModel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime AccessTokenRetrieveTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string GetAccessToken()
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticateResult> LoginAsync(string username, string? password)
        {
            throw new NotImplementedException();
        }

        public void Logout()
        {
            throw new NotImplementedException();
        }

        public Task<string> RefreshTokenAsync()
        {
            throw new NotImplementedException();
        }

        public void SetApiClient(INostrService apiClient)
        {
            throw new NotImplementedException();
        }
    }
}
