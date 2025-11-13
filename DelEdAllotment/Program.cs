using DelEdAllotment.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

// CORS configuration to allow specific origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
        {
            policyBuilder.AllowAnyOrigin() // React front-end running on localhost
                  .AllowAnyHeader()
                  .AllowAnyMethod();
                    
        });
});

// Add services for controllers
builder.Services.AddControllers();
builder.Services.AddRazorPages();


// Add JWT Authentication (if you're using JWT tokens for securing your API)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"], // Example: "https://yourdomain.com"
            ValidAudience = builder.Configuration["Jwt:Audience"], // Example: "https://yourdomain.com"
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])) // Your secret key
        };
    });

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Use the CORS policy
app.UseCors();


//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(Path.Combine(@"Z:\ApplicationsData\DELED2021-22\photos")),
//    RequestPath = "/static"
//});

app.UseStaticFiles();


//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(Path.Combine(@"D:\ApplicationsData\DELED2025-26\photos")),
//    RequestPath = "/static"
//});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(@"Z:\ApplicationsData\DELED2021-22\photos")),
    RequestPath = "/static"
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable Authentication & Authorization (for JWT authentication)

app.UseAuthentication();
app.UseAuthorization();



app.MapControllers();
app.MapRazorPages();

app.Run();
