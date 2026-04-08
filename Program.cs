using System.Text;
using ExpenseTrackerApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(60),
            errorNumbersToAdd: null
        )
    )
);
//options.UseSqlite(connectionString));

var jwtIssuer = builder.Configuration.GetSection("Jwt:Issuer").Value;
var jwtAudience = builder.Configuration.GetSection("Jwt:Audience").Value;
var jwtKey = builder.Configuration.GetSection("Jwt:Key").Value;

if (!string.IsNullOrEmpty(jwtKey))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };
        });
}

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddScoped<ExpenseTrackerApi.Filters.TelegramWebhookFilter>();
builder.Services.AddSingleton<ITelegramBotClient>(_ =>
    new TelegramBotClient(builder.Configuration["Telegram:BotToken"]!));

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ExpenseTracker API", Version = "v1" });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsSqlServer()) {
        db.Database.Migrate();
    }
}

app.UseExceptionHandler(err => err.Run(async ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    ctx.Response.ContentType = "application/json";
    ctx.Response.StatusCode = 500;
    await ctx.Response.WriteAsJsonAsync(new { error = ex?.Error.Message, inner = ex?.Error.InnerException?.Message });
}));

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ExpenseTracker v1"));

app.UseHttpsRedirection();
app.UseAuthentication(); 
app.UseAuthorization();  
app.MapControllers();

app.Run();