using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerApi.Data;
using ExpenseTrackerApi.Extensions;
using ExpenseTrackerApi.Features.Telegram;

namespace ExpenseTrackerApi.Filters;

public class TelegramWebhookFilter(AppDbContext db, IConfiguration config, ITelegramBotClient botClient, IMediator mediator)
    : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var guid = context.RouteData.Values["guid"]?.ToString();
        if (guid != config["Telegram:WebhookGuid"])
        {
            context.Result = new UnauthorizedObjectResult("Invalid Webhook GUID");
            return;
        }

        if (context.HttpContext.Request.Headers["X-Telegram-Bot-Api-Secret-Token"] != config["Telegram:SecretToken"])
        {
            context.Result = new UnauthorizedObjectResult("Invalid Secret Token");
            return;
        }

        var update = context.ActionArguments.Values.OfType<Update>().FirstOrDefault();
        if (update == null) { context.Result = new OkResult(); return; }

        var chatId = update.CallbackQuery?.Message?.Chat.Id ?? update.Message?.Chat.Id ?? 0;
        var user = await db.Users.FirstOrDefaultAsync(u => u.TelegramChatId == chatId);

        if (user == null)
        {
            if (update.Message?.Text?.Trim() == "/start")
                await mediator.Send(new RegisterTelegramUserCommand(chatId, update.Message.From?.Username, update.Message.From?.FirstName));
            else if (update.Message != null)
                await botClient.SendMessage(chatId, "⚠️ Δεν είσαι εγγεγραμμένος. Στείλε /start για να εγγραφείς.");
            context.Result = new OkResult();
            return;
        }

        if (!user.IsAdmin && !user.IsApproved)
        {
            if (update.Message != null)
                await botClient.SendMessage(chatId, "⏳ Ο λογαριασμός σου αναμένει έγκριση από διαχειριστή.");
            context.Result = new OkResult();
            return;
        }

        context.HttpContext.SetTelegramUser(user);
        await next();
    }
}
