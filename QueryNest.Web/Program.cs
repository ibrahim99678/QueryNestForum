using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.BLL.Services;
using QueryNest.DAL;
using QueryNest.DAL.Data;
using QueryNest.DAL.Interfaces;
using QueryNest.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<QueryNestDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<QueryNestDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IInitialSeedService, InitialSeedService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IAnswerService, AnswerService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IVoteService, VoteService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<QueryNestDbContext>();
    dbContext.Database.EnsureCreated();
    EnsureNotificationsTable(dbContext);

    RoleSeeder.Seed(scope.ServiceProvider).GetAwaiter().GetResult();

    var initialSeedService = scope.ServiceProvider.GetRequiredService<IInitialSeedService>();
    initialSeedService.SeedAdminAsync("admin@querynest.com", "Admin@123").GetAwaiter().GetResult();
}

static void EnsureNotificationsTable(QueryNestDbContext dbContext)
{
    using var connection = dbContext.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
        connection.Open();
    }

    using var existsCommand = connection.CreateCommand();
    existsCommand.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Notifications'";
    var exists = existsCommand.ExecuteScalar();
    if (exists is not null)
    {
        return;
    }

    using var createCommand = connection.CreateCommand();
    createCommand.CommandText =
        "CREATE TABLE [dbo].[Notifications](" +
        " [NotificationId] INT IDENTITY(1,1) NOT NULL," +
        " [UserId] INT NOT NULL," +
        " [ActorUserId] INT NOT NULL," +
        " [QuestionId] INT NULL," +
        " [AnswerId] INT NULL," +
        " [CommentId] INT NULL," +
        " [Type] INT NOT NULL," +
        " [Message] NVARCHAR(500) NOT NULL," +
        " [IsRead] BIT NOT NULL CONSTRAINT [DF_Notifications_IsRead] DEFAULT(0)," +
        " [CreatedAt] DATETIME2(3) NOT NULL," +
        " [ReadAt] DATETIME2(3) NULL," +
        " CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([NotificationId] ASC)" +
        ");" +
        "ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserId]);" +
        "ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Users_ActorUserId] FOREIGN KEY([ActorUserId]) REFERENCES [dbo].[Users] ([UserId]);" +
        "ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Questions_QuestionId] FOREIGN KEY([QuestionId]) REFERENCES [dbo].[Questions] ([QuestionId]);" +
        "ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Answers_AnswerId] FOREIGN KEY([AnswerId]) REFERENCES [dbo].[Answers] ([AnswerId]);" +
        "ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Comments_CommentId] FOREIGN KEY([CommentId]) REFERENCES [dbo].[Comments] ([CommentId]);" +
        "CREATE INDEX [IX_Notifications_UserId_IsRead_CreatedAt] ON [dbo].[Notifications]([UserId],[IsRead],[CreatedAt]);";

    createCommand.ExecuteNonQuery();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
