using AiManual.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔥 FORCE SERVER + PORT (NO LOCALHOST ISSUE)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(10000);
});

// 🔧 SERVICES
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔧 DEPENDENCY INJECTION
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<AIService>();
builder.Services.AddSingleton<ChatService>();

var app = builder.Build();

// 🔥 CRITICAL FIX: INITIALIZE EMBEDDINGS (RAG ENGINE START)
using (var scope = app.Services.CreateScope())
{
    var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
    var embeddingService = scope.ServiceProvider.GetRequiredService<EmbeddingService>();

    Console.WriteLine("⏳ Initializing embeddings...");

    await dataService.InitializeEmbeddings(embeddingService);

    Console.WriteLine("✅ Embeddings initialized successfully.");
}

// 🔥 ENABLE SWAGGER
app.UseSwagger();
app.UseSwaggerUI();

// 🔧 MIDDLEWARE
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// 🔥 DEBUG LOG
Console.WriteLine("🚀 API RUNNING ON: http://127.0.0.1:10000/swagger");

app.Run();