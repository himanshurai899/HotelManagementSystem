// api.models.ts — TypeScript interfaces matching all backend DTOs

export const BOOKING_STATUS_LABELS = ['Pending', 'Confirmed', 'Cancelled', 'CheckedOut'];

/** Mirrors BookingType enum from Shared/Enums/BookingType.cs */
export const BookingType = {
  FullDay:   0,
  NightStay: 1,
  LongTerm:  2,
  Yearly:    3,
  Hourly:    4
} as const;
export type BookingType = typeof BookingType[keyof typeof BookingType];

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
  allowHourlyStay: boolean;
  pricePerNight: number;
  /** null = type unavailable for this room */
  hourlyRate:   number | null;
  dailyRate:    number | null;
  monthlyRate:  number | null;
  yearlyRate:   number | null;
}

export interface AmenityDTO {
  id: number;
  name: string;
  description: string;
}

export interface BookingRoomDTO {
  id: number;
  bookingId: number;
  roomId: number;
  roomNumber: string;       // denormalized — avoid complex object graph
  priceAtBooking: number;
}

export interface BookingDTO {
  id: number;
  userId: number;
  userName: string;
  /** Legacy single-room field — server emits the first BookingRoom for backward compat. Read-only on the UI side. */
  roomId: number;
  /** Legacy single-room field — server emits the first BookingRoom's RoomNumber. Read-only on the UI side. */
  roomNumber: string;
  /** Phase 12a — authoritative list of rooms on this booking. */
  rooms: BookingRoomDTO[];
  checkInDate: string;
  checkOutDate: string;
  /** BookingType enum value */
  bookingType: BookingType;
  /** 'HH:mm' string — only set for Hourly bookings */
  checkInTime:  string | null;
  checkOutTime: string | null;
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

export interface UserTenantDTO {
  userId: number;
  userName: string;       // denormalized — avoid complex object graph
  firstName: string;      // denormalized — avoid complex object graph
  lastName: string;       // denormalized — avoid complex object graph
  email: string;          // denormalized — avoid complex object graph
  tenantId: number;
  tenantName: string;     // denormalized — avoid complex object graph
  tenantRole: string | null;
  joinedAt: string;
}

export interface RoleDTO {
  id: number;
  name: string;
  permissions: string[];
}

export interface UserDTO {
  id: number;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  profilePhotoUrl: string | null;
  roleId: number;
  roleName: string;
  roles: string[];
  claims: ClaimDTO[];
  tenants?: UserTenantDTO[];
}

export interface PermissionDTO {
  name: string;
  description: string;
}

export interface CreateUserRequest {
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  password: string;
  roles: string[];
  claims: ClaimDTO[];
  idProofType?: string | null;
  idProofNumber?: string | null;
}

// ── Profile DTOs ───────────────────────────────────────────────────────────────

export interface UpdateProfileRequest {
  userName: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ProfilePhotoResponse {
  profilePhotoUrl: string;
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

// ── Billing & Payments (Phase 8) ──────────────────────────────────────────────

export interface InvoiceItemDTO {
  id: number;
  invoiceId: number;
  description: string;
  amount: number;
  quantity: number;
}

export interface InvoiceDTO {
  id: number;
  bookingId: number;
  guestName: string;       // denormalized — avoid complex object graph
  roomNumber: string;      // denormalized — avoid complex object graph
  issuedDate: string;
  dueDate: string;
  totalAmount: number;
  status: string;
  items: InvoiceItemDTO[];
}

export interface PaymentDTO {
  id: number;
  bookingId: number;
  amount: number;
  paymentDate: string;
  status: string;
}

export interface CompanyProfileDTO {
  id: number;
  companyName: string;
  logoUrl: string | null;
  address: string | null;
  gstinNumber: string | null;
  phoneNumber: string | null;
  email: string | null;
  website: string | null;
  primaryColor: string | null;
  accentColor: string | null;
  fontFamily: string | null;
}
// ── Billing & Payments (Phase 8) ─────────────────────────────────────────────────

export interface InvoiceItemDTO {
  id: number;
  invoiceId: number;
  description: string;
  amount: number;
  quantity: number;
}

export interface InvoiceDTO {
  id: number;
  bookingId: number;
  guestName: string;       // denormalized — avoid complex object graph
  roomNumber: string;      // denormalized — avoid complex object graph
  issuedDate: string;
  dueDate: string;
  totalAmount: number;
  status: string;
  items: InvoiceItemDTO[];
}

export interface PaymentDTO {
  id: number;
  bookingId: number;
  amount: number;
  paymentDate: string;
  status: string;
}

export interface CompanyProfileDTO {
  id: number;
  companyName: string;
  logoUrl: string | null;
  address: string | null;
  gstinNumber: string | null;
  phoneNumber: string | null;
  email: string | null;
  website: string | null;
  primaryColor: string | null;
  accentColor: string | null;
  fontFamily: string | null;
}
// ── Tenants (Phase 9) ────────────────────────────────────────────────────────────

export interface TenantDTO {
  id: number;
  name: string;
  subdomain: string;
  plan: string;
  isActive: boolean;
  createdAt: string;
  currencyCode: string;   // ISO 4217
  locale: string;         // IETF locale tag
}

export interface DefaultCurrencyDTO {
  currencyCode: string;
  locale: string;
}

// ── Booking Calendar (Phase 12) ───────────────────────────────────────────────

export interface BookingCalendarEntry {
  id: number;
  roomId: number;
  roomNumber: string;
  customerName: string;   // denormalized — avoid complex object graph
  checkInDate: string;    // 'yyyy-MM-dd'
  checkOutDate: string;   // 'yyyy-MM-dd'
  status: string;         // 'Pending' | 'Confirmed'
}
