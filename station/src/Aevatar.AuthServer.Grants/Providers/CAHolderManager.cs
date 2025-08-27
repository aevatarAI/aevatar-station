namespace Aevatar.AuthServer.Grants.Providers;

public class CAHolderManagerInfo
{
    public List<CAHolderManager> CaHolderManagerInfo { get; set; }
}

public class CAHolderManager
{
    public string ChainId { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public List<Managers> ManagerInfos { get; set; }
}

public class Managers
{
    public string Address { get; set; }
    public string ExtraData { get; set; }
}