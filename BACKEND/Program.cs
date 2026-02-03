using System.Text;
using System.Security.Claims;
using FichajeApp.Data;
using FichajeApp.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- DB ----------
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("ConnectionSql")));

// ---------- Controllers + JSON ----------
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        // camelCase en las respuestas JSON (token, rol, empleadoId, etc.)
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // enums como texto (Rol.Admin -> "Admin")
        opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ---------- CORS (útil en desarrollo / pruebas desde móvil) ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("Dev", p => p
        .WithOrigins("http://localhost:4200", "https://localhost:4200") // añade aquí IP:puerto si accedes desde móvil
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ---------- Auth JWT ----------
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            // Muy importante: que el middleware lea el claim de rol estándar
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

// ---------- Swagger + botón JWT ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FichajeApp API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Bearer. Ejemplo: **Bearer {token}**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
});

var app = builder.Build();

// ---------- Migrar y seed admin ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    if (!db.Empleados.Any(e => e.Email == "jaime@fichaje.com"))
    {
        db.Empleados.Add(new Empleado
        {
            Nombre = "Jaime",
            Apellidos = "Rodriguez",
            DNI = "71124408J",
            Email = "jaime@fichaje.com",
            Contrasena = BCrypt.Net.BCrypt.HashPassword("Jaime123!"),
            Rol = Rol.Admin
        });
        db.SaveChanges();
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// CORS sólo en desarrollo (quítalo si usas proxy estrictamente)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // ver excepciones detalladas en dev
    app.UseCors("Dev");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Permitir que la app escuche en todas las interfaces (HTTP/HTTPS)
builder.WebHost.UseUrls("http://0.0.0.0:5241", "https://0.0.0.0:7286");

app.Run();
