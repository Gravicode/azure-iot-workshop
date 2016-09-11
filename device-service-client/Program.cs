using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace device_service_client
{
    public static class Program
    {
        private static string connectionString;

        private static void Main(string[] args)
        {
            string message;

            connectionString = GetAndValidateConnectionString(args.Length == 0 ? null : args[0]);

            try
            {
                serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
                serviceClient.OpenAsync().Wait();
                isConnected = true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred while opening connection to Azure IoT Hub (message: {ex.Message}).");
                Console.ResetColor();

            }

            #pragma warning disable 4014
            ReceiveFeedbackAsync();
            #pragma warning restore 4014

            Console.Write("Enter Device ID to send message to: ");
            var deviceId = Console.ReadLine();

            do
            {

                Console.Write("Enter message to be sent (\"quit\") to stop: ");
                message = Console.ReadLine();
                SendCloudToDeviceMessageAsync(deviceId, message).Wait();
            } while (message != "quit");

            CloseConnectionAsync().Wait();
        }

        private static string GetAndValidateConnectionString(string cliargument)
        {
            // TODO: input validation for iot hub connection string format to be added...
            string userInput;
            do
            {
                Console.Write("Enter a connection string to Azure IoT Hub: ");
                userInput = cliargument ?? Console.ReadLine();
                Console.WriteLine($"Connection string: {userInput}");

            } while (userInput == null); 
            return userInput;
        }

        public static async Task OpenConnectionAsync()
        {
            if (isConnected == false)
            { 
                try
                {
                    serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
                    ReceiveFeedbackAsync().Wait();
                    await serviceClient.OpenAsync();
                    Console.WriteLine($"Connection is now established to Azure IoT Hub...");
                    isConnected = true;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"An error occurred while opening connection to Azure IoT Hub (message: {ex.Message}).");
                    Console.ResetColor();
                    isConnected = false;
                }
            }
        }

        public static async Task CloseConnectionAsync()
        {
            if (isConnected)
            {
                try
                {
                    await serviceClient.CloseAsync();
                    Console.WriteLine($"Connection is now closed from Azure IoT Hub...");
                    isConnected = false;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"An error occurred while closing connection to Azure IoT Hub (message: {ex.Message}).");
                    Console.ResetColor();
                    isConnected = true;
                }
            }
        }

        private static async Task SendCloudToDeviceMessageAsync(string deviceId, string message)
        {
            if (isConnected)
            {
                var command = new Message(Encoding.ASCII.GetBytes(message));
                await serviceClient.SendAsync(deviceId, command);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Not connected to Azure IoT Hub.  Please make sure to connect before sending message(s)...");
                Console.ResetColor();
            }
        }

        private static async Task ReceiveFeedbackAsync()
        {
            var feedbackReceiver = serviceClient.GetFeedbackReceiver();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nReceiving C2D feedback from service...");
            Console.ResetColor();

            while (true)
            {
                var feedbackBatch = await feedbackReceiver.ReceiveAsync();
                if (feedbackBatch == null) continue;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Received feedback: {string.Join(", ", feedbackBatch.Records.Select(f => f.StatusCode))}");
                Console.ResetColor();

                await feedbackReceiver.CompleteAsync(feedbackBatch);
            }
        }

        private static bool isConnected;
        private static ServiceClient serviceClient;
    }
}
