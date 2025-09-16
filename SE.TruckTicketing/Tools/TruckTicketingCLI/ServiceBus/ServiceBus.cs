//using Azure.Messaging.ServiceBus;
//using Microsoft.Extensions.Configuration;
//using Newtonsoft.Json;
//using System;
//using System.IO;
//using System.Threading.Tasks;

//namespace TruckTicketingCLI.ServiceBus
//{
//    public class ServiceBus
//    {
//        private static readonly IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true).Build();

//        // The connection string to Service Bus namespace
//        private static readonly string ConnectionString = config.GetConnectionString("ServiceBus");

//        private static ServiceBusClient client;

//        internal static async Task SendTopic(string name, string file)
//        {
//            if (client == null)
//            {
//                client = new ServiceBusClient(ConnectionString);
//            }
//            ServiceBusSender sender = client.CreateSender(name);
//            string json = File.ReadAllText(file);
//            var message = new ServiceBusMessage(json);
//            try
//            {
//                await sender.SendMessageAsync(message);

//                Console.WriteLine("Message sent.");
//            }
//            catch (ServiceBusException e)
//            {
//                Console.WriteLine("Error: {0}", e);
//            }

//        }
//    }
//}


