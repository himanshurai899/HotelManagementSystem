import { Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';

const TOKEN_KEY = 'hotel_token';
/** Show the refresh prompt 5 min before the token expires */
const REFRESH_WARNING_MS = 5 * 60 * 1000;

/** Standard ASP.NET Core Identity role claim URI */
const ROLE_CLAIM_URI =
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
/** Standard NameIdentifier claim URI */
const NAME_CLAIM_URI =
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';

@Injectable({ providedIn: 'root' })
export class AuthService {
  // ── Signals ──────────────────────────────────────────────────────────────

  /** Reactive token signal. All auth-derived values must read via this. */
  private readonly _token = signal<string | null>(
    typeof localStorage !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null
  );
  readonly token = this._token.asReadonly();

  /** Emits true when the token is within REFRESH_WARNING_MS of expiry */
  private readonly _showRefreshPrompt = signal(false);
  readonly showRefreshPrompt = this._showRefreshPrompt.asReadonly();

  private refreshTimer: ReturnType<typeof setTimeout> | null = null;

  // ── Constructor ───────────────────────────────────────────────────────────

  constructor(private router: Router) {
    // Re-schedule the warning timer on app bootstrap if token already exists
    if (this.isLoggedIn()) {
      this.scheduleRefreshPrompt();
    }
  }

  // ── Token storage ─────────────────────────────────────────────────────────

  /**
   * Persists the JWT in localStorage and starts the refresh-prompt timer.
   * Call this immediately after a successful login or token refresh.
   */
  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
    this._token.set(token);
    this._showRefreshPrompt.set(false);
    this.scheduleRefreshPrompt();
  }

  getToken(): string | null {
    return this._token();
  }

  clearToken(): void {
    localStorage.removeItem(TOKEN_KEY);
    this._token.set(null);
    this._showRefreshPrompt.set(false);
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = null;
    }
  }

  // ── Auth state ────────────────────────────────────────────────────────────

  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;
    const payload = this.decodePayload(token);
    if (!payload?.['exp']) return false;
    // Check exp (seconds) against current time
    return (payload['exp'] as number) * 1000 > Date.now();
  }

  logout(): void {
    this.clearToken();
    this.router.navigate(['/login']);
  }

  // ── JWT payload decoder ───────────────────────────────────────────────────

  private decodePayload(token: string): Record<string, unknown> | null {
    try {
      const base64Url = token.split('.')[1];
      if (!base64Url) return null;
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const json = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(json);
    } catch {
      return null;
    }
  }

  getPayload(): Record<string, unknown> | null {
    const token = this.getToken();
    return token ? this.decodePayload(token) : null;
  }

  // ── Roles ─────────────────────────────────────────────────────────────────

  getRoles(): string[] {
    const payload = this.getPayload();
    if (!payload) return [];
    // ASP.NET Core Identity serialises roles under the long claim URI.
    // Fall back to short forms for flexibility.
    const raw = payload[ROLE_CLAIM_URI] ?? payload['role'] ?? payload['roles'];
    if (!raw) return [];
    return Array.isArray(raw) ? (raw as string[]) : [raw as string];
  }

  hasRole(role: string): boolean {
    return this.getRoles().includes(role);
  }

  hasAnyRole(roles: string[]): boolean {
    return roles.some(r => this.hasRole(r));
  }

  /** Returns true when the current user has the SuperAdmin role. */
  isSuperAdmin(): boolean {
    return this.hasRole('SuperAdmin');
  }

  /**
   * Returns true when the user is SuperAdmin OR has the given role.
   * Use for UI visibility decisions that SuperAdmin should always pass.
   */
  isSuperAdminOr(role: string): boolean {
    return this.isSuperAdmin() || this.hasRole(role);
  }

  // ── Claims ────────────────────────────────────────────────────────────────

  /**
   * Returns true if the decoded JWT payload contains a claim whose key
   * matches `type` and whose value (or array of values) includes `value`.
   *
   * @example hasClaim('Permission', 'ManageRooms')
   */
  hasClaim(type: string, value: string): boolean {
    const payload = this.getPayload();
    if (!payload) return false;
    const raw = payload[type];
    if (!raw) return false;
    if (Array.isArray(raw)) return (raw as string[]).includes(value);
    return raw === value;
  }

  // ── User info ─────────────────────────────────────────────────────────────

  getUsername(): string {
    const payload = this.getPayload();
    if (!payload) return '';
    return (
      (payload['sub'] as string) ??
      (payload['unique_name'] as string) ??
      (payload[NAME_CLAIM_URI] as string) ??
      ''
    );
  }

  getUserId(): string {
    const payload = this.getPayload();
    if (!payload) return '';
    return (payload[NAME_CLAIM_URI] as string) ?? '';
  }

  // ── Silent refresh prompt ─────────────────────────────────────────────────

  /**
   * Schedules a one-shot timer that sets `showRefreshPrompt` to true when
   * REFRESH_WARNING_MS milliseconds remain before the token expires.
   * The server issues 30-minute tokens (set in JwtTokenProvider).
   */
  private scheduleRefreshPrompt(): void {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = null;
    }

    const payload = this.getPayload();
    if (!payload?.['exp']) return;

    const expiryMs = (payload['exp'] as number) * 1000;
    const warningAt = expiryMs - REFRESH_WARNING_MS;
    const delay = warningAt - Date.now();

    if (delay <= 0) {
      // Token is already in the warning window (or expired)
      this._showRefreshPrompt.set(true);
      return;
    }

    this.refreshTimer = setTimeout(() => {
      this._showRefreshPrompt.set(true);
    }, delay);
  }

  /** Call this when the user acknowledges the prompt (e.g. clicks "Stay logged in") */
  dismissRefreshPrompt(): void {
    this._showRefreshPrompt.set(false);
  }
}
