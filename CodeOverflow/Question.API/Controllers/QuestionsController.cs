using Contracts;
using FastExpressionCompiler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Question.API.Data;
using Question.API.DTOs;
using Question.API.Services;
using System.Security.Claims;
using Wolverine;

namespace Question.API.Controllers;

[ApiController]
[Route("[controller]")]
public class QuestionsController(QuestionDbContext dbContext, IMessageBus messageBus, TagService tagService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Models.Question>>> GetQuestions(string tag)
    {
        var query = dbContext.Questions.AsQueryable();

        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(x => x.TagSlugs.Contains(tag));
        }

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Models.Question>> GetQuestion(string id)
    {
        var question = await dbContext.Questions.FindAsync(id);

        if (question is null) return NotFound();

        await dbContext.Questions.Where(q => q.Id == id).ExecuteUpdateAsync(setters => setters.SetProperty(q => q.ViewCount, q => q.ViewCount + 1));

        return question;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Models.Question>> CreateQuestion([FromBody] CreateQuestionDto questionDto)
    {
       if(! await tagService.AreTagsValidAsync(questionDto.Tags))
            return BadRequest($"Invalid tags");
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");

        if (userId is null || name is null) return BadRequest("cannot get user details");

        var question = new Models.Question
        {
            Title = questionDto.Title,
            Content = questionDto.Content,
            TagSlugs = questionDto.Tags,
            AskerId = userId,
            AskerDisplayName = name
        };

        dbContext.Questions.Add(question);
        await dbContext.SaveChangesAsync();

        await messageBus.PublishAsync(new QuestionCreated( question.Id, question.Title,question.Content, question.CreatedAt, question.TagSlugs));

        return Created($"/questions/{question.Id}", question);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateQuestion(string id, CreateQuestionDto questionDto)
    {
        var question = await dbContext.Questions.FindAsync(id);
        if (question is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Forbid();

        if (!await tagService.AreTagsValidAsync(questionDto.Tags))
            return BadRequest($"Invalid tags");

        question.Title = questionDto.Title;
        question.Content = questionDto.Content;
        question.TagSlugs = questionDto.Tags;
        question.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        await messageBus.PublishAsync(new QuestionUpdated(question.Id, question.Title, question.Content, question.TagSlugs.AsArray()));

        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteQuestion(string id)
    {
        var question = await dbContext.Questions.FindAsync(id);
        if (question is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Forbid();

        dbContext.Questions.Remove(question);
        await dbContext.SaveChangesAsync();

        await messageBus.PublishAsync(new QuestionDeleted(question.Id));

        return NoContent();
    }
}
