namespace HotelManagementSystem.Shared.Constants
{
    public static class Permissions
    {
        public const string ManageUsers       = "ManageUsers";
        public const string ManageRoles       = "ManageRoles";
        public const string ManagePermissions = "ManagePermissions";
        public const string ManageRooms       = "ManageRooms";
        public const string ManageRoomTypes   = "ManageRoomTypes";
        public const string ManageAmenities   = "ManageAmenities";
        public const string ManageStaff       = "ManageStaff";
        public const string ManageBookings    = "ManageBookings";
        public const string ViewReports       = "ViewReports";
        public const string ViewDashboard     = "ViewDashboard";
        public const string MakeBooking       = "MakeBooking";

        public static readonly IReadOnlyList<PermissionInfo> All = new List<PermissionInfo>
        {
            new PermissionInfo(ManageUsers,       "Create, edit and delete users; assign roles and claims."),
            new PermissionInfo(ManageRoles,       "Create, edit and delete roles."),
            new PermissionInfo(ManagePermissions, "Assign permission claims to roles."),
            new PermissionInfo(ManageRooms,       "CRUD operations on rooms."),
            new PermissionInfo(ManageRoomTypes,   "CRUD operations on room types."),
            new PermissionInfo(ManageAmenities,   "CRUD operations on amenities."),
            new PermissionInfo(ManageStaff,       "CRUD operations on staff records."),
            new PermissionInfo(ManageBookings,    "View and modify all bookings."),
            new PermissionInfo(ViewReports,       "Access reports and analytics dashboards."),
            new PermissionInfo(ViewDashboard,     "Access the main dashboard view."),
            new PermissionInfo(MakeBooking,       "Create bookings as a customer."),
        };
    }

    public class PermissionInfo
    {
        public PermissionInfo(string name, string description)
        {
            Name = name;
            Description = description;
        }
        public string Name { get; }
        public string Description { get; }
    }
}
