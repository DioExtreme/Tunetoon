using static System.Environment;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Tunetoon.Accounts;

namespace Tunetoon.Utilities
{
    class DataHandler
    {
        private readonly string criticalDirectory = GetFolderPath(SpecialFolder.LocalApplicationData) + "\\DioExtreme\\Rescue\\";
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
            bool deleteAfter = false;
            string criticalFile = $"{criticalDirectory}{file}";

            if (File.Exists(criticalFile))
            {
                file = criticalFile;
                deleteAfter = true;
            }
            else if (!File.Exists(file))
            {
                return new T();
            }

            string json;
            using (var sr = new StreamReader(file))
            {
                json = sr.ReadToEnd();
            }

            if (deleteAfter)
            {
                File.Delete(file);
            }

            var obj = JsonConvert.DeserializeObject<T>(json);

            if (obj == null)
            {
                return new T();
            }

            return obj;
        }

        public void DecryptBase(Account account)
        {
            if (account.Username != null)
            {
                account.Username = DataProtection.Decrypt(account.Username, false);
            }
            if (account.Password != null)
            {
                account.Password = DataProtection.Decrypt(account.Password, true);
            }
        }

        public void LoadAccounts(ref AccountList<RewrittenAccount> accountList, string file)
        {
            accountList = Deserialize<AccountList<RewrittenAccount>>(file);

            foreach (var acc in accountList)
            {
                DecryptBase(acc);
            }
        }

        public void LoadAccounts(ref AccountList<ClashAccount> accountList, string file)
        {
            accountList = Deserialize<AccountList<ClashAccount>>(file);

            foreach (var account in accountList)
            {
                DecryptBase(account);
                if (account.LoginToken != null)
                {
                    account.LoginToken = DataProtection.Decrypt(account.LoginToken, true);
                    if (account.LoginToken == null)
                    {
                        account.ToonSlot = 0;
                        account.Authorized = false;
                    }
                }
                else
                {
                    account.ToonSlot = 0;
                    account.Authorized = false;
                }
            }
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
                toons = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(json);
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
            try
            {
                using (var sw = new StreamWriter(file))
                {
                    sw.Write(JsonConvert.SerializeObject(obj, Formatting.Indented));
                }
            }
            catch
            {
                if (!Directory.Exists(criticalDirectory))
                {
                    Directory.CreateDirectory(criticalDirectory);
                }
                string criticalFile = $"{criticalDirectory}{file}";
                using (var sw = new StreamWriter(criticalFile))
                {
                    sw.Write(JsonConvert.SerializeObject(obj, Formatting.Indented));
                }
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

        public void EncryptBase(Account account)
        {
            if (account.Username != null)
            {
                account.Username = DataProtection.Encrypt(Encoding.UTF8.GetBytes(account.Username), false);
            }
            if (account.Password != null)
            {
                account.Password = DataProtection.Encrypt(Encoding.UTF8.GetBytes(account.Password), true);
            }
        }

        public void SaveAccounts(AccountList<RewrittenAccount> accountList, string file)
        {
            foreach (var acc in accountList)
            {
                EncryptBase(acc);
            }
            SaveSerialized(accountList, file);
        }

        public void SaveAccounts(AccountList<ClashAccount> accountList, string file)
        {
            foreach (var account in accountList)
            {
                EncryptBase(account);   
                if (account.LoginToken != null)
                {
                    account.LoginToken = DataProtection.Encrypt(Encoding.UTF8.GetBytes(account.LoginToken), true);
                }
            }
            SaveSerialized(accountList, file);
        }
    }
}
