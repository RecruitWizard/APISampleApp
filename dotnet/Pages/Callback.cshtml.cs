using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecruitWizard.AuthSample.Services;

namespace RecruitWizard.AuthSample.Pages;

public sealed class CallbackModel : PageModel
{
    private readonly StateStore _states;
    private readonly ILogger<CallbackModel> _logger;

    public CallbackModel(StateStore states, ILogger<CallbackModel> logger)
    {
        _states = states;
        _logger = logger;
    }

    public bool Ok { get; private set; }
    public string? Message { get; private set; }
    public string? Code { get; private set; }
    public string? State { get; private set; }

    public IActionResult OnGet(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        State = state;

        if (!string.IsNullOrEmpty(error))
        {
            Ok = false;
            var detail = string.IsNullOrEmpty(errorDescription) ? error : $"{error}: {errorDescription}";
            Message = $"Authorization denied ({detail})";
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Page();
        }

        if (string.IsNullOrEmpty(code))
        {
            Ok = false;
            Message = "Callback hit without a code parameter.";
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Page();
        }

        if (!_states.Consume(state))
        {
            Ok = false;
            Code = code;
            Message = "State mismatch or expired. Please start the login again from the main tab.";
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Page();
        }

        var preview = code.Length > 8 ? code[..8] : code;
        _logger.LogInformation("[auth] Received code ({Preview}...) – waiting for manual exchange.", preview);

        Ok = true;
        Code = code;
        return Page();
    }
}
