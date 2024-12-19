using System;
using System.Linq;
using KasisAPI.Data;
using KasisAPI.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using KasisAPI.Helpers;
using System.Security.Claims;
using KasisAPI.Auth.Model;
using KasisAPI.Auth;
using System.IdentityModel.Tokens.Jwt;
using KasisAPI;

public static class Endpoints
{
    public static void AddTopicsApi(this WebApplication app)
    {
        var topicsGroups = app.MapGroup("/api").AddFluentValidationAutoValidation();

        topicsGroups.MapGet("/topics", async (KasisDbContext dbContext) =>
        {
            var topics = await dbContext.Topics.ToListAsync();
            return topics.Select(topic => topic.ToDto());
        });

        topicsGroups.MapPost("/topics",[Authorize(Roles = ForumRoles.Admin)] async (CreateOrUpdateTopicDto dto, KasisDbContext dbContext, HttpContext httpContext) =>
        {
            var topic = new Topic { 
                Title = dto.Title,
                Description = dto.Description,
                CreatedAt = DateTimeOffset.UtcNow,
                UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) 
            };
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

        topicsGroups.MapPut("/topics/{topicId}",[Authorize(Roles = ForumRoles.Admin)] async (CreateOrUpdateTopicDto dto, int topicId,HttpContext httpContext, KasisDbContext dbContext) =>
        {
            var topic = await dbContext.Topics.FindAsync(topicId);
            if (topic == null)
            {
                return Results.NotFound();
            }

            if (!httpContext.User.IsInRole(ForumRoles.Admin) &&
            httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != topic.UserId)
            {
            return Results.Forbid();
            }

            topic.Title = dto.Description;
            topic.Description = dto.Description;
            dbContext.Topics.Update(topic);
            await dbContext.SaveChangesAsync();

            return Results.Ok(topic.ToDto());
        });

        topicsGroups.MapDelete("/topics/{topicId}",[Authorize(Roles = ForumRoles.Admin)] async (int topicId,HttpContext httpContext, KasisDbContext dbContext) =>
        {
            var topic = await dbContext.Topics.FindAsync(topicId);
            if (topic == null)
            {
                return Results.NotFound();
            }

            if (!httpContext.User.IsInRole(ForumRoles.Admin) &&
            httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != topic.UserId)
            {
            return Results.Forbid();
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

        postsGroups.MapPost("/posts",[Authorize(Roles = ForumRoles.ForumUser + "," + ForumRoles.Admin)] async (int topicId, CreateOrUpdatePostDto dto, KasisDbContext dbContext,HttpContext httpContext) =>
        {
            var topic = await dbContext.Topics.FindAsync(topicId);
            if (topic == null)
            {
                return Results.NotFound();
            }

            var post = new Post {
                Title = dto.Title,
                Body = dto.Body,
                CreatedAt = DateTimeOffset.UtcNow,
                Topic = topic,
                UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) 
            };
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

        postsGroups.MapPut("/posts/{postId}",[Authorize] async (int topicId, int postId, CreateOrUpdatePostDto dto,HttpContext httpContext, KasisDbContext dbContext) =>
        {
            var posts = dbContext.Posts.Include(post => post.Topic);
            var post = await posts.FirstOrDefaultAsync(post => post.Id == postId && post.Topic.Id == topicId);
            if (post == null)
            {
                return Results.NotFound();
            }

            if (!httpContext.User.IsInRole(ForumRoles.Admin) &&
                httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != post.UserId)
            {
                return Results.Forbid();
            }

            post.Title = dto.Title;
            post.Body = dto.Body;
            dbContext.Posts.Update(post);
            await dbContext.SaveChangesAsync();

            return Results.Ok(post.ToDto());
        });

        postsGroups.MapDelete("/posts/{postId}",[Authorize] async (int topicId, int postId,HttpContext httpContext, KasisDbContext dbContext) =>
        {
            var posts = dbContext.Posts.Include(post => post.Topic);
            var post = await posts.FirstOrDefaultAsync(post => post.Id == postId && post.Topic.Id == topicId);
            if (post == null)
            {
                return Results.NotFound();
            }

            if (!httpContext.User.IsInRole(ForumRoles.Admin) &&
                httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != post.UserId)
            {
                return Results.Forbid();
            }

            dbContext.Posts.Remove(post);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });
    }
    
    public static void AddCommentsApi(this WebApplication app)
    {
        var commentsGroups = app.MapGroup("/api/topics/{topicId}/posts/{postId}").AddFluentValidationAutoValidation();

        commentsGroups.MapGet("/comments", async (int topicId, int postId, KasisDbContext dbContext) =>
        {
            var posts = dbContext.Posts.Include(post => post.Topic);
            var post = await posts.FirstOrDefaultAsync(post => post.Id == postId && post.Topic.Id == topicId);

            if (post == null || post.Topic.Id != topicId)
            {
                return Results.NotFound();
            }

            var comments = await dbContext.Comments.Where(comment => comment.Post.Id == postId).ToListAsync();
            return Results.Ok(comments.Select(comment => comment.ToDto()));
        });

        commentsGroups.MapPost("/comments", async (int topicId, int postId, CreateOrUpdateCommentDto dto, KasisDbContext dbContext, HttpContext httpContext) =>
        {
            var posts = dbContext.Posts.Include(post => post.Topic);
            var post = await posts.FirstOrDefaultAsync(post => post.Id == postId && post.Topic.Id == topicId);

            if (post == null || post.Topic.Id != topicId)
            {
                return Results.NotFound();
            }

            var comment = new Comment
            {
                Content = dto.Content,
                CreatedAt = DateTimeOffset.UtcNow,
                Post = post,
                UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            };

            dbContext.Comments.Add(comment);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/api/topics/{topicId}/posts/{postId}/comments/{comment.Id}", comment.ToDto());
        });

        commentsGroups.MapGet("/comments/{commentId}", async (int topicId, int postId, int commentId, KasisDbContext dbContext) =>
        {
            var comment = await dbContext.Comments
                .Include(comment => comment.Post)
                .FirstOrDefaultAsync(comment => comment.Id == commentId && comment.Post.Id == postId && comment.Post.Topic.Id == topicId);

            if (comment == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(comment.ToDto());
        });

        commentsGroups.MapPut("/comments/{commentId}",[Authorize] async (int topicId, int postId, int commentId, CreateOrUpdateCommentDto dto, HttpContext httpContext, KasisDbContext dbContext) =>
        {
            var comment = await dbContext.Comments
                .Include(comment => comment.Post)
                .FirstOrDefaultAsync(comment => comment.Id == commentId && comment.Post.Id == postId && comment.Post.Topic.Id == topicId);

            if (comment == null)
            {
                return Results.NotFound();
            }

            if (!httpContext.User.IsInRole(ForumRoles.Admin) &&
                httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != comment.UserId)
            {
                return Results.Forbid();
            }

            comment.Content = dto.Content;
            dbContext.Comments.Update(comment);
            await dbContext.SaveChangesAsync();

            return Results.Ok(comment.ToDto());
        });

        commentsGroups.MapDelete("/comments/{commentId}",[Authorize] async (int topicId, int postId, int commentId,HttpContext httpContext, KasisDbContext dbContext) =>
        {
            var comment = await dbContext.Comments
                .Include(comment => comment.Post)
                .FirstOrDefaultAsync(comment => comment.Id == commentId && comment.Post.Id == postId && comment.Post.Topic.Id == topicId);

            if (comment == null)
            {
                return Results.NotFound();
            }

            if (!httpContext.User.IsInRole(ForumRoles.Admin) &&
                httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != comment.UserId)
            {
                return Results.Forbid();
            }

            dbContext.Comments.Remove(comment);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
