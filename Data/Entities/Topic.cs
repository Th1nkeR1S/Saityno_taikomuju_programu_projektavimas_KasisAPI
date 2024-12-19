using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KasisAPI.Auth.Model;

namespace KasisAPI.Data.Entities;

public class Topic
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public  string UserId { get; set; }

    public ForumUser User { get; set; }

    public TopicDto ToDto()
    {
        return new TopicDto(Id, Title, Description, CreatedAt);
    }
}
