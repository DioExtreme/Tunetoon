using System.Collections.Generic;
using System.Text.Json.Serialization;
using Tunetoon.Login;

namespace Tunetoon.Accounts
{
    public class ClashAccount : Account
    {
        public bool Authorized = false;
        public string LoginToken { get; set; }

        [JsonIgnore]
        public ClashLoginResult LoginResult = new ClashLoginResult();

        [JsonIgnore]
        public Dictionary<int, string> ClashIngameToons = new Dictionary<int, string>
        {
            // -1 is reserved for bad things in DataGridViews
            {-2, "Main Menu"}
        };

        public int ToonSlot;
    }
}
