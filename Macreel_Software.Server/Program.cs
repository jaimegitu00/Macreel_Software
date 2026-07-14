using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Macreel_Software.DAL;
using Macreel_Software.DAL.Admin;
using Macreel_Software.DAL.Auth;
using Macreel_Software.DAL.Common;
using Macreel_Software.DAL.Master;
using Macreel_Software.DAL.Employee;
using Macreel_Software.Services.AttendanceUpload;
using Macreel_Software.Services.FileUpload.Services;
using Macreel_Software.Services.FirebaseNotification;
using Macreel_Software.Services.MailSender;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.License.SetNonCommercialPersonal("Macreel_Infosoft");

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(
        Path.Combine(
            builder.Environment.ContentRootPath,
            "Firebase",
            "firebase-service-account.json"))
});


builder.Services.AddSingleton<FirebaseNotificationService>();




builder.Services.AddControllers();
builder.Services.AddScoped<JwtTokenProvider>();
builder.Services.AddScoped<ICommonServices, CommonService>();
builder.Services.AddScoped<IMasterService, MasterService>();
builder.Services.AddScoped<IAdminServices, AdminServices>();
builder.Services.AddScoped<IEmployeeService, EmployeeServices>();

builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<MailSender>();
builder.Services.AddScoped<PasswordEncrypt>();
//builder.Services.AddMemoryCache();
//if (builder.Environment.IsDevelopment())
//{
//    builder.Services.AddDistributedMemoryCache();
//}
//else
//{
//    builder.Services.AddStackExchangeRedisCache(options =>
//    {
//        options.Configuration = "127.0.0.1:6379";
//        options.InstanceName = "AuthApp:";
//    });
//}


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:54266", "https://vakiluncle.co.in", "https://macreel-software.firebaseapp.com")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse(); 
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"error\": \"Unauthorized\"}");
        },
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if(!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer"))
            {
                context.Token = authHeader.Substring("Bearer".Length);
                return Task.CompletedTask;
            }

            context.Token = context.Request.Cookies["access_token"];
            return Task.CompletedTask;
        }
    };
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IAuthServices, AuthServices>();
builder.Services.AddScoped<JwtTokenProvider>();
builder.Services.AddScoped<UploadAttendance>();

builder.Services.AddHttpContextAccessor();
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();
