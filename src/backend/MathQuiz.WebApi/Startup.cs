using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MathQuiz.AppLayer.Messages;
using MathQuiz.AppLayer.Services;
using MathQuiz.Configuration;
using MathQuiz.DataAccess.Storage;
using MathQuiz.EventBus.Abstractions;
using MathQuiz.EventBus.RabbitMq;
using MathQuiz.WebApi.Authentication;
using MathQuiz.WebApi.IntegrationEvents;
using MathQuiz.WebApi.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using RabbitMQ.Client;
using Swashbuckle.AspNetCore.Swagger;

namespace MathQuiz.WebApi
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private const string CorsLocalPolicy = "CorsLocalPolicy";

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting(setupAction =>
            {
                setupAction.LowercaseUrls = true;
            });

            var connectionStrings = _configuration.GetSection(nameof(ConnectionStrings)).Get<ConnectionStrings>();
            var authenticationSettings = _configuration.GetSection(nameof(AuthenticationSettings)).Get<AuthenticationSettings>();

            services.Configure<ConnectionStrings>(_configuration.GetSection(nameof(ConnectionStrings)));
            services.Configure<AuthenticationSettings>(_configuration.GetSection(nameof(AuthenticationSettings)));
            services.Configure<QuizSettings>(_configuration.GetSection(nameof(QuizSettings)));

            ConfigureAuthentication(services, authenticationSettings);
            ConfigureStorageServices(services, connectionStrings);
            ConfigureEventBusServices(services, connectionStrings);
            ConfigureSwaggerGen(services);

            services.AddAutoMapper();
            services.AddCors(options => options.AddPolicy(CorsLocalPolicy, builder =>
            {
                builder
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithOrigins("http://localhost:4200");
            }));

            services.AddScoped<IQuizDao, QuizDao>();
            services.AddScoped<IQuizService, QuizService>();
            services.AddScoped<IMathChallengeService, MathChallengeService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();

            services.AddSignalR();
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseCors(CorsLocalPolicy);
            }
            else
            {
                app.UseHsts();
            }

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                });
            }

            app.UseWebSockets();
            app.UseAuthentication();
            app.UseMvc();
            app.UseSignalR(options =>
            {
                options.MapHub<QuizHub>("/hubs/quiz");
            });

            ConfigureEventBusSubscriptions(app);
        }

        private void ConfigureAuthentication(IServiceCollection services, AuthenticationSettings authenticationSettings)
        {
            var signingKey = new SigningSymmetricKey(authenticationSettings.SigningSecurityKey
                                                     ?? throw new InvalidOperationException("Empty signin security key"));
            services.AddSingleton<IJwtSigningEncodingKey>(signingKey);
            var signingDecodingKey = (IJwtSigningDecodingKey)signingKey;

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwtBearerOptions =>
                {
                    jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingDecodingKey.GetKey(),
                        ValidateIssuer = true,
                        ValidIssuer = authenticationSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = authenticationSettings.Audience,
                        ValidateLifetime = true
                    };

                    jwtBearerOptions.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            if (!string.IsNullOrEmpty(accessToken) &&
                                (context.HttpContext.WebSockets.IsWebSocketRequest || context.Request.Headers["Accept"] == "text/event-stream"))
                            {
                                context.Token = context.Request.Query["access_token"];
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

        }

        private void ConfigureSwaggerGen(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "MathQuiz API", Version = "v1" });

                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>> {
                    { "Bearer", Enumerable.Empty<string>() },
                });

                c.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
            });
        }

        private void ConfigureStorageServices(IServiceCollection services, ConnectionStrings connectionStrings)
        {
            // ReSharper disable once RedundantTypeArgumentsOfMethod
            services.AddSingleton<IMongoDatabase>(sp =>
            {
                var conventionPack = new ConventionPack { new CamelCaseElementNameConvention(), new StringObjectIdIdGeneratorConvention() };
                ConventionRegistry.Register(nameof(conventionPack), conventionPack, t => true);

                var mongoConnectionString = connectionStrings.MongoDb
                                            ?? throw new InvalidOperationException("Empty MongoDb connection string");
                var client = new MongoClient(mongoConnectionString);
                return client.GetDatabase("Quiz");
            });
        }

        private void ConfigureEventBusServices(IServiceCollection services, ConnectionStrings connectionStrings)
        {
            services.AddSingleton<IRabbitMqPersistentConnection>(sp =>
            {
                var rabbitMqConnectionString = connectionStrings.RabbitMq
                                               ?? throw new InvalidOperationException("Empty RabbitMq connection strings");

                var logger = sp.GetRequiredService<ILogger<DefaultRabbitMqPersistentConnection>>();
                var factory = new ConnectionFactory
                {
                    HostName = rabbitMqConnectionString,
                    DispatchConsumersAsync = true
                };

                return new DefaultRabbitMqPersistentConnection(factory, logger);
            });
            services.AddSingleton<IEventBus, RabbitMqEventBus>();
            services.AddScoped<ChallengeUpdatedEventHandler>();
            services.AddScoped<ChallengeStartingEventHandler>();
            services.AddScoped<ChallengeFinishedEventHandler>();
            services.AddScoped<UserConnectedEventHandler>();
            services.AddScoped<UserDisconnectedEventHandler>();
            services.AddScoped<UserScoreUpdatedEventHandler>();
        }

        private void ConfigureEventBusSubscriptions(IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

            eventBus.Subscribe<ChallengeStarting, ChallengeStartingEventHandler>();
            eventBus.Subscribe<ChallengeUpdated, ChallengeUpdatedEventHandler>();
            eventBus.Subscribe<ChallengeFinished, ChallengeFinishedEventHandler>();
            eventBus.Subscribe<UserConnected, UserConnectedEventHandler>();
            eventBus.Subscribe<UserDisconnected, UserDisconnectedEventHandler>();
            eventBus.Subscribe<UserScoreUpdated, UserScoreUpdatedEventHandler>();
        }
    }
}