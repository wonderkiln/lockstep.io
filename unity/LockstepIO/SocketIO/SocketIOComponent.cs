using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace SocketIO
{
    public enum SocketPacketType
    {
        UNKNOWN      = -1,
        CONNECT      =  0,
        DISCONNECT   =  1,
        EVENT        =  2,
        ACK          =  3,
        ERROR        =  4,
        BINARY_EVENT =  5,
        BINARY_ACK   =  6,
        CONTROL      =  7
    }
    
    public enum EnginePacketType
    {
        UNKNOWN      = -1,
        OPEN         =  0,
        CLOSE        =  1,
        PING         =  2,
        PONG         =  3,
        MESSAGE      =  4,
        UPGRADE      =  5,
        NOOP         =  6
    }

    public class Ack
    {
        public  int                packetId;
        public  DateTime           time;
        private Action<JSONObject> action;
        
        public Ack (int packetId, Action<JSONObject> action)
        {
            this.packetId = packetId;
            this.time     = DateTime.Now;
            this.action   = action;
        }
        
        public void Invoke (JSONObject ev)
        {
            action.Invoke(ev);
        }
        
        public override string ToString ()
        {
            return string.Format("[Ack: packetId={0}, time={1}, action={2}]", packetId, time, action);
        }
    }

    public class SocketIOEvent
    {
        public string     EventName;
        public JSONObject EventData;

        public SocketIOEvent (string eventName, JSONObject eventData)
        {
            this.EventName = eventName;
            this.EventData = eventData;
        }

        public override string ToString ()
        {
            return string.Format("[SocketIOEvent: name={0}, data={1}]", EventName, EventData);
        }
    }
    
    public class Packet
    {
        public EnginePacketType EnginePacketType;
        public SocketPacketType SocketPacketType;
        public int              PacketAttachmentsCount;
        public string           PacketNamespace;
        public int              PacketID;
        public JSONObject       PacketData;
        
        public Packet (EnginePacketType enginePacketType, SocketPacketType socketPacketType, int packetAttachementsCount, string packetNamespace, int packetID, JSONObject packetData)
        {
            this.EnginePacketType       = enginePacketType;
            this.SocketPacketType       = socketPacketType;
            this.PacketAttachmentsCount = packetAttachementsCount;
            this.PacketNamespace        = packetNamespace;
            this.PacketID               = packetID;
            this.PacketData             = packetData;
        }
        
        public override string ToString ()
        {
            return string.Format("[Packet: enginePacketType={0}, socketPacketType={1}, attachments={2}, nsp={3}, id={4}, json={5}]", EnginePacketType, SocketPacketType, PacketAttachmentsCount, PacketNamespace, PacketID, PacketData);
        }
    }
    
    public class SocketIOComponent : MonoBehaviour
    {
        public bool                  UseLocal          = true;
        public bool                  AutoConnect       = true;
        public int                   ReconnectDelay    = 5;
        public float                 AckExpirationTime = 1800f;
        public float                 PingInterval      = 25f;
        public float                 PingTimeout       = 60f;
        public string                CloudURL = "ws://52.5.116.134:80/socket.io/?EIO=4&transport=websocket";
        public string                LocalURL = "ws://127.0.0.1:80/socket.io/?EIO=4&transport=websocket";
        public string                SocketID;
        public int                   PacketID;
        public volatile bool         TransportConnected;
        public volatile bool         Pinging;
        public volatile bool         Pong;
        public volatile bool         WebSocketConnected;
		public WebSocket             Socket;
        private Thread               SocketThread;
        private Thread               PingThread;
        private object               EventQueueLock;
        private object               AckQueueLock;
        private Queue<SocketIOEvent> EventQueue;
        private Queue<Packet>        AckQueue;
        private List<Ack>            AckList;
		private Dictionary<string, List<Action<SocketIOEvent>>> SocketEventHandlers;

        public void Awake ()
        {
            SocketEventHandlers = new Dictionary<string, List<Action<SocketIOEvent>>>();
            AckList             = new List<Ack>();
            SocketID            = null;
            PacketID            = 0;
            Socket              = new WebSocket(UseLocal ? LocalURL : CloudURL);
            Socket.OnOpen       += OnOpen;
            Socket.OnMessage    += OnMessage;
            Socket.OnError      += OnError;
            Socket.OnClose      += OnClose;
            WebSocketConnected  = false;
            EventQueueLock      = new object();
            AckQueueLock        = new object();
            EventQueue          = new Queue<SocketIOEvent>();
            AckQueue            = new Queue<Packet>();
            TransportConnected  = false;
        }

        public void Start ()
        {
            if (AutoConnect) 
            { 
                Connect(); 
            }
        }

        public void Update ()
        {
            lock (EventQueueLock)
            { 
                while (EventQueue.Count > 0)
                {
                    EmitEvent(EventQueue.Dequeue());
                }
            }

            lock (AckQueueLock)
            {
                while (AckQueue.Count > 0)
                {
                    InvokeAck(AckQueue.Dequeue());
                }
            }

            if(WebSocketConnected != Socket.IsConnected)
            {
                WebSocketConnected = Socket.IsConnected;
                if (WebSocketConnected)
                {
                    EmitEvent("connect");
                } 
                else 
                {
                    EmitEvent("disconnect");
                }
            }

            // GC expired acks
            if (AckList.Count == 0) 
            { 
                return; 
            }
            if (DateTime.Now.Subtract(AckList[0].time).TotalSeconds < AckExpirationTime) 
            { 
                return; 
            }
            AckList.RemoveAt(0);
        }

        public void OnDestroy()
        {
            if (SocketThread != null)
            { 
                SocketThread.Abort(); 
            }
            if (PingThread != null)
            { 
                PingThread.Abort(); 
            }
        }

        public void OnApplicationQuit()
        {
            Close();
        }
        
        public void Connect ()
        {
            TransportConnected = true;
            SocketThread = new Thread(RunSocketThread);
            PingThread   = new Thread(RunPingThread);
            SocketThread.Start(Socket);
            PingThread.Start(Socket);
        }

        public void Close ()
        {
            EmitClose();
            TransportConnected = false;
        }

        public void On (string ev, Action<SocketIOEvent> callback)
        {
            if (!SocketEventHandlers.ContainsKey(ev)) 
            {
                SocketEventHandlers[ev] = new List<Action<SocketIOEvent>>();
            }
            SocketEventHandlers[ev].Add(callback);
        }

        public void Off (string ev, Action<SocketIOEvent> callback)
        {
            if (!SocketEventHandlers.ContainsKey(ev)) 
            {
                return;
            }
            List<Action<SocketIOEvent>> l = SocketEventHandlers [ev];
            if (!l.Contains(callback)) 
            {
                return;
            }
            l.Remove(callback);
            if (l.Count == 0) 
            {
                SocketEventHandlers.Remove(ev);
            }
        }

        public void Emit (string ev)
        {
            EmitMessage(-1, string.Format("[\"{0}\"]", ev));
        }

        public void Emit (string ev, Action<JSONObject> action)
        {
            EmitMessage(++PacketID, string.Format("[\"{0}\"]", ev));
            AckList.Add(new Ack(PacketID, action));
        }

        public void Emit (string ev, JSONObject data)
        {
            EmitMessage(-1, string.Format("[\"{0}\",{1}]", ev, data));
        }

        public void Emit (string ev, JSONObject data, Action<JSONObject> action)
        {
            EmitMessage(++PacketID, string.Format("[\"{0}\",{1}]", ev, data));
            AckList.Add(new Ack(PacketID, action));
        }

        private void RunSocketThread (object obj)
        {
            WebSocket webSocket = (WebSocket)obj;
            while (TransportConnected)
            {
                if (webSocket.IsConnected)
                {
                    Thread.Sleep(ReconnectDelay);
                } 
                else 
                {
                    webSocket.Connect();
                }
            }
            webSocket.Close();
        }

        private void RunPingThread (object obj)
        {
            WebSocket webSocket = (WebSocket)obj;
            int timeoutMilis = Mathf.FloorToInt(PingTimeout * 1000);
            int intervalMilis = Mathf.FloorToInt(PingInterval * 1000);
            DateTime pingStart;
            while (TransportConnected)
            {
                if (!WebSocketConnected)
                {
                    Thread.Sleep(ReconnectDelay);
                } 
                else 
                {
                    Pinging = true;
                    Pong    = false;
					EmitPacket(new Packet(EnginePacketType.PING, SocketPacketType.UNKNOWN, -1, "/", -1, null));
                    pingStart = DateTime.Now;
                    while (webSocket.IsConnected && Pinging && (DateTime.Now.Subtract(pingStart).TotalSeconds < timeoutMilis))
                    {
                        Thread.Sleep(200);
                    }
                    if (!Pong)
                    {
                        webSocket.Close();
                    }
                    Thread.Sleep(intervalMilis);
                }
            }
        }

        private void EmitMessage (int id, string raw)
        {
            EmitPacket(new Packet(EnginePacketType.MESSAGE, SocketPacketType.EVENT, 0, "/", id, new JSONObject(raw)));
        }

        private void EmitClose ()
        {
            EmitPacket(new Packet(EnginePacketType.MESSAGE, SocketPacketType.DISCONNECT, 0, "/", -1, new JSONObject("")));
			EmitPacket(new Packet(EnginePacketType.CLOSE, SocketPacketType.UNKNOWN, -1, "/", -1, null));
        }

        private void EmitPacket (Packet packet)
        {
            try 
            {
                Socket.Send(Encode(packet));
            } 
            catch (Exception ex) 
            {
                Debug.Log(ex.ToString());
            }
        }

        private void OnOpen (object sender, EventArgs e)
        {
            EmitEvent("open");
        }

        private void OnMessage (object sender, MessageEventArgs e)
        {
            Packet packet = Decode(e);
            switch (packet.EnginePacketType) 
            {
                case EnginePacketType.OPEN:     HandleOpen(packet);    break;
                case EnginePacketType.CLOSE:    EmitEvent("close");    break;
                case EnginePacketType.PING:     HandlePing();          break;
                case EnginePacketType.PONG:     HandlePong();          break;
                case EnginePacketType.MESSAGE:  HandleMessage(packet); break;
            }
        }

        private void HandleOpen (Packet packet)
        {
            SocketID = packet.PacketData["sid"].str;
            EmitEvent("open");
        }

        private void HandlePing ()
        {
			EmitPacket(new Packet(EnginePacketType.PONG, SocketPacketType.UNKNOWN, -1, "/", -1, null));
        }

        private void HandlePong ()
        {
            Pong    = true;
            Pinging = false;
        }
        
        private void HandleMessage (Packet packet)
        {
            if(packet.PacketData == null) 
            { 
                return; 
            }
            else if(packet.SocketPacketType == SocketPacketType.ACK) 
            {
                for (int i = 0; i < AckList.Count; i++) 
                {
                    if (AckList[i].packetId != packet.PacketID)
                    { 
                        continue; 
                    }
                    lock (AckQueueLock) 
                    { 
                        AckQueue.Enqueue(packet); 
                    }
                    return;
                }
            }
            else if (packet.SocketPacketType == SocketPacketType.EVENT) 
            {
                SocketIOEvent e = Parse(packet.PacketData);
                lock (EventQueueLock) 
                { 
                    EventQueue.Enqueue(e); 
                }
            }
            else
            {
                return;
            }
        }

        private void OnError (object sender, ErrorEventArgs e)
        {
            EmitEvent("error");
        }

        private void OnClose (object sender, CloseEventArgs e)
        {
            EmitEvent("close");
        }

        private void EmitEvent (string type)
        {
            EmitEvent(new SocketIOEvent(type, null));
        }

        private void EmitEvent (SocketIOEvent ev)
        {
            if (!SocketEventHandlers.ContainsKey(ev.EventName)) 
            { 
                return; 
            }
            foreach (Action<SocketIOEvent> handler in this.SocketEventHandlers[ev.EventName]) 
            {
                try
                {
                    handler(ev);
                } 
                catch(Exception ex)
                {
                    Debug.Log(ex.ToString());
                }
            }
        }

        private void InvokeAck (Packet packet)
        {
            Ack ack;
            for (int i = 0; i < AckList.Count; i++)
            {
                if (AckList[i].packetId != packet.PacketID) 
                { 
                    continue; 
                }
                ack = AckList[i];
                AckList.RemoveAt(i);
                ack.Invoke(packet.PacketData);
                return;
            }
        }

        private string Encode (Packet packet)
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                
                // first is type
                builder.Append((int)packet.EnginePacketType);
                if(!packet.EnginePacketType.Equals(EnginePacketType.MESSAGE))
                {
                    return builder.ToString();
                }
                builder.Append((int)packet.SocketPacketType);
                
                // attachments if we have them
                if (packet.SocketPacketType == SocketPacketType.BINARY_EVENT || packet.SocketPacketType == SocketPacketType.BINARY_ACK) 
                {
                    builder.Append(packet.PacketAttachmentsCount);
                    builder.Append('-');
                }
                
                // if we have a namespace other than '/' we append it followed by a comma ','
                if (!string.IsNullOrEmpty(packet.PacketNamespace) && !packet.PacketNamespace.Equals("/")) 
                {
                    builder.Append(packet.PacketNamespace);
                    builder.Append(',');
                }
                
                // immediately followed by the id
                if (packet.PacketID > -1) 
                {
                    builder.Append(packet.PacketID);
                }
                
                if (packet.PacketData != null && !packet.PacketData.ToString().Equals("null")) 
                {
                    builder.Append(packet.PacketData.ToString());
                }
                return builder.ToString();
            } 
            catch(Exception ex) 
            {
                throw new Exception("Packet encoding failed: " + packet ,ex);
            }
        }

        private Packet Decode (MessageEventArgs e)
        {
            try
            {
                int offset = 0;
                Packet packet = new Packet(EnginePacketType.UNKNOWN, SocketPacketType.UNKNOWN, -1, "/", -1, null);
                
                // handle type
                packet.EnginePacketType = (EnginePacketType)int.Parse(e.Data.Substring(offset, 1));
                if (packet.EnginePacketType == EnginePacketType.MESSAGE) 
                {
                    packet.SocketPacketType = (SocketPacketType)int.Parse(e.Data.Substring(++offset, 1));
                }
                if (e.Data.Length <= 2) 
                {
                    return packet;
                }

                // handle namespaces
                if ('/' == e.Data[offset + 1]) 
                {
                    StringBuilder builder = new StringBuilder();
                    while (offset < e.Data.Length - 1 && e.Data[++offset] != ',') 
                    {
                        builder.Append(e.Data[offset]);
                    }
                    packet.PacketNamespace = builder.ToString();
                } 
                else 
                {
                    packet.PacketNamespace = "/";
                }
                
                // look up id
                char next = e.Data[offset + 1];
                if (next != ' ' && char.IsNumber(next)) 
                {
                    StringBuilder builder = new StringBuilder();
                    while (offset < e.Data.Length - 1) 
                    {
                        char c = e.Data[++offset];
                        if (char.IsNumber(c)) 
                        {
                            builder.Append(c);
                        } 
                        else 
                        {
                            --offset;
                            break;
                        }
                    }
                    packet.PacketID = int.Parse(builder.ToString());
                }
                
                // look up json data
                if (++offset < e.Data.Length - 1) 
                {
                    try 
                    {
                        packet.PacketData = new JSONObject(e.Data.Substring(offset));
                    } 
                    catch (Exception ex) 
                    {
                        Debug.LogException(ex);
                    }
                }
                return packet;
            } 
            catch(Exception ex) 
            {
                throw new Exception("Packet decoding failed: " + e.Data, ex);
            }
        }

        private SocketIOEvent Parse (JSONObject json)
        {
            if (json.Count < 1 || json.Count > 2) 
            {
                throw new Exception("Invalid number of parameters received: " + json.Count);
            }
            else if (json[0].type != JSONObject.JSONType.STRING) 
            {
                throw new Exception("Invalid parameter type. " + json[0].type + " received while expecting " + JSONObject.JSONType.STRING);
            }
            else if (json.Count == 1) 
            {
                return new SocketIOEvent(json[0].str, null);
            } 
            else if (json[1].type != JSONObject.JSONType.OBJECT)
            {
                throw new Exception("Invalid argument type. " + json[1].type + " received while expecting " + JSONObject.JSONType.OBJECT);
            }
            else 
            {
                return new SocketIOEvent(json[0].str, json[1]);
            }
        }
    }
}
