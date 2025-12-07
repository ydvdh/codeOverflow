using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Question.API.Data;
using Question.API.Models;

namespace Question.API.Controllers;

[Route("[controller]")]
[ApiController]
public class TagsController(QuestionDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Tag>>> GetTags(string sortBy)
    {
        return await dbContext.Tags.OrderBy(x => x.Name).ToListAsync();
    }
}
