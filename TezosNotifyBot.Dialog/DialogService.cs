using System.Globalization;
using Google.Cloud.Dialogflow.V2;

namespace TezosNotifyBot.Dialog
{
    public class DialogService
    {
        public SessionsClient Client { get; }
        public string ProjectId { get; }

        public DialogService(SessionsClient client, string projectId)
        {
            Client = client;
            ProjectId = projectId;
        }

        public (string action, string response) Intent(string sessionId, string queryText, CultureInfo queryCulture)
        {
            var session = new SessionName(ProjectId, sessionId);
            var response = Client.DetectIntent(new DetectIntentRequest()
            {
                SessionAsSessionName = session,
                QueryInput = new QueryInput()
                {
                    Text = new TextInput()
                    {
                        Text = queryText,
                        LanguageCode = queryCulture.ToString()
                    },
                }
            });
            
            return (response.QueryResult.Action, response.QueryResult.FulfillmentText);
        }
    }
}