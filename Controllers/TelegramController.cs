using MediatR;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ExpenseTrackerApi.Data;
using ExpenseTrackerApi.Features.Telegram;

namespace ExpenseTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramController(AppDbContext context, IConfiguration config, IMediator mediator, ITelegramBotClient botClient) : ControllerBase
{
    [HttpPost("webhook/{guid}")]
    public async Task<IActionResult> HandleUpdate(string guid, [FromBody] Update update)
    {
        if (guid != config["Telegram:WebhookGuid"])
            return Unauthorized("Invalid Webhook GUID");

        if (Request.Headers["X-Telegram-Bot-Api-Secret-Token"] != config["Telegram:SecretToken"])
            return Unauthorized("Invalid Secret Token");

        var chatId = update.CallbackQuery?.Message?.Chat.Id ?? update.Message?.Chat.Id ?? 0;
        var user = context.Users.FirstOrDefault(u => u.TelegramChatId == chatId);

        if (user == null)
        {
            if (update.Message != null)
                await botClient.SendMessage(chatId, $"⚠️ Δεν είσαι εγγεγραμμένος. Chat ID: `{chatId}`", parseMode: ParseMode.Markdown);
            return Ok();
        }

        if (update.CallbackQuery != null)
            await mediator.Send(new HandleCallbackCommand(update.CallbackQuery, user.Id));
        else if (update.Message?.Text != null)
            await mediator.Send(new HandleMessageCommand(update.Message, user.Id));

        return Ok();
    }
}
