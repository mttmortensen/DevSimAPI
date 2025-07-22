using OpenAI;
using OpenAI.Chat;
using System.Text;

namespace DevSimAPI.Services
{
    public class AiService
    {
        private readonly OpenAIClient _client;

        public AiService(IConfiguration config)
        {
            var apiKey = config["OpenAI:ApiKey"];
            _client = new OpenAIClient(apiKey);
        }
    }
}
