export interface Menu {
    route: string;
    title: string;
    description: string;
    /** Sidebar category group label */
    category: string;
    /** Roles that can see this item. Empty array = visible to all authenticated users. */
    requiredRoles: string[];
    /** Optional Material icon name for the nav link */
    icon?: string;
}

export interface MenuGroup {
    label: string;
    icon: string;
    items: Menu[];
}

/**
 * Master navigation item list.
 * Grouped by category for the left sidenav.
 * Routes with requiredRoles: [] are shown to every authenticated user.
 */
export const menus: Menu[] = [
    {
        route: '',
        title: 'Dashboard',
        description: 'Your overview & summary',
        category: 'Overview',
        requiredRoles: [],
        icon: 'dashboard'
    },
    {
        route: 'browse-rooms',
        title: 'Browse Rooms',
        description: 'Explore available rooms',
        category: 'Overview',
        requiredRoles: [],
        icon: 'search'
    },
    {
        route: 'rooms',
        title: 'Rooms',
        description: 'Manage hotel rooms',
        category: 'Property',
        requiredRoles: ['Administrator', 'SuperAdmin'],
        icon: 'hotel'
    },
    {
        route: 'room-types',
        title: 'Room Types',
        description: 'Manage room categories',
        category: 'Property',
        requiredRoles: ['Administrator', 'SuperAdmin'],
        icon: 'category'
    },
    {
        route: 'amenities',
        title: 'Amenities',
        description: 'Manage room amenities',
        category: 'Property',
        requiredRoles: ['Administrator', 'SuperAdmin'],
        icon: 'spa'
    },
    {
        route: 'staff',
        title: 'Staff',
        description: 'Manage hotel staff',
        category: 'People',
        requiredRoles: ['Administrator', 'SuperAdmin'],
        icon: 'badge'
    },
    {
        route: 'access-control',
        title: 'Access Control',
        description: 'Manage users, roles and permissions',
        category: 'People',
        requiredRoles: ['Administrator', 'SuperAdmin'],
        icon: 'admin_panel_settings'
    },
    {
        route: 'bookings',
        title: 'My Bookings',
        description: 'View and manage bookings',
        category: 'Reservations',
        requiredRoles: ['Customer', 'Administrator', 'SuperAdmin'],
        icon: 'book_online'
    }
];

/** Category metadata used for group header icons */
export const categoryMeta: Record<string, { icon: string }> = {
    'Overview':     { icon: 'home' },
    'Property':     { icon: 'apartment' },
    'People':       { icon: 'groups' },
    'Reservations': { icon: 'event_available' },
};
