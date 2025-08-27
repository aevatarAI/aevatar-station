using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Aevatar.Organizations;

public class MemberInvitationInfo
{
    public Guid Inviter { get; set; }
    public Guid InvitationId { get; set; }
}