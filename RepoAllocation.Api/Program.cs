using RepoAllocation.Api.Application;
using RepoAllocation.Api.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDevClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

builder.Services.AddSingleton<AllocationEngine>();
builder.Services.AddSingleton<SampleSecurityProvider>();
builder.Services.AddSingleton<AllocationApplicationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AngularDevClient");

app.UseAuthorization();

app.MapControllers();

app.Run();