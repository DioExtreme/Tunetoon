namespace Tunetoon.Login
{
    public class RewrittenLoginResult : ILoginResult
    {
        public string Success;
        public string Gameserver;
        public string Cookie;
        public string Banner;
        public string ResponseToken;
        public string QueueToken;
        public string AuthToken;

        public string GameServer => Gameserver;

        string ILoginResult.Cookie => Cookie;
    }
}
