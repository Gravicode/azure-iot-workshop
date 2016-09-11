using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace device_simulator
{
    class Program
    {
        private static void Main(string[] args)
        {
            int userInput;
            PrepareDeviceConnectionString(args.Length == 0 ? null : args[0]);
            
            do
            {
                userInput = UserPrompt();

                switch (userInput)
                {
                    // opens a device connection to iot hub.
                    case 1: Open().Wait(); break;

                    // sends a telemetry once to the iot hub.  if it is a device for Remote Monitoring then it once sends deviceInfo to enable the device.
                    case 2: Send().Wait(); break;
                    // sends given number of telemetries at given interval.  lazy coding here - make sure you check if inputs are integer.
                    case 3: 
                        Console.Write("Please enter number of telemetries to be send: ");
                        var number = Convert.ToInt32(Console.ReadLine());

                        Console.Write("Please enter interval between telemetries (in seconds): ");
                        var interval = Convert.ToInt32(Console.ReadLine());

                        for (var i = 0; i < number; i++)
                        {
                            Send().Wait();
                            Thread.Sleep(interval*1000);
                        }
                        break;
                    // sets to receiving mode (could use Task.Run instead)  
                    case 4: Receive().Wait(); break;
                    // closes and disposes the opened connection / deviceClient 
                    case 5: Close().Wait(); break;
                    // prompts to re-enter device connection string
                    case 6: PrepareDeviceConnectionString(null); break;
                    // invalid entry (other than integer) detected
                    case -100: Console.WriteLine("Invalid option..."); break;
                    default: break;
                }

            } while (userInput != 0);
        }

        private static int UserPrompt()
        {
            int result;

            var connectionStatus = isConnected ? "Connected" : "Disconnected";
            var listeningStatus = isListening ? "Listening" : "Not Listening";

            Console.WriteLine("------------------------------");
            Console.WriteLine("1. Connect to IoT Hub: ");
            Console.WriteLine("2. Send a telemetry: ");
            Console.WriteLine("3. Send multiple telemetries: ");
            Console.WriteLine("4. Start listening for commands: ");
            Console.WriteLine("5. Disconnect from IoT hub: ");
            Console.WriteLine("6. Reset device connection string: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Connection Status: {connectionStatus}");
            Console.WriteLine($"Listening Status: {listeningStatus}");
            Console.ResetColor();
            Console.WriteLine("------------------------------");
            Console.Write("Enter your choice ('0' to exit the program): ");
            var input = Console.ReadLine();
            Console.WriteLine();
            return int.TryParse(input, out result) ? result : -100;
        }

        private static void PrepareDeviceConnectionString(string argument)
        {
            Console.Write("Enter a connection string for this device: ");
            deviceConnectionString = argument ?? Console.ReadLine();
            Console.WriteLine($"Connection string: {deviceConnectionString}");

            Console.Write("Is this RM device ('y' or 'n')? ");
            var input = Console.ReadLine();
            if (input != null) isRemoteMonitoringDevice = (input.ToUpper() == "Y");
            else isRemoteMonitoringDevice = false;

            try
            {
                
                var properties = deviceConnectionString?.Split(';');
                hostName = properties?[0].Split('=')[1];
                deviceId = properties?[1].Split('=')[1];

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Host Name: {hostName}");
                Console.WriteLine($"Device Id: {deviceId}");
                Console.ResetColor();
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An error occurred while preparing a device connection string...");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// simulates sensor readings.  if this is for Remote Monitoring and message count is 0 then the device meta data is sent to the hub.
        /// </summary>
        /// <returns></returns>
        public static string Read()
        {
            var metadata = new {
                IsSimulatedDevice = false,
                Version = "1.0",
                ObjectType = "DeviceInfo",
                DeviceProperties = new {
                    DeviceID = deviceId,
                    HubEnabledState = true,
                    CreatedTime = DateTime.UtcNow,
                    DeviceState = "normal",
                    UpdatedTime = DateTime.UtcNow,
                    Manufacturer = "Micrsoft Global Black-belt",
                    ModelNumber = "MD-996",
                    SerialNumber = "SER5102",
                    FirmwareVersion = "1.96",
                    Platform = "Plat-99",
                    Processor = "i3-7340",
                    InstalledRAM = "128 MB",
                    Latitude = 47.659159,
                    Longitude = -122.141515
                },
                Commands = new[] {
                    new {
                        Name = "ControlTelemetry",
                        Parameters = new[] {
                            new {
                                Name = "LED",
                                Type = "string"
                            },
                            new {
                                Name = "MSG_CONTROL",
                                Type = "string"
                            }
                        }
                    }
                }
            };

            var sensordata = new {
                DeviceID = deviceId,
                TimeStamp = DateTime.Now.ToString(CultureInfo.CurrentCulture),
                Temperature = 18.0*random.NextDouble() + 25.0,
                Humidity = 55.0*random.NextDouble() + 80.0,
                WindSpeed = 20*random.NextDouble() + 35.0,
                Pressure = 95*random.NextDouble() + 125.0,
                Latitude = 47.659159,
                Longitude = -122.141515
            };
            var telemetry = (messageCount < 1 && isRemoteMonitoringDevice) ? JsonConvert.SerializeObject(metadata) : JsonConvert.SerializeObject(sensordata);
            return telemetry;
        }

        public static async Task Open()
        {
            if (isConnected == false)
            {
                try
                {
                    deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString);
                    await deviceClient.OpenAsync();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{deviceId} is connected to Iot hub: {hostName}.");
                    Console.ResetColor();
                    isConnected = true;
                }
                catch (Exception ex)
                {

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"An error occurred while opening connection to IoT hub. Possible that the connection string is malformed (message: {ex.Message}).");
                    Console.ResetColor();
                    isConnected = false;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{deviceId} is already connected to IoT hub.");
                Console.ResetColor();
            }
        }

        public static async Task Close()
        {
            if (isConnected)
            {
                try
                {
                    await deviceClient.CloseAsync();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{deviceId} is now disconnected from IoT Hub.  Please re-connect to send telemetries.");
                    Console.ResetColor();
                    isConnected = false;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"An error occurred while closing connection from IoT hub (message: {ex.Message}).");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"The connection is already closed.");
                Console.ResetColor();
            }
        }

        public static async Task Send()
        {
            try
            {
                var sensordata = Read();
                var message = new Message(Encoding.UTF8.GetBytes(sensordata));
                await deviceClient.SendEventAsync(message);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{sensordata.Length} byte(s) sent: {sensordata}");
                Console.ResetColor();
                messageCount = messageCount + 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred while sendong telemetry to IoT hub (message: {ex.Message}");
                Console.ResetColor();
            }
        }

        public static async Task Receive()
        {
            if (isConnected)
            {
                if (isListening)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"The device {deviceId} is already listending to message(s) from hub...");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine($"Listening to incomming messages from {hostName}");
                    string msg_control = null;

                    while (msg_control != "STOP") 
                    {
                        try
                        {
                            var received = await deviceClient.ReceiveAsync();
                            isListening = true;
                            if (received == null) continue;
                            await deviceClient.CompleteAsync(received);

                            var message = Encoding.ASCII.GetString(received.GetBytes());
                            var cmd = JsonConvert.DeserializeObject<Command>(message);

                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine($"Received message ({message.Length} byte(s)): {message}");

                            var led_control = cmd.Parameters.LED;
                            if (cmd.Parameters.MSG_CONTROL == "STOP")
                            {
                                msg_control = "STOP";
                                isListening = false;
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.WriteLine("STOP message has arrived.  Listening has stopped.");
                                Console.ResetColor();
                                break;
                            }

                            // a bit of control logic for fun
                            switch (led_control)
                            {
                                case "RED":
                                    Console.BackgroundColor = ConsoleColor.Red;
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.WriteLine("RED LED turned on!!");
                                    Console.ResetColor();
                                    break;
                                case "GREEN":
                                    
                                    Console.BackgroundColor = ConsoleColor.Green;
                                    Console.ForegroundColor = ConsoleColor.Black;
                                    Console.WriteLine("Green LED turned on!!");
                                    Console.ResetColor();
                                    break;
                                case "YELLOW":
                                    Console.BackgroundColor = ConsoleColor.Yellow;
                                    Console.ForegroundColor = ConsoleColor.Black;
                                    Console.WriteLine("Yellow LED turned on!!");
                                    Console.ResetColor();
                                    break;
                            }

                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"An error occurred while reading telemetry (commands) from IoT hub (message: {ex.Message}).");
                            Console.ResetColor();
                        }
                    }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"The device {deviceId} is not connected to IoT hub.  Connect before receiving messsage(s).");
                Console.ResetColor();
            }
        }

        private static string deviceId;
        private static string hostName;
        private static DeviceClient deviceClient;
        private static readonly Random random = new Random();
        private static bool isConnected = false;
        private static bool isListening = false;
        private static bool isRemoteMonitoringDevice;
        private static int messageCount = 0;
        private static string deviceConnectionString;

        private class Parameters
        {
            public string LED { get; set; }
            public string MSG_CONTROL { get; set; }
        }
        private class Command
        {
            public string Name { get; set; }
            public string MessageId { get; set; }
            public string CreatedTime { get; set; }
            public string UpdatedTime { get; set; }
            public object Result { get; set; }
            public object ErrorMessage { get; set; }
            public Parameters Parameters { get; set; }
        }
    }
}
