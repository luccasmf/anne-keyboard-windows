﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;

using Windows.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.UI.Core;
using Windows.Devices.Bluetooth;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AnneProKeyboard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private BluetoothLEAdvertisementWatcher Watcher;
        private DeviceWatcher DeviceWatcher;

        private readonly Guid OAD_GUID = new Guid("f000ffc0-0451-4000-b000-000000000000");
        private readonly Guid WRITE_GATT_GUID = new Guid("f000ffc2-0451-4000-b000-000000000000");

        private GattCharacteristic WriteGatt;

        private ObservableCollection<ProfileItem> _keyboardProfiles = new ObservableCollection<ProfileItem>();

        public ObservableCollection<ProfileItem> KeyboardProfiles
        {
            get { return _keyboardProfiles; }
        }

        public MainPage()
        {
            this.InitializeComponent();

            Size window_size = new Size(960, 480);

           ApplicationView.PreferredLaunchViewSize = window_size;
           ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
           ApplicationView.GetForCurrentView().SetPreferredMinSize(window_size);

            FindKeyboard();


            ProfileItem profile_item = new ProfileItem();
            profile_item.Label = "Test Profile";
            profile_item.KeyboardColours = new List<int>();
            SetupProperties(profile_item.KeyboardColours);

            this._keyboardProfiles.Add(profile_item);
        }

        private void SetupProperties(List<int> KeyboardColours)
        {
            for (int i = 0; i < 70; i++)
            {
                KeyboardColours.Add(0);
            }
        }

        private async void FindKeyboard()
        {
            string deviceSelectorInfo = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
            DeviceInformationCollection deviceInfoCollection = await DeviceInformation.FindAllAsync(deviceSelectorInfo, null);

            foreach (DeviceInformation deviceInfo in deviceInfoCollection)
            {
                if (deviceInfo.Name.Contains("ANNE"))
                {
                    ConnectToKeyboard(deviceInfo);
                    break;
                }
            }

            deviceSelectorInfo = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
            deviceInfoCollection = await DeviceInformation.FindAllAsync(deviceSelectorInfo, null);

            foreach (DeviceInformation deviceInfo in deviceInfoCollection)
            {
                if (deviceInfo.Name.Contains("ANNE"))
                {
                    ConnectToKeyboard(deviceInfo);
                    break;
                }
            }
        }

        private void StartScanning()
        {
            Watcher.Start();
            DeviceWatcher.Start();
        }

        private void StopScanning()
        {
            Watcher.Stop();
            DeviceWatcher.Stop();
        }

        private void SetupBluetooth()
        {
            Watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
            Watcher.Received += DeviceFound;

            DeviceWatcher = DeviceInformation.CreateWatcher();
            DeviceWatcher.Added += DeviceAdded;
            DeviceWatcher.Updated += DeviceUpdated;

            StartScanning();
        }

        private async void DeviceFound(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs btAdv)
        {
            if (btAdv.Advertisement.LocalName.Contains("ANNE"))
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    var device = await BluetoothLEDevice.FromBluetoothAddressAsync(btAdv.BluetoothAddress);
                    var result = await device.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None);
                });
            }
        }

        private async void ConnectToKeyboard(DeviceInformation device)
        {
            try
            {
                var keyboard = await BluetoothLEDevice.FromIdAsync(device.Id);

                if (keyboard == null)
                {
                    return;
                }

                if(keyboard.ConnectionStatus != BluetoothConnectionStatus.Connected)
                {
                    return;
                }
                else if(this.Watcher.Status != BluetoothLEAdvertisementWatcherStatus.Started)
                {
                    // automatically start searching for the keyboard
                    this.Watcher.Start();
                    this.Watcher.Start();

                    return;
                }

                var service = keyboard.GetGattService(OAD_GUID);

                if (service == null)
                {
                    return;
                }

                var write_gatt = service.GetCharacteristics(WRITE_GATT_GUID)[0];

                if (write_gatt == null)
                {
                    return;
                }

                WriteGatt = write_gatt;


                // Make sure to disable Bluetooth listener
                this.Watcher.Stop();
                this.Watcher.Stop();

                connectionStatusLabel.Text = "Connected";
                connectionStatusLabel.Foreground = new SolidColorBrush(Colors.Green);

                /* Random Random = new Random();


                 byte[] meta_data = { 0x09, 0xD7, 0x03 }; //  { 0x09, 0x02, 0x01 };
                 byte[] send_data = GenerateKeyboardBLEData(colours);//{ 0x03 };

                 KeyboardWriter keyboard_writer = new KeyboardWriter(Dispatcher, write_gatt, meta_data, send_data);

                 keyboard_writer.WriteToKeyboard();
                 int z = 0;
                 z += 1;

                 /*  var writer = new DataWriter();
                   byte[] test_bytes = { 0x09, 0x02, 0x01, 0x01}; // this will set the keyboard to red
                   writer.WriteBytes(test_bytes);
                   var res = await write_gatt.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse);

                   if (res == GattCommunicationStatus.Success)
                   {
                       Debug.WriteLine("Wrote some data! " );
                   }
                   else
                   {
                       Debug.WriteLine("Failed to write some data!");
                   }*/




            }
            catch
            {
            }
        }

        private void KeyboardProfiles_ItemClick(object sender, ItemClickEventArgs e)
        {
            ProfileItem profile = (e.ClickedItem as ProfileItem);
            chosenProfileName.Text = profile.Label;
        }

        private void initProfileColours(List<int> colours)
        {

        }

        private void DeviceAdded(DeviceWatcher watcher, DeviceInformation device)
        {
            if (device.Name.Contains("ANNE"))
            {
                ConnectToKeyboard(device);
            }
        }

        private byte[] GenerateKeyboardBLEData(List<Int32> colours)
        {
            byte[] bluetooth_data = new byte[214];

            for (int i = 0; i < 70; i++)
            {
                int j = 0;
                if (!(i == 40 || i == 53 || i == 54 || i == 59 || i == 60 || i == 62 || i == 63 || i == 64 || i == 65))
                {
                    int colour = colours[i];
                    byte green = (byte)((65280 & colour) >> 8);
                    byte blue = (byte)(255 & colour);
                    bluetooth_data[(i * 3) + 4] = (byte)((16711680 & colour) >> 16);
                    bluetooth_data[((i * 3) + 4) + 1] = green;
                    bluetooth_data[((i * 3) + 4) + 2] = blue;
                    j++;
                }
            }

            int checksum = CRC16.CalculateChecksum(bluetooth_data, 4, 210);

            byte[] checksum_data = BitConverter.GetBytes(checksum);
            Array.Reverse(checksum_data);

            for (int i = 0; i < 4; i++)
            {
                bluetooth_data[i] = checksum_data[i];
            }

            return bluetooth_data;
        }

        private void DeviceUpdated(DeviceWatcher watcher, DeviceInformationUpdate update)
        {
            //Debug.WriteLine($"Device updated: {update.Id}");
        }

        private void startButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.SetupBluetooth();
        }
    }

    public class ProfileItem
    {
        public string Label { get; set; }
        public List<int> KeyboardColours { get; set; }
    }
}