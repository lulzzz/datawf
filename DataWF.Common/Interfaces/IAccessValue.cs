﻿namespace DataWF.Common
{
    public interface IAccessValue
    {
        bool GetFlag(AccessType type, IUserIdentity user);
        void SetFlag(IAccessGroup group, AccessType type);
    }

    public interface IAccessItem
    {
        int GroupId { get; }
        AccessType Access { get; }        
    }
}