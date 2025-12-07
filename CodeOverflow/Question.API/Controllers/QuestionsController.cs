using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Question.API.Data;
using Question.API.DTOs;
using System.Security.Claims;

namespace Question.API.Controllers;

[ApiController]
[Route("[controller]")]
public class QuestionsController(QuestionDbContext dbContext) : ControllerBase
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
        var validTags = await dbContext.Tags.Where(t => questionDto.Tags.Contains(t.Slug)).ToListAsync();
        var missingTags = questionDto.Tags.Except(validTags.Select(t => t.Slug).ToList()).ToList();

        if (missingTags.Count != 0)
            return BadRequest($"Invalid tags: {string.Join(", ", missingTags)}");
        
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

        var validTags = await dbContext.Tags.Where(t => questionDto.Tags.Contains(t.Slug)).ToListAsync();
        var missingTags = questionDto.Tags.Except(validTags.Select(t => t.Slug).ToList()).ToList();

        if (missingTags.Count != 0)
            return BadRequest($"Invalid tags: {string.Join(", ", missingTags)}");

        question.Title = questionDto.Title;
        question.Content = questionDto.Content;
        question.TagSlugs = questionDto.Tags;
        question.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

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

        return NoContent();
    }
}
