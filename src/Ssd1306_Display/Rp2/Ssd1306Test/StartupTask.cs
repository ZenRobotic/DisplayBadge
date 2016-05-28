using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace Ssd1306Test
{
    public sealed class StartupTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //

            using (var screen = new AdaFruitSSD1306())
            {
                screen.Initialize(VccType.SWITCHCAPVCC);
                screen.ClearScreen();
                screen.InvertDisplay(true);
                drawHalfMoose(screen);
                //screen.DrawString(1, 1, "hello, ssd1306");
                //screen.DrawRectangle(1, 1, 10, 3, Color.Black);
            }
        }
        public  void    drawHalfMoose(AdaFruitSSD1306 screen)
        {
            screen.DrawRectangle(1, 5, 1, 4, Color.White);
            screen.DrawRectangle(1, 8, 5, 1, Color.White);
            screen.DrawRectangle(3, 5, 1, 4, Color.White);
            screen.DrawRectangle(6, 6, 7, 7, Color.White);
            screen.DrawRectangle(7, 8, 1, 3, Color.White);
            screen.SetPixel(64,16, Color.White);
            screen.SetPixel(10, 8, Color.White);
            
            screen.Refresh();
        }
    }
}
