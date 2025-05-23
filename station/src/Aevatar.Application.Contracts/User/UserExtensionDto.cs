using System;

namespace Aevatar.User;

public class UserExtensionDto
{
    public Guid UserId { get; set; }
    /// <summary>
    /// EOA Address or CA Address
    /// </summary>
    public string WalletAddress { get; set; }
}