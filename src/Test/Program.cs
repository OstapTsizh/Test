using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IMqttFactory factory = new MqttFactory();
            IMqttClient subscriberClient = factory.CreateMqttClient();
            IMqttClient publisherClient = factory.CreateMqttClient();

            var optionsSub = new MqttClientOptionsBuilder()
                 .WithClientId("subscriber")
                 .WithCleanSession(false)
                 .WithCredentials("test", "test")
                 .WithWebSocketServer("localhost:15675/ws")
                 .WithCommunicationTimeout(TimeSpan.FromMilliseconds(-1))
                 .Build();

            var optionsPub = new MqttClientOptionsBuilder()
                 .WithClientId("publisher")
                 .WithCleanSession(false)
                 .WithCredentials("test", "test")
                 .WithWebSocketServer("localhost:15675/ws")
                 .WithCommunicationTimeout(TimeSpan.FromMilliseconds(-1))
                 .Build();

            await subscriberClient.ConnectAsync(optionsSub);
            Console.WriteLine("SUBSCRIBER WAS CONNECTED");

            await publisherClient.ConnectAsync(optionsPub);
            Console.WriteLine("PUBLISHER WAS CONNECTED");

            publisherClient.UseDisconnectedHandler(async _ =>
            {
                await publisherClient.ConnectAsync(optionsPub);
                Console.WriteLine("Publisher Reconnected");
            });

            subscriberClient.UseDisconnectedHandler(async _ =>
            {
                await Task.Delay(5_000);
                await subscriberClient.ConnectAsync(optionsSub);
                await subscriberClient.SubscribeAsync(new TopicFilterBuilder()
                   .WithTopic("MyTopic")
                   .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                   .Build());
                Console.WriteLine("Subscriber Reconnected");
                Console.WriteLine();
            });

            await subscriberClient.SubscribeAsync(new TopicFilterBuilder()
                    .WithTopic("MyTopic")
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build());

            subscriberClient.UseApplicationMessageReceivedHandler(e =>
            {
                Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] [SUBSCRIBER] Received \"{Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}\" from topic \"{e.ApplicationMessage.Topic}\" ");
            });

            while (true)
            {
                var message = new MqttApplicationMessageBuilder()
                .WithTopic("MyTopic")
                .WithPayload($"Hello World [{DateTime.Now.ToLongTimeString()}]")
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

                Console.WriteLine();
                await Task.Delay(1_000);

                try
                {
                    var publishResult = await publisherClient.PublishAsync(message);

                    Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] [PUBLISHER] Sent \"{Encoding.UTF8.GetString(message.Payload)}\" to topic \"{message.Topic}\" ");

                    Console.WriteLine(publishResult.ReasonCode);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
