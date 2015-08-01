using SharpDX.DirectInput;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using static GamepadAsKeyboard.Constants;

namespace GamepadAsKeyboard
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HandleJoystick();
        }

        private static void HandleJoystick()
        {
            // Initialize DirectInput
            var directInput = new DirectInput();

            // Find a Joystick Guid
            var joystickGuid = Guid.Empty;

            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad,
                        DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;

            // If Gamepad not found, look for a Joystick
            if (joystickGuid == Guid.Empty)
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick,
                        DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;

            // If Joystick not found, throws an error
            if (joystickGuid == Guid.Empty)
            {
                Debug.WriteLine("No Gamepad found.");
                Environment.Exit(1);
            }

            // Instantiate the joystick
            var joystick = new Joystick(directInput, joystickGuid);

            Debug.WriteLine("Found Gamepad with GUID: {0}", joystickGuid);

            // Set BufferSize in order to use buffered data.
            joystick.Properties.BufferSize = 128;

            // Acquire the joystick
            joystick.Acquire();

            // Poll events from joystick every 1 millisecond
            var timer = Observable.Interval(TimeSpan.FromMilliseconds(1));

            timer
                // Get all joystick input
                .SelectMany(_ => { joystick.Poll(); return joystick.GetBufferedData(); })
                // Filter only menu key button (xbox one controller)
                .Where(a => a.Offset == JoystickOffset.Buttons6)
                // Input will contain UP and Down button events
                .Buffer(2)
                // If button was pressed longer than a second
                .Where(t => (TimeSpan.FromMilliseconds(t.Last().Timestamp) - TimeSpan.FromMilliseconds(t.First().Timestamp)).TotalSeconds >= 1)
                // Press and hold F key for 1.5s
                .Subscribe(t =>
                {
                    SendKeyDown(KeyCode.KEY_F);
                    System.Threading.Thread.Sleep(1500);
                    SendKeyUp(KeyCode.KEY_F);
                });
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

        private static void SendKeyDown(KeyCode keyCode)
        {
            var input = new KEYBDINPUT
            {
                Vk = (ushort)keyCode
            };
          
            SendKeyboardInput(input);
        }

        private static void SendKeyUp(KeyCode keyCode)
        {
            var input = new KEYBDINPUT
            {
                Vk = (ushort)keyCode,
                Flags = 2
            };
            SendKeyboardInput(input);
        }

        private static void SendKeyboardInput(KEYBDINPUT keybInput)
        {
            INPUT input = new INPUT
            {
                Type = 1
            };
            input.Data.Keyboard = keybInput;

            if (SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT))) == 0)
            {
                throw new Exception();
            }
        }
    }
}
