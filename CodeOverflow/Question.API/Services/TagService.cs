using JasperFx.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Question.API.Data;

namespace Question.API.Services;

public class TagService(IMemoryCache memoryCache, QuestionDbContext dbContext)
{
    private const string CacheKey = "tags_cache_key";
    public async Task<List<Models.Tag>> GetTagsAsync()
    {
        return await memoryCache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);

            var tags = await dbContext.Tags.AsNoTracking().ToListAsync();

            return tags;
        }) ?? [];
    }

    public async Task<bool> AreTagsValidAsync(List<string> slugs)
    {
        var tags = await GetTagsAsync();
        var tagSet = tags.Select(t => t.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return slugs.All(slug => tagSet.Contains(slug));
    }
}
