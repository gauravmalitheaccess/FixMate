namespace ErrorLogPrioritization.Api.Models
{

    public class AddMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class RunAgentRequest
    {
        public string AssistantId { get; set; } = string.Empty;
    }


}
