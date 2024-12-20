using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KasisAPI.Auth.Model;

namespace KasisAPI.Data.Entities;

public class Post
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string Body { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    public Topic Topic { get; set; }

    [Required]
    public required string UserId { get; set; }

    public ForumUser User { get; set; }

    public PostDto ToDto()
    {
        return new PostDto(this.Topic?.Id ?? 0, Id, Title, Body, CreatedAt);
    }
}
