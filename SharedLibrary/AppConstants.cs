namespace SharedLibrary
{
    /// <summary>Static constants for role and order-status strings used across API and WPF layers.</summary>
    public static class AppConstants
    {
        /// <summary>User role strings matching the Role column in the User table and JWT claims.</summary>
        public static class Roles
        {
            public const string Admin   = "Admin";
            public const string Manager = "Manager";
            public const string User    = "User";
        }

        /// <summary>Order status strings used in the Status column and API payloads.</summary>
        public static class OrderStatus
        {
            public const string Pending   = "Pending";
            public const string Completed = "Completed";
            public const string Complete  = "Complete";  // legacy alias used in some views
        }
    }
}

