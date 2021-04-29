//CTI internal use only. NO Unauthorized Usage unless approved by Shuo Ding.  
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
//dotnet build --runtime linux-x64
namespace IoTApp
{
    public class Sensors
    {
        public int id { set; get; }
        public int Interval { set; get; }
        public DateTime Timestamp { set; get; }
        public int Desc { set; get; }
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
        public LogFile(string name)
        {
            sw = new StreamWriter(name);
        }
        public void Write(string str)
        {
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
            Sending.Write(str);
        }
        public void LogReceiving(string str)
        {
            Receiving.Write(str);
        }
        public void Flush()
        {
            Receiving.sw.Flush();
            Sending.sw.Flush();
        }
    }
    public class Config
    {
        public SimulationRole ThisRole { set; get; }
        public string Topic { set; get; }
        public string MQTTBroker { set; get; }
        public string MyID { set; get; }
        public byte[] QoS { set; get; }
        public string LogPath { set; get; }
        public string SendFile { set; get; }
        public string RecvFile { set; get; }
        public Loger Log { set; get; }
        public Config(string role)
        {
            //default setting; 
            MQTTBroker = "broker.hivemq.com";
            Topic = "CTI/Sensors/";
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
    class SQLWraper
    {
        string connectionString;
        SqlConnection sqlc;
        public SQLWraper()
        {
            connectionString = "Server=tcp:adosample.database.windows.net,1433;Initial Catalog=ADOSample;Persist Security Info=False;User ID=xxxx;Password=ADO123456#;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            sqlc = new SqlConnection(connectionString);
        }
        public void Insert(int ID, string TimeStamp, int Desc)
        {
            SqlCommand Cmd = new SqlCommand("INSERT INTO IOT " +
        "(ID, TimeStamp, Desc) " +
                "VALUES(@ID, @TimeStamp, @Desc)",
        sqlc);
            // create your parameters
            Cmd.Parameters.Add("@ID", System.Data.SqlDbType.Int);
            Cmd.Parameters.Add("@TimeStamp", System.Data.SqlDbType.Text);
            Cmd.Parameters.Add("@Desc", System.Data.SqlDbType.Int);
            // set values to parameters from textboxes
            Cmd.Parameters["@ID"].Value = ID;
            Cmd.Parameters["@TimeStamp"].Value = TimeStamp;
            Cmd.Parameters["@Desc"].Value = Desc;
            // open sql connection
            sqlc.Open();
            // execute the query and return number of rows affected, should be one
            int RowsAffected = Cmd.ExecuteNonQuery();
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
                   WHERE  TABLE_CATALOG = 'ADOSample'
                       AND TABLE_SCHEMA = 'dbo'
                       AND TABLE_NAME = 'IOT'
                   )
                   BEGIN
                       CREATE TABLE dbo.IOT
                       (
                           [ID] INT IDENTITY PRIMARY KEY,
                           [TimeStamp] VARCHAR(255) NOT NULL,
                           [Desc] INT NOT NULL
                       );
                   END; ", sqlc);
            // open sql connection
            sqlc.Open();
            // execute the query and return number of rows affected, should be one
            int RowsAffected = createTableCommand.ExecuteNonQuery();
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
            // Print SQL table data
            System.Console.WriteLine("[dbo].IOT\nID\t[TimeStamp]\t[Desc]");
            foreach (DataRow row in Results.Rows)
                System.Console.WriteLine($" {row["ID"]}\t {row["TimeStamp"]}\t {row["Desc"]}");
        }
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
        private static bool isclosing = false;
        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            // Put your own handler here
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    isclosing = true;
                    Console.WriteLine("CTRL+C received!");
                    break;

                case CtrlTypes.CTRL_BREAK_EVENT:
                    isclosing = true;
                    Console.WriteLine("CTRL+BREAK received!");
                    break;

                case CtrlTypes.CTRL_CLOSE_EVENT:
                    isclosing = true;
                    Console.WriteLine("Program being closed!");
                    break;

                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    isclosing = true;
                    Console.WriteLine("User is logging off!");
                    break;
            }
            return true;
        }
        public static Config myConfig = new Config("Receiver");
        public static SQLWraper Sql = new SQLWraper();
        static void Main(string[] args)
        {
            /*  String productKey = "a1X2bEnP82z";
              String deviceName = "example1";
              String deviceSecret = "ga7XA6KdlEeiPXQPpRbAjOZXwG8ydgSe";

              // 
              MqttSign sign = new MqttSign();
              sign.calculate(productKey, deviceName, deviceSecret);

              Console.WriteLine("username: " + sign.getUsername());
              Console.WriteLine("password: " + sign.getPassword());
              Console.WriteLine("clientid: " + sign.getClientid());

              // 
              int port = 443;
              String broker = productKey + ".iot-as-mqtt.cn-shanghai.aliyuncs.com";

              MqttClient mqttClient = new MqttClient(broker, port, true, MqttSslProtocols.TLSv1_2, null, null);
              mqttClient.Connect(sign.getClientid(), sign.getUsername(), sign.getPassword());

              Console.WriteLine("broker: " + broker + " Connected");

              //Paho Mqtt  
              String topicReply = "/sys/" + productKey + "/" + deviceName + "/thing/event/property/post_reply";

              mqttClient.MqttMsgPublishReceived += MqttPostProperty_MqttMsgPublishReceived;
              mqttClient.Subscribe(new string[] { topicReply }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
              Console.WriteLine("subscribe: " + topicReply);

              //Paho Mqtt  
              String topic = "/sys/" + productKey + "/" + deviceName + "/thing/event/property/post";
              String message = "{\"id\":\"1\",\"version\":\"1.0\",\"params\":{\"LightSwitch\":0}}";
              mqttClient.Publish(topic, Encoding.UTF8.GetBytes(message));
              Console.WriteLine("publish: " + message);

              while (true)
              {
                  Thread.Sleep(2000);
              }

              //Paho Mqtt  
             // mqttClient.Disconnect();*/

            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);
            Console.WriteLine("CTRL+C the application to exit!");

            //if want to change to receiver role, do this
            // myConfig.SetRole("Receiver");  


            MqttClient mqttClient = new MqttClient(myConfig.MQTTBroker);
            mqttClient.Connect(myConfig.MyID);
            //define a callback when receiving messages 
            if (myConfig.ThisRole == SimulationRole.Receiver || myConfig.ThisRole == SimulationRole.Transceiver)
            {
                mqttClient.MqttMsgPublishReceived += MqttPostProperty_MqttMsgPublishReceived;
                string[] Subscribetopic = new string[] { myConfig.Topic + "#" };
                mqttClient.Subscribe(Subscribetopic, myConfig.QoS);
                Console.WriteLine("Subscribe topic: " + Subscribetopic[0] + "   QoS " + myConfig.QoS[0]);
            }

            if (myConfig.ThisRole == SimulationRole.Sender || myConfig.ThisRole == SimulationRole.Transceiver)
            {
                //set simulation params
                String topic = myConfig.Topic;
                int NumberofSensors = 100;
                int SendingIntervalSeconds = 30;
                int SendingIntervalOffset = 10;
                int SimulationMins = 20;
                int GenerateMaxValueRandom = 100;
                int SleepMs = 50;


                DateTime startTime = DateTime.Now;
                DateTime runningTime = startTime;
                DateTime endTime = startTime.AddMinutes(SimulationMins);//simulate 20mins   
                List<Sensors> SensorsList = new List<Sensors>();
                var rand = new Random();
                for (int i = 0; i < NumberofSensors; i++)
                {
                    Sensors newSensors = new Sensors();
                    newSensors.id = i;
                    newSensors.Timestamp = DateTime.Now;
                    newSensors.Desc = rand.Next(GenerateMaxValueRandom);
                    newSensors.Interval = SendingIntervalSeconds + rand.Next(SendingIntervalOffset);
                    SensorsList.Add(newSensors);
                }

                while (runningTime < endTime && !isclosing)
                {
                    // String message = "{\"version\":\"1.0\",\"params\":{\"LightSwitch\":0}}"; 
                    foreach (Sensors ele in SensorsList)
                    {
                        if (runningTime > ele.Timestamp)
                        {
                            var rand2 = new Random();
                            ele.Interval = SendingIntervalSeconds + rand2.Next(SendingIntervalOffset);
                            ele.Timestamp = runningTime.AddSeconds(ele.Interval);
                            ele.Desc = rand2.Next(100);
                            string a = ele.id.ToString();
                            string b = ele.Desc.ToString();
                            string c = runningTime.ToString();
                            string message = (@"{""fields"":{""id"":""id."" ,""timestamp"":""timestamp."" , ""description"": ""modified.""}}").Replace("id.", a).Replace("modified.", b).Replace("timestamp.", c);
                            mqttClient.Publish(topic + a, Encoding.UTF8.GetBytes(message));
                            Console.WriteLine(c + " Publish to Topic: [" + topic + a + "] Payload: [" + message + "]");
                            myConfig.Log.LogSending(c + " Publish to Topic: [" + topic + a + "] Payload: [" + message + "]");
                        }
                    }
                    runningTime = DateTime.Now;
                    // Console.WriteLine(runningTime.ToString());
                    Thread.Sleep(SleepMs);//50ms
                }
                Console.WriteLine("Publishing End");
                myConfig.Log.Sending.sw.Close();
            }
            if (isclosing)
            {
                Console.WriteLine("Program End");
                return;
            }
        }
        private static void MqttPostProperty_MqttMsgPublishReceived(object sender, uPLibrary.Networking.MQTT.Messages.MqttMsgPublishEventArgs e)
        {
            string Topic = e.Topic;
            string payload = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
            Console.WriteLine("---------" + DateTime.Now + " Received reply topic:[" + Topic + "]  payload: [" + payload + "]");
            try
            {
                myConfig.Log.LogReceiving(DateTime.Now + " Received reply topic:[" + Topic + "]  payload: [" + payload + "]");
                //you can insert into DB here 
                myConfig.Log.Flush();
            }
            catch (Exception e2)
            {
                Console.WriteLine("Exception: " + e2.Message);
            }
        }
    }
}
