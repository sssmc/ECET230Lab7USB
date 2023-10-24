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

	//Most Recent Received Packet
	string newPacket;


	public MainPage()
	{

        comPortIsOpen = false;

		serialPort = new SerialPort();

		InitializeComponent();

		//Get all com ports and add them to the picker drop-down

		comPortPicker.ItemsSource = SerialPort.GetPortNames();
		Loaded += MainPage_Loaded;

	}

	private void MainPage_Loaded(object sender, EventArgs e)
	{
		//Set Serial Port Perameters
        serialPort.BaudRate = 115200;
		serialPort.ReceivedBytesThreshold = 1;
		serialPort.DataReceived += SerialPort_DataRecevied;

		//Reset All Packets Stats
		totalPacketCount = 0;
		totalMissedPacketCount = 0;
		lastPacketCountValue = 0;
    }

	private void SerialPort_DataRecevied(object sender, SerialDataReceivedEventArgs e)
	{
		//When we receive a line from the serial port
		newPacket = serialPort.ReadLine();
		Console.WriteLine(newPacket);
		MainThread.BeginInvokeOnMainThread(MyMainThreadCode);
	}

	private void MyMainThreadCode()
	{
		//Set the raw packet data lable text
		rawPacketLabel.Text = newPacket;

        //Verify Checksum
        try
        {
			//Parse receviced checksum and set the receviced checksum lable text
            receivedChecksum = Convert.ToInt16(newPacket.Substring(34, 3));
            rawChecksumLabel.Text = receivedChecksum.ToString("D3");
        }
        catch (Exception)
        {
			//If we have an error reading the checksum
            rawChecksumLabel.Text = "Error";
			rawChecksumLabel.TextColor = Color.FromRgb(255, 0, 0);
        }

		//Calculate the checksum
        int calculatedChecksum = 0;

		//Sum the data in the packet
        var buffer = Encoding.UTF8.GetBytes(newPacket.Substring(3, 31));
        foreach (byte Byte in buffer)
        {
            calculatedChecksum += Byte;
        }

		//Trucate the checksum
		calculatedChecksum %= 1000;

		//Set the calculated checksum lable
        checksumCalculatedLabel.Text = calculatedChecksum.ToString("D3");

		//Only displays the values if the checksums match
        if (calculatedChecksum == receivedChecksum)
		{

			//Set the checksum text color to green
			rawChecksumLabel.TextColor = Color.FromRgb(0, 255, 0);
			checksumCalculatedLabel.TextColor = Color.FromRgb(0, 255, 0);

			//Parse the packet count
			int currentPacketCount = Convert.ToInt16(newPacket.Substring(3, 3));

			//If this is the first packet we have receied, set inital value of lastPacketCountValue
			if(totalPacketCount == 0)
			{
				lastPacketCountValue = currentPacketCount - 1;
			}

            totalPacketCount++;
			
			//If the last packet is not the current packet - 1(ie we have missed some packets)
            if (currentPacketCount != lastPacketCountValue + 1)
			{
				//Check for the 999-000 roll-over case
				if ((currentPacketCount != 0 && lastPacketCountValue != 999))
				{
					//Calculate how many packets we have missed
					totalMissedPacketCount += currentPacketCount - (lastPacketCountValue + 1);

				}
            }

			//Reset the lastPacketCount value
			lastPacketCountValue = currentPacketCount;

			//Set the total packets received lable text
			packetsReceivedLabel.Text = totalPacketCount.ToString();

			//Calculated the percetage of packets lost and set the packets lost lable text
			packLossLabel.Text = $"{totalMissedPacketCount}({(((float)totalMissedPacketCount/(float)totalPacketCount)*100.0f):00.00}%)";

			//Set packet count lable from the parsed packet count
			rawPacketCountLabel.Text = currentPacketCount.ToString("D3");

			//Parse each AD value from the packet
			rawADCInputs[0] = Convert.ToInt16(newPacket.Substring(6, 4));
			rawADCInputs[1] = Convert.ToInt16(newPacket.Substring(10, 4));
			rawADCInputs[2] = Convert.ToInt16(newPacket.Substring(14, 4));
			rawADCInputs[3] = Convert.ToInt16(newPacket.Substring(18, 4));
			rawADCInputs[4] = Convert.ToInt16(newPacket.Substring(22, 4));
			rawADCInputs[5] = Convert.ToInt16(newPacket.Substring(26, 4));

			//Display the values in the labels
			rawADC0Label.Text = rawADCInputs[0].ToString("D4") + "mV";
			rawADC1Label.Text = rawADCInputs[1].ToString("D4") + "mV";
			rawADC2Label.Text = rawADCInputs[2].ToString("D4") + "mV";
			rawADC3Label.Text = rawADCInputs[3].ToString("D4") + "mV";
			rawADC4Label.Text = rawADCInputs[4].ToString("D4") + "mV";
			rawADC5Label.Text = rawADCInputs[5].ToString("D4") + "mV";

			//Displays the values in the progress bars
			adc0ProgressBar.Progress = (float)rawADCInputs[0] / 3300.0f;
			adc1ProgressBar.Progress = (float)rawADCInputs[1] / 3300.0f;
			adc2ProgressBar.Progress = (float)rawADCInputs[2] / 3300.0f;
			adc3ProgressBar.Progress = (float)rawADCInputs[3] / 3300.0f;
			adc4ProgressBar.Progress = (float)rawADCInputs[4] / 3300.0f;
			adc5ProgressBar.Progress = (float)rawADCInputs[5] / 3300.0f;

			//For every digital input
			string digitalInputsText = "";
			for(int i = 0; i < 4; i++)
			{
				//Parse the value of each bit
				rawDigtalInputs[i] = newPacket.Substring(i + 30, 1) == "1" ? true : false;

				//Update the digital inputs label
				digitalInputsText += newPacket.Substring(i + 30, 1) + " ";
			}

			//Change the color of the circle indicator bases the the digital input
			digitalInput0Ellipse.Fill = rawDigtalInputs[0] ? Color.FromRgb(255,0,0) : Color.FromRgb(128, 128, 128);
			digitalInput1Ellipse.Fill = rawDigtalInputs[1] ? Color.FromRgb(255, 0, 0) : Color.FromRgb(128, 128, 128);
			digitalInput2Ellipse.Fill = rawDigtalInputs[2] ? Color.FromRgb(255, 0, 0) : Color.FromRgb(128, 128, 128);
			digitalInput3Ellipse.Fill = rawDigtalInputs[3] ? Color.FromRgb(255, 0, 0) : Color.FromRgb(128, 128, 128);

			//Write the final digital inputs text to the label
			rawDigitalInputsLabel.Text = digitalInputsText;

		}
		else
		{
			//If our checksums did not match
			totalMissedPacketCount++;

			//Set the checksum text color to red
			rawChecksumLabel.TextColor = Color.FromRgb(255, 0, 0);
			checksumCalculatedLabel.TextColor = Color.FromRgb(255, 0, 0);
		}
    }

    private void comPortStartButton_Clicked(object sender, EventArgs e)
    {
		//If we click the com port button
		if (comPortIsOpen)
		{
			//Close the port
			comPortIsOpen = false;
			serialPort.Close();
			comPortStartButton.Text = "Open";
		}
		else
		{
			//Open the port
			comPortIsOpen = true;
            serialPort.PortName = comPortPicker.SelectedItem.ToString();

			//Reset our packet stats
            totalPacketCount = 0;
            totalMissedPacketCount = 0;
            lastPacketCountValue = 0;

            serialPort.Open();

			comPortStartButton.Text = "Close";
        }
    }

	private void digitalOutput0Switch_Toggled(object sender, ToggledEventArgs e)
	{
		//If the digital output switch was toggled

		//Set the digital output states
		digitalOutputStates[0] = e.Value;

		//Send the packet to the meadow board
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
		//Packet header
		string packet = "###";

		//Only send the packet if the com port is open
		if (comPortIsOpen)
		{
			//Add the digital output states to the packet
			foreach (bool state in digitalOutputStates)
			{
				packet += state ? "1" : "0";
			}


			//Calculate the checksum
			int checksum = 0;

			foreach (byte Byte in Encoding.Unicode.GetBytes(packet.Substring(3, 4)))
			{
				checksum += Byte;
			}

			//Truncate the checksum
			checksum %= 1000;

			//Add the checksum to the packet
			packet += checksum.ToString("D3");

			//Set the packet sent lable text
			rawPacketSentLabel.Text = packet;

			//Send the packet to the meadow board
			serialPort.WriteLine(packet);
		}
    }
}


