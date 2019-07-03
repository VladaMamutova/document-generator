using System;
using System.IO;
using System.Management;
using System.Text;

namespace DocumentGenerator
{
    public class Settings
    {
        private const string FILENAME = "ini";
        private const int LAUNCH_LIMIT = 30;
        public const string KEY_CRYPT = "oNm5kGTf67";
        public const string ACTIVATION_CODE = "xGGzc3vnSZ";

        private static Settings _settings;
        private static readonly object SyncRoot = new object();

        public int LaunchCount { get; private set; }
        public bool IsActivated { get; private set; }
        public string InitialProccessorId { get; private set; }
        public string CurrentProccessorId { get; }

        private Settings()
        {
            LaunchCount = 0;
            IsActivated = false;
            
            InitialProccessorId = "";
            CurrentProccessorId = GetProccessorId();
        }

        public static Settings GetSettings()
        {
            if (_settings == null)
            {
                lock (SyncRoot)
                {
                    _settings = new Settings();
                }
            }

            return _settings;
        }

        public bool CanRunProgram => (IsActivated || LaunchCount < LAUNCH_LIMIT) &&
                                     CanRunOnTheCurrentProccessor();

        public bool CanRunProgramWithoutActivation =>
            !IsActivated && LaunchCount < LAUNCH_LIMIT;

        public bool CanRunOnTheCurrentProccessor()
        {
            return !string.IsNullOrEmpty(InitialProccessorId) &&
                   !string.IsNullOrEmpty(CurrentProccessorId) && string.Compare(
                       InitialProccessorId, GetProccessorId(),
                       StringComparison.InvariantCulture) == 0;
        }

        public bool TryActivate(string code)
        {
            bool activated = string.Compare(code, ACTIVATION_CODE,
                       StringComparison.InvariantCulture) == 0;
            IsActivated = activated;
            return IsActivated;
        }

        public void UpdateLaunchCount()
        {
            LaunchCount++;
        }

        public string GetInitialSettingsString()
        {
           return $"{IsActivated}|{LaunchCount}|{CurrentProccessorId}";
        }

        private string GetSettingsString()
        {
            return $"{IsActivated}|{LaunchCount}|{InitialProccessorId}";
        }

        private string GetProccessorId()
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var queryObj = (ManagementObject)obj;
                        var model = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
                        stringBuilder.Append(model + " : ");
                        stringBuilder.Append(queryObj["ProcessorId"]);
                    }
                }

                return stringBuilder.ToString();
            }
            catch
            {
                return "";
            }
        }

        public bool FileExists(string folder)
        {
            return File.Exists(Path.Combine(folder, FILENAME));
        }

        public void Load(string folder)
        {
            try
            {
                using (var fstream =
                    File.OpenRead(Path.Combine(folder, FILENAME)))
                {
                    byte[] data = new byte[400];
                    int bytes = fstream.Read(data, 0, data.Length);

                    string key = Encoding.UTF8.GetString(data, 0, bytes);

                    string decryptedKey =
                        Encryption.Decrypt(key, Settings.KEY_CRYPT);

                    if (SetSettingFromString(decryptedKey) == false)
                    {
                        throw new Exception();
                    }
                }
            }
            catch
            {
                throw new Exception("Ошибка загрузки настроек программы.");
            }
        }

        public void SaveInitialSettings(string folder)
        {
            try
            {
                using (var fstream =
                    File.Create(Path.Combine(folder, FILENAME)))
                {
                    File.SetAttributes(fstream.Name,
                        FileAttributes.Hidden | FileAttributes.System |
                        FileAttributes.Encrypted);
                    string settings =
                        Encryption.Encrypt(GetInitialSettingsString(),
                            KEY_CRYPT);
                    byte[] settingBytes = Encoding.UTF8.GetBytes(settings);
                    fstream.Write(settingBytes, 0, settingBytes.Length);
                    InitialProccessorId = CurrentProccessorId;
                }
            }
            catch
            {
                throw new Exception("Ошибка сохранения настроек программы.");
            }
        }

        public void Save(string folder)
        {
            try
            {
                using (var fstream =
                    File.OpenWrite(
                        Path.Combine(folder, FILENAME)))
                {
                    string settings =
                        Encryption.Encrypt(GetSettingsString(), KEY_CRYPT);
                    byte[] settingBytes = Encoding.UTF8.GetBytes(settings);
                    fstream.Write(settingBytes, 0, settingBytes.Length);
                }
            }
            catch
            {
                throw new Exception("Ошибка сохранения настроек программы.");
            }
        }


        public bool SetSettingFromString(string settingsString)
        {
            bool result;
            string[] settingsParts = settingsString.Split('|');

            try
            {
                IsActivated = bool.Parse(settingsParts[0]);
                LaunchCount = int.Parse(settingsParts[1]);
                InitialProccessorId = settingsParts[2];
                result = true;
            }
            catch
            {
                result = false;
            }

            return result;
        }
    }
}