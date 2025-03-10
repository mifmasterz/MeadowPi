﻿using LowLevelDrivers;
using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace GHI
{
    /// <summary>
    /// A helper class for the FEZ HAT.
    /// </summary>
    public class FezHat : IDisposable {
        private bool disposed;
        private PCA9685 pwm;
        private ADS7830 analog;
        private MMA8453 accelerometer;
        private GpioPin motorEnable;
        private GpioPin dio16;
        private GpioPin dio26;
        private GpioPin dio24;
        private GpioPin dio18;
        private GpioPin dio22;
        
        /// <summary>
        /// The chip select line exposed on the header used for SPI devices.
        /// </summary>
        public static int SpiChipSelectLine => 0;

        /// <summary>
        /// The SPI device name exposed on the header.
        /// </summary>
        public static string SpiDeviceName => "SPI0";

        /// <summary>
        /// The I2C device name exposed on the header.
        /// </summary>
        public static string I2cDeviceName => "I2C1";

        /// <summary>
        /// The frequency that the onboard PWM controller outputs. All PWM pins use the same frequency, only the duty cycle is controllable.
        /// </summary>
        /// <remarks>
        /// Care needs to be taken when using the exposed PWM pins, motors, or servos. Motors generally require a high frequency while servos require a specific low frequency, usually 50Hz.
        /// If you set the frequency to a certain value, you may impair the ability of another part of the board to function.
        /// </remarks>
        public int PwmFrequency {
            get {
                return this.pwm.Frequency;
            }
            set {
                this.pwm.Frequency = value;
            }
        }

        /// <summary>
        /// The object used to control the motor terminal labeled A.
        /// </summary>
        public Motor MotorA { get; private set; }

        /// <summary>
        /// The object used to control the motor terminal labeled A.
        /// </summary>
        public Motor MotorB { get; private set; }

        /// <summary>
        /// The object used to control the RGB led labeled D2.
        /// </summary>
        public RgbLed D2 { get; private set; }

        /// <summary>
        /// The object used to control the RGB led labeled D3.
        /// </summary>
        public RgbLed D3 { get; private set; }

        /// <summary>
        /// The object used to control the servo header labeled S1.
        /// </summary>
        public Servo S1 { get; private set; }

        /// <summary>
        /// The object used to control the servo header labeled S2.
        /// </summary>
        public Servo S2 { get; private set; }

        /// <summary>
        /// Whether or not the DIO24 led is on or off.
        /// </summary>
        public bool DIO24On {
            get {
                return this.dio24.Read() == true;
            }
            set {
                this.dio24.Write(value ? PinValue.High : PinValue.Low);
            }
        }

        /// <summary>
        /// Whether or not the button labeled DIO18 is pressed.
        /// </summary>
        /// <returns>The pressed state.</returns>
        public bool IsDIO18Pressed() => this.dio18.Read() == false;

        /// <summary>
        /// Whether or not the button labeled DIO18 is pressed.
        /// </summary>
        /// <returns>The pressed state.</returns>
        public bool IsDIO22Pressed() => this.dio22.Read() == false;

        /// <summary>
        /// Gets the light level from the onboard sensor.
        /// </summary>
        /// <returns>The light level between 0 (low) and 1 (high).</returns>
        public double GetLightLevel() => this.analog.Read(5);

        /// <summary>
        /// Gets the temperature in celsius from the onboard sensor.
        /// </summary>
        /// <returns>The temperature.</returns>
        public double GetTemperature() => (this.analog.Read(4) * 3300.0 - 450.0) / 19.5;

        /// <summary>
        /// Gets the acceleration in G's for each axis from the onboard sensor.
        /// </summary>
        /// <param name="x">The current X-axis acceleration.</param>
        /// <param name="y">The current Y-axis acceleration.</param>
        /// <param name="z">The current Z-axis acceleration.</param>
        public void GetAcceleration(out double x, out double y, out double z) => this.accelerometer.GetAcceleration(out x, out y, out z);

        /// <summary>
        /// Disposes of the object releasing control the pins.
        /// </summary>
        public void Dispose() => this.Dispose(true);

        private FezHat() {
            this.disposed = false;
        }

        /// <summary>
        /// Disposes of the object releasing control the pins.
        /// </summary>
        /// <param name="disposing">Whether or not this method is called from Dispose().</param>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                if (disposing) {
                    this.pwm.Dispose();
                    this.analog.Dispose();
                    this.accelerometer.Dispose();
                    i2cBus.Dispose();
                    //this.motorEnable.Dispose();
                    //this.dio16.Dispose();
                    //this.dio26.Dispose();
                    //this.dio24.Dispose();
                    //this.dio18.Dispose();
                    //this.dio22.Dispose();

                    this.MotorA.Dispose();
                    this.MotorB.Dispose();
                }

                this.disposed = true;
            }
        }
        //static Mono.Linux.I2C.I2CBus i2cBus;
        static I2cBus i2cBus;
        /// <summary>
        /// Creates a new instance of the FEZ HAT.
        /// </summary>
        /// <returns>The new instance.</returns>
        public static FezHat Create() {
            var controller = new GpioController();
            //var gpioController = GpioController.GetDefault();
            //var i2cController = (await DeviceInformation.FindAllAsync(I2cDevice.GetDeviceSelector(FEZHAT.I2cDeviceName)))[0];
            var hat = new FezHat();
            if (i2cBus == null)
            {
                //i2cBus = new Mono.Linux.I2C.I2CBus (0x01);
                i2cBus = I2cBus.Create(0x01);

            }
            //I2CDevice i2c_accelerometer = Pi.I2C.AddDevice(MMA8453.GetAddress(false));
            //var i2c_accelerometer = new Mono.Linux.I2C.I2CDevice(i2cBus, MMA8453.GetAddress(false));
            var i2c_accelerometer = I2cDevice.Create(new I2cConnectionSettings(1, MMA8453.GetAddress(false)));//i2cBus.CreateDevice(MMA8453.GetAddress(false));//I2cDevice.Create(new I2cConnectionSettings(1, MMA8453.GetAddress(false)));// new Mono.Linux.I2C.I2CDevice(i2cBus, );
            hat.accelerometer = new MMA8453(i2c_accelerometer);

            //I2CDevice i2c_analog = Pi.I2C.AddDevice(ADS7830.GetAddress(false, false));
            //var i2c_analog = new Mono.Linux.I2C.I2CDevice(i2cBus, ADS7830.GetAddress(false, false));
            var i2c_analog = I2cDevice.Create(new I2cConnectionSettings(1, ADS7830.GetAddress(false, false)));//i2cBus.CreateDevice(ADS7830.GetAddress(false, false));//I2cDevice.Create(new I2cConnectionSettings(1, ADS7830.GetAddress(false, false)));// new Mono.Linux.I2C.I2CDevice(i2cBus, ADS7830.GetAddress(false, false));
            hat.analog = new ADS7830(i2c_analog);

            //I2CDevice i2c_pwm = Pi.I2C.AddDevice(PCA9685.GetAddress(true, true, true, true, true, true));
            //var i2c_pwm = new Mono.Linux.I2C.I2CDevice(i2cBus, PCA9685.GetAddress(true, true, true, true, true, true));
            var i2c_pwm = I2cDevice.Create(new I2cConnectionSettings(1, PCA9685.GetAddress(true, true, true, true, true, true)));//i2cBus.CreateDevice(PCA9685.GetAddress(true, true, true, true, true, true));//I2cDevice.Create(new I2cConnectionSettings(1, PCA9685.GetAddress(true, true, true, true, true, true))); //new Mono.Linux.I2C.I2CDevice(i2cBus, PCA9685.GetAddress(true, true, true, true, true, true));
            var pin13 = controller.OpenPin(13, PinMode.Output);
            hat.pwm = new PCA9685(i2c_pwm, pin13);// Pi.Gpio.Pin23);//Pi.Gpio.Pin13);x
            hat.pwm.OutputEnabled = true;
            hat.pwm.Frequency = 1500;
            var Pin16 = controller.OpenPin(16, PinMode.Output);
            var Pin26 = controller.OpenPin(26, PinMode.Output);
            var Pin24 = controller.OpenPin(24, PinMode.Output);
            var Pin18 = controller.OpenPin(18, PinMode.Output);
            var Pin22 = controller.OpenPin(22, PinMode.Output);
            //mapping wiring pi vs gpio win iot
            hat.dio16 = Pin16;//Pi.Gpio.Pin27;//Pi.Gpio.Pin16;x
            hat.dio26 = Pin26;//Pi.Gpio.Pin25;//Pi.Gpio.Pin26;x
            hat.dio24 = Pin24;//Pi.Gpio.Pin05;//Pi.Gpio.Pin24;x
            hat.dio18 = Pin18;//Pi.Gpio.Pin01;//Pi.Gpio.Pin18;x
            hat.dio22 = Pin22;//Pi.Gpio.Pin03;//Pi.Gpio.Pin22;x

            hat.dio16.SetPinMode(PinMode.Input);// = GpioPinDriveMode.Input;
            hat.dio26.SetPinMode(PinMode.Input);//.PinMode = GpioPinDriveMode.Input;
            hat.dio24.SetPinMode(PinMode.Output);//.PinMode = GpioPinDriveMode.Output;
            hat.dio18.SetPinMode(PinMode.Input);//.PinMode = GpioPinDriveMode.Input;
            hat.dio22.SetPinMode(PinMode.Input);//.PinMode = GpioPinDriveMode.Input;
            var Pin12 = controller.OpenPin(12, PinMode.Output);
            hat.motorEnable = Pin12;// Pi.Gpio.Pin26;//Pi.Gpio.Pin12;x
            hat.motorEnable.SetPinMode(PinMode.Output);//PinMode = GpioPinDriveMode.Output;
            hat.motorEnable.Write(PinValue.High);
            var Pin27 = controller.OpenPin(27, PinMode.Output);
            var Pin6 = controller.OpenPin(6, PinMode.Output);
            var Pin23 = controller.OpenPin(23, PinMode.Output);
            var Pin5 = controller.OpenPin(5, PinMode.Output);
            hat.MotorA = new Motor(hat.pwm, 14, Pin27, Pin23);//Pi.Gpio.Pin02, Pi.Gpio.Pin04);//Pi.Gpio.Pin27, Pi.Gpio.Pin23);x
            hat.MotorB = new Motor(hat.pwm, 13, Pin6, Pin5);//Pi.Gpio.Pin22, Pi.Gpio.Pin21);//Pi.Gpio.Pin06, Pi.Gpio.Pin05);x

            hat.D2 = new RgbLed(hat.pwm, 1, 0, 2);
            hat.D3 = new RgbLed(hat.pwm, 4, 3, 15);

            hat.S1 = new Servo(hat.pwm, 9);
            hat.S2 = new Servo(hat.pwm, 10);

            return hat;
        }

        /// <summary>
        /// Sets the duty cycle of the given pwm pin.
        /// </summary>
        /// <param name="pin">The pin to set the duty cycle for.</param>
        /// <param name="value">The new duty cycle between 0 (off) and 1 (on).</param>
        public void SetPwmDutyCycle(PwmPin pin, double value) {
            if (value < 0.0 || value > 1.0) throw new ArgumentOutOfRangeException(nameof(value));
            if (!Enum.IsDefined(typeof(PwmPin), pin)) throw new ArgumentException(nameof(pin));

            this.pwm.SetDutyCycle((int)pin, value);
        }

        /// <summary>
        /// Write the given value to the given pin.
        /// </summary>
        /// <param name="pin">The pin to set.</param>
        /// <param name="state">The new state of the pin.</param>
        public void WriteDigital(DigitalPin pin, bool state) {
            if (!Enum.IsDefined(typeof(DigitalPin), pin)) throw new ArgumentException(nameof(pin));

            var gpioPin = pin == DigitalPin.DIO16 ? this.dio16 : this.dio26;

            if (gpioPin.GetPinMode() != PinMode.Output)
                gpioPin.SetPinMode(PinMode.Output);

            gpioPin.Write(state ? PinValue.High : PinValue.Low);
        }

        /// <summary>
        /// Reads the current state of the given pin.
        /// </summary>
        /// <param name="pin">The pin to read.</param>
        /// <returns>True if high, false is low.</returns>
        public bool ReadDigital(DigitalPin pin) {
            if (!Enum.IsDefined(typeof(DigitalPin), pin)) throw new ArgumentException(nameof(pin));

            var gpioPin = pin == DigitalPin.DIO16 ? this.dio16 : this.dio26;

            if (gpioPin.GetPinMode() != PinMode.Input)
                gpioPin.SetPinMode(PinMode.Input);

            return gpioPin.Read() == true;
        }

        /// <summary>
        /// Reads the current voltage on the given pin.
        /// </summary>
        /// <param name="pin">The pin to read.</param>
        /// <returns>The voltage between 0 (0V) and 1 (3.3V).</returns>
        public double ReadAnalog(AnalogPin pin) {
            if (!Enum.IsDefined(typeof(AnalogPin), pin)) throw new ArgumentException(nameof(pin));

            return this.analog.Read((byte)pin);
        }

        /// <summary>
        /// The possible analog pins.
        /// </summary>
        public enum AnalogPin {
            /// <summary>An analog pin.</summary>
            Ain1 = 1,
            /// <summary>An analog pin.</summary>
            Ain2 = 2,
            /// <summary>An analog pin.</summary>
            Ain3 = 3,
            /// <summary>An analog pin.</summary>
            Ain6 = 6,
            /// <summary>An analog pin.</summary>
            Ain7 = 7,
        }

        /// <summary>
        /// The possible pwm pins.
        /// </summary>
        public enum PwmPin {
            /// <summary>A pwm pin.</summary>
            Pwm5 = 5,
            /// <summary>A pwm pin.</summary>
            Pwm6 = 6,
            /// <summary>A pwm pin.</summary>
            Pwm7 = 7,
            /// <summary>A pwm pin.</summary>
            Pwm11 = 11,
            /// <summary>A pwm pin.</summary>
            Pwm12 = 12,
        }

        /// <summary>
        /// The possible digital pins.
        /// </summary>
        public enum DigitalPin {
            /// <summary>A digital pin.</summary>
            DIO16,
            /// <summary>A digital pin.</summary>
            DIO26
        }

        /// <summary>
        /// Represents a color of the onboard LEDs.
        /// </summary>
        public class Color {
            /// <summary>
            /// The red channel intensity.
            /// </summary>
            public byte R { get; }
            /// <summary>
            /// The green channel intensity.
            /// </summary>
            public byte G { get; }
            /// <summary>
            /// The blue channel intensity.
            /// </summary>
            public byte B { get; }

            /// <summary>
            /// Constructs a new color.
            /// </summary>
            /// <param name="red">The red channel intensity.</param>
            /// <param name="green">The green channel intensity.</param>
            /// <param name="blue">The blue channel intensity.</param>
            public Color(byte red, byte green, byte blue) {
                this.R = red;
                this.G = green;
                this.B = blue;
            }

            /// <summary>
            /// A predefined red color.
            /// </summary>
            public static Color Red => new Color(255, 0, 0);

            /// <summary>
            /// A predefined green color.
            /// </summary>
            public static Color Green => new Color(0, 255, 0);

            /// <summary>
            /// A predefined blue color.
            /// </summary>
            public static Color Blue => new Color(0, 0, 255);

            /// <summary>
            /// A predefined cyan color.
            /// </summary>
            public static Color Cyan => new Color(0, 255, 255);

            /// <summary>
            /// A predefined magneta color.
            /// </summary>
            public static Color Magneta => new Color(255, 0, 255);

            /// <summary>
            /// A predefined yellow color.
            /// </summary>
            public static Color Yellow => new Color(255, 255, 0);

            /// <summary>
            /// A predefined white color.
            /// </summary>
            public static Color White => new Color(255, 255, 255);

            /// <summary>
            /// A predefined black color.
            /// </summary>
            public static Color Black => new Color(0, 0, 0);
        }

        /// <summary>
        /// Represents an onboard RGB led.
        /// </summary>
        public class RgbLed {
            private PCA9685 pwm;
            private Color color;
            private int redChannel;
            private int greenChannel;
            private int blueChannel;

            /// <summary>
            /// The current color of the LED.
            /// </summary>
            public Color Color {
                get {
                    return this.color;
                }
                set {
                    this.color = value;

                    this.pwm.SetDutyCycle(this.redChannel, value.R / 255.0);
                    this.pwm.SetDutyCycle(this.greenChannel, value.G / 255.0);
                    this.pwm.SetDutyCycle(this.blueChannel, value.B / 255.0);
                }
            }

            internal RgbLed(PCA9685 pwm, int redChannel, int greenChannel, int blueChannel) {
                this.color = Color.Black;
                this.pwm = pwm;
                this.redChannel = redChannel;
                this.greenChannel = greenChannel;
                this.blueChannel = blueChannel;
            }

            /// <summary>
            /// Turns the LED off.
            /// </summary>
            public void TurnOff() {
                this.pwm.SetDutyCycle(this.redChannel, 0.0);
                this.pwm.SetDutyCycle(this.greenChannel, 0.0);
                this.pwm.SetDutyCycle(this.blueChannel, 0.0);
            }
        }

        /// <summary>
        /// Represents an onboard servo.
        /// </summary>
        public class Servo {
            private PCA9685 pwm;
            private int channel;
            private double position;
            private double minAngle;
            private double maxAngle;
            private double scale;
            private double offset;
            private bool limitsSet;

            /// <summary>
            /// The current position of the servo between the minimumAngle and maximumAngle passed to SetLimits.
            /// </summary>
            public double Position {
                get {
                    return this.position;
                }
                set {
                    if (!this.limitsSet) throw new InvalidOperationException($"You must call {nameof(this.SetLimits)} first.");
                    if (value < this.minAngle || value > this.maxAngle) throw new ArgumentOutOfRangeException(nameof(value));

                    this.position = value;

                    this.pwm.SetChannel(this.channel, 0x0000, (ushort)(this.scale * value + this.offset));
                }
            }

            internal Servo(PCA9685 pwm, int channel) {
                this.pwm = pwm;
                this.channel = channel;
                this.position = 0.0;
                this.limitsSet = false;
            }

            /// <summary>
            /// Sets the limits of the servo.
            /// </summary>
            /// <param name="minimumPulseWidth">The minimum pulse width in milliseconds.</param>
            /// <param name="maximumPulseWidth">The maximum pulse width in milliseconds.</param>
            /// <param name="minimumAngle">The minimum angle of input passed to Position.</param>
            /// <param name="maximumAngle">The maximum angle of input passed to Position.</param>
            public void SetLimits(int minimumPulseWidth, int maximumPulseWidth, double minimumAngle, double maximumAngle) {
                if (minimumPulseWidth < 0) throw new ArgumentOutOfRangeException(nameof(minimumPulseWidth));
                if (maximumPulseWidth < 0) throw new ArgumentOutOfRangeException(nameof(maximumPulseWidth));
                if (minimumAngle < 0) throw new ArgumentOutOfRangeException(nameof(minimumAngle));
                if (maximumAngle < 0) throw new ArgumentOutOfRangeException(nameof(maximumAngle));
                if (minimumPulseWidth >= maximumPulseWidth) throw new ArgumentException(nameof(minimumPulseWidth));
                if (minimumAngle >= maximumAngle) throw new ArgumentException(nameof(minimumAngle));

                if (this.pwm.Frequency != 50)
                    this.pwm.Frequency = 50;

                this.minAngle = minimumAngle;
                this.maxAngle = maximumAngle;

                var period = 1000000.0 / this.pwm.Frequency;

                minimumPulseWidth = (int)(minimumPulseWidth / period * 4096.0);
                maximumPulseWidth = (int)(maximumPulseWidth / period * 4096.0);

                this.scale = ((maximumPulseWidth - minimumPulseWidth) / (maximumAngle - minimumAngle));
                this.offset = minimumPulseWidth;

                this.limitsSet = true;
            }
        }

        /// <summary>
        /// Represents an onboard motor.
        /// </summary>
        public class Motor : IDisposable {
            private double speed;
            private bool disposed;
            private PCA9685 pwm;
            private GpioPin direction1;
            private GpioPin direction2;
            private int pwmChannel;

            /// <summary>
            /// The speed of the motor. The sign controls the direction while the magnitude controls the speed (0 is off, 1 is full speed).
            /// </summary>
            public double Speed {
                get {
                    return this.speed;
                }
                set {
                    this.pwm.SetDutyCycle(this.pwmChannel, 0);

                    this.direction1.Write(value > 0 ? PinValue.High : PinValue.Low);
                    this.direction2.Write(value > 0 ? PinValue.Low : PinValue.High);

                    this.pwm.SetDutyCycle(this.pwmChannel, Math.Abs(value));

                    this.speed = value;
                }
            }

            /// <summary>
            /// Disposes of the object releasing control the pins.
            /// </summary>
            public void Dispose() => this.Dispose(true);

            internal Motor(PCA9685 pwm, int pwmChannel, GpioPin direction1Pin, GpioPin direction2Pin) {
                //var gpioController = GpioController.GetDefault();

                this.speed = 0.0;
                this.pwm = pwm;
                this.disposed = false;
               
                this.direction1 = direction1Pin;
                this.direction2 = direction2Pin;
                this.pwmChannel = pwmChannel;

                this.direction1.SetPinMode(PinMode.Output);// = (GpioPinDriveMode.Output);
                this.direction2.SetPinMode(PinMode.Output);//PinMode = (GpioPinDriveMode.Output);
            }

            /// <summary>
            /// Stops the motor.
            /// </summary>
            public void Stop() {
                this.pwm.SetDutyCycle(this.pwmChannel, 0.0);
            }

            /// <summary>
            /// Disposes of the object releasing control the pins.
            /// </summary>
            /// <param name="disposing">Whether or not this method is called from Dispose().</param>
            protected virtual void Dispose(bool disposing) {
                if (!this.disposed) {
                    if (disposing) {
                        //this.direction1.Dispose();
                        //this.direction2.Dispose();
                    }

                    this.disposed = true;
                }
            }
        }
    }
}