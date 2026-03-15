var builder = WebApplication.CreateBuilder(args);

// 1. Adiciona os geradores do Swagger ao container de DI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. Ativa o Middleware para servir o JSON e a UI do Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MenuBuilder API V1");
        c.RoutePrefix = string.Empty; // Faz o Swagger abrir direto na raiz (localhost:5000)
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();