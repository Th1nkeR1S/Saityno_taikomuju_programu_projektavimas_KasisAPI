using System;
using System.Text;
using KasisAPI.Helpers;
using System.Threading.Tasks;
using KasisAPI.Data;
using KasisAPI.Auth;
using KasisAPI.Auth.Model;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Results;
using SharpGrip.FluentValidation.AutoValidation.Shared.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;


public class ProblemDetailsResultFactory : IFluentValidationAutoValidationResultFactory
{
    public IResult CreateResult(EndpointFilterInvocationContext context, ValidationResult validationResult)
    {
        var problemDetails = new HttpValidationProblemDetails(validationResult.ToValidationProblemErrors())
        {
            Type =  "https://tools.ietf.org/html/rfc4918#section-11.2",
            Title = "Unprocessable Entity",
            Status = 422
        };

        return Results.Problem(problemDetails);
    }
}


public record TopicDto(int Id, string Title, string Description, DateTimeOffset CreatedAt);

public record CreateOrUpdateTopicDto(string Title, string Description)
{
    public class CreateOrUpdateTopicDtoValidator : AbstractValidator<CreateOrUpdateTopicDto>
    {
        public CreateOrUpdateTopicDtoValidator()
        {
            RuleFor(x => x.Title).NotEmpty().Length(min:1, max:100);
            RuleFor(x => x.Description).NotEmpty().Length(min:1, max:1000);
        }
    }
}

public record PostDto(int TopicId, int Id, string Title, string Body, DateTimeOffset CreatedAt);

public record CreateOrUpdatePostDto(int TopicId, string Title, string Body)
{
    public class CreateOrUpdatePostDtoValidator : AbstractValidator<CreateOrUpdatePostDto>
    {
        public CreateOrUpdatePostDtoValidator()
        {
            RuleFor(x => x.Title).NotEmpty().Length(min:1, max:100);
            RuleFor(x => x.Body).NotEmpty().Length(min:1, max:10000);
        }
    }
}

public record CommentDto(int PostId, int Id, string Content, DateTimeOffset CreatedAt);

public record CreateOrUpdateCommentDto(int PostId, string Content)
{
    public class CreateOrUpdateCommentDtoValidator : AbstractValidator<CreateOrUpdateCommentDto>
    {
        public CreateOrUpdateCommentDtoValidator()
        {
            RuleFor(x => x.Content).NotEmpty().Length(min:1, max:10000);
        }
    }
}

public class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:3000").AllowAnyMethod().AllowAnyHeader();
        });
        });

        builder.Services.AddDbContext<KasisDbContext>();
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.AddFluentValidationAutoValidation(configuration =>
        {
            configuration.OverrideDefaultResultFactoryWith<ProblemDetailsResultFactory>();
        });
        builder.Services.AddTransient<JwtTokenService>();
        builder.Services.AddTransient<SessionService>();
        builder.Services.AddScoped<AuthSeeder>();

        builder.Services.AddIdentity<ForumUser, IdentityRole>()
            .AddEntityFrameworkStores<KasisDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters.ValidAudience = builder.Configuration["Jwt:ValidAudience"];
            options.TokenValidationParameters.ValidIssuer = builder.Configuration["Jwt:ValidIssuer"];
            options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]));
        });

        builder.Services.AddAuthorization();

        var app = builder.Build();
        using var scope = app.Services.CreateScope();
        app.UseCors();
        var dbContext = scope.ServiceProvider.GetRequiredService<KasisDbContext>();
        dbContext.Database.Migrate();

        var dbSeeder = scope.ServiceProvider.GetRequiredService<AuthSeeder>();
            await dbSeeder.SeedAsync();   

        app.AddAuthApi();

        app.AddTopicsApi();
        app.AddPostsApi();
        app.AddCommentsApi();
        
        app.UseAuthentication();
        app.UseAuthorization();

        app.Run();
        
    }
}
