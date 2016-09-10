using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace device_identity
{
    class Program
    {

        private static RegistryManager registryManager;
        private static bool isConnected = false;

        private static void Main(string[] args)
        {
            string connectionString;
            int userInput;

            // get the connection string from arg or prompt user for input.
            if (args[0].Length > 0)
            {
                connectionString = args[0];
            }
            else
            {
                Console.Write("Enter a connection string to Azure IoT Hub: ");
                connectionString = Console.ReadLine();
            }

            registryManager = RegistryManager.CreateFromConnectionString(connectionString);

            OpenConnection().Wait();
            ListDevices().Wait();

            do
            {
                userInput = UserPrompt();
                switch (userInput)
                {
                    case 1:
                        ListDevices().Wait();
                        break;
                    case 2:
                        GetDeviceInformation().Wait();
                        break;
                    case 3:
                        AddDevice().Wait();
                        break;
                    case 4:
                        RemoveDevice().Wait();
                        break;
                    default:
                        break;
                }
            } while (userInput != 0);

            // close the connection if opened.
            if (!isConnected) return;
            try
            {
                registryManager.CloseAsync().Wait();
                isConnected = false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred while closing connection from Azure IoT Hub (message: {ex.Message}).");
                Console.ResetColor();
            }
        }
        
        public static int UserPrompt()
        {
            int result;
            Console.WriteLine("------------------------------");
            Console.WriteLine("1. List devices in the list: ");
            Console.WriteLine("2. Get device information: ");
            Console.WriteLine("3. Add a device: ");
            Console.WriteLine("4. Remove a device: ");
            Console.WriteLine("------------------------------");
            Console.Write("Enter your choice ('0' to exit the program): ");
            var input = Console.ReadLine();
            Console.WriteLine();
            return int.TryParse(input, out result) ? result : -100;
        }

        /// <summary>
        /// Open a connection to Azure IoT Hub.  
        /// </summary>
        /// <returns></returns>
        public static async Task OpenConnection()
        {
            if (isConnected == true) Console.WriteLine("Connection has been established already.");
            else
            {
                try
                {
                    await registryManager.OpenAsync();
                    Console.WriteLine($"Connection is now established to Azure IoT Hub...");
                    isConnected = true;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"An error occurred while opening connection to Azure IoT Hub (message: {ex.Message}).");
                    Console.ResetColor();
                }
            }
        }

        public static async Task ListDevices()
        {
            if (isConnected == true)
            {
                var devices = await registryManager.GetDevicesAsync(1000);
                var deviceList = devices as IList<Device> ?? devices.ToList();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{deviceList.Count<Device>()} device(s) found in the registry.");
                Console.ForegroundColor = ConsoleColor.Yellow;
                foreach (var device in deviceList)
                {
                    Console.WriteLine($"Device ID: {device.Id}");
                    Console.WriteLine($"Device Key: {device.Authentication.SymmetricKey.PrimaryKey}");
                    Console.WriteLine($"Device Status: {device.Status}");
                    Console.WriteLine();
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Not connected to Azure IoT Hub.");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        public static async Task AddDevice()
        {
            if (isConnected)
            {
                Console.Write("Enter device ID to be added: ");
                var deviceId = Console.ReadLine();
                var device = new Device(deviceId);
                try
                {
                    device = await registryManager.AddDeviceAsync(device);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"A device '{deviceId}' has been successfully added to the IoT Hub.");
                    Console.WriteLine($"Device ID: {device.Id}");
                    Console.WriteLine($"Generation ID: {device.GenerationId}");
                    Console.WriteLine($"Primary Key: {device.Authentication.SymmetricKey.PrimaryKey}");
                    Console.WriteLine($"Secondary Key: {device.Authentication.SymmetricKey.SecondaryKey}");
                    Console.ResetColor();
                }
                catch (DeviceAlreadyExistsException)
                {
                    device = await registryManager.GetDeviceAsync(deviceId);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"A device '{deviceId}' already exits in the registry.");
                    Console.WriteLine($"Device ID: {device.Id}");
                    Console.WriteLine($"Generation ID: {device.GenerationId}");
                    Console.WriteLine($"Primary Key: {device.Authentication.SymmetricKey.PrimaryKey}");
                    Console.WriteLine($"Secondary Key: {device.Authentication.SymmetricKey.SecondaryKey}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"An error while adding a device to the registry (message: {ex.Message}).");
                    Console.ResetColor();
                }
            }
        }

        public static async Task RemoveDevice()
        {
            if (isConnected)
            {
                Console.Write("Enter a device ID to be removed from the IoT Hub: ");
                var deviceId = Console.ReadLine();

                try
                {
                    await registryManager.RemoveDeviceAsync(deviceId);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{deviceId} was successfully removed from the IoT hub registry.");
                    Console.WriteLine();
                    Console.ResetColor();
                    await ListDevices();
                }
                catch (DeviceNotFoundException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{deviceId} was not found in the IoT hub registry.");
                    Console.ResetColor();
                }
            }

        }

        public static async Task GetDeviceInformation()
        {
            if (isConnected)
            {
                Console.Write("Enter a device ID to retrive from IoT Hub registry: ");
                var deviceId = Console.ReadLine();

                try
                {
                    var device = await registryManager.GetDeviceAsync(deviceId);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Device detail for:  '{deviceId}' ");
                    Console.WriteLine($"\tDevice ID: {device.Id}");
                    Console.WriteLine($"\tEtag: {device.ETag}");
                    Console.WriteLine($"\tGeneration ID: {device.GenerationId}");
                    Console.WriteLine($"\tConnection state: {device.ConnectionState}");
                    Console.WriteLine($"\tStatus reason: {device.StatusReason}");
                    Console.WriteLine($"\tC2D message count: {device.CloudToDeviceMessageCount}");
                    Console.WriteLine($"\tPrimary Key: {device.Authentication.SymmetricKey.PrimaryKey}");
                    Console.WriteLine($"\tSecondary Key: {device.Authentication.SymmetricKey.SecondaryKey}");
                    Console.ResetColor();

                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("An error occurred while retrieving device information from the IoT hub registry.  Make sure that the device exists in the registry.");
                    Console.ResetColor();

                }

            }
            else {}
        }
    }
}