namespace Tunetoon.Login
{
    public interface ILoginResult
    {
        string GameServer { get; }
        string Cookie { get; }
    }
}
