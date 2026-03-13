using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ExpenseTrackerApi.Data;
using ExpenseTrackerApi.Models;

namespace ExpenseTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ITelegramBotClient _botClient;

    public TelegramController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
        _botClient = new TelegramBotClient(_config["Telegram:BotToken"]!);
    }

    [HttpPost("webhook/{guid}")]
    public async Task<IActionResult> HandleUpdate(string guid, [FromBody] Update update)
    {
        if (guid != _config["Telegram:WebhookGuid"]) 
            return Unauthorized("Invalid Webhook GUID");

        var secretHeader = Request.Headers["X-Telegram-Bot-Api-Secret-Token"];
        if (secretHeader != _config["Telegram:SecretToken"])
            return Unauthorized("Invalid Secret Token");

        if (update.Message?.Text == null) return Ok();

        var chatId = update.Message.Chat.Id;
        var text = update.Message.Text.Trim();

        var user = _context.Users.FirstOrDefault(u => u.TelegramChatId == chatId);
        if (user == null)
        {
            await _botClient.SendMessage(chatId, $"⚠️ Δεν είσαι εγγεγραμμένος. Chat ID: `{chatId}`", parseMode: ParseMode.Markdown);
            return Ok();
        }

        if (text.StartsWith("/"))
        {
            await HandleCommands(chatId, user.Id, text.ToLower());
        }
        else
        {
            await ProcessExpense(chatId, user.Id, text);
        }

        return Ok();
    }

    private async Task HandleCommands(long chatId, int userId, string command)
    {
        if (command == "/undo")
        {
            var lastExpense = _context.Expenses
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date) 
                .FirstOrDefault();

            if (lastExpense == null)
            {
                await _botClient.SendMessage(chatId, "📭 Δεν βρέθηκε κάποιο έξοδο για να διαγραφεί.");
                return;
            }

            _context.Expenses.Remove(lastExpense);
            await _context.SaveChangesAsync();

            await _botClient.SendMessage(chatId, $"🗑️ Διαγράφηκε το τελευταίο έξοδο: {lastExpense.Amount:N2}€ ({lastExpense.Category}).");
            return;
        }
        

        var isYearly = command == "/year";
        if (command != "/stats" && command != "/year") return;

        var start = isYearly ? new DateTime(DateTime.UtcNow.Year, 1, 1) : new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        
        var stats = _context.Expenses
            .Where(e => e.UserId == userId && e.Date >= start)
            .GroupBy(e => e.Category)
            .Select(g => new { Cat = g.Key, Total = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Total).ToList();

        if (!stats.Any()) {
            await _botClient.SendMessage(chatId, "📭 Δεν υπάρχουν έξοδα για αυτή την περίοδο.");
            return;
        }

        var report = $"📊 *Στατιστικά {(isYearly ? "Έτους" : "Μήνα")}*\n" + 
                     string.Join("\n", stats.Select(s => $"🔹 {s.Cat}: {s.Total:N2}€")) +
                     $"\n\n💰 *Σύνολο: {stats.Sum(x => x.Total):N2}€*";

        await _botClient.SendMessage(chatId, report, parseMode: ParseMode.Markdown);
    }

    private async Task ProcessExpense(long chatId, int userId, string text)
    {
        var parts = text.Split(' ', 2);
        if (parts.Length < 2 || !decimal.TryParse(parts[0], out decimal amount)) {
            await _botClient.SendMessage(chatId, "❌ Γράψε: `Ποσό Κατηγορία` (π.χ. `10 Καφές`)", parseMode: ParseMode.Markdown);
            return;
        }

        _context.Expenses.Add(new Expense {
            UserId = userId,
            Amount = amount,
            Category = parts[1],
            Description = "Telegram Bot",
            Date = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await _botClient.SendMessage(chatId, $"✅ Καταχωρήθηκε {amount:N2}€ στο '{parts[1]}'.");
    }
}