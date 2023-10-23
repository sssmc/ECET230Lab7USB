namespace ECET230Lab7USBDesktop;

using System.IO.Ports;
using System.Text;

public partial class MainPage : ContentPage
{
	SerialPort serialPort;
	bool comPortIsOpen;

	int totalPacketCount;
	int totalMissedPacketCount;
	int lastPacketCountValue;

	int receivedChecksum = 0;

	bool[] digitalOutputStates = new bool[] { false, false, false, false };

	int[] rawADCInputs = new int[] { 0, 0, 0, 0, 0, 0 };

	bool[] rawDigtalInputs = new bool[] { false, false, false, false };

	string newPacket;
	public MainPage()
	{
        comPortIsOpen = false;
		serialPort = new SerialPort();
		InitializeComponent();
		comPortPicker.ItemsSource = SerialPort.GetPortNames();
		Loaded += MainPage_Loaded;

	}

	private void MainPage_Loaded(object sender, EventArgs e)
	{
        serialPort.BaudRate = 115200;
		serialPort.ReceivedBytesThreshold = 1;
		serialPort.DataReceived += SerialPort_DataRecevied;

		totalPacketCount = 0;
		totalMissedPacketCount = 0;
		lastPacketCountValue = 0;
    }

	private void SerialPort_DataRecevied(object sender, SerialDataReceivedEventArgs e)
	{
		newPacket = serialPort.ReadLine();
		Console.WriteLine(newPacket);
		MainThread.BeginInvokeOnMainThread(MyMainThreadCode);
	}

	private void MyMainThreadCode()
	{
		rawPacketLabel.Text = newPacket;

        //Verify Checksum
        try
        {
            receivedChecksum = Convert.ToInt16(newPacket.Substring(34, 3));
            rawChecksumLabel.Text = receivedChecksum.ToString("D3");
        }
        catch (Exception)
        {
            rawChecksumLabel.Text = "Error";
			rawChecksumLabel.TextColor = Color.FromRgb(255, 0, 0);
        }

        int calculatedChecksum = 0;
        var buffer = Encoding.UTF8.GetBytes(newPacket.Substring(3, 31));
        foreach (byte Byte in buffer)
        {
            calculatedChecksum += Byte;
        }
		calculatedChecksum %= 1000;

        checksumCalculatedLabel.Text = calculatedChecksum.ToString("D3");

        if (calculatedChecksum == receivedChecksum)
		{

			rawChecksumLabel.TextColor = Color.FromRgb(0, 255, 0);
			checksumCalculatedLabel.TextColor = Color.FromRgb(0, 255, 0);

			int currentPacketCount = Convert.ToInt16(newPacket.Substring(3, 3));

			if(totalPacketCount == 0)
			{
				lastPacketCountValue = currentPacketCount - 1;
			}

            totalPacketCount++;


            if (currentPacketCount != lastPacketCountValue + 1)
			{
				if ((currentPacketCount != 0 && lastPacketCountValue != 999))
				{
					totalMissedPacketCount += currentPacketCount - (lastPacketCountValue + 1);

				}
            }

			lastPacketCountValue = currentPacketCount;

			packetsReceivedLabel.Text = totalPacketCount.ToString();
			packLossLabel.Text = $"{totalMissedPacketCount}({(((float)totalMissedPacketCount/(float)totalPacketCount)*100.0f):00.00}%)";

			rawPacketCountLabel.Text = currentPacketCount.ToString("D3");

			rawADCInputs[0] = Convert.ToInt16(newPacket.Substring(6, 4));
			rawADCInputs[1] = Convert.ToInt16(newPacket.Substring(10, 4));
			rawADCInputs[2] = Convert.ToInt16(newPacket.Substring(14, 4));
			rawADCInputs[3] = Convert.ToInt16(newPacket.Substring(18, 4));
			rawADCInputs[4] = Convert.ToInt16(newPacket.Substring(22, 4));
			rawADCInputs[5] = Convert.ToInt16(newPacket.Substring(26, 4));

			rawADC0Label.Text = rawADCInputs[0].ToString("D4") + "mV";
			rawADC1Label.Text = rawADCInputs[1].ToString("D4") + "mV";
			rawADC2Label.Text = rawADCInputs[2].ToString("D4") + "mV";
			rawADC3Label.Text = rawADCInputs[3].ToString("D4") + "mV";
			rawADC4Label.Text = rawADCInputs[4].ToString("D4") + "mV";
			rawADC5Label.Text = rawADCInputs[5].ToString("D4") + "mV";
			
			adc0ProgressBar.Progress = (float)rawADCInputs[0] / 3300.0f;
			adc1ProgressBar.Progress = (float)rawADCInputs[1] / 3300.0f;
			adc2ProgressBar.Progress = (float)rawADCInputs[2] / 3300.0f;
			adc3ProgressBar.Progress = (float)rawADCInputs[3] / 3300.0f;
			adc4ProgressBar.Progress = (float)rawADCInputs[4] / 3300.0f;
			adc5ProgressBar.Progress = (float)rawADCInputs[5] / 3300.0f;

			string digitalInputsText = "";
			for(int i = 0; i < 4; i++)
			{
				rawDigtalInputs[i] = newPacket.Substring(i + 30, 1) == "1" ? true : false;
				digitalInputsText += newPacket.Substring(i + 30, 1) + " ";
			}

			digitalInput0Ellipse.Fill = rawDigtalInputs[0] ? Color.FromRgb(255,0,0) : Color.FromRgb(128, 128, 128);
			digitalInput1Ellipse.Fill = rawDigtalInputs[1] ? Color.FromRgb(255, 0, 0) : Color.FromRgb(128, 128, 128);
			digitalInput2Ellipse.Fill = rawDigtalInputs[2] ? Color.FromRgb(255, 0, 0) : Color.FromRgb(128, 128, 128);
			digitalInput3Ellipse.Fill = rawDigtalInputs[3] ? Color.FromRgb(255, 0, 0) : Color.FromRgb(128, 128, 128);

			rawDigitalInputsLabel.Text = digitalInputsText;

		}
		else
		{
			totalMissedPacketCount++;
			rawChecksumLabel.TextColor = Color.FromRgb(255, 0, 0);
			checksumCalculatedLabel.TextColor = Color.FromRgb(255, 0, 0);
		}
    }

    private void comPortStartButton_Clicked(object sender, EventArgs e)
    {
		if (comPortIsOpen)
		{
			comPortIsOpen = false;
			serialPort.Close();
			comPortStartButton.Text = "Open";
		}
		else
		{
			comPortIsOpen = true;
            serialPort.PortName = comPortPicker.SelectedItem.ToString();

            totalPacketCount = 0;
            totalMissedPacketCount = 0;
            lastPacketCountValue = 0;

            serialPort.Open();
			comPortStartButton.Text = "Close";
        }
    }

	private void digitalOutput0Switch_Toggled(object sender, ToggledEventArgs e)
	{
		digitalOutputStates[0] = e.Value;
        sendPacket();
    }
	private void digitalOutput1Switch_Toggled(object sender, ToggledEventArgs e)
	{
		digitalOutputStates[1] = e.Value;
        sendPacket();
    }

	private void digitalOutput2Switch_Toggled(object sender, ToggledEventArgs e)
	{
		digitalOutputStates[2] = e.Value;
        sendPacket();
    }

	private void digitalOutput3Switch_Toggled(object sender, ToggledEventArgs e)
	{
		digitalOutputStates[3] = e.Value;
        sendPacket();
    }

	private void sendPacket()
	{
		string packet = "###";

		foreach(bool state in digitalOutputStates)
		{
			packet += state ? "1" : "0";
		}

		int checksum = 0;

		foreach(byte Byte in Encoding.Unicode.GetBytes(packet.Substring(3, 4)))
		{
			checksum += Byte;
		}

		checksum %= 1000;

		packet += checksum.ToString("D3");

        rawPacketSentLabel.Text = packet;

        serialPort.WriteLine(packet);
    }
}


