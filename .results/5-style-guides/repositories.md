# Style Guide: Repositories

## Unique Conventions in This Project

### 1. Generic Repository — No Specialized Repositories
Only `IRepository<T>` and `Repository<T>` exist. There are no entity-specific repository interfaces or implementations. All CRUD operations go through the generic repository.

### 2. `SaveChangesAsync()` Called Inside Each Method
Each mutating operation (Add, Update, Delete) calls `SaveChangesAsync()` inline — there is no explicit Unit of Work pattern:

```csharp
public async Task AddAsync(T entity)
{
    await _dbSet.AddAsync(entity);
    await _context.SaveChangesAsync();
}
```

### 3. Protected Fields, Not Private
`_context` and `_dbSet` are declared `protected` to allow potential subclassing:

```csharp
protected readonly HotelDbContext _context;
protected readonly DbSet<T> _dbSet;
```

### 4. DbSet Initialized in Constructor
`_dbSet` is set once in the constructor via `context.Set<T>()` — not fetched on each method call.

### 5. DeleteAsync Fetches Entity First
The delete operation uses `GetByIdAsync` internally before removal — no direct `FindAsync` call:

```csharp
public async Task DeleteAsync(int id)
{
    var entity = await GetByIdAsync(id);
    _dbSet.Remove(entity);
    await _context.SaveChangesAsync();
}
```
