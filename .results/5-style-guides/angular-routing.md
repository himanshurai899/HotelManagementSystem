# Style Guide: Angular Routing

## Unique Conventions in This Project

### 1. Single Flat Routes Array in app.routes.ts
All routes are in one flat array — no nested children, no lazy loading modules:

```typescript
export const routes: Routes = [
    { path: '', component: DashboardComponent },
    { path: 'error', component: ErrorComponent }
];
```

### 2. Components Imported Directly (No Lazy Loading)
Route components are imported at the top of `app.routes.ts` and referenced directly — not loaded via `loadComponent`.

### 3. Registered via provideRouter in app.config.ts
Routes are registered using the standalone bootstrap API:

```typescript
export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimationsAsync()
  ]
};
```
