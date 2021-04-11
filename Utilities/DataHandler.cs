using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Tunetoon.Accounts;

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

            if (!clashIngameToons.ContainsKey(account.Username.ToLower()))
            {
                return;
            }
            
            var toons = clashIngameToons[account.Username.ToLower()];
            
            foreach(var toon in toons)
            {
                if (!account.ClashIngameToons.ContainsKey(toon.Value))
                {
                    account.ClashIngameToons.Add(toon.Value, toon.Key);
                }
            }
        }

        public T Deserialize<T>(string file) where T : new()
        {
            var accountList = new T();

            if (!File.Exists(file))
            {
                return accountList;
            }

            string json;
            using (var sr = new StreamReader(file))
            {
                json = sr.ReadToEnd();
            }

            accountList = JsonConvert.DeserializeObject<T>(json);

            if (accountList == null)
            {
                return new T();
            }

            return accountList;
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
                    FindClashIngameToons(account);
                }
                else
                {
                    account.ToonSlot = 0;
                    account.Authorized = false;
                }
            }
        }

        public Config LoadConfig(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return new Config();
            }
            using (var sr = new StreamReader(fileName))
            {
                string json = sr.ReadToEnd();
                var loadedConfig = JsonConvert.DeserializeObject<Config>(json);
                if (loadedConfig == null)
                {
                    return new Config();
                }
                return loadedConfig;
            }
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

        public void SaveConfig(Config config, string file)
        {
            try
            {
                using (var sw = new StreamWriter(file))
                {
                    sw.Write(JsonConvert.SerializeObject(config, Formatting.Indented));
                }
            }
            catch
            {
                // ignored
            }
        }

        public void SaveSerialized<T>(T accountList, string file)
        {
            try
            {
                using (var sw = new StreamWriter(file))
                {
                    sw.Write(JsonConvert.SerializeObject(accountList, Formatting.Indented));
                }
            }
            catch
            {
                // ignored
            }
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
