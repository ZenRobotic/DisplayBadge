using System;
using System.Threading;

namespace Ssd1306Test
{
    public sealed class AdaFruitSSD1306 : IDisposable
    {
        private const bool Data = true;
        private const bool DisplayCommand = false;

        private const int bufferSize = 1024;
        private const int Width = 128;
        private const int Height = 32;
        private const int I2CTransactionTimeout = 1000; // ms

        protected OutputPort dcPin;
        public byte[] displayBuffer = new byte[bufferSize];
        protected OutputPort resetPin;

        protected SPI Spi;
        protected byte[] SpiBuffer = new byte[1];

        public AdaFruitSSD1306(ushort I2C_ADDRESS = 0x3C, int I2C_ClockRateKHz = 400, Cpu.Pin reset = (Cpu.Pin) 54)
        {
            AutoRefreshScreen = false;
            I2c = new I2CDevice.Configuration(I2C_ADDRESS, I2C_ClockRateKHz);
            //SoftwareI2CBus i2cbus = new SoftwareI2CBus(SecretLabs.NETMF.Hardware.Netduino.Pins.GPIO_PIN_A4, SecretLabs.NETMF.Hardware.Netduino.Pins.GPIO_PIN_A5);        
            //I2c = i2cbus.CreateI2CDevice(new I2CDevice.Configuration(0x3C, I2CTransactionTimeout));        
            //Connect the RST pin of the display to this Netduino pin        
            resetPin = new OutputPort(Pins.GPIO_PIN_D10, false);
            //resetPin = new OutputPort(reset, false);        
        }

        public AdaFruitSSD1306(Cpu.Pin dc, Cpu.Pin reset, Cpu.Pin chipSelect,
            SPI.SPI_module spiModule = SPI.SPI_module.SPI1, uint speedKHz = 10000)
        {
            AutoRefreshScreen = false;

            var spiConfig = new SPI.Configuration(
                SPI_mod: spiModule,
                ChipSelect_Port: chipSelect,
                ChipSelect_ActiveState: false,
                ChipSelect_SetupTime: 0,
                ChipSelect_HoldTime: 0,
                Clock_IdleState: false,
                Clock_Edge: true,
                Clock_RateKHz: speedKHz
                );

            Spi = new SPI(spiConfig);

            dcPin = new OutputPort(dc, false);
            resetPin = new OutputPort(reset, false);
        }

        public I2CDevice.Configuration I2c { get; set; }

        public bool AutoRefreshScreen { get; set; }

        public void Dispose()
        {
            dcPin.Dispose();
            resetPin.Dispose();
            Spi.Dispose();

            dcPin = null;
            resetPin = null;
            Spi = null;
            SpiBuffer = null;
            displayBuffer = null;
        }

        public void DrawBitmap(int x, int y, ref byte[] bitmap, int w, int h, Color color)
        {
            for (var j = 0; j < h; j++)
            {
                for (var i = 0; i < w; i++)
                {
                    var Pixel = bitmap[i + (j/8)*w];
                    Pixel &= (byte) (1 << (j%8));
                    if ((Pixel) != 0)
                    {
                        SetPixel(x + i, y + j, color);
                    }
                }
            }
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        public void DrawString(int x, int line, string str)
        {
            foreach (var c in str.ToCharArray())
            {
                DrawCharacter(x, line, c);

                x += 6; // 6 pixels wide

                if (x + 6 >= Width)
                {
                    x = 0; // ran out of this line
                    line++;
                }

                if (line >= Height/8)
                {
                    return; // ran out of space :(
                }
            }
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        protected void DrawCharacter(int x, int line, char c)
        {
            for (var i = 0; i < 5; i++)
            {
                displayBuffer[x + (line*128)] = Font[(c*5) + i];
                x++;
            }
        }

        // bresenham's algorithm - thx wikipedia
        public void DrawLine(int x0, int y0, int x1, int y1, Color color)
        {
            var steep = (Math.Abs(y1 - y0) > Math.Abs(x1 - x0)) ? 1 : 0;

            if (steep != 0)
            {
                Swap(ref x0, ref y0);
                Swap(ref x1, ref y1);
            }

            if (x0 > x1)
            {
                Swap(ref x0, ref x1);
                Swap(ref y0, ref y1);
            }

            int dx, dy;
            dx = x1 - x0;
            dy = Math.Abs(y1 - y0);

            var err = dx/2;
            int ystep;

            if (y0 < y1)
            {
                ystep = 1;
            }
            else
            {
                ystep = -1;
            }

            for (; x0 < x1; x0++)
            {
                if (steep != 0)
                {
                    SetPixel(y0, x0, color);
                }
                else
                {
                    SetPixel(x0, y0, color);
                }

                err -= dy;

                if (err < 0)
                {
                    y0 += ystep;
                    err += dx;
                }
            }

            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        public void DrawRectangle(int x, int y, int w, int h, Color color)
        {
            for (var i = x; i < x + w; i++)
            {
                SetPixel(i, y, color);
                SetPixel(i, y + h - 1, color);
            }
            for (var i = y; i < y + h; i++)
            {
                SetPixel(x, i, color);
                SetPixel(x + w - 1, i, color);
            }
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        public void FillRectangle(int x, int y, int w, int h, Color color)
        {
            for (var i = x; i < x + w; i++)
            {
                for (var j = y; j < y + h; j++)
                {
                    SetPixel(i, j, color);
                }
            }
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        public void DrawCircle(int x0, int y0, int r, Color color)
        {
            var f = 1 - r;
            var ddF_x = 1;
            var ddF_y = -2*r;
            var x = 0;
            var y = r;

            SetPixel(x0, y0 + r, color);
            SetPixel(x0, y0 - r, color);
            SetPixel(x0 + r, y0, color);
            SetPixel(x0 - r, y0, color);

            while (x < y)
            {
                if (f >= 0)
                {
                    y--;
                    ddF_y += 2;
                    f += ddF_y;
                }

                x++;
                ddF_x += 2;
                f += ddF_x;

                SetPixel(x0 + x, y0 + y, color);
                SetPixel(x0 - x, y0 + y, color);
                SetPixel(x0 + x, y0 - y, color);
                SetPixel(x0 - x, y0 - y, color);

                SetPixel(x0 + y, y0 + x, color);
                SetPixel(x0 - y, y0 + x, color);
                SetPixel(x0 + y, y0 - x, color);
                SetPixel(x0 - y, y0 - x, color);
            }
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        public void FillCircle(int x0, int y0, int r, Color color)
        {
            var f = 1 - r;
            var ddF_x = 1;
            var ddF_y = -2*r;
            var x = 0;
            var y = r;

            for (var i = y0 - r; i <= y0 + r; i++)
            {
                SetPixel(x0, i, color);
            }

            while (x < y)
            {
                if (f >= 0)
                {
                    y--;
                    ddF_y += 2;
                    f += ddF_y;
                }

                x++;
                ddF_x += 2;
                f += ddF_x;

                for (var i = y0 - y; i <= y0 + y; i++)
                {
                    SetPixel(x0 + x, i, color);
                    SetPixel(x0 - x, i, color);
                }

                for (var i = y0 - x; i <= y0 + x; i++)
                {
                    SetPixel(x0 + y, i, color);
                    SetPixel(x0 - y, i, color);
                }
            }
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        public void SetPixel(int x, int y, Color color)
        {
            if ((x >= Width) || (y >= Height))
            {
                return;
            }

            if (color == Color.White)
            {
                displayBuffer[x + (y/8)*128] |= (byte) (1 << (y%8));
            }
            else
            {
                displayBuffer[x + (y/8)*128] &= (byte) ~(1 << (y%8));
            }
        }

        public void ClearScreen()
        {
            displayBuffer[0] = 0;
            displayBuffer[1] = 0;
            displayBuffer[2] = 0;
            displayBuffer[3] = 0;
            displayBuffer[4] = 0;
            displayBuffer[5] = 0;
            displayBuffer[6] = 0;
            displayBuffer[7] = 0;
            displayBuffer[8] = 0;
            displayBuffer[9] = 0;
            displayBuffer[10] = 0;
            displayBuffer[11] = 0;
            displayBuffer[12] = 0;
            displayBuffer[13] = 0;
            displayBuffer[14] = 0;
            displayBuffer[15] = 0;
            Array.Copy(displayBuffer, 0, displayBuffer, 16, 16);
            Array.Copy(displayBuffer, 0, displayBuffer, 32, 32);
            Array.Copy(displayBuffer, 0, displayBuffer, 64, 64);
            Array.Copy(displayBuffer, 0, displayBuffer, 128, 128);
            Array.Copy(displayBuffer, 0, displayBuffer, 256, 256);
            Array.Copy(displayBuffer, 0, displayBuffer, 512, 512);

            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        protected void Swap(ref int a, ref int b)
        {
            var t = a;
            a = b;
            b = t;
        }

        protected void SendCommand(Command cmd)
        {
            SpiBuffer[0] = (byte) cmd;

            if (Spi != null)
            {
                Spi.Write(SpiBuffer);
            }

            if (I2c != null)
            {
                I2CBus.GetInstance().Write(I2c, new byte[] {0x00, (byte) cmd}, I2CTransactionTimeout);
            }
        }

        public void InvertDisplay(bool cmd)
        {
            if (Spi != null)
            {
                dcPin.Write(DisplayCommand);
            }

            if (cmd)
            {
                SendCommand(Command.INVERTDISPLAY);
            }
            else
            {
                SendCommand(Command.NORMALDISPLAY);
            }

            if (Spi != null)
            {
                dcPin.Write(Data);
            }
        }

        public virtual void Refresh()
        {
            if (Spi != null)
            {
                Spi.Write(displayBuffer);
            }
            else if (I2c != null)
            {
                var I2CCommand = new byte[displayBuffer.Length + 1];
                I2CCommand[0] = 0x40;
                Array.Copy(displayBuffer, 0, I2CCommand, 1, displayBuffer.Length);
                I2CBus.GetInstance().Write(I2c, I2CCommand, I2CTransactionTimeout);
            }
        }

        public void Initialize(VccType vcctype = VccType.SWITCHCAPVCC)
        {
            if (Spi != null)
            {
                resetPin.Write(true);

                Thread.Sleep(1);
                // VDD (3.3V) goes high at start, lets just chill for a ms        

                resetPin.Write(false); // bring reset low        

                Thread.Sleep(10);
                // wait 10ms        

                resetPin.Write(true); // bring out of reset
                dcPin.Write(DisplayCommand);
                SendCommand(Command.DISPLAYOFF); // 0xAE
                SendCommand(Command.SETLOWCOLUMN | 0x0); // low col = 0
                SendCommand(Command.SETHIGHCOLUMN | 0x0); // hi col = 0
                SendCommand(Command.SETSTARTLINE | 0x0); // line #0
                SendCommand(Command.SETCONTRAST); // 0x81

                if (vcctype == VccType.EXTERNALVCC)
                {
                    SendCommand((Command) 0x9F); // external 9V        
                }

                else
                {
                    SendCommand((Command) 0xCF); // chargepump        
                }

                SendCommand((Command) 0xA1); // setment remap 95 to 0 (?)
                SendCommand(Command.NORMALDISPLAY); // 0xA6
                SendCommand(Command.DISPLAYALLON_RESUME); // 0xA4
                SendCommand(Command.SETMULTIPLEX); // 0xA8
                //SendCommand((Command)0x3F);  // 0x3F 1/64 duty
                SendCommand((Command) 0x1F); // 0x1F 1/32 duty
                SendCommand(Command.SETDISPLAYOFFSET); // 0xD3
                SendCommand(0x0); // no offset
                SendCommand(Command.SETDISPLAYCLOCKDIV); // 0xD5
                SendCommand((Command) 0x80); // the suggested ratio 0x80
                SendCommand(Command.SETPRECHARGE); // 0xd9        

                if (vcctype == VccType.EXTERNALVCC)
                {
                    SendCommand((Command) 0x22); // external 9V        
                }

                else
                {
                    SendCommand((Command) 0xF1); // DC/DC        
                }

                SendCommand(Command.SETCOMPINS); // 0xDA
                //SendCommand((Command)0x12); // disable COM left/right remap
                SendCommand((Command) 0x02); ////128_32 = 02       128_64 = 12
                SendCommand(Command.SETVCOMDETECT); // 0xDB
                SendCommand((Command) 0x40); // 0x20 is default
                SendCommand(Command.MEMORYMODE); // 0x20
                SendCommand(0x00); // 0x0 act like ks0108        

                // left to right scan
                SendCommand(Command.SEGREMAP | (Command) 0x1); //0xA0
                SendCommand(Command.COMSCANDEC);
                //0xC8
                SendCommand(Command.CHARGEPUMP); //0x8D        

                if (vcctype == VccType.EXTERNALVCC)
                {
                    SendCommand((Command) 0x10); // disable        
                }
                else
                {
                    SendCommand((Command) 0x14); // disable        
                }

                SendCommand(Command.DISPLAYON); //--turn on oled panel
                // Switch to 'data' mode
                dcPin.Write(Data);
            }
            else
            {
                byte[] intBuffer;

                if (Height == 32)
                {
                    intBuffer = new byte[]
                    {
                        (byte) Command.DISPLAYOFF,

                        //turn off display(RESET=OFF)        
                        0x00, 0x00,
                        //low column nibble(RESET=0),high column nibble(RESET=0)        
                        0xB0,
                        //start page address(RESET=0)        
                        (byte) Command.MEMORYMODE, 0x00,
                        //memory address mode(RESET=02 [page])        
                        (byte) Command.SETDISPLAYCLOCKDIV, 0x80,
                        //oscillator frequency and divider(RESET=80)00  
                        (byte) Command.SETMULTIPLEX, 0x1F,
                        //mux ratio(RESET=3F [64 lines]        
                        (byte) Command.SETDISPLAYOFFSET, 0x00,
                        //display offset,COM vertical shift(RESET=0)        
                        (byte) Command.CHARGEPUMP, (byte) (vcctype == VccType.EXTERNALVCC ? 0x10 : 0x14),
                        //enable charge pump(RESET=10 [OFF])        
                        0xA1, 0x00,
                        //segment remap(RESET=SEG0, COL0) 00 mirror image        
                        (byte) Command.COMSCANDEC,
                        //COM output scan(RESET=C0, C8 flips display)        
                        (byte) Command.SETCOMPINS, 0x02,
                        //COM pins hardware config(RESET=12[alternate])        
                        (byte) Command.SETCONTRAST, 0xCF,
                        //contrast CF(RESET=7F)        
                        (byte) Command.SETPRECHARGE, (byte) (vcctype == VccType.EXTERNALVCC ? 0x22 : 0xF1),
                        //pre-charge period F1(RESET=22)        
                        (byte) Command.SETVCOMDETECT, 0x30,
                        //Vcom deselect(RESET=20)        
                        (byte) Command.DISPLAYALLON_RESUME,
                        //turn all on ignore RAM A5/RAM A4(RESET=A4)        
                        (byte) Command.NORMALDISPLAY,
                        //normal display A6/inverted A7(RESET=A6)        
                        (byte) Command.DISPLAYON
                    };

                    //turn on display        
                }
                else if (Height == 64)
                {
                    intBuffer = new byte[]
                    {
                        (byte) Command.DISPLAYOFF,
                        //turn off display(RESET=OFF)        
                        0x00, 0x00,
                        //low column nibble(RESET=0),high column nibble(RESET=0)        
                        0xB0,
                        //start page address(RESET=0)        
                        (byte) Command.MEMORYMODE, 0x00,
                        //memory address mode(RESET=02 [page])        
                        (byte) Command.SETDISPLAYCLOCKDIV, 0x80,
                        //oscillator frequency and divider(RESET=80)00
                        (byte) Command.SETMULTIPLEX, 0x3F,
                        //mux ratio(RESET=3F [64 lines]        
                        (byte) Command.SETDISPLAYOFFSET, 0x00,
                        //display offset,COM vertical shift(RESET=0)        
                        (byte) Command.CHARGEPUMP, (byte) (vcctype == VccType.EXTERNALVCC ? 0x10 : 0x14),
                        //enable charge pump(RESET=10 [OFF])        
                        0xA1, 0x00,
                        //segment remap(RESET=SEG0, COL0) 00 mirror image        
                        (byte) Command.COMSCANDEC,
                        //COM output scan(RESET=C0, C8 flips display)        
                        (byte) Command.SETCOMPINS, 0x12,
                        //COM pins hardware config(RESET=12[alternate])        
                        (byte) Command.SETCONTRAST, 0xCF,
                        //contrast CF(RESET=7F)        
                        (byte) Command.SETPRECHARGE, (byte) (vcctype == VccType.EXTERNALVCC ? 0x22 : 0xF1),
                        //pre-charge period F1(RESET=22)        
                        (byte) Command.SETVCOMDETECT, 0x40,
                        //Vcom deselect(RESET=20)        
                        (byte) Command.DISPLAYALLON_RESUME,
                        //turn all on ignore RAM A5/RAM A4(RESET=A4)        
                        (byte) Command.NORMALDISPLAY,
                        //normal display A6/inverted A7(RESET=A6)        
                        (byte) Command.DISPLAYON
                    };

                    //turn on display        
                }

                I2CBus.GetInstance().Write(I2c, intBuffer, I2CTransactionTimeout);
            }
        }
    }

    public enum VccType
    {
        EXTERNALVCC = 0x1,
        SWITCHCAPVCC = 0x2
    }

    public enum Command
    {
        SETCONTRAST = 0x81,
        DISPLAYALLON_RESUME = 0xA4,
        DISPLAYALLON = 0xA5,
        NORMALDISPLAY = 0xA6,
        INVERTDISPLAY = 0xA7,
        DISPLAYOFF = 0xAE,
        DISPLAYON = 0xAF,
        SETDISPLAYOFFSET = 0xD3,
        SETCOMPINS = 0xDA,
        SETVCOMDETECT = 0xDB,
        SETDISPLAYCLOCKDIV = 0xD5,
        SETPRECHARGE = 0xD9,
        SETMULTIPLEX = 0xA8,
        SETLOWCOLUMN = 0x00,
        SETHIGHCOLUMN = 0x10,
        SETSTARTLINE = 0x40,
        MEMORYMODE = 0x20,
        COMSCANINC = 0xC0,
        COMSCANDEC = 0xC8,
        SEGREMAP = 0xA0,
        CHARGEPUMP = 0x8D
    }

    public enum Color
    {
        Black,
        White
    }
}