using BellwoodGlobal.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// add EF Core
builder.Services.AddDbContext<BellwoodGlobalDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("BellwoodDatabase")));

// jwt bearer authentication configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      // 1) Use the HTTPS address of your AuthServer
      options.Authority = "https://localhost:7157";

      // 2) In dev you can allow HTTP metadata, but we’re using HTTPS now
      options.RequireHttpsMetadata = true;

      // 3) The API scope you defined for ride endpoints
      options.Audience = "ride.api";
  });


// add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireRideScope", policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim("scope", "ride.api"));
    options.AddPolicy("RequireRatingScope", policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim("scope", "rating.api"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
