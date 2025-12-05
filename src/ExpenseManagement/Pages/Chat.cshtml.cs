using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class ChatModel : PageModel
{
    private readonly IConfiguration _configuration;
    public bool GenAIConfigured { get; set; }

    public ChatModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnGet()
    {
        var endpoint = _configuration["OpenAI:Endpoint"];
        GenAIConfigured = !string.IsNullOrEmpty(endpoint);
    }
}
