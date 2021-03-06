using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Configs;

public class HangfireConfig
{
    public string BaseUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string[] Queues { get; set; }
    public int WorkerMultiplier { get; set; }

    public HangfireDataStore DataStore { get; set; }
    public string Sqlite { get; set; }
    public string LiteDb { get; set; }
    public string Redis { get; set; }
    public string MysqlConnection { get; set; }
}
