# Style Guide: Angular Components

## Unique Conventions in This Project

### 1. Standalone Components Only (No NgModules)
Every Angular component uses `standalone: true` by default (Angular 19 default). Angular Material modules and other imports are declared directly in the component's `imports` array:

```typescript
@Component({
  selector: 'app-nav-bar',
  imports: [MatToolbarModule, MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss'
})
```

### 2. `styleUrl` (Singular) Not `styleUrls` (Plural)
Angular 17+ single stylesheet syntax is used:
```typescript
styleUrl: './component.scss'  // ✅
// styleUrls: ['./component.scss']  ❌ not used
```

### 3. SCSS for Styles
All component styles use `.scss` extension — not `.css`.

### 4. Component Selector Prefix `app-`
All selectors follow the `app-{name}` convention:
```typescript
selector: 'app-dashboard'
selector: 'app-nav-bar'
```

### 5. Angular Material Imported Per-Component
Material modules are not globally registered — they are imported in each individual component that uses them.

### 6. Typed Model Imports from `model/`
Shared TypeScript interfaces and constants live in `src/app/model/`. Components import typed interfaces directly:

```typescript
import { Menu, menus } from '../../model/menus';
```
