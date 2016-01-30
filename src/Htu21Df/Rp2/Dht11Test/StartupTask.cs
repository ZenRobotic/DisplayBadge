using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Foundation;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace Dht11Test
{
    public sealed class StartupTask : IBackgroundTask
    {
        private const byte ReadReg = 0xe7;
        private const byte Reset = 0xfe;
        private const byte ReadTemp = 0xE3;
        private const byte ReadHum = 0xE5;
        private const ushort I2C_ADDRESS = 0x40;
        private const int I2C_ClockRateKHz = 400;
        private I2cDevice _dht11;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //
            var deferral = taskInstance.GetDeferral();
            var settings = new I2cConnectionSettings(I2C_ADDRESS);
            settings.BusSpeed = I2cBusSpeed.FastMode;
            var aqs = I2cDevice.GetDeviceSelector();                     /* Get a selector string that will return all I2C controllers on the system */
            var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the I2C bus controller devices with our selector string             */
            _dht11 = await I2cDevice.FromIdAsync(dis[0].Id, settings);    /* Create an I2cDevice with our selected bus controller and I2C settings    */
            await begin();

            while (true)
            {
                var temp = await readTemperature();
                var hum = await readHumidity();
                Debug.WriteLine($"{temp} C & {hum}% humidity");
                await Task.Delay(2000);
            }

            deferral.Complete();
        }

        private async Task<Boolean> begin()
        {
            await reset();
            var buffer = new byte[1];

            buffer[0] = ReadReg;
            _dht11.Write(buffer);


            buffer[0] = 0;
            _dht11.Read(buffer);

            return await Task.FromResult(buffer[0] == 0x2); // after reset should be 0x2


        }

        private async Task reset()
        {
            try
            {
                var buffer = new byte[1];
                buffer[0] = Reset;
                _dht11.Write(buffer);
            }
            catch (Exception)
            {
            }

            await Task.Delay(15);

        }

        private async Task<float?> readTemperature()
        {
            // 1) write ReadTemp to Addr (Bus has address)
            var buffer = new byte[1];

            buffer[0] = ReadTemp;
            _dht11.Write(buffer);

            // 2) wait 50 ms - delay between write and read
            await Task.Delay(50);

            // 3) read reply from Addr (Bus has address)
            buffer = new byte[3];

            _dht11.Read(buffer);

            // 		a) we need to read 3 bytes, a 2 byte answer and a 1 byte CRC
            UInt16 t = buffer[0];
            t = (UInt16)(t << 8);
            t = (UInt16)(t + buffer[1]);
            var crc = buffer[2];
            var computedCrc = computeCrc(buffer, 0, 2);

            if (crc != computedCrc)
            {
                return null;
            }

            // 4) Does math to figure out temp.
            float temp = t;
            temp *= 175.72f;
            temp /= 65536;
            temp -= 46.85f;

            return temp;
        }

        private async Task<float?> readHumidity()
        {
            var buffer = new byte[1];
            buffer[0] = ReadHum;
            _dht11.Write(buffer);

            await Task.Delay(50);

            buffer = new byte[3];

            _dht11.Read(buffer);

            UInt16 h = buffer[0];
            h = (UInt16)(h << 8);
            h = (UInt16)(h + buffer[1]);
            var crc = buffer[2];
            var computedCrc = computeCrc(buffer, 0, 2);

            if (crc != computedCrc)
            {
                return null;
            }

            float hum = h;
            hum *= 125;
            hum /= 65536;
            hum -= 6;

            return hum;
        }

        private Byte computeCrc(Byte[] buffer, Int32 start, Int32 length)
        {
            Byte b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0, b7 = 0, b8 = 0;
            var bitOffset = 0;

            for (var i = 0; i < 8 * length; i++)
            {
                var current = buffer[start + i / 8];
                var bitPosition = 7 - bitOffset++ % 8;
                var mask = 0x1;
                var invert = ((current >> bitPosition) & mask) ^ b7;
                b7 = b6;
                b6 = b5;
                b5 = (byte)((b4 ^ invert) & 0x1);
                b4 = (byte)((b3 ^ invert) & 0x1);
                b3 = b2;
                b2 = b1;
                b1 = b0;
                b0 = (byte)invert;
            }

            Byte final = 0;
            final |= b7;
            final = (byte)(final << 1);
            final |= b6;
            final = (byte)(final << 1);
            final |= b5;
            final = (byte)(final << 1);
            final |= b4;
            final = (byte)(final << 1);
            final |= b3;
            final = (byte)(final << 1);
            final |= b2;
            final = (byte)(final << 1);
            final |= b1;
            final = (byte)(final << 1);
            final |= b0;

            return final;
        }
    }
}
