/*CTI internal use only. NO unauthorized use unless approved by Shuo Ding

 * Realtime IoT MQTT Simulator - RIMSim v3.0
 *   
 * Author Dr Shuo Ding, 24 June 2021 
 * 
 * Website: https://IoTNextDay.com
 * 
 * Shuo.Ding.Australia@gmail.com 
*/
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using uPLibrary.Networking.MQTT.Messages;
using uPLibrary.Networking.MQTT;
using System.Runtime.InteropServices;
using System.Xml;
using System.Text.Json;
using System.Xml.Serialization;
using System.Device.Location;

//dotnet build --runtime linux-x64
namespace IoTApp
{
    public enum SimulationRole
    {
        Sender = 0,
        Receiver,
        Transceiver
    }
    public enum EntityType
    {
        Static = 0,
        Vehicle
    }
    public class DataValue
    {
        public int Max { set; get; }
        public int Min { set; get; }
        public int Value { set; get; }
    }

    /*Actual Json msg sent via IoT MQTT
     NOTE: Only 3 integer values are generated and inserted into SQL database, v1, v2, v3, which is enough to deal with 99% scenarios. 
     If the users require to carry more values in a MQTT message, they should change the Json message format and SQL schema accordingly
     The XML config class reserved the function to configure more values in a value list */
    public class JsonMsg
    {
        public int id { set; get; }
        public string Timestamp { set; get; }
        public string Desc { set; get; }
        public string EntityType { set; get; }  //Static, or Vehicle 
        public int v1 { set; get; }
        public int v2 { set; get; }
        public int v3 { set; get; }
        public float Latitude { set; get; }
        public float Longitude { set; get; }
        public string GetLogString()
        {
            string logString = "Current Time: " + DateTime.Now + " [ Sensor ID ] " + id + " [ TimeStamp ] " + Timestamp + " [ Desc ] " + Desc + " [ Type ] " + EntityType + " [ v1 ] " + v1 + " [ v2 ] " + v2 + " [ v3 ] " + v3 + " [ Lat ] " + Latitude + " [ Lon ] " + Longitude;
            Console.WriteLine(logString);
            return logString;
        }
    }
    public class Sensors
    {
        public int id { set; get; }
        public int Interval { set; get; }
        public int SendingIntervalSeconds { set; get; }
        public int SendingIntervalOffset { set; get; }
        public DateTime Timestamp { set; get; }
        public string Desc { set; get; }
        public EntityType EntityType { set; get; }
        public List<DataValue> ValueList { set; get; }
        public float Latitude { set; get; }
        public float Longitude { set; get; }
        public Sensors()
        {
            id = 0;
            Interval = 0;
            SendingIntervalSeconds = 0;
            SendingIntervalOffset = 0;
            ValueList = new List<DataValue>();
            Timestamp = DateTime.Now;
            EntityType = EntityType.Static;
            Desc = "Null";
            Latitude = 0;
            Longitude = 0;
        }
        public JsonMsg GenerateJsonMsg()
        {
            JsonMsg jsonMsg = new JsonMsg();
            jsonMsg.id = id;
            jsonMsg.Timestamp = Timestamp.ToString();
            jsonMsg.Desc = Desc;
            jsonMsg.EntityType = EntityType.ToString();

            //NOTE: We only select 3 values from the ValueList. Users should be aware of this if they insert more values in the ValueList
            jsonMsg.v1 = 0;
            jsonMsg.v2 = 0;
            jsonMsg.v3 = 0;
            int n = ValueList.Count;
            if (n == 1)
                jsonMsg.v1 = ValueList[0].Value;
            if (n == 2)
            {
                jsonMsg.v1 = ValueList[0].Value;
                jsonMsg.v2 = ValueList[1].Value;
            }
            if (n == 3)
            {
                jsonMsg.v1 = ValueList[0].Value;
                jsonMsg.v2 = ValueList[1].Value;
                jsonMsg.v3 = ValueList[2].Value;
            }
            jsonMsg.Latitude = Latitude;
            jsonMsg.Longitude = Longitude;
            return jsonMsg;
        }
        public byte[] GenerateJsonMsgUTF8()
        {
            JsonMsg jsonMsg = GenerateJsonMsg();

            byte[] jsonUtf8Bytes =
                JsonSerializer.SerializeToUtf8Bytes(jsonMsg, new JsonSerializerOptions { WriteIndented = true });
            return jsonUtf8Bytes;
        }
    }
    public class LogFile
    {
        public StreamWriter sw;
        public void Close()
        {
            sw.Close();
        }
        public void Flush()
        {
            sw.Flush();
        }
        public LogFile(string name)
        {
            sw = new StreamWriter(name);
        }
        public void Write(string str)
        {
            Console.WriteLine(str);
            sw.WriteLine(str);
        }
    }
    public class GpxBlob
    {
        /* GPX route map parsing. Users can choose direction in Google map, and copy the link to https://mapstogpx.com/ to download the .gpx file. 
         NOTE: the "xmlns" namespace shown in the Gpx file must be the same as the namespace in the class */

        [XmlRoot(ElementName = "trkpt", Namespace = "http://www.topografix.com/GPX/1/1")]
        public class Trkpt
        {
            [XmlAttribute("lat")]
            public double lat { get; set; }
            [XmlAttribute("lon")]
            public double lon { get; set; }
            public string name { get; set; }
            public void Show()
            {
                Console.WriteLine(name + ", " + lat + ", " + lon);
            }
        }
        [XmlRoot(ElementName = "trkseg", Namespace = "http://www.topografix.com/GPX/1/1")]
        public class Trkseg
        {
            [XmlElement("trkpt")]
            public List<Trkpt> trkpt { get; set; }
            public void Show()
            {
                double dis = 0;
                for (var i = 0; i < trkpt.Count; i++)
                {
                    trkpt[i].Show();

                    if (i < trkpt.Count - 1)
                    {
                        dis += GetSegDistance(trkpt[i].lat, trkpt[i].lon, trkpt[i + 1].lat, trkpt[i + 1].lon);
                        Console.WriteLine("Total segment distance (meter) from Start Point " + dis);
                    }
                }
            }
            public GeoCoordinate GetCurrentPosition(double distanceTravelFromStartPoint)
            {
                GeoCoordinate currentLocation = new GeoCoordinate();
                double dis = 0;
                for (var i = 0; i < trkpt.Count; i++)
                {
                    trkpt[i].Show();

                    if (i < trkpt.Count - 1)
                    {
                        double LengthOfThisSegment = GetSegDistance(trkpt[i].lat, trkpt[i].lon, trkpt[i + 1].lat, trkpt[i + 1].lon);
                        if ((trkpt[i].lat == trkpt[i + 1].lat) || (trkpt[i].lon == trkpt[i + 1].lon))
                        {
                            continue;
                        }
                        dis += LengthOfThisSegment;
                        if (distanceTravelFromStartPoint < dis)
                        {
                            double fractiontraveledonsegment = (LengthOfThisSegment - (dis - distanceTravelFromStartPoint)) / LengthOfThisSegment;
                            if (fractiontraveledonsegment < 0)
                                fractiontraveledonsegment = 0;

                            currentLocation.Latitude = trkpt[i].lat + fractiontraveledonsegment * (trkpt[i + 1].lat - trkpt[i].lat);
                            currentLocation.Longitude = trkpt[i].lon + fractiontraveledonsegment * (trkpt[i + 1].lon - trkpt[i].lon);
                            Console.WriteLine("Distance traveled from Source " + distanceTravelFromStartPoint + " Fraction Traveled On this Segment" + fractiontraveledonsegment + " Current Pos " + currentLocation.Latitude + "," + currentLocation.Longitude + " Segment Node A " + trkpt[i].lat + "," + trkpt[i].lon + " Segment Node B " + trkpt[i + 1].lat + "," + trkpt[i + 1].lon + " LengthOfThisSegment " + LengthOfThisSegment + " TotalSegmentDistance " + dis);
                            break;
                        }
                        Console.WriteLine("Distance Traveled from Source " + dis);
                    }
                }
                return currentLocation;
            }
        }
        [XmlRoot(ElementName = "trk", Namespace = "http://www.topografix.com/GPX/1/1")]
        public class Trk
        {
            public string name { get; set; }
            public string number { get; set; }
            public Trkseg trkseg { get; set; }
            public void Show()
            {
                Console.WriteLine("[ Route Name ] " + name + " [ Number ] " + number);
                trkseg.Show();
            }
        }
        [XmlRoot(ElementName = "gpx", Namespace = "http://www.topografix.com/GPX/1/1")]
        public class Gpx
        {
            public Trk trk { get; set; }
            public void Show()
            {
                trk.Show();
            }
        }
        public Gpx GpxObj;
        public GpxBlob()
        {
            const string FILENAME = "map.gpx";
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Gpx), "http://www.topografix.com/GPX/1/1");
            FileStream fs = new FileStream(FILENAME, FileMode.Open);
            XmlReader reader = XmlReader.Create(fs);
            GpxObj = (Gpx)xmlSerializer.Deserialize(reader);
        }
        public static double GetSegDistance(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            // The distance between the two coordinates, in meters.
            GeoCoordinate firstLocation = new GeoCoordinate(latitude1, longitude1);
            GeoCoordinate secondLocation = new GeoCoordinate(latitude2, longitude2);
            double distance = firstLocation.GetDistanceTo(secondLocation);
            return distance;
        }
        public GeoCoordinate GetCurrentPosition(double distanceTravelFromStartPoint)
        {
            //the vehicle's current lat lon after travelling some distances from start point
            return GpxObj.trk.trkseg.GetCurrentPosition(distanceTravelFromStartPoint);
        }
    }
    public class XMLConfigBlob
    {
        /*Note: The namespace in the class must be the same in the Config.xml file*/
        [XmlRoot(ElementName = "global", Namespace = "https://iot.com")]
        public class Global
        {
            [XmlElement("MqttBroker")]
            public string MqttBroker { get; set; }
            public string Role { get; set; }
            public string LogPath { get; set; }
            public string SqlConnectionString { get; set; }
            public string PublishTopic { get; set; }
            public string SubscribeTopic { get; set; }
            public int SimulationMins { get; set; }
            public int SleepMs { get; set; }

            public void Show()
            {
                Console.WriteLine("\n\n\n\t\t************* XML Configuration ***************");

                Console.WriteLine("[MQTT Broker] " + MqttBroker + " [Role] " + Role + " [LogPath] " + LogPath + " [SqlConnectionString] " + SqlConnectionString + " [PublishTopic] " + PublishTopic + " [SubscribeTopic] " + SubscribeTopic + " [SimulationMins] " + SimulationMins + " [SleepMs] " + SleepMs);
            }
        }
        [XmlRoot(ElementName = "value", Namespace = "https://iot.com")]
        public class Value
        {
            [XmlAttribute("id")]
            public int Id { get; set; }
            [XmlAttribute("min")]
            public int Min { get; set; }
            [XmlAttribute("max")]
            public int Max { get; set; }
            public void Show()
            {
                Console.WriteLine(" value " + Id + " min " + Min + " max " + Max);
            }
        }
        [XmlRoot(ElementName = "gpszone", Namespace = "https://iot.com")]
        public class Gpszone
        {
            [XmlAttribute("latmin")]
            public double LatMin { get; set; }
            [XmlAttribute("latmax")]
            public double LatMax { get; set; }
            [XmlAttribute("lonmin")]
            public double LonMin { get; set; }
            [XmlAttribute("lonmax")]
            public double LonMax { get; set; }
            public void Show()
            {
                Console.WriteLine(" [zone] " + LatMin + "," + LonMin + "," + LatMax + "," + LonMax);
            }
        }
        [XmlRoot(ElementName = "task", Namespace = "https://iot.com")]
        public class Task
        {
            [XmlAttribute("id")]
            public string Id { get; set; }
            public string EntityType { get; set; }
            public int NumberofSensors { get; set; }
            public int SendingIntervalSeconds { get; set; }
            public int SendingIntervalOffset { get; set; }
            [XmlElement("value")]
            public List<Value> ListOfValues { get; set; }
            [XmlElement("gpszone")]
            public Gpszone Zone { get; set; }
            public void Show()
            {
                Console.WriteLine("[Task Id] " + Id + " [EntityType] " + EntityType + " [NumberofSensors] " + NumberofSensors + " [SendingIntervalSeconds] " + SendingIntervalSeconds + " [SendingIntervalOffset] " + SendingIntervalOffset);

                foreach (Value obj in ListOfValues)
                {
                    obj.Show();
                }
                Zone.Show();
            }
        }
        [XmlRoot(ElementName = "Config", Namespace = "https://iot.com")]
        public class ConfigBlob
        {
            [XmlElement("global")]
            public Global GlobalSetting { get; set; }
            [XmlElement("task")]
            public List<Task> ListOfTasks { get; set; }
            public void Show()
            {
                GlobalSetting.Show();
                foreach (Task obj in ListOfTasks)
                {
                    obj.Show();
                }
            }
        }
        public ConfigBlob ConfigBlobObj;
        public XMLConfigBlob()
        {
            const string FILENAME = "Config.xml";
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(XMLConfigBlob.ConfigBlob), "https://iot.com");
            FileStream fs = new FileStream(FILENAME, FileMode.Open);
            XmlReader reader = XmlReader.Create(fs);
            ConfigBlobObj = (XMLConfigBlob.ConfigBlob)xmlSerializer.Deserialize(reader);
        }
    }

    public class Loger
    {
        public LogFile Sending;
        public LogFile Receiving;
        public Loger(string sendingFile, string receivingFile, string path)
        {
            Directory.CreateDirectory(path);
            string generateTime = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            Sending = new LogFile(path + sendingFile + generateTime + ".txt");
            Receiving = new LogFile(path + receivingFile + generateTime + ".txt");
        }
        public void LogSending(string str)
        {
            try
            {
                Sending.Write(str);
                Sending.Flush();
            }
            catch (Exception e2)
            {
                Console.WriteLine("Sending Write Exception: " + e2.Message);
            }
        }
        public void LogReceiving(string str)
        {
            try
            {
                Receiving.Write(str);
                Receiving.Flush();
            }
            catch (Exception e2)
            {
                Console.WriteLine("Receiving Write Exception: " + e2.Message);
            }
        }
        public void Flush()
        {
            Receiving.Flush();
            Sending.Flush();
        }
        public void Close()
        {
            Receiving.Close();
            Sending.Close();
        }
    }
    public class SQLWraper
    {
        public string connectionString { set; get; }
        private SqlConnection sqlc;
        public SQLWraper()
        {
            connectionString = "Data Source=.\\sqlexpress; Initial Catalog=IoTDB; Integrated Security=True";
            Connect();
        }
        public void Connect()
        {
            try
            {
                if (!String.IsNullOrEmpty(connectionString))
                {
                    sqlc = new SqlConnection(connectionString);
                    Console.WriteLine("Connected to SQL Server");
                }
            }
            catch (Exception e2)
            {
                Console.WriteLine("SQL Connection Exception: " + e2.Message);
            }
        }
        public void Insert(JsonMsg jsonMsg)
        {
            //Check if IoT table exist, if not create one
            Create();
            SqlCommand Cmd = new SqlCommand("INSERT INTO dbo.IOT " +
        "(SensorId, TimeStamp, Description, Type, V1, V2, V3, Latitude, Longitude) " +
                "VALUES(@SensorID, @Timestamp, @Description, @Type, @Value1, @Value2, @Value3, @Lat, @Lon)",
        sqlc);
            // create your parameters
            Cmd.Parameters.Add("@SensorID", System.Data.SqlDbType.Int);
            Cmd.Parameters.Add("@Timestamp", System.Data.SqlDbType.Text);
            Cmd.Parameters.Add("@Description", System.Data.SqlDbType.Text);
            Cmd.Parameters.Add("@Type", System.Data.SqlDbType.Text);
            Cmd.Parameters.Add("@Value1", System.Data.SqlDbType.Int);
            Cmd.Parameters.Add("@Value2", System.Data.SqlDbType.Int);
            Cmd.Parameters.Add("@Value3", System.Data.SqlDbType.Int);
            Cmd.Parameters.Add("@Lat", System.Data.SqlDbType.Float);
            Cmd.Parameters.Add("@Lon", System.Data.SqlDbType.Float);
            // set values to parameters from textboxes
            Cmd.Parameters["@SensorID"].Value = jsonMsg.id;
            Cmd.Parameters["@Timestamp"].Value = jsonMsg.Timestamp;
            Cmd.Parameters["@Description"].Value = jsonMsg.Desc;
            Cmd.Parameters["@Type"].Value = jsonMsg.EntityType;
            Cmd.Parameters["@Value1"].Value = jsonMsg.v1;
            Cmd.Parameters["@Value2"].Value = jsonMsg.v2;
            Cmd.Parameters["@Value3"].Value = jsonMsg.v3;
            Cmd.Parameters["@Lat"].Value = jsonMsg.Latitude;
            Cmd.Parameters["@Lon"].Value = jsonMsg.Longitude;
            // open sql connection
            sqlc.Open();
            // execute the query and return number of rows affected, should be one
            try
            {
                int RowsAffected = Cmd.ExecuteNonQuery();
            }
            catch (Exception e2)
            {
                Console.WriteLine("SQL Inserting Exception: " + e2.Message);
            }
            // close connection when done
            sqlc.Close();
        }
        public void Create()
        {
            SqlCommand createTableCommand = new SqlCommand(@"
                   IF NOT EXISTS
                   (
                   SELECT *
                   FROM INFORMATION_SCHEMA.TABLES
                   WHERE  TABLE_CATALOG = 'IoTDB'
                       AND TABLE_SCHEMA = 'dbo'
                       AND TABLE_NAME = 'IOT'
                   )
                   BEGIN
                       CREATE TABLE dbo.IOT
                       (   
                           [SensorId] INT NOT NULL,                          
                           [TimeStamp] VARCHAR(255) NOT NULL,
                           [Description] VARCHAR(255),
                           [Type] VARCHAR(255) NOT NULL,
                           [V1] INT NOT NULL,
                           [V2] INT NOT NULL,
                           [V3] INT NOT NULL,
                           [Latitude] FLOAT(4) NOT NULL,
                           [Longitude] FLOAT(4) NOT NULL                     
                       );
                   END; ", sqlc);
            // open sql connection
            sqlc.Open();
            // execute the query and return number of rows affected, should be one
            try
            {
                int RowsAffected = createTableCommand.ExecuteNonQuery();
            }
            catch (Exception e2)
            {
                Console.WriteLine("SQL Create Table Exception: " + e2.Message);
            }
            // close connection when done
            sqlc.Close();
        }
        public void Read()
        {
            //Read DB table 
            SqlCommand cmd = new SqlCommand(@"
               SELECT * FROM dbo.IOT
               ", sqlc);
            DataTable Results = new DataTable();
            // Read table from database and store it
            sqlc.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            Results.Load(reader);
            sqlc.Close();
            foreach (DataRow row in Results.Rows)
            {
                JsonMsg jsonMsg = new JsonMsg();
                jsonMsg.id = (int)row["SensorId"];
                jsonMsg.Timestamp = row["TimeStamp"].ToString();
                jsonMsg.v1 = (int)row["V1"];
                jsonMsg.v2 = (int)row["V2"];
                jsonMsg.v3 = (int)row["V3"];
                jsonMsg.Desc = row["Description"].ToString();
                jsonMsg.EntityType = row["Type"].ToString();
                jsonMsg.Latitude = Convert.ToSingle(row["Latitude"]);
                jsonMsg.Longitude = Convert.ToSingle(row["Longitude"]);
                Console.WriteLine(jsonMsg.GetLogString());
            }
        }
    }
    public class Config
    {
        public SimulationRole ThisRole { set; get; }
        public string SubscribeTopic { set; get; }
        public string PublishTopic { set; get; }
        public string MQTTBroker { set; get; }
        public string MyID { set; get; }
        public byte[] QoS { set; get; }
        public string LogPath { set; get; }
        public string SendFile { set; get; }
        public string RecvFile { set; get; }
        public int SimulationMins { get; set; }
        public int SleepMs { get; set; }

        public string SqlConnectionString;
        public Loger Log { set; get; }
        public SQLWraper SqlWraper { set; get; }
        public XMLConfigBlob XMLBlob { set; get; }

        public Config()
        {
            XMLBlob = new XMLConfigBlob();
            string role = XMLBlob.ConfigBlobObj.GlobalSetting.Role;
            if (role == "Sender")
            {
                ThisRole = SimulationRole.Sender;
                MyID = "IoTSender";
            }
            else if (role == "Receiver")
            {
                ThisRole = SimulationRole.Receiver;
                MyID = "IoTReceiver";
            }
            else if (role == "Transceiver")
            {
                ThisRole = SimulationRole.Transceiver;
                MyID = "IoTTransceiver";
            }
            SendFile = "SendLog";
            RecvFile = "RecvLog";
            MQTTBroker = XMLBlob.ConfigBlobObj.GlobalSetting.MqttBroker;
            PublishTopic = XMLBlob.ConfigBlobObj.GlobalSetting.PublishTopic;
            SubscribeTopic = XMLBlob.ConfigBlobObj.GlobalSetting.SubscribeTopic;
            QoS = new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };
            LogPath = XMLBlob.ConfigBlobObj.GlobalSetting.LogPath;
            SimulationMins = XMLBlob.ConfigBlobObj.GlobalSetting.SimulationMins;
            SleepMs = XMLBlob.ConfigBlobObj.GlobalSetting.SleepMs;
            string SqlConnectionString = XMLBlob.ConfigBlobObj.GlobalSetting.SqlConnectionString;
            SqlWraper = new SQLWraper();
            Log = new Loger(SendFile, RecvFile, LogPath);
            ConnectSQL();
        }
        public void ConnectSQL()
        {
            if (!String.IsNullOrEmpty(SqlConnectionString))
            {
                SqlWraper.connectionString = SqlConnectionString;
                try
                {
                    Console.WriteLine("Connect to SQL Server");
                    SqlWraper.Connect();
                }
                catch (Exception e2)
                {
                    Console.WriteLine("SQL Connection Exception: " + e2.Message);
                }
            }
        }
    }
    public enum CtrlTypes
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT,
        CTRL_CLOSE_EVENT,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT
    }
    class Program
    {
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);
        // An enumerated type for the control messages
        // sent to the handler routine. 
        private static bool isClosing = false;
        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            // Put your own handler here
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    isClosing = true;
                    Console.WriteLine("CTRL+C received!");
                    break;

                case CtrlTypes.CTRL_BREAK_EVENT:
                    isClosing = true;
                    Console.WriteLine("CTRL+Fn+BREAK received!");
                    break;

                case CtrlTypes.CTRL_CLOSE_EVENT:
                    isClosing = true;
                    Console.WriteLine("Program being closed!");
                    break;

                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    isClosing = true;
                    Console.WriteLine("User is logging off!");
                    break;
            }
            return true;
        }

        public static Config myConfig = new Config();
        public static GpxBlob GpxStore = new GpxBlob();
        static void Main(string[] args)
        {
            Console.WriteLine("Realtime IoT MQTT Simulator v3.0 - by Dr Shuo Ding 2021");
            Console.WriteLine("CTRL+C the application to exit!");
            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);

            //Connect to MQTT broker 
            MqttClient mqttClient = new MqttClient(myConfig.MQTTBroker);
            mqttClient.Connect(myConfig.MyID);

            /*Reserved for Azure IoT hub secure connection, users need to open Azure IoT hub           
            MqttClient mqttClient = new MqttClient("MyIoTHubConnection.azure-devices.net", 8883, true, null, null, MqttSslProtocols.TLSv1_2);
            mqttClient.Connect(myConfig.MyID, "MyIoTHubConnection.azure-devices.net/MyIoTDevices/api-version=2018-06-30", "VEdTAUHL3DkWSxbyYZrJj/PR4pBFf3xaOhPxbvKECAM=");
            */
            Console.WriteLine("Connected to MQTT Broker - " + myConfig.MQTTBroker);
            GpxStore.GpxObj.trk.trkseg.Show();

            //define a callback when receiving messages 
            if (myConfig.ThisRole == SimulationRole.Receiver || myConfig.ThisRole == SimulationRole.Transceiver)
            {
                mqttClient.MqttMsgPublishReceived += MqttPostProperty_MqttMsgPublishReceived;
                string[] Subscribetopic = new string[] { myConfig.SubscribeTopic };
                mqttClient.Subscribe(Subscribetopic, myConfig.QoS);
                Console.WriteLine("Subscribe topic: " + Subscribetopic[0] + " QoS " + myConfig.QoS[0]);
            }
            if (myConfig.ThisRole == SimulationRole.Sender || myConfig.ThisRole == SimulationRole.Transceiver)
            {
                //set simulation params
                String topic = myConfig.PublishTopic;
                List<XMLConfigBlob.Task> TaskList = myConfig.XMLBlob.ConfigBlobObj.ListOfTasks;

                myConfig.XMLBlob.ConfigBlobObj.Show();
                int SimulationMins = myConfig.SimulationMins;
                int SleepMs = myConfig.SleepMs;
                var rand = new Random();
                DateTime startTime = DateTime.Now;
                DateTime runningTime = startTime;
                DateTime endTime = startTime.AddMinutes(SimulationMins);//simulate 20mins   
                //Create IoT Sensors based on Config
                List<Sensors> SensorsList = new List<Sensors>();
                int TotalNumberOfSensors = 0;

                foreach (XMLConfigBlob.Task task in TaskList)
                {
                    if (task.EntityType == "Static")
                    {
                        for (int i = TotalNumberOfSensors; i < TotalNumberOfSensors + task.NumberofSensors; i++)
                        {
                            Sensors newSensors = new Sensors();
                            newSensors.id = i;
                            newSensors.EntityType = EntityType.Static;
                            newSensors.Timestamp = DateTime.Now;



                            for (int n = 0; n < task.ListOfValues.Count; n++)
                            {
                                DataValue val = new DataValue();
                                val.Min = task.ListOfValues[n].Min;
                                val.Max = task.ListOfValues[n].Max;
                                val.Value = rand.Next(val.Min, val.Max);
                                newSensors.ValueList.Add(val);
                            }
                            newSensors.SendingIntervalSeconds = task.SendingIntervalSeconds;
                            newSensors.SendingIntervalOffset = task.SendingIntervalOffset;
                            newSensors.Interval = newSensors.SendingIntervalSeconds + rand.Next(newSensors.SendingIntervalOffset);
                            int difLatRandom = rand.Next((int)Math.Abs(Math.Round((task.Zone.LatMax - task.Zone.LatMin) * 10000)));
                            int difLonRandom = rand.Next((int)Math.Abs(Math.Round((task.Zone.LonMax - task.Zone.LonMin) * 10000)));
                            newSensors.Latitude = (float)(task.Zone.LatMin + (double)difLatRandom / 10000);
                            newSensors.Longitude = (float)(task.Zone.LonMin + (double)difLonRandom / 10000);
                            SensorsList.Add(newSensors);
                        }
                        TotalNumberOfSensors += task.NumberofSensors;
                    }
                    if (task.EntityType == "Vehicle")
                    {
                        for (int i = TotalNumberOfSensors; i < TotalNumberOfSensors + task.NumberofSensors; i++)
                        {
                            Sensors newSensors = new Sensors();
                            newSensors.id = i;
                            newSensors.EntityType = EntityType.Vehicle;
                            newSensors.Timestamp = DateTime.Now;


                            for (int n = 0; n < task.ListOfValues.Count; n++)
                            {
                                DataValue val = new DataValue();
                                val.Min = task.ListOfValues[n].Min;
                                val.Max = task.ListOfValues[n].Max;
                                val.Value = rand.Next(val.Min, val.Max);
                                newSensors.ValueList.Add(val);
                            }
                            newSensors.SendingIntervalSeconds = task.SendingIntervalSeconds;
                            newSensors.SendingIntervalOffset = task.SendingIntervalOffset;
                            newSensors.Interval = newSensors.SendingIntervalSeconds + rand.Next(newSensors.SendingIntervalOffset);

                            //initial postion, does not matter 
                            newSensors.Latitude = (float)-37.5569;
                            newSensors.Longitude = (float)145.896;
                            SensorsList.Add(newSensors);
                        }
                        TotalNumberOfSensors += task.NumberofSensors;
                    }
                }
                while (runningTime < endTime && !isClosing)
                {
                    // String message = "{\"version\":\"1.0\",\"params\":{\"LightSwitch\":0}}"; 
                    foreach (Sensors ele in SensorsList)
                    {
                        if (runningTime > ele.Timestamp)
                        {
                            var rand2 = new Random();
                            ele.Interval = ele.SendingIntervalSeconds + rand2.Next(ele.SendingIntervalOffset);

                            if (ele.EntityType == EntityType.Static)
                            {
                                //static sensor only update random number
                                foreach (DataValue val in ele.ValueList)
                                {
                                    val.Value = rand2.Next(val.Min, val.Max);
                                }
                            }
                            if (ele.EntityType == EntityType.Vehicle)
                            {
                                //vehicle sensor updates random speed v[0] distance v[1], and pausetime[v2]
                                if (ele.ValueList.Count == 3)
                                {
                                    double speed = ele.ValueList[0].Value;
                                    double distance = ele.ValueList[1].Value;
                                    double pausetime = ele.ValueList[2].Value;

                                    //the car traveled Interval - pause time so far (the timestamp < current time , so in the interval - pause car has travelled some distance
                                    double traveltime = (ele.Interval - pausetime);
                                    if (traveltime < 0)
                                        traveltime = 0;

                                    double distanceTraveledinLastInterval = speed * 1000 / 3600 * traveltime;
                                    //update current total traveled distance
                                    ele.ValueList[1].Value += (int)distanceTraveledinLastInterval;
                                    //update lat lon based on Gpx map 

                                    GeoCoordinate position = GpxStore.GetCurrentPosition((double)ele.ValueList[1].Value);

                                    ele.Latitude = (float)position.Latitude;
                                    ele.Longitude = (float)position.Longitude;

                                    if (Double.IsNaN(ele.Latitude) || (Double.IsNaN(ele.Longitude)))
                                    {
                                        ele.Latitude = 0;
                                        ele.Longitude = 0;
                                    }
                                    //update new random speed and pause time 
                                    ele.ValueList[0].Value = rand2.Next(ele.ValueList[0].Min, ele.ValueList[0].Max);
                                    ele.ValueList[2].Value = rand2.Next(ele.ValueList[2].Min, ele.ValueList[2].Max);
                                }
                            }
                            ele.Desc = "Send Msg to MQTT Topic " + topic + ele.id;
                            JsonMsg jsonMsg = ele.GenerateJsonMsg();
                            byte[] jsonUtf8Bytes = ele.GenerateJsonMsgUTF8();
                            mqttClient.Publish(topic + ele.id, jsonUtf8Bytes);
                            //set timestamp for next time after this publish                             
                            string reableJson = Encoding.UTF8.GetString(jsonUtf8Bytes);
                            string logString = jsonMsg.GetLogString();
                            myConfig.Log.LogSending(logString);
                            // myConfig.Log.LogSending(runningTime.ToString() + " Publish to Topic: [" + topic + ele.id + "] Payload: [" + reableJson + "]");
                            myConfig.SqlWraper.Insert(jsonMsg);
                            ele.Timestamp = runningTime.AddSeconds(ele.Interval);
                        }
                    }
                    runningTime = DateTime.Now;
                    // Console.WriteLine(runningTime.ToString());
                    Thread.Sleep(SleepMs);//50ms
                }
                Console.WriteLine("Publishing MQTT messages End");
                myConfig.Log.Sending.Close();
            }
            while (!isClosing)
            {
                //wait for other event to finish 
                Thread.Sleep(50);
            }
            //it means isclosing is true. it is time to finish all events. 
            Console.WriteLine("Exit Main() Gracely");
            myConfig.Log.Close();
            mqttClient.Disconnect();
        }
        private static void MqttPostProperty_MqttMsgPublishReceived(object sender, uPLibrary.Networking.MQTT.Messages.MqttMsgPublishEventArgs e)
        {
            string Topic = e.Topic;
            string payload = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
            var utf8Reader = new Utf8JsonReader(e.Message);
            JsonMsg jsonMsg = JsonSerializer.Deserialize<JsonMsg>(ref utf8Reader);
            jsonMsg.Desc = "Recv Msg from MQTT Topic " + Topic;
            string logString = jsonMsg.GetLogString();
            myConfig.Log.LogReceiving(logString);
            myConfig.SqlWraper.Insert(jsonMsg);
        }
    }
}