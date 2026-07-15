using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Rag.Api.Infrastructure;
using Rag.Application.Documents;
using Rag.Infrastructure.Local;

var builder = WebApplication.CreateBuilder(args);

const long MultipartOverheadBytes = 64 * 1024;
var configuredMaxFileSize = builder.Configuration.GetValue<long?>(
    $"{DocumentUploadOptions.SectionName}:MaxFileSizeBytes");
var maxFileSize = configuredMaxFileSize is > 0
    ? configuredMaxFileSize.Value
    : DocumentUploadOptions.DefaultMaxFileSizeBytes;
var maxRequestBodySize = checked(maxFileSize + MultipartOverheadBytes);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DocumentValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOptions<DocumentUploadOptions>()
    .Bind(builder.Configuration.GetSection(DocumentUploadOptions.SectionName))
    .Validate(options => options.MaxFileSizeBytes > 0, "DocumentUpload:MaxFileSizeBytes must be positive.")
    .ValidateOnStart();
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = maxRequestBodySize);
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = maxRequestBodySize);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "RAG API", Version = "v1" });
});

builder.Services.AddLocalInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    if (context.Request.ContentLength is long contentLength
        && contentLength > maxRequestBodySize)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status413PayloadTooLarge,
            Title = "Request body too large",
            Detail = $"The request body must not exceed {maxRequestBodySize} bytes.",
            Type = "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.14",
            Instance = context.Request.Path
        });
        return;
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RAG API v1");
    });
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.Run();

public partial class Program;
