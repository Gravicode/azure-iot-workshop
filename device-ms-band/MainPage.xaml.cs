using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Band;
using Microsoft.Band.Sensors;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Windows.Devices.Geolocation;
using Windows.Storage;
using static Windows.ApplicationModel.Core.CoreApplication;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace device_ms_band
{
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Main entry for the app
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            this.InitializeBand();
            this.InitializeGeoLocator();
            this.InitializeAzureIoTHub();
        }

        private void InitializeSensors()
        {

            band.SensorManager.HeartRate.ReadingChanged += HeartRate_ReadingChanged;
            band.SensorManager.Distance.ReadingChanged += Distance_ReadingChanged;
            band.SensorManager.Calories.ReadingChanged += Calories_ReadingChanged;
            band.SensorManager.Contact.ReadingChanged += Contact_ReadingChanged;
            band.SensorManager.UV.ReadingChanged += UV_ReadingChanged;
            band.SensorManager.Barometer.ReadingChanged += Barometer_ReadingChanged;
            band.SensorManager.SkinTemperature.ReadingChanged += SkinTemperature_ReadingChanged;
            band.SensorManager.Pedometer.ReadingChanged += Pedometer_ReadingChanged;
            band.SensorManager.AmbientLight.ReadingChanged += AmbientLight_ReadingChanged;
        }

        private async void InitializeUserConsent()
        {
            if (band.SensorManager.HeartRate.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await band.SensorManager.HeartRate.RequestUserConsentAsync();
            }

            if (band.SensorManager.Distance.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await band.SensorManager.Distance.RequestUserConsentAsync();
            }

            if (band.SensorManager.Calories.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await band.SensorManager.Calories.RequestUserConsentAsync();
            }

            if (band.SensorManager.Contact.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await band.SensorManager.Contact.RequestUserConsentAsync();
            }

            if (band.SensorManager.UV.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await band.SensorManager.UV.RequestUserConsentAsync();
            }

            // Barometer
            if (band.SensorManager.Barometer.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await band.SensorManager.Barometer.RequestUserConsentAsync();
            }

            // Skin Temperature
            if (band.SensorManager.SkinTemperature.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await band.SensorManager.SkinTemperature.RequestUserConsentAsync();
            }

            // Pedometer
            if (band.SensorManager.Pedometer.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await band.SensorManager.Pedometer.RequestUserConsentAsync();
            }

            // Light Sensor
            if (band.SensorManager.AmbientLight.GetCurrentUserConsent() != UserConsent.Granted)
            {
                await band.SensorManager.AmbientLight.RequestUserConsentAsync();
            }
        }

        private async void InitializeAzureIoTHub()
        {
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(ApplicationData.Current.LocalSettings.Values["DeviceConnectionString"].ToString());
                await deviceClient.OpenAsync();
                isConnected = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("An error occurred while connecting to Azure IoT Hub...");
            }
        }

        private void UpdateLocationData(Geoposition position)
        {
            if (position != null)
            {
                latitude = position.Coordinate.Point.Position.Latitude;
                longitude = position.Coordinate.Point.Position.Longitude;
            }
            else
            {
                latitude = 0.00;
                longitude = 0.00;
            }

        }

        private async void InitializeGeoLocator()
        {
            this.locationAccessStatus = await Geolocator.RequestAccessAsync();
            switch (this.locationAccessStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    Geolocator geolocator = new Geolocator { DesiredAccuracyInMeters = 1, ReportInterval = 1000 };
                    geolocator.StatusChanged += Geolocator_StatusChanged; ;
                    geolocator.PositionChanged += Geolocator_PositionChanged;
                    // Carry out the operation.
                    var pos = await geolocator.GetGeopositionAsync();
                    UpdateLocationData(pos);
                    break;

                case GeolocationAccessStatus.Denied:
                    UpdateLocationData(null);
                    break;

                case GeolocationAccessStatus.Unspecified:
                    UpdateLocationData(null);
                    break;
            }
        }

        private async void Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            longitude = args.Position.Coordinate.Point.Position.Longitude;
            latitude = args.Position.Coordinate.Point.Position.Latitude;

            var locationData = new
            {
                Longitude = longitude,
                Latitude = latitude,
                TimeStamp = DateTime.Now.ToString(CultureInfo.CurrentCulture)
            };

            var bandTelemetry = new
            {
                SensorType = "Location",
                Data = locationData
            };

            await MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                mapCurrentLocation.Center = new Geopoint(new BasicGeoposition()
                {
                    Latitude = this.latitude,
                    Longitude = this.longitude
                });
            });

            var telemetry = JsonConvert.SerializeObject(bandTelemetry);
            SendTelemetry(telemetry);
        }

        private void Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {

        }

        private async void InitializeBand()
        {
            try
            {
                pairedBands = await BandClientManager.Instance.GetBandsAsync();
                band = await BandClientManager.Instance.ConnectAsync(pairedBands[0]);
                firmwareVersion = await band.GetFirmwareVersionAsync();
                hardwareVersion = await band.GetHardwareVersionAsync();

                txtFirmwareVersion.Text = firmwareVersion;
                txtHardwareVersion.Text = hardwareVersion;

                InitializeUserConsent();
                InitializeSensors();
                isReady = true;
            }
            catch (BandException ex)
            {
                var dialog = new Windows.UI.Popups.MessageDialog("A critical error has occurred while initializing Microsoft Band device (message: " + ex.Message + ".)", "Critical Error...");
                await dialog.ShowAsync();
            }
        }

        private async void SendTelemetry(string telemetry)
        {
            var message = new Message(Encoding.UTF8.GetBytes(telemetry));
            try
            {
                await deviceClient.SendEventAsync(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while sending a telemetry to Azure IoT Hub (message: {ex.Message}).");
            }
        }
        private void Contact_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandContactReading> e)
        {
            var bandTelemetry = new
            {
                SensorType = "Contact",
                Data = e.SensorReading
            };
            var telemetry = JsonConvert.SerializeObject(bandTelemetry);
            SendTelemetry(telemetry);
            bandStatus = e.SensorReading.State == BandContactState.Worn ? "Worn" : "Not Worn";
            UpdateUserInterface();
        }

        private void Calories_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandCaloriesReading> e)
        {
            var bandTelemetry = new
            {
                SensorType = "Calories",
                Data = e.SensorReading
            };
            var telemetry = JsonConvert.SerializeObject(bandTelemetry);
            SendTelemetry(telemetry);
            totalCaloriesBurned = e.SensorReading.Calories.ToString();
            todayCaloriesBurned = e.SensorReading.CaloriesToday.ToString();
            UpdateUserInterface();
        }

        private void Distance_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandDistanceReading> e)
        {
            var bandTelemetry = new
            {
                SensorType = "Distance",
                Data = e.SensorReading
            };

            var telemetry = JsonConvert.SerializeObject(bandTelemetry);
            SendTelemetry(telemetry);
            currentMotion = e.SensorReading.CurrentMotion.ToString();
            todayDistance = e.SensorReading.DistanceToday.ToString();
            totalDistance = e.SensorReading.TotalDistance.ToString();
            currentSpeed = e.SensorReading.Speed.ToString(CultureInfo.CurrentCulture);
            currentPace = e.SensorReading.Pace.ToString(CultureInfo.CurrentCulture);
            UpdateUserInterface();
        }

        private void HeartRate_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandHeartRateReading> e)
        {
            var bandTelemetry = new
            {
                SensorType = "Heartbeat",
                Data = e.SensorReading
            };
            var telemetry = JsonConvert.SerializeObject(bandTelemetry);
            SendTelemetry(telemetry);

            currentHeartRate = e.SensorReading.HeartRate.ToString();
            currentHeartRateQuality = e.SensorReading.Quality == HeartRateQuality.Locked ? "Locked" : "Acquiring";
            UpdateUserInterface();
        }

        private void AmbientLight_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandAmbientLightReading> e)
        {
            var bandTelemetry = new
            {
                SensorType = "AmbientLight",
                Data = e.SensorReading
            };
            var telemetry = JsonConvert.SerializeObject(bandTelemetry);
            SendTelemetry(telemetry);
            currentAmbientLight = e.SensorReading.Brightness.ToString();
            UpdateUserInterface();
        }

        private void Pedometer_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandPedometerReading> e)
        {
            var bandTelemetry = new
            {
                SensorType = "Pedometer",
                Data = e.SensorReading
            };
            var telemetry = JsonConvert.SerializeObject(bandTelemetry);
            SendTelemetry(telemetry);
            stepsToday = e.SensorReading.StepsToday.ToString();
            stepsTotal = e.SensorReading.TotalSteps.ToString();
            UpdateUserInterface();
        }

        private void SkinTemperature_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandSkinTemperatureReading> e)
        {
            var bandTelemetry = new
            {
                SensorType = "SkinTemperature",
                Data = e.SensorReading
            };
            var telemetry = JsonConvert.SerializeObject(bandTelemetry);
            SendTelemetry(telemetry);
            currentSkinTemperature = e.SensorReading.Temperature.ToString(CultureInfo.CurrentCulture);
            UpdateUserInterface();
        }

        private void Barometer_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandBarometerReading> e)
        {
            var bandTelemetry = new
            {
                SensorType = "Barometer",
                Data = e.SensorReading
            };

            var telemetry = JsonConvert.SerializeObject(bandTelemetry);
            Debug.WriteLine(telemetry);
            SendTelemetry(telemetry);

            currentAirPressure = e.SensorReading.AirPressure.ToString(CultureInfo.CurrentCulture);
            currentAirTemperature = e.SensorReading.Temperature.ToString(CultureInfo.CurrentCulture);
            UpdateUserInterface();
        }

        private void UV_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandUVReading> e)
        {
            var bandTelemetry = new
            {
                SensorType = "UV",
                Data = e.SensorReading
            };
            var telemetry = JsonConvert.SerializeObject(bandTelemetry);
            SendTelemetry(telemetry);
            uvExposureToday = e.SensorReading.ExposureToday.ToString(CultureInfo.CurrentCulture);
            currentUVLevel = e.SensorReading.IndexLevel.ToString();
            UpdateUserInterface();
        }

        private async void StopAllSensors()
        {
            try
            {
                await band.SensorManager.HeartRate.StopReadingsAsync();
                await band.SensorManager.Distance.StopReadingsAsync();
                await band.SensorManager.Calories.StopReadingsAsync();
                await band.SensorManager.Contact.StopReadingsAsync();
                await band.SensorManager.UV.StopReadingsAsync();
                await band.SensorManager.AmbientLight.StopReadingsAsync();
                await band.SensorManager.Pedometer.StopReadingsAsync();
                await band.SensorManager.Barometer.StopReadingsAsync();
                await band.SensorManager.SkinTemperature.StopReadingsAsync();
                isRunning = false;
            }
            catch (Exception ex)
            {

            }
        }

        private async void StartAllSensors()
        {
            try
            {
                await band.SensorManager.HeartRate.StartReadingsAsync();
                await band.SensorManager.Distance.StartReadingsAsync();
                await band.SensorManager.Calories.StartReadingsAsync();
                await band.SensorManager.Contact.StartReadingsAsync();
                await band.SensorManager.UV.StartReadingsAsync();
                await band.SensorManager.AmbientLight.StartReadingsAsync();
                await band.SensorManager.Pedometer.StartReadingsAsync();
                await band.SensorManager.Barometer.StartReadingsAsync();
                await band.SensorManager.SkinTemperature.StartReadingsAsync();
                isRunning = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while starting to read from Band sensors (messages: {ex.Message}).");
            }
        }

        private async void UpdateUserInterface()
        {
            await MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                txtBandStatus.Text = bandStatus;
                txtCurrentHeartRate.Text = currentHeartRate;
                txtHeartRateQuality.Text = currentHeartRateQuality;
                txtCaloriesBurnedToday.Text = todayCaloriesBurned;
                txtCaloriesBurnedTotal.Text = totalCaloriesBurned;
                txtCurrentPace.Text = currentPace;
                txtCurrentSpeed.Text = currentSpeed;
                txtDistanceToday.Text = todayDistance;
                txtDistanceTotal.Text = totalDistance;
                txtMotionType.Text = currentMotion;
                txtUVLevel.Text = currentUVLevel;
                txtUVExposedToday.Text = uvExposureToday;
                txtAmbientLight.Text = currentAmbientLight;
                txtStepsToday.Text = stepsToday;
                txtStepsTotal.Text = stepsTotal;
                txtAirTemperature.Text = currentAirTemperature;
                txtAirePressure.Text = currentAirPressure;
                txtSkinTemperature.Text = currentSkinTemperature;
                txtLongitude.Text = longitude.ToString(CultureInfo.CurrentCulture);
                txtLatitude.Text = latitude.ToString(CultureInfo.CurrentCulture);
            });
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            if (isReady != true) return;
            if (isRunning)
            {
                StopAllSensors();
                isRunning = false;
                btnRun.Content = "Start";
            }
            else
            {
                StartAllSensors();
                isRunning = true;
                btnRun.Content = "Stop";
            }
        }

        private void MapLoaded(object sender, RoutedEventArgs e)
        {
            mapCurrentLocation.Center = new Geopoint(new BasicGeoposition()
            {
                Latitude = this.latitude,
                Longitude = this.longitude
            });

            mapCurrentLocation.LandmarksVisible = true;
            mapCurrentLocation.ZoomLevel = 15;
        }

        private Geolocator geoLocator;
        private GeolocationAccessStatus locationAccessStatus;
        private IBandInfo[] pairedBands;
        private string firmwareVersion;
        private string hardwareVersion;
        private bool isRunning = false;
        private string currentHeartRate = "";
        private string currentHeartRateQuality = "";
        private string bandStatus = "";
        private string totalCaloriesBurned = "";
        private string todayCaloriesBurned = "";
        private string currentMotion = "";
        private string todayDistance = "";
        private string totalDistance = "";
        private string currentSpeed = "";
        private string currentPace = "";

        private string currentUVLevel = "";
        private string currentSkinTemperature = "";
        private string currentAmbientLight = "";
        private string stepsTotal = "";
        private string stepsToday = "";
        private string currentAirPressure = "";
        private string currentAirTemperature = "";
        private string uvExposureToday = "";

        private bool isReady = false;
        private DeviceClient deviceClient = null;
        public bool isConnected = false;
        private double latitude = 0.000000;
        private double longitude = 0.000000;
        private IBandClient band = null;
    }
}
