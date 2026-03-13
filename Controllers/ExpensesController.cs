using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerApi.Data;
using ExpenseTrackerApi.Models;
using System.Security.Claims;

namespace ExpenseTrackerApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ExpensesController(AppDbContext context)
    {
        _context = context;
    }

    // --- GET: /api/expenses ---
    [HttpGet]
    public IActionResult GetMyExpenses()
    {
        var userId = GetCurrentUserId();

        var expenses = _context.Expenses
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Date) // Τα πιο πρόσφατα πρώτα
            .ToList();

        return Ok(expenses);
    }

    // --- POST: /api/expenses ---
    // Προσθέτει ένα νέο έξοδο
    [HttpPost]
    public IActionResult AddExpense([FromBody] ExpenseCreateDto request)
    {
        var userId = GetCurrentUserId();

        var newExpense = new Expense
        {
            UserId = userId,
            Description = request.Description,
            Amount = request.Amount,
            Category = request.Category,
            Date = DateTime.UtcNow
        };

        _context.Expenses.Add(newExpense);
        _context.SaveChanges();

        return Ok(new { message = "Το έξοδο καταχωρήθηκε!", expenseId = newExpense.Id });
    }

    // --- DELETE: /api/expenses/{id} ---
    [HttpDelete("{id}")]
    public IActionResult DeleteExpense(int id)
    {
        var userId = GetCurrentUserId();
        
        var expense = _context.Expenses.FirstOrDefault(e => e.Id == id && e.UserId == userId);

        if (expense == null)
            return NotFound("Το έξοδο δεν βρέθηκε ή δεν σου ανήκει.");

        _context.Expenses.Remove(expense);
        _context.SaveChanges();

        return Ok(new { message = "Το έξοδο διαγράφηκε." });
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        return int.Parse(userIdClaim!);
    }

    [HttpGet("stats")]
    public IActionResult GetStats([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var userId = GetCurrentUserId();

        var query = _context.Expenses.Where(e => e.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(e => e.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.AddDays(1).AddTicks(-1);
            query = query.Where(e => e.Date <= endOfDay);
        }

        var categoryStats = query
            .GroupBy(e => e.Category)
            .Select(g => new CategoryStatDto(g.Key, g.Sum(e => e.Amount)))
            .OrderByDescending(s => s.TotalAmount)
            .ToList();

        var totalExpenses = categoryStats.Sum(s => s.TotalAmount);

        return Ok(new 
        { 
            FilterStartDate = startDate,
            FilterEndDate = endDate,
            TotalSpent = totalExpenses,
            Breakdown = categoryStats 
        });
    }

}

public record ExpenseCreateDto(string Description, decimal Amount, string Category);
public record CategoryStatDto(string Category, decimal TotalAmount);