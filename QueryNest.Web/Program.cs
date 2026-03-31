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
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<QueryNestDbContext>();
    dbContext.Database.EnsureCreated();
    EnsureNotificationsTable(dbContext);
    EnsureReportsTable(dbContext);
    EnsureTagFollowsTable(dbContext);

    RoleSeeder.Seed(scope.ServiceProvider).GetAwaiter().GetResult();

    var initialSeedService = scope.ServiceProvider.GetRequiredService<IInitialSeedService>();
    initialSeedService.SeedAdminAsync("admin@querynest.com", "Admin@123").GetAwaiter().GetResult();
}

static void EnsureNotificationsTable(QueryNestDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Notifications')
BEGIN
    CREATE TABLE [dbo].[Notifications](
        [NotificationId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [ActorUserId] INT NOT NULL,
        [QuestionId] INT NULL,
        [AnswerId] INT NULL,
        [CommentId] INT NULL,
        [Type] INT NOT NULL,
        [Message] NVARCHAR(500) NOT NULL,
        [IsRead] BIT NOT NULL CONSTRAINT [DF_Notifications_IsRead] DEFAULT(0),
        [CreatedAt] DATETIME2(3) NOT NULL,
        [ReadAt] DATETIME2(3) NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([NotificationId] ASC)
    );
    ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserId]);
    ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Users_ActorUserId] FOREIGN KEY([ActorUserId]) REFERENCES [dbo].[Users] ([UserId]);
    ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Questions_QuestionId] FOREIGN KEY([QuestionId]) REFERENCES [dbo].[Questions] ([QuestionId]);
    ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Answers_AnswerId] FOREIGN KEY([AnswerId]) REFERENCES [dbo].[Answers] ([AnswerId]);
    ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Comments_CommentId] FOREIGN KEY([CommentId]) REFERENCES [dbo].[Comments] ([CommentId]);
    CREATE INDEX [IX_Notifications_UserId_IsRead_CreatedAt] ON [dbo].[Notifications]([UserId],[IsRead],[CreatedAt]);
END");
}

static void EnsureReportsTable(QueryNestDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Reports')
BEGIN
    CREATE TABLE [dbo].[Reports](
        [ReportId] INT IDENTITY(1,1) NOT NULL,
        [ReporterUserId] INT NOT NULL,
        [TargetType] INT NOT NULL,
        [QuestionId] INT NULL,
        [AnswerId] INT NULL,
        [CommentId] INT NULL,
        [Reason] NVARCHAR(200) NOT NULL,
        [Details] NVARCHAR(1000) NULL,
        [Status] INT NOT NULL CONSTRAINT [DF_Reports_Status] DEFAULT(1),
        [ReviewedByUserId] INT NULL,
        [ReviewNote] NVARCHAR(1000) NULL,
        [CreatedAt] DATETIME2(3) NOT NULL,
        [ReviewedAt] DATETIME2(3) NULL,
        CONSTRAINT [PK_Reports] PRIMARY KEY CLUSTERED ([ReportId] ASC),
        CONSTRAINT [CK_Reports_ExactlyOneTarget] CHECK (
            ([QuestionId] IS NOT NULL AND [AnswerId] IS NULL AND [CommentId] IS NULL) OR
            ([QuestionId] IS NULL AND [AnswerId] IS NOT NULL AND [CommentId] IS NULL) OR
            ([QuestionId] IS NULL AND [AnswerId] IS NULL AND [CommentId] IS NOT NULL)
        )
    );
    ALTER TABLE [dbo].[Reports]  WITH CHECK ADD  CONSTRAINT [FK_Reports_Users_ReporterUserId] FOREIGN KEY([ReporterUserId]) REFERENCES [dbo].[Users] ([UserId]);
    ALTER TABLE [dbo].[Reports]  WITH CHECK ADD  CONSTRAINT [FK_Reports_Users_ReviewedByUserId] FOREIGN KEY([ReviewedByUserId]) REFERENCES [dbo].[Users] ([UserId]);
    ALTER TABLE [dbo].[Reports]  WITH CHECK ADD  CONSTRAINT [FK_Reports_Questions_QuestionId] FOREIGN KEY([QuestionId]) REFERENCES [dbo].[Questions] ([QuestionId]);
    ALTER TABLE [dbo].[Reports]  WITH CHECK ADD  CONSTRAINT [FK_Reports_Answers_AnswerId] FOREIGN KEY([AnswerId]) REFERENCES [dbo].[Answers] ([AnswerId]);
    ALTER TABLE [dbo].[Reports]  WITH CHECK ADD  CONSTRAINT [FK_Reports_Comments_CommentId] FOREIGN KEY([CommentId]) REFERENCES [dbo].[Comments] ([CommentId]);
    CREATE INDEX [IX_Reports_Status_CreatedAt] ON [dbo].[Reports]([Status],[CreatedAt]);
    CREATE UNIQUE INDEX [UX_Reports_Reporter_Question_Pending] ON [dbo].[Reports]([ReporterUserId],[QuestionId]) WHERE [QuestionId] IS NOT NULL AND [Status] = 1;
    CREATE UNIQUE INDEX [UX_Reports_Reporter_Answer_Pending] ON [dbo].[Reports]([ReporterUserId],[AnswerId]) WHERE [AnswerId] IS NOT NULL AND [Status] = 1;
    CREATE UNIQUE INDEX [UX_Reports_Reporter_Comment_Pending] ON [dbo].[Reports]([ReporterUserId],[CommentId]) WHERE [CommentId] IS NOT NULL AND [Status] = 1;
END");
}

static void EnsureTagFollowsTable(QueryNestDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'TagFollows')
BEGIN
    CREATE TABLE [dbo].[TagFollows](
        [UserId] INT NOT NULL,
        [TagId] INT NOT NULL,
        [CreatedAt] DATETIME2(3) NOT NULL,
        CONSTRAINT [PK_TagFollows] PRIMARY KEY CLUSTERED ([UserId] ASC, [TagId] ASC)
    );
    ALTER TABLE [dbo].[TagFollows]  WITH CHECK ADD  CONSTRAINT [FK_TagFollows_Users_UserId] FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[TagFollows]  WITH CHECK ADD  CONSTRAINT [FK_TagFollows_Tags_TagId] FOREIGN KEY([TagId]) REFERENCES [dbo].[Tags] ([TagId]) ON DELETE CASCADE;
    CREATE INDEX [IX_TagFollows_TagId] ON [dbo].[TagFollows]([TagId]);
END");
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
