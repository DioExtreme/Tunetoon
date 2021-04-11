using Newtonsoft.Json;
using Tunetoon.Utilities;

namespace Tunetoon.Accounts
{
    public abstract class Account : AsyncNotifyPropertyChanged
    {
        [JsonIgnore]
        private bool loginWanted;

        public bool LoginWanted
        {
            get { return loginWanted; }
            set { loginWanted = value; NotifyPropertyChanged("loginWanted"); }
        }

        public string Toon { get; set; }

        public string Username { get; set; }
       
        public string Password { get; set; }

        [JsonIgnore]
        public bool EndWanted { get; set; }

        [JsonIgnore]
        private bool loggedIn;

        [JsonIgnore]
        public bool LoggedIn 
        { 
            get { return loggedIn; } 
            set { loggedIn = value; NotifyPropertyChanged("loggedIn"); } 
        }

        public bool CanLogin()
        {
            return !LoggedIn && LoginWanted && Username != null && Password != null;
        }
    }
}
