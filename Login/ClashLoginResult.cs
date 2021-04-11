namespace Tunetoon.Login
{
    public class ClashLoginResult : ILoginResult
    {
        public bool Status;
        public bool Toonstep;
        public int Reason;
        public string Token;
        public bool bad_token;

        public string GameServer => "gs.corporateclash.net";

        public string Cookie => Token;
    }
}
