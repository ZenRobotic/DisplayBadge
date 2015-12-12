using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace Display
{
    public class AdaFruitHtu21Df
    {
        private const byte ReadReg = 0xe7;
        private const byte Reset = 0xfe;
        private const byte ReadTemp = 0xE3;
        public AdaFruitHtu21Df()
        {
            I2cConfiguration = new I2CDevice.Configuration(I2C_ADDRESS, I2C_ClockRateKHz);
            Bus = new I2CDevice(I2cConfiguration);
        }
       
        public I2CDevice.Configuration I2cConfiguration { get; set; }
        public I2CDevice Bus { get; set; }


        public Boolean begin()
        {
            reset();
            var buffer = new byte[1];

            buffer[0] = ReadReg;
            var transaction =  I2CDevice.CreateWriteTransaction(buffer);
            Bus.Execute(new[] { transaction }, 1000);


            buffer[0] = 0;
            var readTx =           I2CDevice.CreateReadTransaction(buffer);
            Bus.Execute(new [] { readTx }, 1000);

            return (buffer[0] == 0x2); // after reset should be 0x2


        }

        public float readTemperature() {
  
        // 1) write ReadTemp to Addr (Bus has address)
            var buffer = new byte[1];

            buffer[0] = ReadTemp;
            var transaction = I2CDevice.CreateWriteTransaction(buffer);
            Bus.Execute(new[] { transaction }, 1000);
			
        // 2) wait 50 ms - delay between write and read
            Thread.Sleep(50);
			
        // 3) read reply from Addr (Bus has address)
            buffer[0] = 0;
            var readTx = I2CDevice.CreateReadTransaction(buffer);
            Bus.Execute(new[] { readTx }, 1000);
	
		// 		a) we need to read 3 bytes, a 2 byte answer and a 1 byte CRC
  Wire.requestFrom(HTU21DF_I2CADDR, 3);
  while (!Wire.available()) {}

  uint16_t t = Wire.read();
  t <<= 8;
  t |= Wire.read();

  uint8_t crc = Wire.read();
  
        // 4) Does math to figure out temp.
		// TODO: the math
		

  float temp = t;
  temp *= 175.72;
  temp /= 65536;
  temp -= 46.85;

  return temp; }
        public float readHumidity() { }
        public void reset()
        {
            var buffer = new byte[1];
            buffer[0] = Reset;
            var transaction = I2CDevice.CreateWriteTransaction(buffer);
            Bus.Execute(new[] { transaction }, 1000);
            Thread.Sleep(15);
            
        }

        private Boolean readData() { }
        private float humidity;
        private float temp;
        private ushort I2C_ADDRESS = 0x40;
        private int I2C_ClockRateKHz = 400;
    }
}
