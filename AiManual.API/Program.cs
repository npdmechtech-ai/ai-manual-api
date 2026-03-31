using AiManual.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔥 Render PORT fix
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// 🔥 Prevent crash
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<AIService>();
builder.Services.AddScoped<ChatService>();

var app = builder.Build();

// Initialize embeddings
using (var scope = app.Services.CreateScope())
{
    var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
    var embeddingService = scope.ServiceProvider.GetRequiredService<EmbeddingService>();

    await dataService.InitializeEmbeddings(embeddingService);
}

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();

// ✅ IMPORTANT — only controllers
app.MapControllers();

// Root test
app.MapGet("/", () => "API Running ✅");

app.Run();
