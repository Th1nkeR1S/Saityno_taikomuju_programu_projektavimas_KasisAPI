using System;
using System.Linq;
using KasisAPI.Data;
using KasisAPI.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using KasisAPI.Auth.Model;
using KasisAPI;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;



public static class Endpoints
{

    public static void AddTestApi(this WebApplication app)
{
    app.MapGet("/api/test", async (KasisDbContext dbContext) =>
    {
        try
        {
            var topics = await dbContext.Topics.ToListAsync();
            return Results.Ok(topics);
        }
        catch (Exception ex)
        {
            return Results.Problem("Database connection failed: " + ex.Message);
        }
    });
}
    public static void AddTopicsApi(this WebApplication app)
    {
        var topicsGroups = app.MapGroup("/api").AddFluentValidationAutoValidation();

        topicsGroups.MapGet("/topics", async (KasisDbContext dbContext) =>
        {
            var topics = await dbContext.Topics.ToListAsync();
            return topics.Select(topic => topic.ToDto());
        });

        topicsGroups.MapPost("/topics", [Authorize(Roles = ForumRoles.ForumUser)] async (CreateOrUpdateTopicDto dto, KasisDbContext dbContext,LinkGenerator linkGenerator, HttpContext httpContext) =>
       {
           var topic = new Topic { Title = dto.Title, Description = dto.Description, CreatedAt = DateTimeOffset.UtcNow,
              UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)};
           dbContext.Topics.Add(topic);
           await dbContext.SaveChangesAsync();

           return Results.Created($"api/topics/{topic.Id}", topic.ToDto());
       });

        topicsGroups.MapGet("/topics/{topicId}", async (int topicId, KasisDbContext dbContext) =>
        {
            var topic = await dbContext.Topics.FindAsync(topicId);
            if (topic == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(topic.ToDto());
        });

        topicsGroups.MapPut("/topics/{topicId}", [Authorize] async (CreateOrUpdateTopicDto dto, int topicId, HttpContext httpContext, KasisDbContext dbContext) =>
        {
            var topic = await dbContext.Topics.FindAsync(topicId);
            if (topic == null)
            {
                return Results.NotFound();
            }

            if (!httpContext.User.IsInRole(ForumRoles.Admin) &&
            httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != topic.UserId)
            {
            // NotFound()
            return Results.Forbid();
            }

            topic.Title = dto.Title;
            topic.Description = dto.Description;
            dbContext.Topics.Update(topic);
            await dbContext.SaveChangesAsync();

            return Results.Ok(topic.ToDto());
        });

        topicsGroups.MapDelete("/topics/{topicId}", async (int topicId, KasisDbContext dbContext) =>
        {
            var topic = await dbContext.Topics.FindAsync(topicId);
            if (topic == null)
            {
                return Results.NotFound();
            }

            dbContext.Topics.Remove(topic);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    public static void AddPostsApi(this WebApplication app)
    {
        var postsGroups = app.MapGroup("/api/topics/{topicId}").AddFluentValidationAutoValidation();

        postsGroups.MapGet("/posts", async (int topicId, KasisDbContext dbContext) =>
        {
            var topic = await dbContext.Topics.FindAsync(topicId);
            if (topic == null)
            {
                return Results.NotFound();
            }

            var posts = await dbContext.Posts.Where(post => post.Topic.Id == topicId).ToListAsync();
            return Results.Ok(posts.Select(post => post.ToDto()));
        });

        
postsGroups.MapPost("/posts", async (int topicId, CreateOrUpdatePostDto dto, KasisDbContext dbContext, HttpContext httpContext) =>
{
    var topic = await dbContext.Topics.FindAsync(topicId);
    if (topic == null)
    {
        return Results.NotFound();
    }

    var userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub); // Get UserId from the JWT
    var post = new Post { Title = dto.Title, Body = dto.Body, CreatedAt = DateTimeOffset.UtcNow, Topic = topic, UserId = userId };

    dbContext.Posts.Add(post);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/topics/{topicId}/posts/{post.Id}", post.ToDto());
});

        postsGroups.MapGet("/posts/{postId}", async (int topicId, int postId, KasisDbContext dbContext) =>
        {
            var posts = dbContext.Posts.Include(post => post.Topic);
            var post = await posts.FirstOrDefaultAsync(post => post.Id == postId && post.Topic.Id == topicId);
            if (post == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(post.ToDto());
        });

        postsGroups.MapPut("/posts/{postId}", async (int topicId, int postId, CreateOrUpdatePostDto dto, KasisDbContext dbContext) =>
        {
            var posts = dbContext.Posts.Include(post => post.Topic);
            var post = await posts.FirstOrDefaultAsync(post => post.Id == postId && post.Topic.Id == topicId);
            if (post == null)
            {
                return Results.NotFound();
            }

            post.Title = dto.Title;
            post.Body = dto.Body;
            dbContext.Posts.Update(post);
            await dbContext.SaveChangesAsync();

            return Results.Ok(post.ToDto());
        });

        postsGroups.MapDelete("/posts/{postId}", async (int topicId, int postId, KasisDbContext dbContext) =>
        {
            var posts = dbContext.Posts.Include(post => post.Topic);
            var post = await posts.FirstOrDefaultAsync(post => post.Id == postId && post.Topic.Id == topicId);
            if (post == null)
            {
                return Results.NotFound();
            }

            dbContext.Posts.Remove(post);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    public static void AddCommentsApi(this WebApplication app)
{
    var commentsGroups = app.MapGroup("/api/topics/{topicId}/posts/{postId}").AddFluentValidationAutoValidation();

    // Get comments for a post
    commentsGroups.MapGet("/comments", async (int topicId, int postId, KasisDbContext dbContext) =>
    {
        var post = await dbContext.Posts.Include(p => p.Topic).FirstOrDefaultAsync(p => p.Id == postId && p.Topic.Id == topicId);

        if (post == null)
        {
            return Results.NotFound();
        }

        var comments = await dbContext.Comments.Where(c => c.Post.Id == postId).ToListAsync();
        return Results.Ok(comments.Select(comment => comment.ToDto()));
    });

    // Create a new comment on a post
    commentsGroups.MapPost("/comments", async (int topicId, int postId, CreateOrUpdateCommentDto dto, KasisDbContext dbContext, HttpContext httpContext) =>
    {
        // Find the post associated with the topic
        var post = await dbContext.Posts.Include(p => p.Topic).FirstOrDefaultAsync(p => p.Id == postId && p.Topic.Id == topicId);
        if (post == null)
        {
            return Results.NotFound(); // Post not found for the given topicId
        }

        // Getting UserId from JWT (User is logged in)
        var userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        // Create and save the comment
        var comment = new Comment
        {
            Content = dto.Content,
            CreatedAt = DateTimeOffset.UtcNow,
            UserId = userId, // Assign the logged-in user's ID to the comment
            Post = post // Associate the comment with the correct post
        };

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync();

        // Return the created comment with its DTO representation
        return Results.Created($"/api/topics/{topicId}/posts/{postId}/comments/{comment.Id}", comment.ToDto());
    });

    // Get a specific comment by ID
    commentsGroups.MapGet("/comments/{commentId}", async (int topicId, int postId, int commentId, KasisDbContext dbContext) =>
    {
        var comment = await dbContext.Comments
            .Include(c => c.Post)
            .ThenInclude(p => p.Topic)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.Post.Id == postId && c.Post.Topic.Id == topicId);

        if (comment == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(comment.ToDto());
    });

    // Update a comment
    commentsGroups.MapPut("/comments/{commentId}", async (int topicId, int postId, int commentId, CreateOrUpdateCommentDto dto, KasisDbContext dbContext, HttpContext httpContext) =>
    {
        var comment = await dbContext.Comments
            .Include(c => c.Post)
            .ThenInclude(p => p.Topic)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.Post.Id == postId && c.Post.Topic.Id == topicId);

        if (comment == null)
        {
            return Results.NotFound();
        }

        // Ensure the user is the one who created the comment, or is an admin
        var userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (comment.UserId != userId && !httpContext.User.IsInRole(ForumRoles.Admin))
        {
            return Results.Forbid(); // User cannot edit comment if not the creator or admin
        }

        comment.Content = dto.Content;
        dbContext.Comments.Update(comment);
        await dbContext.SaveChangesAsync();

        return Results.Ok(comment.ToDto());
    });

    // Delete a comment
    commentsGroups.MapDelete("/comments/{commentId}", async (int topicId, int postId, int commentId, KasisDbContext dbContext, HttpContext httpContext) =>
    {
        var comment = await dbContext.Comments
            .Include(c => c.Post)
            .ThenInclude(p => p.Topic)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.Post.Id == postId && c.Post.Topic.Id == topicId);

        if (comment == null)
        {
            return Results.NotFound();
        }

        // Ensure the user is the one who created the comment, or is an admin
        var userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (comment.UserId != userId && !httpContext.User.IsInRole(ForumRoles.Admin))
        {
            return Results.Forbid(); // User cannot delete comment if not the creator or admin
        }

        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    });
}
}