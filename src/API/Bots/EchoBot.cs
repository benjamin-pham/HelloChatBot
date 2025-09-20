using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace API.Bots;

public class EchoBot : ActivityHandler
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    OpenAIPromptExecutionSettings _openAIPromptExecutionSettings = new()
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        Temperature = 0.7,
        TopP = 0.5,
        FrequencyPenalty = 0.0,
        PresencePenalty = 0.0,
    };
    public EchoBot(Kernel kernel)
    {
        _kernel = kernel;
        _chatService = _kernel.GetRequiredService<IChatCompletionService>();
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var userText = turnContext.Activity.Text ?? "";

        var result = await _chatService.GetChatMessageContentAsync(
            userText,
            executionSettings: _openAIPromptExecutionSettings,
            _kernel);

        await turnContext.SendActivityAsync(MessageFactory.Text(result.Content), cancellationToken);
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var welcomeText = "Hello and welcome!";
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
            }
        }
    }
}
