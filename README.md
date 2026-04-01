# QueryNestForum (QueryNest)

An ASP.NET Core MVC forum-style application with questions, answers, threaded comments, voting, topic follows, notifications, moderation/reporting, and dashboards.

## Tech Stack

- ASP.NET Core MVC (net8.0)
- ASP.NET Core Identity (authentication/roles/lockout)
- Entity Framework Core (SQL Server)
- Bootstrap (UI)

## Solution Structure

- `QueryNest.Domain` (domain entities/enums)
- `QueryNest.Contract` (DTOs)
- `QueryNest.DAL` (EF Core DbContext + repositories + UnitOfWork)
- `QueryNest.BLL` (services/business logic)
- `QueryNest.Web` (MVC app)

## Quick Start

1. Configure a SQL Server connection string named `DefaultConnection` in `QueryNest.Web/appsettings.json`.
2. Build and run:

```bash
dotnet build .\QueryNestForum.sln -c Debug
dotnet run --project .\QueryNest.Web\QueryNest.Web.csproj
```

3. Open the app:
   - `https://localhost:<port>/Questions`

## Default Admin User

In Development, an admin is seeded by default.

In Production, admin seeding is disabled unless you enable it via configuration (`SeedAdmin:Enabled=true`).

- Email: `admin@querynest.com`
- Password: `Admin@123`

Roles used:

- `Admin`
- `Moderator`

### Option B: Promote an Existing User to Admin (Manual)

If you already registered a normal user and want to make them an admin without seeding, you can do it directly in SQL Server.

1. Find the user in Identity:

```sql
SELECT Id, UserName, Email
FROM AspNetUsers
WHERE Email = 'user@example.com';
```

2. Find the Admin role:

```sql
SELECT Id, Name
FROM AspNetRoles
WHERE Name = 'Admin';
```

3. Insert the user-role link (if it doesn’t exist yet):

```sql
IF NOT EXISTS (
    SELECT 1
    FROM AspNetUserRoles
    WHERE UserId = '<USER_ID>' AND RoleId = '<ROLE_ID>'
)
BEGIN
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES ('<USER_ID>', '<ROLE_ID>');
END
```

4. Re-login to the app (cookie needs to refresh role claims). You should now see the Admin menu in the navbar.

## Database Notes

This project currently uses `dbContext.Database.EnsureCreated()` on startup.

Some newer features add tables with conditional SQL at startup to keep existing databases working:

- `Notifications`
- `Reports`
- `TagFollows`
- Performance indexes (for common query paths)

If you prefer EF migrations, you can migrate this approach later, but the current startup behavior is designed to avoid runtime crashes when running against an existing DB.

## Caching (Optional Redis)

Caching uses `IDistributedCache`.

- If a connection string named `Redis` is configured, the app uses Redis.
- Otherwise it falls back to in-memory distributed cache.

## Deploy to IIS (Windows)

Prerequisites:

- Install the .NET 8 Hosting Bundle on the server (enables ASP.NET Core Module for IIS).
- SQL Server reachable from the server.

Publish:

```bash
dotnet publish .\QueryNest.Web\QueryNest.Web.csproj -c Release -o .\publish
```

IIS setup:

1. Create a folder like `C:\inetpub\querynest\` and copy the contents of `publish\` into it.
2. In IIS Manager:
   - Create a new Application Pool:
     - .NET CLR version: No Managed Code
     - Pipeline mode: Integrated
   - Create a new Website (or Application) pointing to `C:\inetpub\querynest\`
   - Assign the site to the new Application Pool
3. Give the App Pool identity write permission to:
   - `C:\inetpub\querynest\wwwroot\uploads\` (avatars/uploads)
   - Data protection keys folder (recommended)

Recommended production settings:

- Set `ASPNETCORE_ENVIRONMENT=Production`
- Configure `ConnectionStrings:DefaultConnection`
- Persist data-protection keys (prevents logouts after app restarts):
  - Set `DataProtection:KeysPath` to a writable folder, e.g. `C:\inetpub\querynest-keys\`
- Keep `SeedAdmin:Enabled=false` in Production and create admins through an internal process or a one-time secure bootstrap.

## Features

### Core Q&A

- Create/edit/delete questions
- Post answers
- Threaded comments (reply-to-comment)

### Voting + Reputation

- Upvote/downvote for questions, answers, and comments
- Prevent duplicate votes (same vote toggles off, opposite vote switches)
- Prevent self-voting
- Updates reputation by vote delta

### Search & Feed

- Questions feed with:
  - Search by text (`q=...`)
  - Sort (`sort=latest|trending`)
  - Filter by tag (`tagId=...`)
  - Pagination (`page=...`)

### Notifications

- Notifications for:
  - New answer on your question
  - New comment on your answer
  - New reply to your comment
- Navbar dropdown + unread badge (polling)

### Moderation & Reporting

- Report question/answer/comment
- Moderation dashboard for Admin/Moderator:
  - Review reports (approve/reject)
  - Ban/unban users using Identity lockout

### Tags & Topics

- Browse tags (topics)
- Follow/unfollow topics
- Tag management for Admin/Moderator

### Dashboard & Analytics

- User dashboard: personal stats + last-14-days activity bars
- Admin dashboard: totals + last-14-days activity bars + top tags/users

### Categories

- Category CRUD for Admin/Moderator
- Category picker on “Ask question”

## Rich Text / Highlighting (Description & Content)

Question description (and answer/comment rendering) supports a lightweight formatting syntax plus a toolbar in the question editor pages:

- Bold: `**text**`
- Italic: `_text_`
- Inline code: `` `code` ``
- Code block:

```
```
code
```
```

- Quote: `> quoted line`
- Highlight: `==highlighted==`

Rendering is done safely (HTML-encoded first), then formatted into HTML.

## UI Notes

- Login/Register pages use a custom “auth card” layout.
- Password fields include show/hide toggles and subtle input icons.
- Navbar is a Reddit-style layout with:
  - Center search
  - Suggestions dropdown (recent searches + top topics)
  - Avatar-based account dropdown

## Common Dev Issues

### “File is being used by another process” during build

If the web app is running (or Visual Studio is debugging), DLLs can be locked and `dotnet build` may fail.

Fix:

- Stop the running app/debug session, then rebuild.

## Routes (Quick Reference)

- Questions feed: `/Questions`
- Ask: `/Questions/Create`
- Tags: `/Tags`
- Dashboard: `/Dashboard`
- Notifications: `/Notifications`
- Moderation (Admin/Moderator): `/Moderation`
- Categories (Admin/Moderator): `/Categories/Manage`
