using Newtonsoft.Json;
using System.Reflection;

namespace NostrLib.Tests
{
    public class ClientBaseFixture
    {
        protected static readonly string BaseUrl = ConfigVariable("BASE_URL");
        protected Client? Client;
        private static readonly string _assemblyLocation = typeof(ClientBaseFixture).GetTypeInfo().Assembly.Location;

        protected ClientBaseFixture()
        {
            AssertSettingsAvailable();

            Client = new Client(new Uri("wss://relay.damus.io"));
        }

        private static void AssertSettingsAvailable()
        {
            Assert.NotNull(BaseUrl);
        }

        /// <summary>
        /// Retrieve the config variable from the environment,
        /// or app.config if the environment doesn't specify it.
        /// </summary>
        /// <param name="variableName">The name of the environment variable to get.</param>
        /// <returns></returns>
        private static string ConfigVariable(string variableName)
        {
            string? retval = null;
            //this is here to allow us to have a config that isn't committed to source control, but still allows the project to build
            var configFile = Environment.GetEnvironmentVariable("NOSTRLIB_CONFIG_FILE_NAME") ?? "testing_keys.json";
            try
            {
                var location = Path.GetFullPath(_assemblyLocation);
                var pathComponents = location.Split(Path.DirectorySeparatorChar).ToList();
                var componentsCount = pathComponents.Count;
                var keyPath = "";
                while (componentsCount > 0)
                {
                    keyPath = Path.Combine(new[] { Path.DirectorySeparatorChar.ToString() }
                        .Concat(pathComponents.Take(componentsCount)
                            .Concat(new[] { configFile }))
                        .ToArray());
                    if (File.Exists(keyPath))
                    {
                        break;
                    }

                    componentsCount--;
                }

                var values = JsonConvert.DeserializeObject<Dictionary<String, String>>(File.ReadAllText(keyPath)) ?? new();
                retval = values[variableName];
            }
            catch
            {
                //This is OK, it just doesn't exist.. no big deal.
            }

            return string.IsNullOrWhiteSpace(retval) ? Environment.GetEnvironmentVariable(variableName) ?? string.Empty : retval;
        }
    }
}