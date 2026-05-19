using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LegacyOrderApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyOrderApi.Repositories
{
    public interface IOrderRepository
    {
        Task<User> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default);
        Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);
        Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default);
        Task AddOrderAsync(Order order, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _db;

        public OrderRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<User> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
        }

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
        {
            _db.Users.Update(user);
            return Task.CompletedTask;
        }

        public Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
        {
            _db.Products.Update(product);
            return Task.CompletedTask;
        }

        public async Task AddOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            await _db.Orders.AddAsync(order, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
