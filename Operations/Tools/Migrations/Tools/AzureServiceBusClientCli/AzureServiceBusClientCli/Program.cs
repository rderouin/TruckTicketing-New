using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureServiceBusClientCli
{
    /// <summary>
    /// 
    /// </summary>
    public static class Program
    {
        //private const string ServiceBusConnectionStringMasterData = "Endpoint=sb://zcac-sb-uat-public-t732ibds2apqm.servicebus.windows.net/;SharedAccessKeyName=SharedAppKey;SharedAccessKey=SwQ7ADiaYZp2HzlGK5oJbXY0LkyG7g8FuSoD3tMbxqQ=";
        private const string ServiceBusConnectionStringMasterData = "Endpoint=sb://zcac-sb-devint-public-s3skko4ttcwak.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=lpCQCvvRBKNGkNIpxcz0WIhcHuu9HuGkHSVm+ux8KS8=";
        private const string TopicBusinessEvents = "d365fo-business-events";

        //private const string ServiceBusConnectionStringTT = "Endpoint=sb://zcac-sb-uat-private-t732ibds2apqm.servicebus.windows.net/;SharedAccessKeyName=SharedAppKey;SharedAccessKey=XoaIxzhCXXQlQTwX+V5QewXqjhzeBTM494LEDAAeDck=";
        //private const string ServiceBusConnectionStringTT = "Endpoint=sb://zcac-sb-dev-integrations.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=FU1LqDtsiQNi8iqI8WGvbbyRzJnmOJLtMyzTgxx0uUc=";
        private const string ServiceBusConnectionStringTT = "Endpoint=sb://zcac-sb-devint-private-s3skko4ttcwak.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=WPjJnrI02lr1vTAQn4eRf8+iudl+WKKCOckkYEnQ/JI=";
        private const string TopicEntityUpdates = "enterprise-entity-updates";
        
        private const string SqlConnectionString = @"Data Source=AHQDsv-D365REP1.secure-energy.ca;Initial Catalog=TruckTicketDataMigration;User ID=TTDataMigration;Password=4wgn7oE4FFkpebCoFEh9Hp";
        private const string TestMsg = @"{
												""BusinessEventId"": ""HSInventSiteBusinessEvent"",
												""BusinessEventLegalEntity"": ""sesc"",
												""ContextRecordSubject"": """",
												""ControlNumber"": 5637181333,
												""CorrelationId"": ""{1BE0ED0D-6FE2-416A-AE3B-2C9DC7421F72}"",
												""Country"": ""CA"",
												""DataAreaId"": ""sesc"",
												""EventId"": ""1833EE10-D1DB-4AA3-AEAD-FF22434C7443"",
												""EventTime"": ""/Date(1672810954000)/"",
												""EventTimeIso8601"": ""2023-01-04T05:42:34.2046961Z"",
												""FacilityType"": ""FST"",
												""HSAdminEmail"": """",
												""HSCalendarId"": """",
												""HSFacilityRegulatoryCodePipeline"": """",
												""HSFacilityRegulatoryCodeTerminalling"": """",
												""HSFacilityRegulatoryCodeTreating"": """",
												""HSFacilityRegulatoryCodeWaste"": """",
												""HSFacilityRegulatoryCodeWater"": """",
												""HSFacilityType"": ""FST"",
												""HSInvoiceContact"": 0,
												""HSIsPipelineConnected"": ""No"",
												""HSIsRailConnected"": ""No"",
												""HSIsTareWeightSignRequired"": ""No"",
												""HSOvertonnageAnalyticalContract"": 0,
												""HSProductionAccountantContact"": 0,
												""HSTaxGroup"": """",
												""HSUWI"": ""TSTNR03UWI"",
												""InitiatingUserAADObjectId"": ""{5E2D28CB-A456-4512-B6AB-F3455D765946}"",
												""IsActive"": false,
												""MajorVersion"": 0,
												""MessageDate"": ""2023-01-04T05:42:34+00:00"",
												""MessageType"": ""Facility"",
												""MinorVersion"": 0,
												""Operation"": ""Update"",
												""ParentContextRecordSubjects"": [],
												""Province"": ""AB"",
												""SiteId"": ""TSTNR03"",
												""SiteName"": ""Test site NR 03"",
												""Source"": ""D365FO"",
												""SourceId"": ""{A5A1DC5B-62A5-47DD-859B-9FD7A5D4D22C}""
											}";

        private static CancellationToken token = new CancellationToken();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceBusConnectionString">Connection string for ASB not including the EntityPath</param>
        /// <param name="topicName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        //public static async Task main(string serviceBusConnectionString = ServiceBusConnectionStringMasterData, string topicName = TopicBusinessEvents, string message = TestMsg)
        public static async Task Main()
        {
            try
            {
                await ProcessMasterDataMessages();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
            }

            Console.ReadLine();
        }

        private static async Task ProcessMasterDataMessages()
        {
            var sqlClient = new SqlServerClient(SqlConnectionString);
            string LastMessageType = "LegalEntity";
            int delay = 5000;
            int calcDelay = 0;
            int msgTypeCount = 0;
            var Clients = new Dictionary<string, SBClient>()
            {
                {TopicBusinessEvents,  new SBClient(ServiceBusConnectionStringMasterData, TopicBusinessEvents)},
                { TopicEntityUpdates , new SBClient(ServiceBusConnectionStringTT, TopicEntityUpdates) }
            };         

            var totalMsgStr = sqlClient.GetMasterDataMessagesCount();
            var masterDataMessages = sqlClient.GetDataMessages();
            var counter = 0;

            Console.WriteLine("Batch size: " + masterDataMessages.Count().ToString());

            do
            {          
                if (masterDataMessages != null)
                {                                                                 
                    var messages = masterDataMessages.ToList();
                    var LastMessageTopic = TopicBusinessEvents;
                    foreach (var message in messages)
                    {
                        counter++;
                        
                        var msg = $"Sending MasterData message {counter} of {totalMsgStr}";
                        WriteProgress(msg, 0);
                        WriteProgress($"MessageType: {message.MessageType}             ", 0, 2);

                        if (LastMessageType != message.MessageType)
                        {
                            //calcDelay = (msgTypeCount > 100) ? (msgTypeCount * 100) : 0;
                            //await Task.Delay(delay + calcDelay);
                            if (LastMessageTopic == TopicBusinessEvents) {
                                WriteProgress("Press any key to continue.", 0, 3);
                                Console.ReadKey();
                                WriteProgress("                           ", 0, 3);
                            }
                            msgTypeCount = 0;
                        }


                        await Clients[message.TopicName].Send(message.Message, token); 
                        LastMessageType = message.MessageType;
                        LastMessageTopic = message.TopicName;
                    }

                    var entityIds = messages.Select(x => x.EntityId.ToString()).ToList();
                    sqlClient.MarkMessagesProcessed(entityIds);

                    masterDataMessages = sqlClient.GetDataMessages();
                }

            } while (masterDataMessages != null && masterDataMessages.Count() > 0);
        }

        private static void WriteProgress(string s, int x, int top=1)
        {
            int origRow = Console.CursorTop;
            int origCol = Console.CursorLeft;
            // Console.WindowWidth = 10;  // this works. 
            int width = Console.WindowWidth;
            x = x % width;
            try
            {
                Console.SetCursorPosition(x, top);
                Console.Write(s);
            }
            catch (ArgumentOutOfRangeException e)
            {

            }
            finally
            {
                try
                {
                    Console.SetCursorPosition(origCol, origRow);
                }
                catch (ArgumentOutOfRangeException e)
                {
                }
            }
        }

    }
}