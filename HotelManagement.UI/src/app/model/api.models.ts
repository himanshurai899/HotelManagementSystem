// api.models.ts — TypeScript interfaces matching all backend DTOs

export const BOOKING_STATUS_LABELS = ['Pending', 'Confirmed', 'Cancelled', 'CheckedOut'];

export interface RoomTypeDTO {
  id: number;
  name: string;
  basePrice: number;
  capacity: number;
}

export interface RoomDTO {
  id: number;
  roomNumber: string;
  roomTypeId: number;
  roomTypeName: string;
  isAvailable: boolean;
}

export interface AmenityDTO {
  id: number;
  name: string;
  description: string;
}

export interface BookingDTO {
  id: number;
  userId: number;
  userName: string;
  roomId: number;
  roomNumber: string;
  checkInDate: string;
  checkOutDate: string;
  totalPrice: number;
  status: number;
}

export interface StaffDTO {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  position: string;
  hireDate: string;
}

// ── Access Control DTOs ────────────────────────────────────────────────────────

export interface ClaimDTO {
  type: string;
  value: string;
}

export interface RoleDTO {
  id: number;
  name: string;
  permissions: string[];
}

export interface UserDTO {
  id: number;
  username: string;
  email: string;
  phoneNumber: string;
  roleId: number;
  roleName: string;
  roles: string[];
  claims: ClaimDTO[];
}

export interface PermissionDTO {
  name: string;
  description: string;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  phoneNumber: string;
  password: string;
  roles: string[];
}

// ── Auth DTOs ──────────────────────────────────────────────────────────────────

/** Legacy minimal shape used by old UsersComponent — kept for backward compat */
export interface UserInfo {
  id: number;
  userName: string;
  email: string;
}

export interface LoginRequest {
  userName: string;
  password: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
  phoneNumber: string;
}

export interface AuthResponse {
  token: string;
}

