using AiManual.API.Services;

var builder = WebApplication.CreateBuilder(args);


// 🔥 IMPORTANT FOR RENDER (PORT FIX)
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");


// 🔹 Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔹 Custom Services
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<AIService>();
builder.Services.AddScoped<ChatService>();


var app = builder.Build();


// 🔥 Initialize Embeddings (IMPORTANT)
using (var scope = app.Services.CreateScope())
{
    var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
    var embeddingService = scope.ServiceProvider.GetRequiredService<EmbeddingService>();

    await dataService.InitializeEmbeddings(embeddingService);
}


// 🔹 Enable Swagger (ALWAYS ENABLE FOR NOW)
app.UseSwagger();
app.UseSwaggerUI();


// 🔹 Middleware
app.UseHttpsRedirection();
app.UseAuthorization();


// 🔹 Map Controllers
app.MapControllers();


// 🔹 Run App
app.Run();
