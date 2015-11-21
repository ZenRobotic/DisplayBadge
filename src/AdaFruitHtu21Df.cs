using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace Display
{
    public class AdaFruitHtu21Df
    {
        private const byte ReadReg = 0xe7;
        public AdaFruitHtu21Df()
        {
            I2cConfiguration = new I2CDevice.Configuration(I2C_ADDRESS, I2C_ClockRateKHz);
            Bus = new I2CDevice(I2cConfiguration);
        }
       
        public I2CDevice.Configuration I2cConfiguration { get; set; }
        public I2CDevice Bus { get; set; }


        public Boolean begin()
        {

            var buffer = new byte[1];
            buffer[0] = ReadReg;
            var transaction =  I2CDevice.CreateWriteTransaction(buffer);
            Bus.Execute(new[] { transaction }, 1000);
            reset();

           
           
            I2CDevice.CreateReadTransaction(, 1);
            return (I2cConfiguration.read() == 0x2); // after reset should be 0x2


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
