using System.ComponentModel.DataAnnotations;

namespace SampleRAG.Models;

public class ChatRequest
{
    [Required]
    [MinLength(3)]
    public string Question { get; set; } = string.Empty;
}