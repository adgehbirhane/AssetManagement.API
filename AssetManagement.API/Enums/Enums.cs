namespace AssetManagement.API.Enums;

public enum CategoryStatus
{
    ACTIVE,
    INACTIVE
}

public enum AssetStatus
{
    Available,
    Assigned,
    Maintenance,
    Retired
}

public enum AssetRequestStatus
{
    Pending,
    Approved,
    Rejected,
    // PreAssigned
}

public enum UserRole
{
    User,
    Admin
}
