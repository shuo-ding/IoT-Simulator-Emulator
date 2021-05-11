/*CTI internal use only. NO Unauthorized Usage unless approved by Shuo Ding

 * Realtime IoT MQTT Simulator - RIMSim
 *   
 * Author Dr Shuo Ding, 2021 
 * 
 * Website: www.IoTNextDay.com
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
using System.Text.Json.Serialization;

//dotnet build --runtime linux-x64
namespace IoTApp
{
    //Actual Json msg sent via IoT MQTT
    public class JsonMsg
    {
        public int id { set; get; }
        public string Timestamp { set; get; }
        public string Desc { set; get; }
        public int v1 { set; get; }
        public int v2 { set; get; }
        public int v3 { set; get; }
        public float Latitude { set; get; }
        public float Longitude { set; get; }
        public string GetLogString()
        {
            string logString = "Current Time: " + DateTime.Now + " [Sensor ID] " + id + " [TimeStamp] " + Timestamp + " [Desc] " + Desc +
                " [v1] " + v1 + " [v2] " + v2 + " [v3] " + v3 + " [Lat] " + Latitude + " [Lon] " + Longitude;
            Console.WriteLine(logString);
            return logString;
        }
    }
    public class Sensors
    {
        public int id { set; get; }
        public int Interval { set; get; }
        public DateTime Timestamp { set; get; }
        public string Desc { set; get; }
        public int v1 { set; get; }
        public int v2 { set; get; }
        public int v3 { set; get; }
        public float Latitude { set; get; }
        public float Longitude { set; get; }
        public JsonMsg GenerateJsonMsg()
        {
            JsonMsg jsonMsg = new JsonMsg();
            jsonMsg.id = id;
            jsonMsg.Timestamp = Timestamp.ToString();
            jsonMsg.Desc = Desc;
            jsonMsg.v1 = v1;
            jsonMsg.v2 = v2;
            jsonMsg.v3 = v3;
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
    public enum SimulationRole
    {
        Sender = 0,
        Receiver,
        Transceiver
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
    public class Loger
    {
        public LogFile Sending;
        public LogFile Receiving;
        public Loger(string sendingFile, string receivingFile, string path)
        {
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
    public class SimulationParameters
    {
        public Int32 NumberofSensors { set; get; }
        public Int32 SendingIntervalSeconds { set; get; }
        public Int32 SendingIntervalOffset { set; get; }
        public Int32 SimulationMins { set; get; }
        public Int32 SleepMs { set; get; }
        public Int32 value1Min { set; get; }
        public Int32 value1Max { set; get; }
        public Int32 value2Min { set; get; }
        public Int32 value2Max { set; get; }
        public Int32 value3Min { set; get; }
        public Int32 value3Max { set; get; }
        public double LatMin { set; get; }
        public double LatMax { set; get; }
        public double LonMin { set; get; }
        public double LonMax { set; get; }

        public SimulationParameters()
        {
            NumberofSensors = 100;
            SendingIntervalSeconds = 30;
            SendingIntervalOffset = 10;
            SimulationMins = 20;
            value1Min = 10;
            value1Max = 100; //humidity
            value2Min = 0;
            value2Max = 45;  //temperature 
            value3Min = 10;
            value3Max = 500; //AQI
            SleepMs = 50;
            LatMin = 0;
            LatMax = 0;
            LonMin = 0;
            LonMax = 0; 
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
        "(SensorId, TimeStamp, Description, V1, V2, V3, Latitude, Longitude) " +
                "VALUES(@SensorID, @Timestamp, @Description, @Value1, @Value2, @Value3, @Lat, @Lon)",
        sqlc);
            // create your parameters
            Cmd.Parameters.Add("@SensorID", System.Data.SqlDbType.Int);
            Cmd.Parameters.Add("@Timestamp", System.Data.SqlDbType.Text);
            Cmd.Parameters.Add("@Description", System.Data.SqlDbType.Text);
            Cmd.Parameters.Add("@Value1", System.Data.SqlDbType.Int);
            Cmd.Parameters.Add("@Value2", System.Data.SqlDbType.Int);
            Cmd.Parameters.Add("@Value3", System.Data.SqlDbType.Int);
            Cmd.Parameters.Add("@Lat", System.Data.SqlDbType.Float);
            Cmd.Parameters.Add("@Lon", System.Data.SqlDbType.Float);
            // set values to parameters from textboxes
            Cmd.Parameters["@SensorID"].Value = jsonMsg.id;
            Cmd.Parameters["@Timestamp"].Value = jsonMsg.Timestamp;
            Cmd.Parameters["@Description"].Value = jsonMsg.Desc;
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
        public Loger Log { set; get; }
        public SimulationParameters Parameters { set; get; }
        public SQLWraper SqlWraper { set; get; }
        public Config()
        {
            //Default Setting; 
            string role = "Sender";
            MQTTBroker = "localhost";
            PublishTopic = "CTI/Sensors/";
            SubscribeTopic = "CTI/Sensors/#";
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
            QoS = new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };
            LogPath = "D:\\IoTData\\";
            SendFile = "SendLog";
            RecvFile = "RecvLog";
            Log = new Loger(SendFile, RecvFile, LogPath);
            Parameters = new SimulationParameters();
            SqlWraper = new SQLWraper();
        }
        public int ParseXML()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Configs.xml");
            XmlNodeList nodes = doc.DocumentElement.SelectNodes("/Config/task");
            foreach (XmlNode node in nodes)
            {
                MQTTBroker = node.SelectSingleNode("MqttBroker").InnerText;
                string role = node.SelectSingleNode("Role").InnerText;
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
                LogPath = node.SelectSingleNode("LogPath").InnerText;
                PublishTopic = node.SelectSingleNode("PublishTopic").InnerText;
                SubscribeTopic = node.SelectSingleNode("SubscribeTopic").InnerText;
                Parameters.NumberofSensors = Int32.Parse(node.SelectSingleNode("NumberofSensors").InnerText);
                Parameters.SendingIntervalSeconds = Int32.Parse(node.SelectSingleNode("SendingIntervalSeconds").InnerText);
                Parameters.SendingIntervalOffset = Int32.Parse(node.SelectSingleNode("SendingIntervalOffset").InnerText);
                Parameters.SimulationMins = Int32.Parse(node.SelectSingleNode("SimulationMins").InnerText);
                Parameters.SleepMs = Int32.Parse(node.SelectSingleNode("SleepMs").InnerText);
                Parameters.value1Min = Int32.Parse(node.SelectSingleNode("value1Min").InnerText);
                Parameters.value1Max = Int32.Parse(node.SelectSingleNode("value1Max").InnerText);
                Parameters.value2Min = Int32.Parse(node.SelectSingleNode("value2Min").InnerText);
                Parameters.value2Max = Int32.Parse(node.SelectSingleNode("value2Max").InnerText);
                Parameters.value3Min = Int32.Parse(node.SelectSingleNode("value3Min").InnerText);
                Parameters.value3Max = Int32.Parse(node.SelectSingleNode("value3Max").InnerText);
                Parameters.LatMin = double.Parse(node.SelectSingleNode("LatMin").InnerText);
                Parameters.LatMax = double.Parse(node.SelectSingleNode("LatMax").InnerText);
                Parameters.LonMin = double.Parse(node.SelectSingleNode("LonMin").InnerText);
                Parameters.LonMax = double.Parse(node.SelectSingleNode("LonMax").InnerText); 

                string connectionString = node.SelectSingleNode("SqlConnectionString").InnerText;
                if (!String.IsNullOrEmpty(connectionString))
                {
                    SqlWraper.connectionString = connectionString;
                    SqlWraper.Connect();
                }
            }
            Console.WriteLine("Parse XML Successfully\n");
            return 0;
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

        static void Main(string[] args)
        {
            Console.WriteLine("Realtime IoT MQTT Simulator - by Dr Shuo Ding");
            Console.WriteLine("CTRL+C the application to exit!");
            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);
            //parse the XML to overwrite default values
            myConfig.ParseXML();
            MqttClient mqttClient = new MqttClient(myConfig.MQTTBroker);

            mqttClient.Connect(myConfig.MyID);
            Console.WriteLine("Connected to MQTT Broker - " + myConfig.MQTTBroker);

            //define a callback when receiving messages 
            if (myConfig.ThisRole == SimulationRole.Receiver || myConfig.ThisRole == SimulationRole.Transceiver)
            {
                mqttClient.MqttMsgPublishReceived += MqttPostProperty_MqttMsgPublishReceived;
                string[] Subscribetopic = new string[] { myConfig.SubscribeTopic };
                mqttClient.Subscribe(Subscribetopic, myConfig.QoS);
                Console.WriteLine("Subscribe topic: " + Subscribetopic[0] + "   QoS " + myConfig.QoS[0]);
            }

            if (myConfig.ThisRole == SimulationRole.Sender || myConfig.ThisRole == SimulationRole.Transceiver)
            {
                //set simulation params
                String topic = myConfig.PublishTopic;
                SimulationParameters pr = myConfig.Parameters;
                int NumberofSensors = pr.NumberofSensors;
                int SendingIntervalSeconds = pr.SendingIntervalSeconds;
                int SendingIntervalOffset = pr.SendingIntervalOffset;
                int SimulationMins = pr.SimulationMins;
                int value1Min = pr.value1Min;
                int value1Max = pr.value1Max; //humidity
                int value2Min = pr.value2Min;
                int value2Max = pr.value2Max; //temperature 
                int value3Min = pr.value3Min;
                int value3Max = pr.value3Max; //AQI 
                double LatMin = pr.LatMin;
                double LatMax = pr.LatMax;
                double LonMin = pr.LonMin;
                double LonMax = pr.LonMax;
                var rand = new Random();
                          

                int SleepMs = pr.SleepMs;

                DateTime startTime = DateTime.Now;
                DateTime runningTime = startTime;
                DateTime endTime = startTime.AddMinutes(SimulationMins);//simulate 20mins   
                List<Sensors> SensorsList = new List<Sensors>();
                
                for (int i = 0; i < NumberofSensors; i++)
                {
                    Sensors newSensors = new Sensors();
                    newSensors.id = i;
                    newSensors.Timestamp = DateTime.Now;
                    newSensors.v1 = rand.Next(value1Min, value1Max);
                    newSensors.v2 = rand.Next(value2Min, value2Max);
                    newSensors.v3 = rand.Next(value3Min, value3Max);
                    newSensors.Interval = SendingIntervalSeconds + rand.Next(SendingIntervalOffset);
                    int difLatRandom = rand.Next((int)Math.Abs(Math.Round((LatMax - LatMin) * 10000)));
                    int difLonRandom = rand.Next((int)Math.Abs(Math.Round((LonMax - LonMin) * 10000)));
                    newSensors.Latitude = (float)(LatMin + (double)difLatRandom / 10000);
                    newSensors.Longitude = (float)(LonMin + (double)difLonRandom / 10000);
                    SensorsList.Add(newSensors);
                }

                while (runningTime < endTime && !isClosing)
                {
                    // String message = "{\"version\":\"1.0\",\"params\":{\"LightSwitch\":0}}"; 
                    foreach (Sensors ele in SensorsList)
                    {
                        if (runningTime > ele.Timestamp)
                        {
                            var rand2 = new Random();
                            ele.Interval = SendingIntervalSeconds + rand2.Next(SendingIntervalOffset);
                            ele.v1 = rand2.Next(value1Min, value1Max);
                            ele.v2 = rand2.Next(value2Min, value2Max);
                            ele.v3 = rand2.Next(value3Min, value3Max);
                            ele.Timestamp = DateTime.Now;
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
