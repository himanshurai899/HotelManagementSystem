# Style Guide: Angular Models

## Unique Conventions in This Project

### 1. Interface + Const Array in the Same File
TypeScript model files export both the interface type and a static constant array of that type from the same file:

```typescript
// menus.ts
export interface Menu {
    route: string;
    title: string;
    description: string;
}

export const menus: Menu[] = [
    { route: '', title: 'Dashboard', description: 'Dashboard Panel' }
];
```

### 2. Kept in `src/app/model/` Directory
All TypeScript model/interface files live under `src/app/model/` — not `src/app/models/` (no plural).

### 3. File Named After the Concept (Plural Noun)
Model files are named after the data type they represent in plural: `menus.ts` (not `menu.ts` or `menu.model.ts`).
