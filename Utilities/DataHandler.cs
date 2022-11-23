using System.Collections.Generic;
using System.IO;
using Tunetoon.Accounts;
using System.Text.Json;
using Tunetoon.Encrypt;

namespace Tunetoon.Utilities
{
    class DataHandler
    {
        private Dictionary<string, Dictionary<string, int>> clashIngameToons;

        public void FindClashIngameToons(ClashAccount account)
        {
            if (account.Username == null)
            { 
                return;
            }

            string usernameLowercase = account.Username.ToLower();
            if (!clashIngameToons.ContainsKey(usernameLowercase))
            {
                return;
            }
            
            var toons = clashIngameToons[usernameLowercase];
            
            foreach(var toon in toons)
            {
                if (!account.ClashIngameToons.ContainsKey(toon.Value))
                {
                    account.ClashIngameToons.Add(toon.Value, toon.Key);
                }
            }
        }

        public void FindClashIngameToons(AccountList<ClashAccount> clashAccountList)
        {
            foreach (ClashAccount clashAccount in clashAccountList)
            {
                FindClashIngameToons(clashAccount);
            }
        }

        public T Deserialize<T>(string file) where T : new()
        {
            if (!File.Exists(file))
            {
                return new T();
            }

            string json;
            using (var sr = new StreamReader(file))
            {
                json = sr.ReadToEnd();
            }

            var obj = JsonSerializer.Deserialize<T>(json);

            if (obj == null)
            {
                return new T();
            }

            return obj;
        }

        public AccountList<T> LoadEncrypted<T>(string masterPassword, string file) where T : Account
        {
            string json = DataProtection.ReadJsonFromEncryptedFile(masterPassword, file);
            return JsonSerializer.Deserialize<AccountList<T>>(json);
        }

        public Config LoadConfig(string file)
        {
            return Deserialize<Config>(file);
        }

        public Dictionary<string, Dictionary<string, int>> LoadToonJson(Config config)
        {
            string file = config.ClashPath + "toons.json";

            var toons = new Dictionary<string, Dictionary<string, int>>();

            if (!File.Exists(file))
            {
                return toons;
            }

            string json;
            using (var sr = new StreamReader(file))
            {
                json = sr.ReadToEnd();
            }

            try
            {
                toons = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json);
            }
            catch
            {
                // corrupted toons.json
                File.Delete(file);
            }
            return toons;
        }

        public void LoadClashIngameToons(Config config)
        {
            clashIngameToons = LoadToonJson(config);
        }

        private void WriteObjectToFile<T>(T obj, string file)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            try
            {
                using (var sw = new StreamWriter(file))
                {
                    sw.Write(JsonSerializer.Serialize(obj, options));
                }
            }
            catch
            {
               // ignored
            }
        }

        private void WriteObjectToEncryptedFile<T>(T obj, string file)
        {
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(obj);
            try
            {
                DataProtection.WriteJsonToEncryptedFile(jsonBytes, file);
            }
            catch
            {
                throw;
            }
        }

        public void SaveConfig(Config config, string file)
        {
            WriteObjectToFile(config, file);
        }

        public void SaveSerialized<T>(T accountList, string file)
        {
            WriteObjectToFile(accountList, file);
        }

        public void SaveEncrypted<T>(AccountList<T> accountList, string file) where T : Account
        {
            WriteObjectToEncryptedFile(accountList, file);
        }
    }
}
