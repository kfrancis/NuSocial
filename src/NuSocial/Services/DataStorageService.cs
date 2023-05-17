using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.Services
{
    public interface IDataStorageService
    {
        void ClearSessionPersistance();

        Task<AuthenticateResult> RetrieveAuthenticateResult();

        Task<User> RetrieveLoginInfo();

        Task StoreAccessTokenAsync(string newAccessToken);

        Task StoreAuthenticateResultAsync(AuthenticateResult authenticateResultModel);

        Task StoreLoginInformationAsync(User user);
    }

    public interface IDataStorageManager
    {
        Task<bool> HasKeyAsync(string key);

        Task RemoveIfExists(string key);

        Task<T> Retrieve<T>(string key, T? defaultValue = default, bool shouldDecrypt = false);

        Task StoreAsync<T>(string key, T value, bool shouldEncrypt = false);
    }
}
