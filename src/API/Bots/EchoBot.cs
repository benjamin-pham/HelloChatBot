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
    private readonly ILogger<EchoBot> _logger;
    private readonly IStatePropertyAccessor<ChatHistory> _chatHistoryAccessor;
    private readonly ConversationState _conversationState;
    private readonly OpenAIPromptExecutionSettings _openAIPromptExecutionSettings = new()
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        Temperature = 0.7,
        TopP = 0.5,
        FrequencyPenalty = 0.0,
        PresencePenalty = 0.0,
    };
    public EchoBot(ConversationState conversationState, Kernel kernel, ILogger<EchoBot> logger)
    {
        _conversationState = conversationState;
        _kernel = kernel;
        _chatService = _kernel.GetRequiredService<IChatCompletionService>("geminiService");
        _logger = logger;
        _chatHistoryAccessor = conversationState.CreateProperty<ChatHistory>("ChatHistory");
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var userText = turnContext.Activity.Text ?? "";

        var chatHistory = await _chatHistoryAccessor.GetAsync(turnContext,
            () => InitContext(), cancellationToken);

        chatHistory.AddUserMessage(userText);

        // var sb = new StringBuilder();

        // await foreach (var messageChunk in _chatService.GetStreamingChatMessageContentsAsync(
        //             chatHistory,
        //             executionSettings: _openAIPromptExecutionSettings,
        //             kernel: _kernel))
        // {
        //     await turnContext.SendActivityAsync(MessageFactory.Text(messageChunk?.Content ?? ""), cancellationToken);
        //     sb.Append(messageChunk?.Content);
        // }

        var reply = await _chatService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: _openAIPromptExecutionSettings,
            _kernel);

        chatHistory.AddAssistantMessage(reply?.Content ?? "");
        await _chatHistoryAccessor.SetAsync(turnContext, chatHistory, cancellationToken);
        await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        await turnContext.SendActivityAsync(MessageFactory.Text(reply?.Content ?? ""), cancellationToken);
    }

    private ChatHistory InitContext()
    {
        ChatHistory chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("Bạn là một trợ lý về lĩnh vực công nghệ.");
        chatHistory.AddSystemMessage("Trả lời bằng giọng điệu thân thiện.");
        chatHistory.AddSystemMessage("Nên trả lời ngắn gọn, không lặp lại.");
        chatHistory.AddSystemMessage("Giới hạn câu trả lời tối đa 100 từ.");
        chatHistory.AddSystemMessage("Không nói về chính trị.");
        return chatHistory;
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var welcomeText = "Hello and welcome!";
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                ChatHistory chatHistory = InitContext();
                await _chatHistoryAccessor.SetAsync(turnContext, chatHistory, cancellationToken);
                await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
            }
        }
    }
}
