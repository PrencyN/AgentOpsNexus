using AgentOpsNexus.AgentApi.Agents;
using AgentOpsNexus.AgentApi.Repositories;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request validation failed",
            Detail = "One or more request fields are invalid.",
            Type = "https://agentops-nexus/errors/request-validation-failed"
        };

        problem.Extensions["code"] = "REQUEST_VALIDATION_FAILED";
        problem.Extensions["retryable"] = false;

        return new BadRequestObjectResult(problem);
    };
});

builder.Services.AddSingleton<
    IAgentRequestRepository,
    InMemoryAgentRequestRepository>();

builder.Services.AddSingleton<IIncidentAgent, MockIncidentAgent>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "AgentOpsNexus.AgentApi",
    timestamp = DateTimeOffset.UtcNow
}));

app.Run();

public partial class Program;