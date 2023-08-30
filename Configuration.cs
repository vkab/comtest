using System.Reflection;

namespace TestCom
{
    internal class Configuration
    {
        private static readonly string _configName = "appsettings.json";
        private static readonly object _syncRoot = new();
        private static Configuration? _instance;

        public static Configuration GetInstance()
        {
            lock (_syncRoot)
            {
                if (_instance == null)
                    _instance = CreateInstance();
                return _instance;
            }
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Configuration() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        private static Configuration CreateInstance()
        {
            IConfigurationRoot? configRoot = null;

            try
            {
                configRoot = new ConfigurationBuilder()
                 .AddJsonFile(_configName, optional: false)
                 .Build();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowEx(Strings.ConfigReadError01, _configName, ex.Message);
            }

            Configuration? result = null;

            try
            {
                result = configRoot.Get<Configuration>(opt => opt.BindNonPublicProperties = false);
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowEx(Strings.ConfigBuildError0, ex.Message);
            }

            foreach (var prop in typeof(Configuration).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                switch (prop.GetValue(result!))
                {
                    case string s when s.Length == 0:
                    case int i when i == default:
                    case null:
                        ExceptionHelper.ThrowEx(Strings.ConfigBuildError0, String.Format(Strings.ConfigMissingKey0, prop.Name));
                        break;
                };
            }

            return result!;
        }

        public int TimeoutMs { get; init; }
        public string PortName { get; init; }
        public int PortBaudRate { get; init; }
        public int PortFrameSize { get; init; }
        public string NatsUrl { get; init; }
        public bool NatsSecured { get; init; }
        public string NatsSubject { get; init; }
    }
}
