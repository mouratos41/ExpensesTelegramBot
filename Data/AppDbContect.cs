using Microsoft.EntityFrameworkCore;
using ExpenseTrackerApi.Models;
using System.Security.Cryptography.X509Certificates;

namespace ExpenseTrackerApi.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<User> Users => Set<User>();

    
}