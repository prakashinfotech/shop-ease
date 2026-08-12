using System.Linq.Expressions;
using EBayClone.Domain.Common;
using EBayClone.Domain.Interfaces;
using EBayClone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EBayClone.Infrastructure.Repositories;

public class GenericRepository<T>(AppDbContext context) : IRepository<T>
    where T : BaseEntity
{
    private readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _dbSet.FindAsync([id], ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
        await _dbSet.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await _dbSet.FirstOrDefaultAsync(predicate, ct);

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await _dbSet.AnyAsync(predicate, ct);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default) =>
        predicate is null
            ? await _dbSet.CountAsync(ct)
            : await _dbSet.CountAsync(predicate, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await _dbSet.AddAsync(entity, ct);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) =>
        await _dbSet.AddRangeAsync(entities, ct);

    public void Update(T entity)
    {
        _dbSet.Attach(entity);
        context.Entry(entity).State = EntityState.Modified;
    }

    public void SoftDelete(T entity)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        Update(entity);
    }

    public IQueryable<T> Query() => _dbSet;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);
}