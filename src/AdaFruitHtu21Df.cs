using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace Display
{
    public class AdaFruitHtu21Df
    {

        public AdaFruitHtu21Df() { I2c = new I2CDevice.Configuration(I2C_ADDRESS, I2C_ClockRateKHz); }
        public I2CDevice.Configuration I2c { get; set; }

        public Boolean begin()
        {

            Wire.begin();

            reset();

            Wire.beginTransmission(HTU21DF_I2CADDR);
            Wire.write(HTU21DF_READREG);
            Wire.endTransmission();
            Wire.requestFrom(HTU21DF_I2CADDR, 1);
            return (Wire.read() == 0x2); // after reset should be 0x2


        }
        public float readTemperature() { }
        public float readHumidity() { }
        public void reset() { }

        private Boolean readData() { }
        private float humidity;
        private float temp;
        private ushort I2C_ADDRESS = 0x40;
        private int I2C_ClockRateKHz = 400;
    }
}
