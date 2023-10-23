using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.Leds;
using Meadow.Hardware;
using Meadow.Peripherals.Leds;
using Meadow.Units;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MeadowApp
{
    // Change F7FeatherV2 to F7FeatherV1 for V1.x boards
    public class MeadowApp : App<F7FeatherV2>
    {
        RgbPwmLed onboardLed;

        ISerialMessagePort serialPort;

        IAnalogInputPort[] analogInputs = new IAnalogInputPort[6];

        int[] analogValues = new int[] { 0, 0, 0, 0, 0, 0 };

        IDigitalInputPort[] digitalInputs = new IDigitalInputPort[4];

        IDigitalOutputPort[] digitalOutputs = new IDigitalOutputPort[4];

        bool[] digitalOutputStates = new bool[] { false, false, false, false };

        string outputString = "###p##ADC0ADC1ADC2ADC3ADC4ADC5bbbbcs#\r\n";

        int packetCounter;

        public override Task Initialize()
        {
            Resolver.Log.Info("Initialize...");

            analogInputs[0] = Device.CreateAnalogInputPort(Device.Pins.A00);
            analogInputs[1] = Device.CreateAnalogInputPort(Device.Pins.A01);
            analogInputs[2] = Device.CreateAnalogInputPort(Device.Pins.A02);
            analogInputs[3] = Device.CreateAnalogInputPort(Device.Pins.A03);
            analogInputs[4] = Device.CreateAnalogInputPort(Device.Pins.A04);
            analogInputs[5] = Device.CreateAnalogInputPort(Device.Pins.A05);

            analogInputs[0].Updated += AnalogIn00_Updated;
            analogInputs[1].Updated += AnalogIn01_Updated;
            analogInputs[2].Updated += AnalogIn02_Updated;
            analogInputs[3].Updated += AnalogIn03_Updated;
            analogInputs[4].Updated += AnalogIn04_Updated;
            analogInputs[5].Updated += AnalogIn05_Updated;

            int analogMeasureIntervalMilliseconds = 100;

            analogInputs[0].StartUpdating(TimeSpan.FromMilliseconds(analogMeasureIntervalMilliseconds));
            analogInputs[1].StartUpdating(TimeSpan.FromMilliseconds(analogMeasureIntervalMilliseconds));
            analogInputs[2].StartUpdating(TimeSpan.FromMilliseconds(analogMeasureIntervalMilliseconds));
            analogInputs[3].StartUpdating(TimeSpan.FromMilliseconds(analogMeasureIntervalMilliseconds));
            analogInputs[4].StartUpdating(TimeSpan.FromMilliseconds(analogMeasureIntervalMilliseconds));
            analogInputs[5].StartUpdating(TimeSpan.FromMilliseconds(analogMeasureIntervalMilliseconds));

            digitalInputs[0] = Device.CreateDigitalInputPort(Device.Pins.D01, InterruptMode.EdgeBoth);
            digitalInputs[1] = Device.CreateDigitalInputPort(Device.Pins.D02, InterruptMode.EdgeBoth);
            digitalInputs[2] = Device.CreateDigitalInputPort(Device.Pins.D03, InterruptMode.EdgeBoth);
            digitalInputs[3] = Device.CreateDigitalInputPort(Device.Pins.D04, InterruptMode.EdgeBoth);

            digitalOutputs[0] = Device.CreateDigitalOutputPort(Device.Pins.D05, false);
            digitalOutputs[1] = Device.CreateDigitalOutputPort(Device.Pins.D06, false);
            digitalOutputs[2] = Device.CreateDigitalOutputPort(Device.Pins.D07, false);
            digitalOutputs[3] = Device.CreateDigitalOutputPort(Device.Pins.D08, false);

            onboardLed = new RgbPwmLed(
                redPwmPin: Device.Pins.OnboardLedRed,
                greenPwmPin: Device.Pins.OnboardLedGreen,
                bluePwmPin: Device.Pins.OnboardLedBlue,
                CommonType.CommonAnode);

            serialPort = Device.CreateSerialMessagePort(
                Device.PlatformOS.GetSerialPortName("COM1"),
                suffixDelimiter: Encoding.UTF8.GetBytes("\n"),
                preserveDelimiter: true);

            serialPort.MessageReceived += SerialPort_MessageReceived;
            serialPort.BaudRate = 115200;
            serialPort.Open();
            onboardLed.SetColor(Color.Green);

            return base.Initialize();
        }

        private void AnalogIn00_Updated(object sender, IChangeResult<Voltage> e)
        {
            //Console.WriteLine(e.New.Millivolts);
            int milliVolts = Convert.ToInt32(e.New.Millivolts);
            analogValues[0] = milliVolts; ;
            //Console.WriteLine(milliVolts00);

        }
        private void AnalogIn01_Updated(object sender, IChangeResult<Voltage> e)
        {
            //Console.WriteLine(e.New.Millivolts);
            int milliVolts = Convert.ToInt32(e.New.Millivolts);
            analogValues[1] = milliVolts;
            //Console.WriteLine(milliVolts00);

        }
        private void AnalogIn02_Updated(object sender, IChangeResult<Voltage> e)
        {
            //Console.WriteLine(e.New.Millivolts);
            int milliVolts = Convert.ToInt32(e.New.Millivolts);
            analogValues[2] = milliVolts; ;
            //Console.WriteLine(milliVolts00);

        }
        private void AnalogIn03_Updated(object sender, IChangeResult<Voltage> e)
        {
            //Console.WriteLine(e.New.Millivolts);
            int milliVolts = Convert.ToInt32(e.New.Millivolts);
            analogValues[3] = milliVolts; ;
            //Console.WriteLine(milliVolts00);

        }
        private void AnalogIn04_Updated(object sender, IChangeResult<Voltage> e)
        {
            //Console.WriteLine(e.New.Millivolts);
            int milliVolts = Convert.ToInt32(e.New.Millivolts);
            analogValues[4] = milliVolts;
            //Console.WriteLine(milliVolts00);

        }
        private void AnalogIn05_Updated(object sender, IChangeResult<Voltage> e)
        {
            //Console.WriteLine(e.New.Millivolts);
            int milliVolts = Convert.ToInt32(e.New.Millivolts);
            analogValues[5] = milliVolts;
            //Console.WriteLine(milliVolts00);

        }

        void SerialPort_MessageReceived(object sender, SerialMessageData e)
        {
            string packet = e.GetMessageString(Encoding.UTF8);
            Console.WriteLine($"Packet Recevied: {packet}");
            if(packet.Substring(0,3) == "###")
            {
                //We have a valid header
                int checksumRecevied = Convert.ToInt16(packet.Substring(7, 3));
                int checksumCalculated = 0;
                byte[] packetByteArray = System.Text.Encoding.Unicode.GetBytes(packet.Substring(3,4));
                foreach(byte Byte in packetByteArray)
                {
                    checksumCalculated += Byte;
                }
                if(checksumRecevied == checksumCalculated)
                {
                    Console.WriteLine("Valid Checksum");
                    for(int i = 0; i < 4; i++)
                    {
                        if(packet.Substring(i + 3,1) == "1")
                        {
                            digitalOutputs[i].State = true;
                            Console.WriteLine($"Output #{i+3} set HIGH");
                        }else if(packet.Substring(i + 3, 1) == "0")
                        {
                            digitalOutputs[i].State = false;
                            Console.WriteLine($"Output #{i+3} set LOW");
                        }
                        else
                        {
                            Console.WriteLine($"Data Parsing Error at bit #{i+3}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Checksum Error. Received:{checksumRecevied} - Calculated:{checksumCalculated}");
                }
            }
            else
            {
                Console.WriteLine("Invalid Packet Header");
            }
        }

        public override Task Run()
        {
            Resolver.Log.Info("Run...");

            //var buffer = System.Text.Encoding.Unicode.GetBytes(outputString);
            //


            while (true)
            {
                outputString = "###";
                //Packet Counter
                packetCounter %= 1000;
                outputString += packetCounter++.ToString("D3");

                //Analog Values
                foreach(int analogValue in analogValues)
                {
                    outputString += analogValue.ToString("D4");
                }

                //Digital Values
                foreach(IDigitalInputPort inputPin in digitalInputs)
                {
                    outputString += (inputPin.State ? "1" : "0").ToString();
                }

                //Checksum
                int checkSum = 0;
                var buffer = Encoding.UTF8.GetBytes(outputString.Substring(3,31));

                foreach (byte Byte in buffer)
                {
                    checkSum += Byte;
                }
                checkSum %= 1000;

                outputString += checkSum.ToString();    

                outputString += "\n";

                buffer = Encoding.UTF8.GetBytes(outputString);
                serialPort.Write(buffer);
                //Console.WriteLine(outputString);
                Thread.Sleep(100);
            }

            return base.Run();
        }

    }
}
