using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace Display
{
    public class Program
    {
        private static readonly InputPort _button = new InputPort(Pins.ONBOARD_BTN, true, Port.ResistorMode.Disabled);
        private static readonly OutputPort _led = new OutputPort(Pins.ONBOARD_LED, false);
        private static readonly AdaFruitSSD1306 _display = new AdaFruitSSD1306(I2C_ClockRateKHz: 100);

        public static void Main()
        {
            using (_display)
            {
                _display.Initialize();
                _display.InvertDisplay(true);
            }
        }
    }
}