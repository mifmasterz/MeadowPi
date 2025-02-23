using System.Threading.Tasks;
using System.Threading;
using Meadow;
using System;
using LowLevelDrivers;
using Meadow.Hardware;

namespace meadowpi
{
    public class MeadowApp : App<RaspberryPi>
    {
        
        private static GHI.FezHat hat;
        private static Timer timer;
        private static bool next;
        private static int i;
        public override Task Initialize()
        {
            Resolver.Log.Info("Initialize...");
           
            return Task.CompletedTask;
        }

        public override Task Run()
        {
            Resolver.Log.Info("Run...");

            Resolver.Log.Info("Hello, Raspberry Pi!");
            TestFezHat();
            return Task.CompletedTask;
        }
        public void TestFezHat()
        {

            hat = GHI.FezHat.Create();

            hat.S1.SetLimits(500, 2400, 0, 180);
            hat.S2.SetLimits(500, 2400, 0, 180);
            timer = new Timer(TimerCallback, null, 0, 100);

        }
        private void TimerCallback(Object o)
        {
            double x, y, z;

            hat.GetAcceleration(out x, out y, out z);

            var LightTextBox = hat.GetLightLevel().ToString("P2");
            var TempTextBox = hat.GetTemperature().ToString("N2");
            var AccelTextBox = $"({x:N2}, {y:N2}, {z:N2})";
            var Button18TextBox = hat.IsDIO18Pressed().ToString();
            var Button22TextBox = hat.IsDIO22Pressed().ToString();
            var AnalogTextBox = hat.ReadAnalog(GHI.FezHat.AnalogPin.Ain1).ToString("N2");
            Console.WriteLine($"light : {LightTextBox} - Temp : {TempTextBox} - Accel : {AccelTextBox} -" +
                $"Button18 : {Button18TextBox} - Button22 : {Button22TextBox} - Analog : {AnalogTextBox}");
            if ((i++ % 5) == 0)
            {
                var LedsTextBox = next.ToString();
                Console.WriteLine($"Led : {LedsTextBox}");
                hat.DIO24On = next;
                hat.D2.Color = next ? GHI.FezHat.Color.Green : GHI.FezHat.Color.Black;
                hat.D3.Color = next ? GHI.FezHat.Color.Blue : GHI.FezHat.Color.Black;

                hat.WriteDigital(GHI.FezHat.DigitalPin.DIO16, next);
                hat.WriteDigital(GHI.FezHat.DigitalPin.DIO26, next);

                hat.SetPwmDutyCycle(GHI.FezHat.PwmPin.Pwm5, next ? 1.0 : 0.0);
                hat.SetPwmDutyCycle(GHI.FezHat.PwmPin.Pwm6, next ? 1.0 : 0.0);
                hat.SetPwmDutyCycle(GHI.FezHat.PwmPin.Pwm7, next ? 1.0 : 0.0);
                hat.SetPwmDutyCycle(GHI.FezHat.PwmPin.Pwm11, next ? 1.0 : 0.0);
                hat.SetPwmDutyCycle(GHI.FezHat.PwmPin.Pwm12, next ? 1.0 : 0.0);
                
                next = !next;
            }

            if (hat.IsDIO18Pressed())
            {
                hat.S1.Position += 5.0;
                hat.S2.Position += 5.0;

                if (hat.S1.Position >= 180.0)
                {
                    hat.S1.Position = 0.0;
                    hat.S2.Position = 0.0;
                }
            }

            if (hat.IsDIO22Pressed())
            {
                if (hat.MotorA.Speed == 0.0)
                {
                    hat.MotorA.Speed = 0.5;
                    hat.MotorB.Speed = -0.7;
                }
            }
            else
            {
                if (hat.MotorA.Speed != 0.0)
                {
                    hat.MotorA.Speed = 0.0;
                    hat.MotorB.Speed = 0.0;
                }
            }

        }
    }
}