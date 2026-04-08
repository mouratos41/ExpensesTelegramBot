using MediatR;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using ExpenseTrackerApi.Extensions;
using ExpenseTrackerApi.Filters;
using ExpenseTrackerApi.Features.Telegram;

namespace ExpenseTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramController(IMediator mediator) : ControllerBase
{
    private static readonly HashSet<string> AdminCommands = ["/pending"];

    [HttpPost("webhook/{guid}")]
    [ServiceFilter(typeof(TelegramWebhookFilter))]
    public async Task<IActionResult> HandleUpdate([FromBody] Update update)
    {
        var user = HttpContext.GetTelegramUser();

        if (update.CallbackQuery != null)
        {
            var data = update.CallbackQuery.Data ?? string.Empty;
            if (user.IsAdmin && (data.StartsWith("approve_") || data.StartsWith("reject_")))
                await mediator.Send(new HandleAdminCallbackCommand(update.CallbackQuery));
            else
                await mediator.Send(new HandleCallbackCommand(update.CallbackQuery, user.Id));
            return Ok();
        }

        if (update.Message?.Text != null)
        {
            var text = update.Message.Text.Trim().ToLower();
            if (user.IsAdmin && AdminCommands.Contains(text))
                await mediator.Send(new HandleAdminMessageCommand(update.Message, user.Id));
            else
                await mediator.Send(new HandleMessageCommand(update.Message, user.Id));
        }

        return Ok();
    }
}
