using AiManual.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔥 Render Port Fix
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// 🔹 Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<AIService>();
builder.Services.AddScoped<ChatService>();

var app = builder.Build();

// 🔥 Fix for Render crash (IMPORTANT)
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");

// 🔹 Initialize Embeddings
using (var scope = app.Services.CreateScope())
{
    var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
    var embeddingService = scope.ServiceProvider.GetRequiredService<EmbeddingService>();

    await dataService.InitializeEmbeddings(embeddingService);
}

// 🔹 Swagger
app.UseSwagger();
app.UseSwaggerUI();

// 🔹 Middleware
app.UseHttpsRedirection();
app.UseAuthorization();

// 🔹 Controllers
app.MapControllers();


// ✅ ADD THIS SIMPLE ENDPOINT (VERY IMPORTANT)

app.MapPost("/chat", async (HttpContext context, ChatService chatService) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    var request = System.Text.Json.JsonSerializer.Deserialize<ChatRequest>(body);

    var response = await chatService.GetResponse(request.message);

    return Results.Json(new { response = response });
});


// 🔹 Run
app.Run();


// 🔥 REQUEST MODEL
public class ChatRequest
{
    public string message { get; set; }
}
