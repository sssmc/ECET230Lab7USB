namespace ECET230Lab7USBDesktop;
using System.IO.Ports;

public partial class MainPage : ContentPage
{
	SerialPort serialPort;
	bool comPortIsOpen;

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
            serialPort.Open();
			comPortStartButton.Text = "Close";
		}
    }
}


