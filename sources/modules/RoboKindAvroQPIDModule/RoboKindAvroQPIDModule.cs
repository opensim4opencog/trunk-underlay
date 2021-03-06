﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Apache.Qpid.Messaging;
using Avro;
using Avro.Generic;
using Avro.Specific;
using MushDLR223.ScriptEngines;
using MushDLR223.Utilities;
#if (COGBOT_LIBOMV || USE_STHREADS)
using ThreadPoolUtil;
using Thread = ThreadPoolUtil.Thread;
using ThreadPool = ThreadPoolUtil.ThreadPool;
using Monitor = ThreadPoolUtil.Monitor;
#endif


namespace RoboKindAvroQPID
{
    public delegate void QPIDMessageDelegate(string topic,string prefix, Dictionary<string, object> map);

    public class RoboKindEventModule : YesAutoLoad, IDisposable
    {
        // amqp://admin:admin@clientid/test?brokerlist='
        //CURRENT public static string RK_QPID_URI = "amqp://admin:admin@client01/test?brokerlist='tcp://192.168.0.102:5672'";
        public static string RK_QPID_URI = "amqp://admin:admin@client01/test?brokerlist='tcp://127.0.0.1:5672'";
        //public static string RK_QPID_URI = "amqp://client:guest@client01/test?brokerlist='tcp://localhost:5672'";
        //  public static string RK_QPID_URI = "amqp://admin:admin@clientid/test?brokerlist='tcp://192.168.1.85:5672'";

        /// <summary> Holds the routing key for cogbot management messages. </summary>
        public static string COGBOT_CONTROL_ROUTING_KEY = "cogbot_control_route";

        /// <summary> Holds the main queue for cogbot management messages. </summary>
        public static string COGBOT_CONTROL_QUEUE_KEY = "cogbot_control_queue";

        /// <summary> Holds the main queue for cogbot management messages. </summary>
        public static string COGBOT_CONTROL_EXCHANGE_KEY = "amq.topic";

        /// <summary> Holds the routing key for events. </summary>
        public static string COGBOT_EVENT_ROUTING_KEY = "cogbot_event";

        /// <summary> Holds the routing key for the queue to send reports to. </summary>
        public static string ROBOKIND_RESPONSE_ROUTING_KEY = "response";



        private readonly List<object> cogbotSendersToNotSendToCogbot = new List<object>();

        public static bool DISABLE_AVRO = false;
        public static string REPORT_TEST = "REPORT_REQUEST";
        public IMessageConsumer RK_listener;
        public RoboKindConnectorQPID RK_publisher;

        protected bool IsQPIDRunning
        {
            get { return RK_listener != null && RK_publisher != null; }
        }

        public bool LogEventFromCogbot(object sender, CogbotEvent evt)
        {
            if (DISABLE_AVRO) return true;
            EnsureStarted();
            if (evt.Sender == this) return false;
            if (!IsQPIDRunning) return false;
            if (!cogbotSendersToNotSendToCogbot.Contains(sender)) cogbotSendersToNotSendToCogbot.Add(sender);
            string ss = evt.ToEventString();
            var im = RK_publisher.CreateTextMessage(ss);
            int num = 0;
            foreach (var s in evt.Parameters)
            {
                string sKey = s.Key;
                if (!im.Headers.Contains(sKey))
                {
                    num = 0;
                }
                else
                {
                    num++;
                    sKey = sKey + "_" + num;
                    while (im.Headers.Contains(sKey))
                    {
                        num++;
                        sKey = s.Key + "_" + num;
                    }
                }
                im.Headers.SetString(sKey, "" + s.Value);
            }
            im.Headers.SetString("verb", "" + evt.Verb);
            im.Headers.SetBoolean("personal", evt.IsPersonal != null);
            im.Headers.SetString("evstatus", "" + evt.EventStatus);
            im.Timestamp = evt.Time.ToFileTime();
            im.Type = "" + evt.EventType1;
            RK_publisher.SendMessage(RoboKindEventModule.COGBOT_EVENT_ROUTING_KEY, im);
            return false;
        }


        public bool LogEventFromRoboKind(object sender, CogbotEvent evt)
        {
            if (DISABLE_AVRO) return false;
            EnsureStarted();
            if (cogbotSendersToNotSendToCogbot.Contains(sender))
            {
                return false;
            }
            evt.Sender = sender ?? evt.Sender;
            //@todo client.SendPipelineEvent(evt);
            return true;
        }

        public void OnEvent(CogbotEvent evt)
        {
            if (DISABLE_AVRO) return;
            EnsureStarted();
            if (evt.IsEventType(SimEventType.DATA_UPDATE)) return;
            LogEventFromCogbot(evt.Sender, evt);
        }

        public void Dispose()
        {
            EventsEnabled = false;
            if (RK_listener != null)
            {
                RK_listener.Close();
                RK_listener.OnMessage -= AvroReceived;
                RK_listener.Dispose();
            }
            if (RK_publisher != null)
            {
                RK_publisher.Shutdown();
            }
            RK_publisher = null;
        }

        public bool EventsEnabled { get; set; }


        private bool EnsuredStarted = false;

        public void EnsureStarted()
        {
            if (DISABLE_AVRO) return;
            if (EnsuredStarted) return;
            EnsuredStarted = true;
            try
            {
                EnsureLoginToQIPD();
                //@todo client.EachSimEvent += SendEachSimEvent;
                //@todo client.AddBotMessageSubscriber(this);
                EventsEnabled = true;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR: " + e);
                EnsuredStarted = false;
            }
        }

        private void EnsureLoginToQIPD()
        {
            if (RK_listener != null) return;
            var uri = RoboKindEventModule.RK_QPID_URI;
            try
            {
                LoginToQPID(uri);
            }
            catch (Exception e)
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process(); // Declare New Process
                proc.StartInfo.FileName = @"QPIDServer\StartQPID.bat";
                proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                proc.StartInfo.CreateNoWindow = false;
                proc.StartInfo.ErrorDialog = true;
                //proc.StartInfo.Domain = AppDomain.CurrentDomain.Id;
                proc.Start();
                Thread.Sleep(10000);
                LoginToQPID(uri);
            }
        }

        public void LoginToQPID(string uri)
        {
            if (RK_listener != null)
            {
                RK_listener.OnMessage -= AvroReceived;
            }
            try
            {
                RK_publisher = new RoboKindConnectorQPID(uri);
                RK_publisher.initRKListener(uri);
                RK_listener = RK_publisher.CreateListener(
                    COGBOT_CONTROL_QUEUE_KEY,
                    COGBOT_CONTROL_ROUTING_KEY,
                    COGBOT_CONTROL_EXCHANGE_KEY,
                    ExchangeNameDefaults.TOPIC, true, false, false,
                    AvroReceived);
            }
            catch (Exception e)
            {
                RK_listener = null;
                RK_publisher = null;
                throw e;
            }
        }

        private void SendEachSimEvent(object sender, EventArgs e)
        {
            if (!IsQPIDRunning || !EventsEnabled) return;
            if (e is CogbotEvent)
            {
                CogbotEvent cbe = (CogbotEvent) e;
                OnEvent(cbe);
                return;
            }
        }

        private void AvroReceived(IMessage msg)
        {
            if (DISABLE_AVRO) return;
            EnsureStarted();
            LogEventFromRoboKind(this, MsgToCogEvent(this, msg));
        }

        private CogbotEvent MsgToCogEvent(object sender, IMessage msg)
        {
            var evt = ACogbotEvent.CreateEvent(sender, SimEventType.Once, msg.Type,
                                               SimEventType.UNKNOWN | SimEventType.PERSONAL | SimEventType.DATA_UPDATE);
            List<NamedParam> from = ScriptManager.GetMemberValues("", msg);
            foreach (var f in from)
            {
                if (f.Type != typeof (byte[])) evt.AddParam(f.Key, f.Value);
            }
            if (msg is IBytesMessage)
            {
                IBytesMessage ibm = (IBytesMessage) msg;
                var actual = RK_publisher.DecodeMessage(ibm);
                foreach (var f in ScriptManager.GetMemberValues("", actual))
                {
                    if (f.Type != typeof (byte[])) evt.AddParam(f.Key, f.Value);
                }
            }
            DLRConsole.DebugWriteLine("msg=" + evt);
            return evt;
        }

        /*
 
                    RK_publisher.CreateListener("speechRecEvent", ExchangeNameDefaults.DIRECT, (o) => Eveything(o, "direct"));
                    RK_publisher.CreateListener("#", ExchangeNameDefaults.DIRECT, (o) => Eveything(o, "#direct"));
                    RK_publisher.CreateListener("speechRecEvent", ExchangeNameDefaults.TOPIC, (o) => Eveything(o, "topic"));
                    RK_publisher.CreateListener("#", ExchangeNameDefaults.TOPIC, (o) => Eveything(o, "#topic"));

                //CreateHashSpy("speechRecEvent");
                //CreateHashSpy("animPrompt");
                //CreateHashSpy("");
                //CreateHashSpy("interpreterInstanceCheck");
                //CreateHashSpy("speechRequest");
                //CreateSpy(null, "speechRequest", "speechRequest");
                // SpyQueue("ping");
                //SpyQueue("queue");
         *             SpyQueue("test-ping");
                    SpyQueue("test-queue");
                    SpyQueue("");
                    SpyQueue("speechCommand");

                     *             CreateSpy("speechCommand", false);
                    CreateSpy("speechEvent", false);
                    CreateSpy("interpreterInstanceCheck", false);

                    //RK_publisher.CreateListener("speechRecEvent", "speechRecEvent", (o) => Eveything(o, "speechRecEvent"));
                    //RK_publisher.CreateListener("speechRecEvent", "", (o) => Eveything(o, "blank"));

        
                */
        public void Spy()
        {
            EnsureStarted();
           // RK_publisher.SpyOnQueueAndTopic("amq.topic", ExchangeNameDefaults.TOPIC, "faceEmotion0Event", GotMessage);

            RK_publisher.RKListener(RoboKindEventModule.RK_QPID_URI, "faceEmotion0Event", QPIDProcessor);
            RK_publisher.RKListener(RoboKindEventModule.RK_QPID_URI, "faceEmotion0Error", QPIDProcessor);

            Console.WriteLine("spying on AQM");
            RK_publisher.Connection.Start();
            Thread.Sleep(6000);
        }

        public void Spy0()
        {
            EnsureStarted();
            
            RK_publisher.SpyOnQueueAndTopic("#", ExchangeNameDefaults.DIRECT, "", GotMessage);
            RK_publisher.SpyOnQueueAndTopic("", ExchangeNameDefaults.DIRECT, "#", GotMessage);

            //RK_publisher.SpyOnQueueAndTopic("visionproc0", GotMessage);
            //RK_publisher.SpyOnQueueAndTopic("camera0", GotMessage);
            //RK_publisher.SpyOnQueueAndTopic("speech", GotMessage);
            


            // matters!
         //   SpyQueue("speechRequest");

            // matters?
            //SpyQueue("speechEvent");

            // matters?
          //  SpyQueue("speechCommand");

            // matters?
         //   SpyQueue("camera0Request");

            SpyQueue("camera0Event");
            SpyQueue("visionproc0Event");
            SpyQueue("faceEmotion0Event");
           
            // will crash capture SpyQueue("camera0Command");


            // consumer.Receive();
            // sapiserver
            //SpyQueue("speechCommand");
            //SpyQueue("speechRequest");
            /*
            // sapiclient
            CreateHashSpy("speechEvent");

            // FaceRec
            CreateHashSpy("queue");
            CreateHashSpy("ping");
            CreateHashSpy("test-ping");
            CreateHashSpy("test-queue");
            SpyQueue("visionproc0Event");

            // cammeracapture.exe
            SpyQueue("camera0Command");

            // camera display
            SpyQueue("camera0Event");


            */
            Console.WriteLine("spying on AQM");
            Thread.Sleep(6000);
        }

        public QPIDMessageDelegate QPIDProcessor=null;
        public void GotMessage(Dictionary<string, object> map)
        {
            //Console.WriteLine("MESG: " + ToStr(map));
            if (QPIDProcessor != null)
            {
                string topic = (string)map["JMS_routingKey"];
                QPIDProcessor(topic,"", map);
            }
        }

        public void Block()
        {
            while (true)
            {
                System.Console.Error.Flush();
                string s = System.Console.ReadLine();
                
                
                if (s == "next") return;
            }
        }
        public void BlockBusyForever()
        {
            while (true)
            {
                Thread.Sleep(100);
            }
        }

        private void CreateHashSpy(string queuename)
        {
            RK_publisher.CreateListener(null, "#", queuename, "", true, false, false,
                                        (o) => Eveything(o, "#" + queuename));
        }

        private void SpyQueue(string s)
        {

            RK_publisher.SpyOnQueueAndTopic(s,
                                            ExchangeNameDefaults.DIRECT, s, GotMessage);

            RK_publisher.SpyOnQueueAndTopic("#", ExchangeNameDefaults.DIRECT,s, GotMessage);
            RK_publisher.SpyOnQueueAndTopic(s, ExchangeNameDefaults.DIRECT, "#", GotMessage);

            // CreateSpy(null, "", s);
            // CreateSpy(null, s, "amq.topic");
        }

        private void CreateSpy(string name, bool hash)
        {
            //RK_publisher.CreateListener("", name, (o) => Eveything(o, "Just" + name));
            CreateSpy(name, "", "*");
            CreateSpy(null, name, "");
            /// RK_publisher.CreateListener(name, ExchangeNameDefaults.DIRECT, (o) => Eveything(o, "direct_" + name));
        }

        private void CreateSpy(string queueName, string routingKey, string exchangeName)
        {
            RK_publisher.CreateListener(
                queueName, routingKey, exchangeName, ExchangeClassConstants.DIRECT, true, false, false,
                (o) =>
                Eveything(o,
                          string.Format("Q:R:E='{0}:{1}:{2}'", queueName ?? "!", routingKey ?? "!", exchangeName ?? "!")));
        }

        private void Eveything(IMessage msg, string type)
        {
            lock (GetType())
            {
                Eveything0(msg, type);
            }
        }

        private void Eveything0(IMessage msg, string type)
        {
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("TYPE: " + type + "=" + msg.GetType());
            Console.WriteLine("---------------------------------------");
            try
            {
                Dictionary<string, object> map = RK_publisher.DecodeMessage(msg);
                foreach (KeyValuePair<string, object> o in map)
                {
                    Console.WriteLine(o.Key + "=" + ToStr(o.Value));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(type + "=" + msg);
            }
            Console.WriteLine("---------------------------------------");
            System.Console.Out.Flush();
        }

        public string ToStr(object value)
        {
            if (value == null) return "<NULL>";
            if (value is IConvertible) return "" + value;
            if (value is IEnumerable)
            {
                string ars = " [ ";
                bool needsSep = false;
                foreach (object c in (IEnumerable) value)
                {
                    if (needsSep) ars += ",";
                    else needsSep = true;
                    ars += ToStr(c);
                }
                return ars + " ] ";
            }
            var t1 = value as KeyValuePair<string, object>?;
            if (t1.HasValue)
            {
                var o = t1.Value;
                return o.Key + "=" + ToStr(o.Value);
            }
            return "" + value;
        }
    }
}