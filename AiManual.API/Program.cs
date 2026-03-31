using AiManual.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔥 FIX: Render PORT binding
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// 🔥 FIX: Prevent file watcher crash in Render
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");

// 🔹 Add Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔹 Custom Services
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<AIService>();
builder.Services.AddScoped<ChatService>();

var app = builder.Build();


// 🔥 Initialize Embeddings (IMPORTANT for RAG)
using (var scope = app.Services.CreateScope())
{
    var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
    var embeddingService = scope.ServiceProvider.GetRequiredService<EmbeddingService>();

    await dataService.InitializeEmbeddings(embeddingService);
}


// 🔹 Enable Swagger (for testing)
app.UseSwagger();
app.UseSwaggerUI();


// 🔹 Middleware
app.UseHttpsRedirection();
app.UseAuthorization();


// 🔹 Use Controllers (THIS HANDLES YOUR /api/chat/ask)
app.MapControllers();


// 🔹 Root endpoint (optional - for health check)
app.MapGet("/", () => "AI Manual API is running ✅");


// 🔹 Run App
app.Run();
