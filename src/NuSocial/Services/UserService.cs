using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.Services
{
    public class UserService
    {
        private const string USERS_KEY = nameof(UserService) + "_Users";
        private string USER_KEY(string user) => $"{nameof(UserService)}_User_{user}";

        public UserService()
        {
        }

        public Dictionary<string, string>? GetAvailableUsers()
        {
            return Preferences.Default.Get(USERS_KEY, new Dictionary<string, string>());
        }

        public async Task<User?> GetUser(string userId, string? passphrase)
        {
            var userString = Preferences.Default.Get(USER_KEY(userId), string.Empty);
            if (!string.IsNullOrWhiteSpace(passphrase))
            {
                userString = DataEncryptor.Decrypt(userString, passphrase);
            }
            var byteArray = Encoding.UTF8.GetBytes(userString);
            using var stream = new MemoryStream(byteArray);
            return await JsonSerializer.DeserializeAsync<User>(stream);
        }

        public Task SetUser(User user, string? passphrase = null)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var updated = false;
            var users = GetAvailableUsers() ?? new Dictionary<string, string>();

            if (users.TryGetValue(user.Key, out var username) && username != user.Username)
            {
                users[user.Key] = user.Username;
                updated = true;
            }
            else if (username is null)
            {
                users.TryAdd(user.Key, user.Username);
                updated = true;
            }

            if (updated)
            {
                var usersString = JsonSerializer.Serialize(users);
                if (!string.IsNullOrEmpty(passphrase))
                {
                    usersString = DataEncryptor.Encrypt(usersString, passphrase);
                }

                Preferences.Default.Set(USERS_KEY, usersString);
            }

            var key = USER_KEY(user.Key);

            var userString = JsonSerializer.Serialize(user);
            if (!string.IsNullOrEmpty(passphrase))
            {
                userString = DataEncryptor.Encrypt(userString, passphrase);
            }
            Preferences.Default.Set(key, userString);
            return Task.CompletedTask;
        }
    }
}
