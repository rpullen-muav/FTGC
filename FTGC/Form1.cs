using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml.Serialization;

namespace FTGC
{
    public partial class Form1 : Form
    {
		public const int SIO_UDP_CONNRESET = -1744830452;
		public bool connected = false;
		public UDPListener UDPL;
		public NodeHandler NodeManager;
		public DeviceMessage DM;
		public List<double> LatList;
		public List<double> LonList;



		public Form1()
        {
            InitializeComponent();
			tbTerminal.ScrollBars = ScrollBars.Vertical;
			NodeManager = new NodeHandler();
			NodeManager.onGetUpdate += new NodeHandler.StatusUpdateHandler(GetUpdate);
			DM = new DeviceMessage();
			pictureBox1.Image = TrackingCompass.DrawCompass(0, 0, 0, 80, 0, 80, pictureBox1.Size);
			chart1.ChartAreas[0].AxisY.IsStartedFromZero = false;
			chart1.ChartAreas[0].AxisX.IsStartedFromZero = false;
			chart1.Series.Clear();
			LatList = new List<double>();
			LonList = new List<double>();
			chart1.ChartAreas[0].AxisY.LabelStyle.Format = "0.00000";

		}
		private void Form1_Load(object sender, EventArgs e)
		{
			

		}
		public System.Windows.Forms.Timer timer1;
		public void InitTimer()
		{
			timer1 = new System.Windows.Forms.Timer();
			timer1.Tick += new EventHandler(timer1_Tick);
			timer1.Interval = 500; // in miliseconds
			timer1.Start();
		}


		private void timer1_Tick(object sender, EventArgs e)
		{
			//Connect(tbIP.Text, tbPort.Text);
			//GetData(s);

		}

		public void btnConnect_Click(object sender, EventArgs e)
		{
			Button btn = sender as Button;

			toggleConnnection(btn);
		}

		public void toggleConnnection(Button btn)
		{
			if (!connected && btn.Text == "Connect")
			{
				button2.Enabled = false;
				btnAddGroup.Enabled = true;
				btnRemoveGroup.Enabled = true;
				btn.Text = "Disconnect";
				btn.BackColor = Color.IndianRed;
				connected = true;
				UDPL = new UDPListener(tbIP.Text, tbPort.Text);
				UDPL.NewMessageReceived += UDPL_NewMessageReceived;
				UDPL.StartListener(1024);
			}
			else
			{
				btnAddGroup.Enabled = false;
				btnRemoveGroup.Enabled = false;
				button2.Enabled = true;
				UDPL.NewMessageReceived -= UDPL_NewMessageReceived;
				UDPL.StopListener();
				UDPL.CloseListener();
				connected = false;
				btn.Text = "Connect";
				btn.BackColor = SystemColors.Control;
			}
		}

		private void GetUpdate(object sender, MyMessageArgs2 e)
		{
			if (NodeManager.NodeAdded)
			{
				RowStyle temp = tlpCotTable.RowStyles[tlpCotTable.RowCount - 1];
				NodeManager.AddRowToTable(tlpCotTable, e.data);
				NodeManager.NodeAdded = false;
				if (chart1.Series.IsUniqueName(e.data.DeviceName))
				{
					chart1.Invoke((Action)delegate { chart1.Series.Add(e.data.DeviceName); });
					if (e.data.DeviceName.Contains("sr.455082028"))
					{
						chart1.Invoke((Action)delegate { chart1.Series[e.data.DeviceName].Color = NodeManager.TargetColor; });
						NodeManager.NodeCount -= 1;
					}
					else
					{
						chart1.Invoke((Action)delegate { chart1.Series[e.data.DeviceName].Color = e.data.ChartColor; });
					}
					//chart1.Invoke((Action)delegate { chart1.Series[e.data.DeviceName].Color = e.data.ChartColor; });
					chart1.Invoke((Action)delegate { chart1.Series[e.data.DeviceName].ChartType = SeriesChartType.Point; });
				}
				
				
			}

			if (NodeManager.DataAdded)
			{
				LatList.Add(Convert.ToDouble(e.data.Lattitude, System.Globalization.CultureInfo.GetCultureInfo("en-US")));
				LonList.Add(Convert.ToDouble(e.data.Longitude, System.Globalization.CultureInfo.GetCultureInfo("en-US")));
				
				foreach (KeyValuePair<string, Node> d in NodeManager.NodeDictionary)
				{
					tlpCotTable.Invoke((Action)delegate { tlpCotTable.SuspendLayout(); });

					
					Label lat = new Label();
					lat = (Label)GetLabelByName(tlpCotTable, $"{e.data.DeviceName}_Lat");
					double latlat = double.Parse(lat.Text);
					lat.Invoke((Action)delegate { lat.Text = e.data.Lattitude; });

					Label lon = (Label)GetLabelByName(tlpCotTable, $"{e.data.DeviceName}_Lon");
					double lonlon = Convert.ToDouble(lon.Text, System.Globalization.CultureInfo.GetCultureInfo("en-US")); // grab the value before it changes
					
					lon.Invoke((Action)delegate { lon.Text = e.data.Longitude; });

					Label hae = (Label)GetLabelByName(tlpCotTable, $"{e.data.DeviceName}_Hae");
					hae.Invoke((Action)delegate { hae.Text = e.data.HeightAboveGeoid; });

					Label ce = (Label)GetLabelByName(tlpCotTable, $"{e.data.DeviceName}_Ce");
					ce.Invoke((Action)delegate { ce.Text = e.data.CircularError; });

					Label le = (Label)GetLabelByName(tlpCotTable, $"{e.data.DeviceName}_Le");
					le.Invoke((Action)delegate { le.Text = e.data.LinearError; });
					tlpCotTable.Invoke((Action)delegate {tlpCotTable.ResumeLayout();});

					chart1.Invoke
							((Action)delegate
								{
									chart1.Series[d.Value.DeviceName].Points.AddXY
									(
									NodeManager.StringLL2Double(d.Value.Longitude),
									NodeManager.StringLL2Double(d.Value.Lattitude)
									);
								}
							);

					NodeManager.TargetExists = NodeManager.NodeDictionary.ContainsKey("sr.455082028");
					if (d.Value.isTracked)
					{
						if (!NodeManager.TargetExists)
						{
							double.TryParse(d.Value.Course, out double Number);
							pictureBox1.Invoke((Action)delegate { pictureBox1.Image = TrackingCompass.DrawCompass(Number, 0.0, 0, 80, 0, 80, pictureBox1.Size); });
							tbDistance.Invoke((Action)delegate { tbDistance.Text = "No Target"; });
						}
						else if (NodeManager.TargetExists)
						{
							Tuple<double, double> TargetBearingDistance = Geo.DegreeBearing(
								NodeManager.StringLL2Double(d.Value.Lattitude),
								NodeManager.StringLL2Double(d.Value.Longitude),
								NodeManager.TargetLat,
								NodeManager.TargetLon
								);

							if (NodeManager.UpdateTarget)
							{
								double.TryParse(d.Value.Course, out double Number);
								pictureBox1.Invoke((Action)delegate { pictureBox1.Image = TrackingCompass.DrawCompass(Number, TargetBearingDistance.Item1, 0, 80, 0, 80, pictureBox1.Size); });
								tbDistance.Invoke((Action)delegate { tbDistance.Text = $"{TargetBearingDistance.Item2.ToString("00.00")} NM"; });
								NodeManager.UpdateTarget = false;
							}
						}
					}
				}
				NodeManager.DataAdded = false;
			}
		}

		public Label GetLabelByName(TableLayoutPanel pnl, string Name)
		{
			foreach (Control c in pnl.Controls)
				if (c is Label)
					if (c.Name == Name)
						return (Label)c;
			return null;
		}

		public Button GetButtonByName(TableLayoutPanel pnl, string Name)
		{
			foreach (Button c in pnl.Controls)
				if (c.Name == Name)
					return c;

			return null;
		}

		private void UDPL_NewMessageReceived(object sender, MyMessageArgs e)
		{
			DM.ParseMessage(e, NodeManager, tbTerminal);
		}


		public Event DeserializeCOTMessage(string cot)
		{
			var serializer = new XmlSerializer(typeof(Event));
			Event result;
			using (TextReader reader = new StringReader(cot))
			{
				result = (Event)serializer.Deserialize(reader);
			}
			return result;
		}


		private void btnRaw_Click(object sender, EventArgs e)
		{
			NodeManager.ViewRaw = !NodeManager.ViewRaw;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			tbTerminal.Clear();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			
			//toggleConnnection(btnConnect);
			NodeManager.ClearTable(tlpCotTable);
			NodeManager.NodeDictionary = new Dictionary<string, Node>();
			//NodeManager.NodeAdded = true;
			NodeManager.RePopulateTable(tlpCotTable);
		}

		private void btnAddGroup_Click(object sender, EventArgs e)
		{
			UDPL.AddMulticastGroup(tbIP.Text);
		}

		private void btnRemoveGroup_Click(object sender, EventArgs e)
		{
			UDPL.DropMulticastGroup(tbIP.Text);
		}

		private void btnForget_Click(object sender, EventArgs e)
		{
			NodeManager.TargetLat = 0;
			NodeManager.TargetLon = 0;
		}
	}

	public class UDPListener
	{
		private int m_portToListen { get; set; }
		public string IP { get; set; }
		public UdpClient listener { get; set; }
		private volatile bool listening;
		Thread m_ListeningThread;
		public event EventHandler<MyMessageArgs> NewMessageReceived;
		

		//constructor
		public UDPListener(string ip, string Port)
		{
			m_portToListen = int.Parse(Port);
			IP = ip;
			listening = false;
			listener = new UdpClient(m_portToListen);
		}

		public void StartListener(int exceptedMessageLength)
		{
			if (!listening)
			{
				m_ListeningThread = new Thread(ListenForUDPPackages)
				{
					IsBackground = true
				};
				listening = true;
				m_ListeningThread.Start();
			}
		}

		public void StopListener()
		{
			listening = false;
		}

		public void CloseListener()
		{
			listener.Close();
		}

		public void AddMulticastGroup(string ip)
		{
			listener.JoinMulticastGroup(IPAddress.Parse(ip));
		}

		public void DropMulticastGroup(string ip)
		{
			listener.DropMulticastGroup(IPAddress.Parse(ip));
		}

		public void ListenForUDPPackages()
		{
			if (listener != null)
			{
				IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, m_portToListen);
				IPAddress multicastaddress = IPAddress.Parse(IP);
				NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

				foreach (NetworkInterface networkInterface in networkInterfaces)
				{
					if ((!networkInterface.Supports(NetworkInterfaceComponent.IPv4)) ||
						(networkInterface.OperationalStatus != OperationalStatus.Up))
					{
						continue;
					}

					IPInterfaceProperties adapterProperties = networkInterface.GetIPProperties();
					UnicastIPAddressInformationCollection unicastIPAddresses = adapterProperties.UnicastAddresses;
					IPAddress ipAddress = null;

					foreach (UnicastIPAddressInformation unicastIPAddress in unicastIPAddresses)
					{
						if (unicastIPAddress.Address.AddressFamily != AddressFamily.InterNetwork)
						{
							continue;
						}

						ipAddress = unicastIPAddress.Address;
						break;
					}

					if (ipAddress == null)
					{
						continue;
					}

					listener.JoinMulticastGroup(multicastaddress, ipAddress);
				}
					//listener.JoinMulticastGroup(multicastaddress);
				//listener.JoinMulticastGroup(IPAddress.Parse("239.10.212.230"));


				try
				{
					while (listening)
					{
						//Console.WriteLine("Waiting for UDP broadcast to port " + m_portToListen);
						byte[] bytes = listener.Receive(ref groupEP);

						//raise event                        
						NewMessageReceived(this, new MyMessageArgs(Tuple.Create(bytes, groupEP)));
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				finally
				{
					listener.Close();
					Console.WriteLine("Done listening for UDP broadcast");
				}
			}
		}
	}

	public class MyMessageArgs : EventArgs
	{
		public Tuple<byte[], IPEndPoint> data { get; set; }

		public MyMessageArgs(Tuple<byte[], IPEndPoint> newData)
		{
			data = newData;
		}
	}

	public class NodeHandler
	{
		public delegate void StatusUpdateHandler(object sender, MyMessageArgs2 e);
		public event StatusUpdateHandler onGetUpdate;


		public bool NewData { get; set; }
		public bool NodeAdded { get; set; }
		public bool DataAdded { get; set; }
		public bool locked { get; set; }
		public bool ViewRaw { get; set; }
		public List<Button> TrackingButtonList { get; set; }
		public Dictionary<string, Node> NodeDictionary { get; set; }
		public string TargetName { get; set; }
		public string TrackerName { get; set; }
		public double TargetLat { get; set; }
		public double TargetLon { get; set; }
		public double OldLat = 0.0;
		public double OldLon = 0.0;
		public bool UpdateTarget = false;
		public int NodeCount { get; set; }
		public Color TargetColor { get; set; }
		public Dictionary<int, Color> ColorDict { get; set; }
		public bool TargetExists = false;


		public NodeHandler()
		{
			NewData = false;
			NodeAdded = false;
			DataAdded = false;
			locked = false;
			ViewRaw = false;
			NodeDictionary = new Dictionary<string, Node>();
			TrackingButtonList = new List<Button>();
			TargetName = string.Empty;
			TargetLat = 0.0;
			TargetLon = 0.0;
			TrackerName = string.Empty;
			TargetColor = Color.Red;
			NodeCount = 0;
			ColorDict = new Dictionary<int, Color>()
			{
				{ 1, Color.Blue },
				{ 2, Color.Lime },
				{ 3, Color.Orange },
				{ 4, Color. Pink},
				{ 5, Color.Cyan },
				{ 6, Color.IndianRed },
				{ 7, Color.Goldenrod }
			};
		}
	

		public void GetUpdate(Node d)
		{
			MyMessageArgs2 args = new MyMessageArgs2(d);
			onGetUpdate(this, args);
		}

		public void TrackerButton_MouseDown(object sender, MouseEventArgs e)
		{
			Button btn = sender as Button;
			
			if (e.Clicks >= 2)
			{
				btn.BackColor = SystemColors.Control;
				TargetExists = true;
				TargetLat = StringLL2Double(NodeDictionary[btn.Text].Lattitude);
				TargetLon = StringLL2Double(NodeDictionary[btn.Text].Longitude);
			}
			else if (e.Clicks < 2)
			{
				
				if (!NodeDictionary[btn.Text].isTracked)
				{
					TrackingButtonList.Add(btn);
					btn.BackColor = Color.LightGreen;
					NodeDictionary[btn.Text].isTracked = true;
					Debug.WriteLine($"{btn.Text} Started Tracking");
				}
				else
				{
					btn.BackColor = SystemColors.Control;
					NodeDictionary[btn.Text].isTracked = false;
					Debug.WriteLine($"{btn.Text} Stopped Tracking");
					TrackingButtonList.Remove(btn);
						
				}
			}
		}

		public double StringLL2Double(string LL)
		{
			return Convert.ToDouble(LL, System.Globalization.CultureInfo.GetCultureInfo("en-US"));
		}

		public void RePopulateTable(TableLayoutPanel tbl)
		{
			foreach (Node msg in NodeDictionary.Values)
			{
				AddRowToTable(tbl, msg);
			}
		}

		public void AddRowToTable(TableLayoutPanel tbl, Node msg)
		{
			RowStyle temp = new RowStyle();
			// stop table from updating
			tbl.Invoke((Action)delegate 
			{ 
				tbl.SuspendLayout();
			});

			// copy row style in order to duplicate
			tbl.Invoke((Action)delegate
			{
				temp = tbl.RowStyles[tbl.RowCount - 1];
			});

			//increase table rows count by one
			tbl.Invoke((Action)delegate
			{
				tbl.RowCount++;
			});

			//add a new RowStyle as a copy of the previous one
			tbl.Invoke((Action)delegate
			{
				tbl.RowStyles.Add(new RowStyle(temp.SizeType, temp.Height));
			});

			Button btn = new Button();
			btn.Dock = DockStyle.Fill;
			btn.Name = $"{msg.DeviceName}_Uid";
			btn.Text = msg.DeviceName;
			btn.TextAlign = ContentAlignment.MiddleCenter;
			btn.Font = new Font("Microsoft Sans Serif", 10f);
			btn.Tag = "Trackable";
			btn.MouseDown += this.TrackerButton_MouseDown;

			tbl.Invoke((Action)delegate
			{
				tbl.Controls.Add
				(
					btn,
					0, // table column
					 tbl.RowCount - 1 // last row of table
				) ;
				
			});

			tbl.Invoke((Action)delegate
			{
				tbl.Controls.Add
				(
					new Label() // control to be added to table cell
					{
						Text = msg.Lattitude,
						Dock = DockStyle.Fill,
						Name = $"{msg.DeviceName}_Lat",
						TextAlign = ContentAlignment.MiddleCenter,
						Font = new Font("Microsoft Sans Serif", 10f)
					},
					1, // table column
					 tbl.RowCount - 1 // last row of table
				);
				
			});
			
			tbl.Invoke((Action)delegate
			{
				tbl.Controls.Add
				(
					new Label() // control to be added to table cell
					{
						Text = msg.Longitude,
						Dock = DockStyle.Fill,
						Name = $"{msg.DeviceName}_Lon",
						TextAlign = ContentAlignment.MiddleCenter,
						Font = new Font("Microsoft Sans Serif", 10f)
					},
					2, // table column
					 tbl.RowCount - 1 // last row of table
				);
			});

			tbl.Invoke((Action)delegate
			{
				tbl.Controls.Add
				(
					new Label() // control to be added to table cell
					{
						Text = msg.HeightAboveGeoid,
						Dock = DockStyle.Fill,
						Name = $"{msg.DeviceName}_Hae",
						TextAlign = ContentAlignment.MiddleCenter,
						Font = new Font("Microsoft Sans Serif", 10f)
					},
					3, // table column
					 tbl.RowCount - 1 // last row of table
				);
			});

			tbl.Invoke((Action)delegate
			{
				tbl.Controls.Add
				(
					new Label() // control to be added to table cell
					{
						Text = msg.CircularError,
						Dock = DockStyle.Fill,
						Name = $"{msg.DeviceName}_Ce",
						TextAlign = ContentAlignment.MiddleCenter,
						Font = new Font("Microsoft Sans Serif", 10f)
					},
					4, // table column
					 tbl.RowCount - 1 // last row of table
				);
			});

			tbl.Invoke((Action)delegate
			{
				tbl.Controls.Add
				(
					new Label() // control to be added to table cell
					{
						Text = msg.LinearError,
						Dock = DockStyle.Fill,
						Name = $"{msg.DeviceName}_Le",
						TextAlign = ContentAlignment.MiddleCenter,
						Font = new Font("Microsoft Sans Serif", 10f)
					},
					5, // table column
					 tbl.RowCount - 1 // last row of table
				);
			});

			tbl.Invoke((Action)delegate
			{
				tbl.ResumeLayout();
			});
		}

		

		public void ClearTable(TableLayoutPanel tbl)
		{
			//PlayerAdded = true;
			tbl.SuspendLayout();
			int rc = tbl.RowCount;
			for (int j = rc - 1; j > 0; j--)
			{
				//Debug.WriteLine($"J is {j}, Row Count is {tbl.RowCount}");
				for (int i = 0; i < tbl.ColumnCount; i++)
				{
					Control Control = tbl.GetControlFromPosition(i, j);
					tbl.Controls.Remove(Control);
				}
				tbl.RowStyles.RemoveAt(j);
				tbl.RowCount--;
			}

			tbl.ResumeLayout();
		}
	}

	public class MyMessageArgs2 : EventArgs
	{
		//public Dictionary<string, Node> data { get; set; }
		public Node data { get; set; }

		public MyMessageArgs2(Node newData)
		{
			data = newData;
		}
	}


	public static class Geo
	{
		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
		{
			// Unix timestamp is seconds past epoch
			System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
			return dtDateTime;
		}
		public static Tuple<double, double> DegreeBearing(double lat1, double lon1, double lat2, double lon2)
		{
			var dLon = ToRad(lon2 - lon1);
			var dPhi = Math.Log(
				Math.Tan(ToRad(lat2) / 2 + Math.PI / 4) / Math.Tan(ToRad(lat1) / 2 + Math.PI / 4));
			if (Math.Abs(dLon) > Math.PI)
				dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
			return Tuple.Create(ToBearing(Math.Atan2(dLon, dPhi)), Distance(lat1, lon1, lat2, lon2, 'N'));
			//double x = Math.Cos(DegreesToRadians(lat1)) * Math.Sin(DegreesToRadians(lat2)) - Math.Sin(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) * Math.Cos(DegreesToRadians(lon2 - lon1));
			//double y = Math.Sin(DegreesToRadians(lon2 - lon1)) * Math.Cos(DegreesToRadians(lat2));

			//// Math.Atan2 can return negative value, 0 <= output value < 2 * PI expected
			//return (Math.Atan2(y, x) + Math.PI * 2) % (Math.PI * 2);
		}
		public static double GetAzimuth(double lat1, double lon1, double lat2, double lon2)
		{
			var longitudinalDifference = lon2 - lon1;
			var latitudinalDifference = lat2 - lat1;
			var azimuth = (Math.PI * .5d) - Math.Atan(latitudinalDifference / longitudinalDifference);
			if (longitudinalDifference > 0) return ToDegrees(azimuth);
			else if (longitudinalDifference < 0) return ToDegrees(azimuth + Math.PI);
			else if (latitudinalDifference < 0) return ToDegrees(Math.PI);
			return 0d;
		}



		public static double DegreesToRadians(double angle)
		{
			return angle * Math.PI / 180.0d;
		}

		public static double ToRad(double degrees)
		{
			return degrees * (Math.PI / 180);
		}

		public static double ToDegrees(double radians)
		{
			return radians * 180 / Math.PI;
		}

		public static double ToBearing(double radians)
		{
			// convert radians to degrees (as bearing: 0...360)
			return (ToDegrees(radians) + 360) % 360;
		}
		// unit is M = statute miles, K = kilometers, N = nautical miles
		public static double Distance(double lat1, double lon1, double lat2, double lon2, char unit)
		{
			if ((lat1 == lat2) && (lon1 == lon2))
			{
				return 0;
			}
			else
			{
				double theta = lon1 - lon2;
				double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
				dist = Math.Acos(dist);
				dist = rad2deg(dist);
				dist = dist * 60 * 1.1515;
				if (unit == 'K')
				{
					dist = dist * 1.609344;
				}
				else if (unit == 'N')
				{
					dist = dist * 0.8684;
				}
				return (dist);
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		//::  This function converts decimal degrees to radians             :::
		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public static double deg2rad(double deg)
		{
			return (deg * Math.PI / 180.0);
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		//::  This function converts radians to decimal degrees             :::
		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public static double rad2deg(double rad)
		{
			return (rad / Math.PI * 180.0);
		}

	}

	

	[XmlRoot(ElementName = "point")]
	public class Point
	{
		[XmlAttribute(AttributeName = "lat")]
		public string Lat { get; set; }
		[XmlAttribute(AttributeName = "lon")]
		public string Lon { get; set; }
		[XmlAttribute(AttributeName = "hae")]
		public string Hae { get; set; }
		[XmlAttribute(AttributeName = "ce")]
		public string Ce { get; set; }
		[XmlAttribute(AttributeName = "le")]
		public string Le { get; set; }
	}

	[XmlRoot(ElementName = "__video")]
	public class __video
	{
		[XmlAttribute(AttributeName = "url")]
		public string Url { get; set; }
	}

	[XmlRoot(ElementName = "track")]
	public class Track
	{
		[XmlAttribute(AttributeName = "course")]
		public string Course { get; set; }
		[XmlAttribute(AttributeName = "speed")]
		public string Speed { get; set; }
	}

	[XmlRoot(ElementName = "detail")]
	public class Detail
	{
		[XmlElement(ElementName = "__video")]
		public __video __video { get; set; }
		[XmlElement(ElementName = "track")]
		public Track Track { get; set; }
	}

	[XmlRoot(ElementName = "event")]
	public class Event
	{
		[XmlElement(ElementName = "point")]
		public Point Point { get; set; }
		[XmlElement(ElementName = "detail")]
		public Detail Detail { get; set; }
		[XmlAttribute(AttributeName = "version")]
		public string Version { get; set; }
		[XmlAttribute(AttributeName = "uid")]
		public string Uid { get; set; }
		[XmlAttribute(AttributeName = "type")]
		public string Type { get; set; }
		[XmlAttribute(AttributeName = "time")]
		public string Time { get; set; }
		[XmlAttribute(AttributeName = "start")]
		public string Start { get; set; }
		[XmlAttribute(AttributeName = "stale")]
		public string Stale { get; set; }
		[XmlAttribute(AttributeName = "how")]
		public string How { get; set; }
	}



	public class Node
	{
		public string DeviceType { get; set; }
		public string DeviceName { get; set; }
		public IPAddress DeviceIP { get; set; }
		public int DeviceUDPPort { get; set; }
		public int DeviceTCPPort { get; set; }
		public string DeviceMessageType { get; set; }
		public bool isAlive { get; set; }
		public bool isStale { get; set; }
		public bool isTracked { get; set; }
		public List<HeartBeat> HeartBeatMessages { get; set; }
		public List<Event> EventMessages { get; set; }
		public int StaleThresh { get; set; }
		public int StaleCount { get; set; }
		private Stopwatch StaleCounter { get; set; }
		public string Lattitude { get; set; }
		public string Longitude { get; set; }
		public int Altitude { get; set; }
		public string HeightAboveGeoid { get; set; }
		public string CircularError { get; set; }
		public string LinearError { get; set; }
		public string Time { get; set; }
		public string Start { get; set; }
		public string Stale { get; set; }
		public double Var1 { get; set; }
		public string Var2 { get; set; }
		public string Var3 { get; set; }
		public string Var4 { get; set; }
		public string Var5 { get; set; }
		public string TrackedNodeName { get; set; }
		public string Course { get; set; }
		public string Speed { get; set; }
		public Color ChartColor { get; set; }


		//Constructor for cot messages
		public Node(Event msg, IPEndPoint ipe)
		{
			DeviceType = string.Empty;
			DeviceName = msg.Uid;
			DeviceIP = ipe.Address;
			DeviceUDPPort = ipe.Port;
			DeviceTCPPort = 0;
			DeviceMessageType = "CoT";
			isAlive = false;
			isStale = false;
			EventMessages = new List<Event>() { msg };
			StaleThresh = 2;
			StaleCount = 0;
			Lattitude = msg.Point.Lat;
			Longitude = msg.Point.Lon;
			Altitude = 0;
			HeightAboveGeoid = msg.Point.Hae;
			CircularError = msg.Point.Ce;
			LinearError = msg.Point.Le;
			StaleCounter = new Stopwatch();
			isTracked = false;
			Time = msg.Time;
			Start = msg.Start;
			Stale = msg.Stale;
			Var1 = 0.0;
			Var2 = string.Empty;
			Var3 = string.Empty;
			Var4 = string.Empty;
			Var5 = string.Empty;
			TrackedNodeName = string.Empty;
			//Course = msg.Detail.Track.Course;
			//Speed = msg.Detail.Track.Speed;
			Course = string.Empty;
			Speed = string.Empty;
		}
		//Constructor for HeartBeat messages
		public Node(HeartBeat msg)
		{
			DeviceType = msg.Type;
			DeviceName = msg.Name;
			DeviceIP = IPAddress.Parse(msg.IP);
			DeviceUDPPort = msg.UDPPort;
			DeviceTCPPort = msg.TCPPort;
			DeviceMessageType = "HeartBeat";
			isAlive = false;
			isStale = false;
			HeartBeatMessages = new List<HeartBeat>() { msg };
			StaleThresh = 2;
			StaleCount = 0;
			Lattitude = msg.Lattitude;
			Longitude = msg.Longitude;
			Altitude = 0;
			HeightAboveGeoid = msg.HeightAboveGeoid;
			CircularError = msg.CircularError;
			LinearError = msg.LinearError;
			StaleCounter = new Stopwatch();
			isTracked = msg.isTracking;
			Time = msg.Time;
			Start = msg.Start;
			Stale = msg.Stale;
			Var1 = 0.0;
			Var2 = string.Empty;
			Var3 = string.Empty;
			Var4 = string.Empty;
			Var5 = string.Empty;
			TrackedNodeName = string.Empty;
		}

	}

	public class HeartBeat
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string Lattitude { get; set; }
		public string Longitude { get; set; }
		public string Altitude { get; set; }
		public string HeightAboveGeoid { get; set; }
		public string CircularError { get; set; }
		public string LinearError { get; set; }
		public bool isTracking { get; set; }
		public string TrackedNodeName { get; set; }
		public double Lastangle { get; set; }
		public double LastAzimuth { get; set; }
		public int LastUpdateMS { get; set; }
		public string MulticastIP { get; set; }
		public string IP { get; set; }
		public int UDPPort { get; set; }
		public int TCPPort { get; set; }
		public string Epoch { get; set; }
		public string Time { get; set; }
		public string Start { get; set; }
		public string Stale { get; set; }
		public double Var1 { get; set; }
		public string Var2 { get; set; }
		public string Var3 { get; set; }
		public string Var4 { get; set; }
		public string Var5 { get; set; }


	}


	public class CommandMessage
	{
		public string DeviceType { get; set; }
		public string DeviceName { get; set; }
		public string TargetIP { get; set; }
		public string TargetPort { get; set; }
		public double Lattitude { get; set; }
		public double Longitude { get; set; }
		public double Altitude { get; set; }
		public double Heading { get; set; }
		public double Azimuth { get; set; }
		public byte StartVideo { get; set; }
		public byte StreamVideo { get; set; }
		public byte StartSA { get; set; }
		public byte StreamSA { get; set; }
		public byte StartCoT { get; set; }
		public byte StreamCoT { get; set; }
		public byte StreamStatus { get; set; }

	}

	public class RequestMessage
	{

	}

	public class StatusMessage
	{
		public string DeviceType { get; set; }
		public string DeviceName { get; set; }
		public double Lattitude { get; set; }
		public double Longitude { get; set; }
		public double Altitude { get; set; }
		public double Heading { get; set; }
		public double Azimuth { get; set; }
		public byte StartVideo { get; set; }
		public byte StreamingVideo { get; set; }
		public byte StreamingSA { get; set; }
		public byte StreamingStatus { get; set; }
		public double batteryVoltage { get; set; }
		public int FaultField { get; set; }
		public string CoTIP { get; set; }
		public string CoTPort { get; set; }
		public string VideoIP { get; set; }
		public string VideoPort { get; set; }
		public string SAIP { get; set; }
		public string SAPort { get; set; }
		public string TimeStamp { get; set; }
		public string TimeStart { get; set; }
		public string UpTime { get; set; }
		public int LastUpdateMS { get; set; }
		public string Var1 { get; set; }
		public string Var2 { get; set; }
		public string Var3 { get; set; }
		public string Var4 { get; set; }
		public string Var5 { get; set; }
		public int ErrorField { get; set; }

	}

	public class FaultMessage
	{

	}

	public class DeviceMessage
	{
		public string SenderDeviceType { get; set; } = string.Empty;
		public string SenderMessageType { get; set; } = string.Empty;
		public int SenderMessageLength { get; set; } = 0;

		public int DeviceTypeStartPosition { get; set; } = 0;
		public int DeviceTypeLength { get; set; } = 6;

		public int MessageTypeStartPosition { get; set; } = 6;
		public int MessageTypeLength { get; set; } = 4;

		public int PayloadLengthStartPosition { get; set; } = 10;
		public int PayloadLength { get; set; } = 12;

		public int ActualPayloadstartPosition { get; set; } = 12;

		public Event DeserializeCOTMessage(string cot)
		{
			var serializer = new XmlSerializer(typeof(Event));
			Event result;
			using (TextReader reader = new StringReader(cot))
			{
				result = (Event)serializer.Deserialize(reader);
			}
			return result;
		}


		public void ParseMessage(MyMessageArgs e, NodeHandler NodeManager, TextBox tbTerminal)
		{
			//Debug.WriteLine(Encoding.Default.GetString(e.data.Item1, 0, e.data.Item1.Length));
			byte[] tag = e.data.Item1.Take(6).ToArray();
			SenderDeviceType = Encoding.ASCII.GetString(tag, 0, tag.Count());
			

			if (SenderDeviceType == "PTZCAM" || SenderDeviceType == "GNDRAD")
			{
				//Debug.WriteLine($"TAG: {SenderDeviceType}");
				byte[] type = e.data.Item1.Skip(6).Take(4).ToArray();
				SenderMessageType = Encoding.ASCII.GetString(type, 0, type.Count());
				//Debug.WriteLine($"TYPE: {SenderMessageType.ToString()}");

				SenderMessageLength = BitConverter.ToInt32(e.data.Item1, 10);
				//Debug.WriteLine($"MSG LEN: {SenderMessageLength}");

				byte[] Payload = e.data.Item1.Skip(14).Take(SenderMessageLength).ToArray();
				//Debug.WriteLine(Encoding.ASCII.GetString(Payload, 0, Payload.Count()));
				if (SenderMessageType == "STAT")
				{
					try
					{
						NodeManager.locked = true;
						HeartBeat e2 = JsonConvert.DeserializeObject<HeartBeat>(Encoding.Default.GetString(Payload, 0, Payload.Count()));
						//Debug.WriteLine(e2.MulticastIP);
						if (!NodeManager.NodeDictionary.ContainsKey(e2.Name))
						{
							NodeManager.NodeDictionary.Add(e2.Name, new Node(e2));
							NodeManager.NodeCount += 1;
							NodeManager.NodeDictionary[e2.Name].ChartColor = NodeManager.ColorDict[NodeManager.NodeCount];
							NodeManager.NodeAdded = true;
							

						}
						else
						{
							NodeManager.NodeDictionary[e2.Name].Lattitude = e2.Lattitude;
							NodeManager.NodeDictionary[e2.Name].Longitude = e2.Longitude;
							NodeManager.NodeDictionary[e2.Name].HeightAboveGeoid = e2.HeightAboveGeoid;
							NodeManager.NodeDictionary[e2.Name].CircularError = e2.CircularError;
							NodeManager.NodeDictionary[e2.Name].LinearError = e2.LinearError;
							NodeManager.NodeDictionary[e2.Name].Time = e2.Time;
							NodeManager.NodeDictionary[e2.Name].Start = e2.Start;
							NodeManager.NodeDictionary[e2.Name].Stale = e2.Stale;

							NodeManager.NodeDictionary[e2.Name].HeartBeatMessages.Add(e2);
							
							NodeManager.DataAdded = true;


						}

						NodeManager.locked = false;
						NodeManager.GetUpdate(NodeManager.NodeDictionary[e2.Name]);
						//Debug.WriteLine(Encoding.Default.GetString(Payload, 0, Payload.Count()));

					}
					catch (System.InvalidOperationException exception)
					{
						NodeManager.locked = false;
						//Debug.WriteLine($"Error with decoding Message {exception}");
					}

				}
				else
				{
					Debug.WriteLine("sender not stat");
				}
				if (NodeManager.ViewRaw)
				{
					tbTerminal.Invoke((Action)delegate { tbTerminal.AppendText(Encoding.Default.GetString(Payload, 0, Payload.Count()) + Environment.NewLine + Environment.NewLine); });
					//tbTerminal.Invoke((Action)delegate { tbTerminal.AppendText($"{CoT.Players[e2.Uid].Uid}, {CoT.Players[e2.Uid].Point.Hae}, {CoT.Players[e2.Uid].Point.Ce}, {CoT.Players[e2.Uid].Point.Le}" + Environment.NewLine + Environment.NewLine); });
				}

			}
			else
			{
				//Debug.WriteLine($"RAW XML: {Encoding.Default.GetString(e.data.Item1, 0, e.data.Item1.Length)}");
				try
				{
					NodeManager.locked = true;
					string Targetname = string.Empty;
					Event e2 = DeserializeCOTMessage(Encoding.Default.GetString(e.data.Item1, 0, e.data.Item1.Length));
					//Debug.WriteLine($"Initially {e2.Uid}");
					//Debug.WriteLine(Encoding.Default.GetString(e.data.Item1, 0, e.data.Item1.Length));
					bool isTarget = e2.Uid.Contains("sr.455082028");
					if (isTarget)
					{
						e2.Uid = "sr.455082028";

						NodeManager.TargetLat = Convert.ToDouble(e2.Point.Lat, System.Globalization.CultureInfo.GetCultureInfo("en-US"));
						NodeManager.TargetLon = Convert.ToDouble(e2.Point.Lon, System.Globalization.CultureInfo.GetCultureInfo("en-US"));
						//Debug.WriteLine("Ident target");
					}

					//Debug.WriteLine(e2.Uid);
					if (!NodeManager.NodeDictionary.ContainsKey(e2.Uid))
					{
						//Debug.WriteLine($"Before fail {e2.Uid}");
						NodeManager.NodeDictionary.Add(e2.Uid, new Node(e2, e.data.Item2));
						if (!isTarget)
						{
							NodeManager.NodeDictionary[e2.Uid].Course = e2.Detail.Track.Course;
							NodeManager.NodeDictionary[e2.Uid].Speed = e2.Detail.Track.Speed;
						}
						NodeManager.NodeCount += 1;
						NodeManager.NodeDictionary[e2.Uid].ChartColor = NodeManager.ColorDict[NodeManager.NodeCount];
						NodeManager.NodeAdded = true;
						

					}
					else
					{
						NodeManager.NodeDictionary[e2.Uid].Lattitude = e2.Point.Lat;
						NodeManager.NodeDictionary[e2.Uid].Longitude = e2.Point.Lon;
						NodeManager.NodeDictionary[e2.Uid].HeightAboveGeoid = e2.Point.Hae;
						NodeManager.NodeDictionary[e2.Uid].CircularError = e2.Point.Ce;
						NodeManager.NodeDictionary[e2.Uid].LinearError = e2.Point.Le;
						NodeManager.NodeDictionary[e2.Uid].Time = e2.Time;
						NodeManager.NodeDictionary[e2.Uid].Start = e2.Start;
						NodeManager.NodeDictionary[e2.Uid].Stale = e2.Stale;
						if (!isTarget)
						{
							//Debug.WriteLine($"{e2.Uid} @ {e2.Detail.Track.Course}");
							NodeManager.NodeDictionary[e2.Uid].Course = e2.Detail.Track.Course;
							NodeManager.NodeDictionary[e2.Uid].Speed = e2.Detail.Track.Speed;
						}
						

						NodeManager.NodeDictionary[e2.Uid].EventMessages.Add(e2);
						NodeManager.DataAdded = true;
						//Debug.WriteLine("Finished adding");
					}
					NodeManager.UpdateTarget = true;
					NodeManager.locked = false;
					NodeManager.GetUpdate(NodeManager.NodeDictionary[e2.Uid]);
					//Debug.WriteLine(Encoding.Default.GetString(e.data, 0, e.data.Length));
					
				}
				catch (System.InvalidOperationException exception)
				{
					NodeManager.locked = false;
					//Debug.WriteLine($"Error with decoding Message {exception}");
				}
				if (NodeManager.ViewRaw)
				{
					tbTerminal.Invoke((Action)delegate { tbTerminal.AppendText(Encoding.Default.GetString(e.data.Item1, 0, e.data.Item1.Length) + Environment.NewLine + Environment.NewLine); });
					//tbTerminal.Invoke((Action)delegate { tbTerminal.AppendText($"{CoT.Players[e2.Uid].Uid}, {CoT.Players[e2.Uid].Point.Hae}, {CoT.Players[e2.Uid].Point.Ce}, {CoT.Players[e2.Uid].Point.Le}" + Environment.NewLine + Environment.NewLine); });
				}
			}



		}

	}
	public class Compass
	{
		public static Bitmap DrawCompass(double degree, double pitch, double maxpitch, double tilt, double maxtilt, Size s)
		{
			double maxRadius = s.Width > s.Height ? s.Height / 2 : s.Width / 2;
			double sizeMultiplier = maxRadius / 200;
			double relativepitch = pitch / maxpitch;
			double relativetilt = tilt / maxtilt;

			Bitmap result = null;
			SolidBrush drawBrushWhite = new SolidBrush(Color.FromArgb(255, 244, 255));
			SolidBrush drawBrushRed = new SolidBrush(Color.FromArgb(240, 255, 0, 0));
			SolidBrush drawBrushOrange = new SolidBrush(Color.FromArgb(240, 255, 150, 0));
			//SolidBrush drawBrushBlue = new SolidBrush(Color.FromArgb(100, 0, 250, 255));
			SolidBrush drawBrushBlue = new SolidBrush(Color.FromArgb(200, 0, 255, 0));
			SolidBrush drawBrushWhiteGrey = new SolidBrush(Color.FromArgb(20, 255, 255, 255));
			double outerradius = (((maxRadius - sizeMultiplier * 60) / maxRadius) * maxRadius);
			double innerradius = (((maxRadius - sizeMultiplier * 90) / maxRadius) * maxRadius);
			double degreeRadius = outerradius + 37 * sizeMultiplier;
			double dirRadius = innerradius - 30 * sizeMultiplier;
			double TriRadius = outerradius + 20 * sizeMultiplier;
			double PitchTiltRadius = innerradius * 0.55;
			if (s.Width * s.Height > 0)
			{
				result = new Bitmap(s.Width, s.Height);
				using (Font font2 = new Font("Arial", (float)(16 * sizeMultiplier)))
				{
					using (Font font1 = new Font("Arial", (float)(14 * sizeMultiplier)))
					{
						//using (Pen penblue = new Pen(Color.FromArgb(100, 0, 250, 255), ((int)(sizeMultiplier) < 4 ? 4 : (int)(sizeMultiplier))))
						using (Pen penblue = new Pen(Color.FromArgb(200, 0, 255, 0), ((int)(sizeMultiplier) < 4 ? 4 : (int)(sizeMultiplier))))
						{
							using (Pen penorange = new Pen(Color.FromArgb(255, 150, 0), ((int)(sizeMultiplier) < 1 ? 1 : (int)(sizeMultiplier))))
							{
								using (Pen penred = new Pen(Color.FromArgb(255, 0, 0), ((int)(sizeMultiplier) < 1 ? 1 : (int)(sizeMultiplier))))
								{

									using (Pen pen1 = new Pen(Color.FromArgb(255, 255, 255), (int)(sizeMultiplier * 4)))
									{

										using (Pen pen2 = new Pen(Color.FromArgb(255, 255, 255), ((int)(sizeMultiplier) < 1 ? 1 : (int)(sizeMultiplier))))
										{
											using (Pen pen3 = new Pen(Color.FromArgb(0, 255, 255, 255), ((int)(sizeMultiplier) < 1 ? 1 : (int)(sizeMultiplier))))
											{
												using (Graphics g = Graphics.FromImage(result))
												{

													// Calculate some image information.
													double sourcewidth = s.Width;
													double sourceheight = s.Height;

													int xcenterpoint = (int)(s.Width / 2);
													int ycenterpoint = (int)((s.Height / 2));// maxRadius;

													System.Drawing.Point pA1 = new System.Drawing.Point(xcenterpoint, ycenterpoint - (int)(sizeMultiplier * 45));
													System.Drawing.Point pB1 = new System.Drawing.Point(xcenterpoint - (int)(sizeMultiplier * 7), ycenterpoint - (int)(sizeMultiplier * 45));
													System.Drawing.Point pC1 = new System.Drawing.Point(xcenterpoint, ycenterpoint - (int)(sizeMultiplier * 90));
													System.Drawing.Point pB2 = new System.Drawing.Point(xcenterpoint + (int)(sizeMultiplier * 7), ycenterpoint - (int)(sizeMultiplier * 45));

													System.Drawing.Point[] a2 = new System.Drawing.Point[] { pA1, pB1, pC1 };
													System.Drawing.Point[] a3 = new System.Drawing.Point[] { pA1, pB2, pC1 };

													g.DrawPolygon(penred, a2);
													g.FillPolygon(drawBrushRed, a2);
													g.DrawPolygon(penred, a3);
													g.FillPolygon(drawBrushWhite, a3);

													double[] Cos = new double[360];
													double[] Sin = new double[360];

													////draw centercross
													//g.DrawLine(pen2, new System.Drawing.Point(((int)(xcenterpoint - (PitchTiltRadius - sizeMultiplier * 50))), ycenterpoint), new System.Drawing.Point(((int)(xcenterpoint + (PitchTiltRadius - sizeMultiplier * 50))), ycenterpoint));
													//g.DrawLine(pen2, new System.Drawing.Point(xcenterpoint, (int)(ycenterpoint - (PitchTiltRadius - sizeMultiplier * 50))), new System.Drawing.Point(xcenterpoint, ((int)(ycenterpoint + (PitchTiltRadius - sizeMultiplier * 50)))));

													////draw pitchtiltcross
													//System.Drawing.Point PitchTiltCenter = new System.Drawing.Point((int)(xcenterpoint + PitchTiltRadius * relativetilt), (int)(ycenterpoint - PitchTiltRadius * relativepitch));
													//int rad = (int)(sizeMultiplier * 8);
													//int rad2 = (int)(sizeMultiplier * 25);

													//Rectangle r = new Rectangle((int)(PitchTiltCenter.X - rad2), (int)(PitchTiltCenter.Y - rad2), (int)(rad2 * 2), (int)(rad2 * 2));
													//g.DrawEllipse(pen3, r);
													//g.FillEllipse(drawBrushWhiteGrey, r);
													//g.DrawLine(penorange, PitchTiltCenter.X - rad, PitchTiltCenter.Y, PitchTiltCenter.X + rad, PitchTiltCenter.Y);
													//g.DrawLine(penorange, PitchTiltCenter.X, PitchTiltCenter.Y - rad, PitchTiltCenter.X, PitchTiltCenter.Y + rad);

													//prep here because need before and after for red triangle.
													for (int d = 0; d < 360; d++)
													{
														//   map[y] = new long[src.Width];
														double angleInRadians = ((((double)d) + 270d) - degree) / 180F * Math.PI;
														Cos[d] = Math.Cos(angleInRadians);
														Sin[d] = Math.Sin(angleInRadians);
													}

													for (int d = 0; d < 360; d++)
													{


														System.Drawing.Point p1 = new System.Drawing.Point((int)(outerradius * Cos[d]) + xcenterpoint, (int)(outerradius * Sin[d]) + ycenterpoint);
														System.Drawing.Point p2 = new System.Drawing.Point((int)(innerradius * Cos[d]) + xcenterpoint, (int)(innerradius * Sin[d]) + ycenterpoint);

														//Draw Degree labels
														if (d % 30 == 0)
														{
															g.DrawLine(penblue, p1, p2);
															

															System.Drawing.Point p3 = new System.Drawing.Point((int)(degreeRadius * Cos[d]) + xcenterpoint, (int)(degreeRadius * Sin[d]) + ycenterpoint);
															
															if (d > 180)
															{
																int f = d - 360;
																SizeF s1 = g.MeasureString(d.ToString(), font1);
																p3.X = p3.X - (int)(s1.Width / 2);
																p3.Y = p3.Y - (int)(s1.Height / 2);
																g.DrawString(f.ToString(), font1, drawBrushWhite, p3);
															}
															else
															{
																SizeF s1 = g.MeasureString(d.ToString(), font1);
																p3.X = p3.X - (int)(s1.Width / 2);
																p3.Y = p3.Y - (int)(s1.Height / 2);
																g.DrawString(d.ToString(), font1, drawBrushWhite, p3);
															}
															//g.DrawString(d.ToString(), font1, drawBrushWhite, p3);
															System.Drawing.Point pA = new System.Drawing.Point((int)(TriRadius * Cos[d]) + xcenterpoint, (int)(TriRadius * Sin[d]) + ycenterpoint);

															int width = (int)(sizeMultiplier * 3);
															int dp = d + width > 359 ? d + width - 360 : d + width;
															int dm = d - width < 0 ? d - width + 360 : d - width;

															System.Drawing.Point pB = new System.Drawing.Point((int)((TriRadius - (15 * sizeMultiplier)) * Cos[dm]) + xcenterpoint, (int)((TriRadius - (15 * sizeMultiplier)) * Sin[dm]) + ycenterpoint);
															System.Drawing.Point pC = new System.Drawing.Point((int)((TriRadius - (15 * sizeMultiplier)) * Cos[dp]) + xcenterpoint, (int)((TriRadius - (15 * sizeMultiplier)) * Sin[dp]) + ycenterpoint);

															Pen p = penblue;
															Brush b = drawBrushBlue;
															if (d == 0)
															{
																p = penred;
																b = drawBrushRed;
															}
															System.Drawing.Point[] a = new System.Drawing.Point[] { pA, pB, pC };

															g.DrawPolygon(p, a);
															g.FillPolygon(b, a);
														}
														else if (d % 2 == 0)
															g.DrawLine(pen2, p1, p2);

														//draw N,E,S,W
														//if (d % 90 == 0)
														if (d == 0)
														{
															//string dir = (d == 0 ? "TGT" : (d == 90 ? "E" : (d == 180 ? "S" : "W")));
															string dir = "TGT"; //(d == 0 ? "TGT" : (d == 90 ? "E" : (d == 180 ? "S" : "W")));
															System.Drawing.Point p4 = new System.Drawing.Point((int)(dirRadius * Cos[d]) + xcenterpoint, (int)(dirRadius * Sin[d]) + ycenterpoint);
															SizeF s2 = g.MeasureString(dir, font1);
															p4.X = p4.X - (int)(s2.Width / 2);
															p4.Y = p4.Y - (int)(s2.Height / 2);

															//g.DrawString(dir, font1, d == 0 ? drawBrushRed : drawBrushBlue, p4);
															g.DrawString(dir, font1,drawBrushRed, p4);

															//}
															////Draw red triangle at 0 degrees
															//if (d == 0)
															//{

														}

													}
													//draw course

													//g.DrawLine(pen1, new System.Drawing.Point(xcenterpoint, ycenterpoint - (int)innerradius), new System.Drawing.Point(xcenterpoint, ycenterpoint - ((int)outerradius + (int)(sizeMultiplier * 50))));
													double d2 = 0.0;
													if (Math.Abs(degree) > 180)
													{
														d2 = 360+ degree;
														String deg = Math.Round(-(d2), 2).ToString("0.00") + "°";
														SizeF s3 = g.MeasureString(deg, font1);

														g.DrawString(deg, font2, drawBrushOrange, new System.Drawing.Point(xcenterpoint - (int)(s3.Width / 2), ycenterpoint - (int)(sizeMultiplier * 40)));
													}
													else
													{
														String deg = Math.Round(-(degree), 2).ToString("0.00") + "°";
														SizeF s3 = g.MeasureString(deg, font1);

														g.DrawString(deg, font2, drawBrushOrange, new System.Drawing.Point(xcenterpoint - (int)(s3.Width / 2), ycenterpoint - (int)(sizeMultiplier * 40)));

													}

												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return result;
		}
	}

	public class TrackingCompass
	{
		public static Bitmap DrawCompass(double degree, double degree2, double pitch, double maxpitch, double tilt, double maxtilt, Size s)
		{
			//Debug.WriteLine("Started Drawing");

			double maxRadius = s.Width > s.Height ? s.Height / 2 : s.Width / 2; // max radius of the shortest side

			double sizeMultiplier = maxRadius / 200;
			double relativepitch = pitch / maxpitch;
			double relativetilt = tilt / maxtilt;

			Bitmap result = null;
			SolidBrush drawBrushWhite = new SolidBrush(Color.FromArgb(255, 244, 255));
			SolidBrush drawBrushRed = new SolidBrush(Color.FromArgb(240, 255, 0, 0));
			SolidBrush drawBrushOrange = new SolidBrush(Color.FromArgb(240, 255, 150, 0));
			SolidBrush drawBrushBlue = new SolidBrush(Color.FromArgb(100, 0, 250, 255));
			SolidBrush drawBrushWhiteGrey = new SolidBrush(Color.FromArgb(20, 255, 255, 255));
			double outerradius2 = (((maxRadius - sizeMultiplier * 100) / maxRadius) * maxRadius); // added for testing
			double outerradius = (((maxRadius - sizeMultiplier * 60) / maxRadius) * maxRadius);
			double innerradius = (((maxRadius - sizeMultiplier * 90) / maxRadius) * maxRadius);
			double degreeRadius = outerradius + 37 * sizeMultiplier;
			double dirRadius = innerradius - 30 * sizeMultiplier;
			double TriRadius = outerradius + 20 * sizeMultiplier;
			double PitchTiltRadius = innerradius * 0.55;
			if (s.Width * s.Height > 0)
			{
				result = new Bitmap(s.Width, s.Height);
				using (Font font2 = new Font("Arial", (float)(16 * sizeMultiplier)))
				{
					using (Font font1 = new Font("Arial", (float)(14 * sizeMultiplier)))
					{
						using (Pen penblue = new Pen(Color.FromArgb(100, 0, 250, 255), ((int)(sizeMultiplier) < 4 ? 4 : (int)(sizeMultiplier))))
						{
							using (Pen penorange = new Pen(Color.FromArgb(255, 150, 0), ((int)(sizeMultiplier) < 1 ? 1 : (int)(sizeMultiplier))))
							{
								using (Pen penred = new Pen(Color.FromArgb(255, 0, 0), ((int)(sizeMultiplier) < 1 ? 1 : (int)(sizeMultiplier))))
								{

									using (Pen pen1 = new Pen(Color.FromArgb(255, 255, 255), (int)(sizeMultiplier * 2)))
									{

										using (Pen pen2 = new Pen(Color.FromArgb(255, 255, 255), ((int)(sizeMultiplier) < 1 ? 1 : (int)(sizeMultiplier))))
										{
											using (Pen pen3 = new Pen(Color.FromArgb(0, 255, 255, 255), ((int)(sizeMultiplier) < 1 ? 1 : (int)(sizeMultiplier))))
											{
												using (Graphics g = Graphics.FromImage(result))
												{

													// Calculate some image information.
													double sourcewidth = s.Width;
													double sourceheight = s.Height;

													int xcenterpoint = (int)(s.Width / 2);
													int ycenterpoint = (int)((s.Height / 2));// maxRadius;

													System.Drawing.Point pA1 = new System.Drawing.Point(xcenterpoint, ycenterpoint - (int)(sizeMultiplier * 45));
													System.Drawing.Point pB1 = new System.Drawing.Point(xcenterpoint - (int)(sizeMultiplier * 7), ycenterpoint - (int)(sizeMultiplier * 45));
													System.Drawing.Point pC1 = new System.Drawing.Point(xcenterpoint, ycenterpoint - (int)(sizeMultiplier * 90));
													System.Drawing.Point pB2 = new System.Drawing.Point(xcenterpoint + (int)(sizeMultiplier * 7), ycenterpoint - (int)(sizeMultiplier * 45));

													System.Drawing.Point[] a2 = new System.Drawing.Point[] { pA1, pB1, pC1 };
													System.Drawing.Point[] a3 = new System.Drawing.Point[] { pA1, pB2, pC1 };

													//g.DrawEllipse(penred, new Rectangle(5, 5, s.Width-10, s.Height-10));
													g.DrawPolygon(penred, a2);
													g.FillPolygon(drawBrushRed, a2);
													g.DrawPolygon(penred, a3);
													g.FillPolygon(drawBrushWhite, a3);

													double[] Cos = new double[360];
													double[] Sin = new double[360];
													double[] Cos2 = new double[360];
													double[] Sin2 = new double[360];

													//draw centercross
													g.DrawLine(pen2, new System.Drawing.Point(((int)(xcenterpoint - (PitchTiltRadius - sizeMultiplier * 50))), ycenterpoint), new System.Drawing.Point(((int)(xcenterpoint + (PitchTiltRadius - sizeMultiplier * 50))), ycenterpoint));
													g.DrawLine(pen2, new System.Drawing.Point(xcenterpoint, (int)(ycenterpoint - (PitchTiltRadius - sizeMultiplier * 50))), new System.Drawing.Point(xcenterpoint, ((int)(ycenterpoint + (PitchTiltRadius - sizeMultiplier * 50)))));

													//draw pitchtiltcross
													System.Drawing.Point PitchTiltCenter = new System.Drawing.Point((int)(xcenterpoint + PitchTiltRadius * relativetilt), (int)(ycenterpoint - PitchTiltRadius * relativepitch));
													int rad = (int)(sizeMultiplier * 8);
													int rad2 = (int)(sizeMultiplier * 25);

													Rectangle r = new Rectangle((int)(PitchTiltCenter.X - rad2), (int)(PitchTiltCenter.Y - rad2), (int)(rad2 * 2), (int)(rad2 * 2));
													g.DrawEllipse(pen3, r);
													g.FillEllipse(drawBrushWhiteGrey, r);
													g.DrawLine(penorange, PitchTiltCenter.X - rad, PitchTiltCenter.Y, PitchTiltCenter.X + rad, PitchTiltCenter.Y);
													g.DrawLine(penorange, PitchTiltCenter.X, PitchTiltCenter.Y - rad, PitchTiltCenter.X, PitchTiltCenter.Y + rad);

													//prep here because need before and after for red triangle.
													for (int d = 0; d < 360; d++)
													{
														//   map[y] = new long[src.Width];
														double angleInRadians = ((((double)d) + 270d) - degree) / 180F * Math.PI;
														Cos[d] = Math.Cos(angleInRadians);
														Sin[d] = Math.Sin(angleInRadians);
													}


													for (int d = 0; d < 360; d++)
													{


														System.Drawing.Point p1 = new System.Drawing.Point((int)(outerradius * Cos[d]) + xcenterpoint, (int)(outerradius * Sin[d]) + ycenterpoint);
														System.Drawing.Point p2 = new System.Drawing.Point((int)(innerradius * Cos[d]) + xcenterpoint, (int)(innerradius * Sin[d]) + ycenterpoint);

														if (d == (int)degree2)
														{
															//Debug.WriteLine($"Found target degree {d} : {(int)degree2}");
															// for target triangle
															System.Drawing.Point pA2 = new System.Drawing.Point((int)(TriRadius * Cos[d]) + xcenterpoint, (int)(TriRadius * Sin[d]) + ycenterpoint);

															int width2 = (int)(sizeMultiplier * 3);
															int dp2 = d + width2 > 359 ? d + width2 - 360 : d + width2;
															int dm2 = d - width2 < 0 ? d - width2 + 360 : d - width2;

															System.Drawing.Point pBb = new System.Drawing.Point((int)((TriRadius + (15 * sizeMultiplier)) * Cos[dm2]) + xcenterpoint, (int)((TriRadius + (15 * sizeMultiplier)) * Sin[dm2]) + ycenterpoint);
															System.Drawing.Point pC2 = new System.Drawing.Point((int)((TriRadius + (15 * sizeMultiplier)) * Cos[dp2]) + xcenterpoint, (int)((TriRadius + (15 * sizeMultiplier)) * Sin[dp2]) + ycenterpoint);
															System.Drawing.Point[] a2b = new System.Drawing.Point[] { pA2, pBb, pC2 };

															g.DrawPolygon(penred, a2b);
															g.FillPolygon(drawBrushRed, a2b);
														}

														//Draw Degree labels
														if (d % 30 == 0)
														{
															g.DrawLine(penblue, p1, p2);

															System.Drawing.Point p3 = new System.Drawing.Point((int)(degreeRadius * Cos[d]) + xcenterpoint, (int)(degreeRadius * Sin[d]) + ycenterpoint);
															SizeF s1 = g.MeasureString(d.ToString(), font1);
															p3.X = p3.X - (int)(s1.Width / 2);
															p3.Y = p3.Y - (int)(s1.Height / 2);

															g.DrawString(d.ToString(), font1, drawBrushWhite, p3);
															System.Drawing.Point pA = new System.Drawing.Point((int)(TriRadius * Cos[d]) + xcenterpoint, (int)(TriRadius * Sin[d]) + ycenterpoint);

															int width = (int)(sizeMultiplier * 3);
															int dp = d + width > 359 ? d + width - 360 : d + width;
															int dm = d - width < 0 ? d - width + 360 : d - width;

															System.Drawing.Point pB = new System.Drawing.Point((int)((TriRadius - (15 * sizeMultiplier)) * Cos[dm]) + xcenterpoint, (int)((TriRadius - (15 * sizeMultiplier)) * Sin[dm]) + ycenterpoint);
															System.Drawing.Point pC = new System.Drawing.Point((int)((TriRadius - (15 * sizeMultiplier)) * Cos[dp]) + xcenterpoint, (int)((TriRadius - (15 * sizeMultiplier)) * Sin[dp]) + ycenterpoint);

															Pen p = penblue;
															Brush b = drawBrushBlue;
															if (d == 0)
															{
																p = penred;
																b = drawBrushRed;
															}
															System.Drawing.Point[] a = new System.Drawing.Point[] { pA, pB, pC };

															g.DrawPolygon(p, a);
															g.FillPolygon(b, a);


														}
														else if (d % 2 == 0)
														{
															g.DrawLine(pen2, p1, p2);
														}



														//draw N,E,S,W
														if (d % 90 == 0)
														{
															string dir = (d == 0 ? "N" : (d == 90 ? "E" : (d == 180 ? "S" : "W")));
															System.Drawing.Point p4 = new System.Drawing.Point((int)(dirRadius * Cos[d]) + xcenterpoint, (int)(dirRadius * Sin[d]) + ycenterpoint);
															SizeF s2 = g.MeasureString(dir, font1);
															p4.X = p4.X - (int)(s2.Width / 2);
															p4.Y = p4.Y - (int)(s2.Height / 2);

															g.DrawString(dir, font1, d == 0 ? drawBrushRed : drawBrushBlue, p4);
														}


													}
													//draw course

													//g.DrawLine(pen1, new Point(xcenterpoint, ycenterpoint - (int)innerradius), new Point(xcenterpoint, ycenterpoint - ((int)outerradius + (int)(sizeMultiplier * 50))));


													String deg = Math.Round(degree, 2).ToString("0.00") + "°";
													SizeF s3 = g.MeasureString(deg, font1);

													g.DrawString(deg, font2, drawBrushOrange, new System.Drawing.Point(xcenterpoint - (int)(s3.Width / 2), ycenterpoint - (int)(sizeMultiplier * 40)));

												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return result;
		}
	}
	//public class DBTableLayoutPanel : TableLayoutPanel
	//{
	//	protected override void OnCreateControl()
	//	{
	//		base.OnCreateControl();
	//		this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.CacheText, true);
	//	}

	//	protected override CreateParams CreateParams
	//	{
	//		get
	//		{
	//			CreateParams cp = base.CreateParams;
	//			cp.ExStyle |= NativeMethods.WS_EX_COMPOSITED;
	//			return cp;
	//		}
	//	}

	//	public void BeginUpdate()
	//	{
	//		NativeMethods.SendMessage(this.Handle, NativeMethods.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
	//	}

	//	public void EndUpdate()
	//	{
	//		NativeMethods.SendMessage(this.Handle, NativeMethods.WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
	//		Parent.Invalidate(true);
	//	}
	//}
}
