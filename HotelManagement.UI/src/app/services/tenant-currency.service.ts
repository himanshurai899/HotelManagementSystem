import { Injectable, inject, signal, effect } from '@angular/core';
import { AuthService } from './auth.service';
import { ApiService } from './api.service';
import { DefaultCurrencyDTO } from '../model/api.models';

/** Curated locale → currency mapping used across the platform. */
export interface LocaleCurrencyEntry {
  locale: string;
  currencyCode: string;
  label: string;
}

export const LOCALE_CURRENCY_LIST: LocaleCurrencyEntry[] = [
  { locale: 'en-IN', currencyCode: 'INR', label: 'India — INR (₹)' },
  { locale: 'en-US', currencyCode: 'USD', label: 'United States — USD ($)' },
  { locale: 'en-GB', currencyCode: 'GBP', label: 'United Kingdom — GBP (£)' },
  { locale: 'de-DE', currencyCode: 'EUR', label: 'Germany — EUR (€)' },
  { locale: 'fr-FR', currencyCode: 'EUR', label: 'France — EUR (€)' },
  { locale: 'ja-JP', currencyCode: 'JPY', label: 'Japan — JPY (¥)' },
  { locale: 'zh-CN', currencyCode: 'CNY', label: 'China — CNY (¥)' },
  { locale: 'ar-AE', currencyCode: 'AED', label: 'UAE — AED (د.إ)' },
  { locale: 'en-AU', currencyCode: 'AUD', label: 'Australia — AUD (A$)' },
  { locale: 'en-CA', currencyCode: 'CAD', label: 'Canada — CAD (C$)' },
  { locale: 'en-SG', currencyCode: 'SGD', label: 'Singapore — SGD (S$)' },
  { locale: 'ko-KR', currencyCode: 'KRW', label: 'South Korea — KRW (₩)' },
  { locale: 'pt-BR', currencyCode: 'BRL', label: 'Brazil — BRL (R$)' },
  { locale: 'ru-RU', currencyCode: 'RUB', label: 'Russia — RUB (₽)' },
  { locale: 'tr-TR', currencyCode: 'TRY', label: 'Turkey — TRY (₺)' },
  { locale: 'th-TH', currencyCode: 'THB', label: 'Thailand — THB (฿)' },
  { locale: 'id-ID', currencyCode: 'IDR', label: 'Indonesia — IDR (Rp)' },
  { locale: 'pl-PL', currencyCode: 'PLN', label: 'Poland — PLN (zł)' },
  { locale: 'fr-CH', currencyCode: 'CHF', label: 'Switzerland — CHF (Fr)' },
  { locale: 'es-MX', currencyCode: 'MXN', label: 'Mexico — MXN (Mex$)' },
];

/**
 * Root-scoped service that resolves and exposes the active tenant's currency and locale.
 *
 * Priority:
 *  1. CurrencyCode + Locale claims decoded from the JWT (populated on login).
 *  2. GET /api/tenants/default-currency (INR / en-IN fallback) for unauthenticated visitors.
 *
 * Reactively updates whenever the token changes (login / logout).
 */
@Injectable({ providedIn: 'root' })
export class TenantCurrencyService {
  private readonly auth = inject(AuthService);
  private readonly api  = inject(ApiService);

  /** ISO 4217 currency code — e.g. "INR", "USD" */
  readonly currencyCode = signal<string>('INR');
  /** IETF locale tag — e.g. "en-IN", "en-US" */
  readonly locale = signal<string>('en-IN');

  constructor() {
    // Re-resolve whenever the JWT token changes (login / logout)
    effect(() => {
      const _token = this.auth.token(); // reactive dependency
      const cc  = this.auth.getCurrencyCode();
      const loc = this.auth.getLocale();
      if (cc && loc) {
        this.currencyCode.set(cc);
        this.locale.set(loc);
      } else {
        // Unauthenticated or token has no currency claims → fetch public default
        this.api.get<DefaultCurrencyDTO>('tenants/default-currency').subscribe({
          next: d => {
            this.currencyCode.set(d.currencyCode ?? 'INR');
            this.locale.set(d.locale ?? 'en-IN');
          },
          error: () => {
            // Hard fallback — INR
            this.currencyCode.set('INR');
            this.locale.set('en-IN');
          }
        });
      }
    });
  }

  /** Returns the currency code for the given locale from the curated list (or the input if unknown). */
  static currencyForLocale(locale: string): string {
    return LOCALE_CURRENCY_LIST.find(e => e.locale === locale)?.currencyCode ?? 'INR';
  }
}
