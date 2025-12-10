using System;

namespace DM.Domain.Entities;

public enum ChatType
{
    Direct = 0,
    Group = 1
}

public enum ChatMemberRole
{
    Owner = 0,
    Admin = 1,
    Member = 2
}

public enum MessagePayloadType
{
    Text = 0,
    Image = 1,
    File = 2,
    // amplía según necesites
}
