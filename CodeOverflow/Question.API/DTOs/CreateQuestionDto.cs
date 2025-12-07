using Question.API.Validators;
using System.ComponentModel.DataAnnotations;

namespace Question.API.DTOs;

public record CreateQuestionDto(
    [Required] string Title,
    [Required] string Content,
    [Required] [TagListValidator(1,5)] List<string> Tags);
