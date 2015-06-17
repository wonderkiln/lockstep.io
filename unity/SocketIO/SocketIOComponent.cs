//#define SOCKET_IO_DEBUG           // Uncomment this for debug
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
        public string name { get; set; }
        
        public JSONObject data { get; set; }
        
        public SocketIOEvent(string name) : this(name, null) { }
        
        public SocketIOEvent(string name, JSONObject data)
        {
            this.name = name;
            this.data = data;
        }

        public override string ToString()
        {
            return string.Format("[SocketIOEvent: name={0}, data={1}]", name, data);
        }
    }
    
    public class Packet
    {
        public EnginePacketType enginePacketType;
        public SocketPacketType socketPacketType;
        
        public int attachments;
        public string nsp;
        public int id;
        public JSONObject json;
        
        public Packet() : this(EnginePacketType.UNKNOWN, SocketPacketType.UNKNOWN, -1, "/", -1, null) { }
        public Packet(EnginePacketType enginePacketType) : this(enginePacketType, SocketPacketType.UNKNOWN, -1, "/", -1, null) { }
        
        public Packet(EnginePacketType enginePacketType, SocketPacketType socketPacketType, int attachments, string nsp, int id, JSONObject json)
        {
            this.enginePacketType = enginePacketType;
            this.socketPacketType = socketPacketType;
            this.attachments = attachments;
            this.nsp = nsp;
            this.id = id;
            this.json = json;
        }
        
        public override string ToString()
        {
            return string.Format("[Packet: enginePacketType={0}, socketPacketType={1}, attachments={2}, nsp={3}, id={4}, json={5}]", enginePacketType, socketPacketType, attachments, nsp, id, json);
        }
    }
    
    public class Encoder
    {
        public string Encode(Packet packet)
        {
            try
            {
                #if SOCKET_IO_DEBUG
                Debug.Log("[SocketIO] Encoding: " + packet.json);
                #endif
                
                StringBuilder builder = new StringBuilder();
                
                // first is type
                builder.Append((int)packet.enginePacketType);
                if(!packet.enginePacketType.Equals(EnginePacketType.MESSAGE)){
                    return builder.ToString();
                }
                
                builder.Append((int)packet.socketPacketType);
                
                // attachments if we have them
                if (packet.socketPacketType == SocketPacketType.BINARY_EVENT || packet.socketPacketType == SocketPacketType.BINARY_ACK) {
                    builder.Append(packet.attachments);
                    builder.Append('-');
                }
                
                // if we have a namespace other than '/'
                // we append it followed by a comma ','
                if (!string.IsNullOrEmpty(packet.nsp) && !packet.nsp.Equals("/")) {
                    builder.Append(packet.nsp);
                    builder.Append(',');
                }
                
                // immediately followed by the id
                if (packet.id > -1) {
                    builder.Append(packet.id);
                }
                
                if (packet.json != null && !packet.json.ToString().Equals("null")) {
                    builder.Append(packet.json.ToString());
                }
                
                #if SOCKET_IO_DEBUG
                Debug.Log("[SocketIO] Encoded: " + builder);
                #endif
                
                return builder.ToString();
                
            } catch(Exception ex) {
                throw new Exception("Packet encoding failed: " + packet ,ex);
            }
        }
    }
    
    public class Decoder
    {
        public Packet Decode(MessageEventArgs e)
        {
            try
            {
                #if SOCKET_IO_DEBUG
                Debug.Log("[SocketIO] Decoding: " + e.Data);
                #endif
                
                string data = e.Data;
                Packet packet = new Packet();
                int offset = 0;
                
                // look up packet type
                int enginePacketType = int.Parse(data.Substring(offset, 1));
                packet.enginePacketType = (EnginePacketType)enginePacketType;
                
                if (enginePacketType == (int)EnginePacketType.MESSAGE) {
                    int socketPacketType = int.Parse(data.Substring(++offset, 1));
                    packet.socketPacketType = (SocketPacketType)socketPacketType;
                }
                
                // connect message properly parsed
                if (data.Length <= 2) {
                    #if SOCKET_IO_DEBUG
                    Debug.Log("[SocketIO] Decoded: " + packet);
                    #endif
                    return packet;
                }
                
                // look up namespace (if any)
                if ('/' == data [offset + 1]) {
                    StringBuilder builder = new StringBuilder();
                    while (offset < data.Length - 1 && data[++offset] != ',') {
                        builder.Append(data [offset]);
                    }
                    packet.nsp = builder.ToString();
                } else {
                    packet.nsp = "/";
                }
                
                // look up id
                char next = data [offset + 1];
                if (next != ' ' && char.IsNumber(next)) {
                    StringBuilder builder = new StringBuilder();
                    while (offset < data.Length - 1) {
                        char c = data [++offset];
                        if (char.IsNumber(c)) {
                            builder.Append(c);
                        } else {
                            --offset;
                            break;
                        }
                    }
                    packet.id = int.Parse(builder.ToString());
                }
                
                // look up json data
                if (++offset < data.Length - 1) {
                    try {
                        #if SOCKET_IO_DEBUG
                        Debug.Log("[SocketIO] Parsing JSON: " + data.Substring(offset));
                        #endif
                        packet.json = new JSONObject(data.Substring(offset));
                    } catch (Exception ex) {
                        Debug.LogException(ex);
                    }
                }
                
                #if SOCKET_IO_DEBUG
                Debug.Log("[SocketIO] Decoded: " + packet);
                #endif
                
                return packet;
                
            } catch(Exception ex) {
                throw new Exception("Packet decoding failed: " + e.Data ,ex);
            }
        }
    }


    public class SocketIOComponent : MonoBehaviour
    {

        #region Public Properties
    
        public bool useLocal = true;
        public bool autoConnect = true;
        public int reconnectDelay = 5;
        public float ackExpirationTime = 1800f;
        public float pingInterval = 25f;
        public float pingTimeout = 60f;
        

        public WebSocket socket { get { return ws; } }
        public string sid { get; set; }
        public bool IsConnected { get { return connected; } }

        #endregion

        #region Private Properties
        private string CLOUDURL = "ws://52.5.116.134:80/socket.io/?EIO=4&transport=websocket";
        private string LOCALURL = "ws://127.0.0.1:80/socket.io/?EIO=4&transport=websocket";
        private volatile bool connected;
        private volatile bool thPinging;
        private volatile bool thPong;
        private volatile bool wsConnected;

        private Thread socketThread;
        private Thread pingThread;
        private WebSocket ws;

        private Encoder encoder;
        private Decoder decoder;

        private Dictionary<string, List<Action<SocketIOEvent>>> handlers;
        private List<Ack> ackList;

        private int packetId;

        private object eventQueueLock;
        private Queue<SocketIOEvent> eventQueue;

        private object ackQueueLock;
        private Queue<Packet> ackQueue;

        #endregion

        #if SOCKET_IO_DEBUG
        public Action<string> debugMethod;
        #endif

        #region Unity interface

        public void Awake()
        {
            encoder = new Encoder();
            decoder = new Decoder();
            handlers = new Dictionary<string, List<Action<SocketIOEvent>>>();
            ackList = new List<Ack>();
            sid = null;
            packetId = 0;

            ws = new WebSocket(useLocal ? LOCALURL : CLOUDURL);
            ws.OnOpen += OnOpen;
            ws.OnMessage += OnMessage;
            ws.OnError += OnError;
            ws.OnClose += OnClose;
            wsConnected = false;

            eventQueueLock = new object();
            eventQueue = new Queue<SocketIOEvent>();

            ackQueueLock = new object();
            ackQueue = new Queue<Packet>();

            connected = false;

            #if SOCKET_IO_DEBUG
            if(debugMethod == null) { debugMethod = Debug.Log; };
            #endif
        }

        public void Start()
        {
            if (autoConnect) { Connect(); }
        }

        public void Update()
        {
            lock(eventQueueLock){ 
                while(eventQueue.Count > 0){
                    EmitEvent(eventQueue.Dequeue());
                }
            }

            lock(ackQueueLock){
                while(ackQueue.Count > 0){
                    InvokeAck(ackQueue.Dequeue());
                }
            }

            if(wsConnected != ws.IsConnected){
                wsConnected = ws.IsConnected;
                if(wsConnected){
                    EmitEvent("connect");
                } else {
                    EmitEvent("disconnect");
                }
            }

            // GC expired acks
            if(ackList.Count == 0) { return; }
            if(DateTime.Now.Subtract(ackList[0].time).TotalSeconds < ackExpirationTime){ return; }
            ackList.RemoveAt(0);
        }

        public void OnDestroy()
        {
            if (socketThread != null)   { socketThread.Abort(); }
            if (pingThread != null)     { pingThread.Abort(); }
        }

        public void OnApplicationQuit()
        {
            Close();
        }

        #endregion

        #region Public Interface
        
        public void Connect()
        {
            connected = true;

            socketThread = new Thread(RunSocketThread);
            socketThread.Start(ws);

            pingThread = new Thread(RunPingThread);
            pingThread.Start(ws);
        }

        public void Close()
        {
            EmitClose();
            connected = false;
        }

        public void On(string ev, Action<SocketIOEvent> callback)
        {
            if (!handlers.ContainsKey(ev)) {
                handlers[ev] = new List<Action<SocketIOEvent>>();
            }
            handlers[ev].Add(callback);
        }

        public void Off(string ev, Action<SocketIOEvent> callback)
        {
            if (!handlers.ContainsKey(ev)) {
                #if SOCKET_IO_DEBUG
                debugMethod.Invoke("[SocketIO] No callbacks registered for event: " + ev);
                #endif
                return;
            }

            List<Action<SocketIOEvent>> l = handlers [ev];
            if (!l.Contains(callback)) {
                #if SOCKET_IO_DEBUG
                debugMethod.Invoke("[SocketIO] Couldn't remove callback action for event: " + ev);
                #endif
                return;
            }

            l.Remove(callback);
            if (l.Count == 0) {
                handlers.Remove(ev);
            }
        }

        public void Emit(string ev)
        {
            EmitMessage(-1, string.Format("[\"{0}\"]", ev));
        }

        public void Emit(string ev, Action<JSONObject> action)
        {
            EmitMessage(++packetId, string.Format("[\"{0}\"]", ev));
            ackList.Add(new Ack(packetId, action));
        }

        public void Emit(string ev, JSONObject data)
        {
            EmitMessage(-1, string.Format("[\"{0}\",{1}]", ev, data));
        }

        public void Emit(string ev, JSONObject data, Action<JSONObject> action)
        {
            EmitMessage(++packetId, string.Format("[\"{0}\",{1}]", ev, data));
            ackList.Add(new Ack(packetId, action));
        }

        #endregion

        #region Private Methods

        private void RunSocketThread(object obj)
        {
            WebSocket webSocket = (WebSocket)obj;
            while(connected){
                if(webSocket.IsConnected){
                    Thread.Sleep(reconnectDelay);
                } else {
                    webSocket.Connect();
                }
            }
            webSocket.Close();
        }

        private void RunPingThread(object obj)
        {
            WebSocket webSocket = (WebSocket)obj;

            int timeoutMilis = Mathf.FloorToInt(pingTimeout * 1000);
            int intervalMilis = Mathf.FloorToInt(pingInterval * 1000);

            DateTime pingStart;

            while(connected)
            {
                if(!wsConnected){
                    Thread.Sleep(reconnectDelay);
                } else {
                    thPinging = true;
                    thPong =  false;
                    
                    EmitPacket(new Packet(EnginePacketType.PING));
                    pingStart = DateTime.Now;
                    
                    while(webSocket.IsConnected && thPinging && (DateTime.Now.Subtract(pingStart).TotalSeconds < timeoutMilis)){
                        Thread.Sleep(200);
                    }
                    
                    if(!thPong){
                        webSocket.Close();
                    }

                    Thread.Sleep(intervalMilis);
                }
            }
        }

        private void EmitMessage(int id, string raw)
        {
            EmitPacket(new Packet(EnginePacketType.MESSAGE, SocketPacketType.EVENT, 0, "/", id, new JSONObject(raw)));
        }

        private void EmitClose()
        {
            EmitPacket(new Packet(EnginePacketType.MESSAGE, SocketPacketType.DISCONNECT, 0, "/", -1, new JSONObject("")));
            EmitPacket(new Packet(EnginePacketType.CLOSE));
        }

        private void EmitPacket(Packet packet)
        {
            #if SOCKET_IO_DEBUG
            debugMethod.Invoke("[SocketIO] " + packet);
            #endif
            
            try {
                ws.Send(encoder.Encode(packet));
            } catch(Exception ex) {
                Debug.Log(ex.ToString());
                #if SOCKET_IO_DEBUG
                debugMethod.Invoke(ex.ToString());
                #endif
            }
        }

        private void OnOpen(object sender, EventArgs e)
        {
            EmitEvent("open");
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            #if SOCKET_IO_DEBUG
            debugMethod.Invoke("[SocketIO] Raw message: " + e.Data);
            #endif
            Packet packet = decoder.Decode(e);

            switch (packet.enginePacketType) {
                case EnginePacketType.OPEN:     HandleOpen(packet);     break;
                case EnginePacketType.CLOSE:    EmitEvent("close");     break;
                case EnginePacketType.PING:     HandlePing();           break;
                case EnginePacketType.PONG:     HandlePong();           break;
                case EnginePacketType.MESSAGE:  HandleMessage(packet);  break;
            }
        }

        private void HandleOpen(Packet packet)
        {
            #if SOCKET_IO_DEBUG
            debugMethod.Invoke("[SocketIO] Socket.IO sid: " + packet.json["sid"].str);
            #endif
            sid = packet.json["sid"].str;
            EmitEvent("open");
        }

        private void HandlePing()
        {
            EmitPacket(new Packet(EnginePacketType.PONG));
        }

        private void HandlePong()
        {
            thPong = true;
            thPinging = false;
        }
        
        private void HandleMessage(Packet packet)
        {
            if(packet.json == null) { return; }

            if(packet.socketPacketType == SocketPacketType.ACK){
                for(int i = 0; i < ackList.Count; i++){
                    if(ackList[i].packetId != packet.id){ continue; }
                    lock(ackQueueLock){ ackQueue.Enqueue(packet); }
                    return;
                }

                #if SOCKET_IO_DEBUG
                debugMethod.Invoke("[SocketIO] Ack received for invalid Action: " + packet.id);
                #endif
            }

            if (packet.socketPacketType == SocketPacketType.EVENT) {
                SocketIOEvent e = Parse(packet.json);
                lock(eventQueueLock){ eventQueue.Enqueue(e); }
            }
        }

        private SocketIOEvent Parse (JSONObject json)
        {
            if (json.Count < 1 || json.Count > 2) 
            {
                throw new Exception("Invalid number of parameters received: " + json.Count);
            }
            
            if (json[0].type != JSONObject.Type.STRING) {
                throw new Exception("Invalid parameter type. " + json[0].type + " received while expecting " + JSONObject.Type.STRING);
            }
            
            if (json.Count == 1) {
                return new SocketIOEvent(json[0].str);
            } 
            
            if (json[1].type != JSONObject.Type.OBJECT) {
                throw new Exception("Invalid argument type. " + json[1].type + " received while expecting " + JSONObject.Type.OBJECT);
            }
            
            return new SocketIOEvent(json[0].str, json[1]);
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            EmitEvent("error");
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            EmitEvent("close");
        }

        private void EmitEvent(string type)
        {
            EmitEvent(new SocketIOEvent(type));
        }

        private void EmitEvent(SocketIOEvent ev)
        {
            if (!handlers.ContainsKey(ev.name)) { return; }
            foreach (Action<SocketIOEvent> handler in this.handlers[ev.name]) {
                try{
                    handler(ev);
                } catch(Exception ex){
                    Debug.Log(ex.ToString());
                    #if SOCKET_IO_DEBUG
                    debugMethod.Invoke(ex.ToString());
                    #endif
                }
            }
        }

        private void InvokeAck(Packet packet)
        {
            Ack ack;
            for(int i = 0; i < ackList.Count; i++){
                if(ackList[i].packetId != packet.id){ continue; }
                ack = ackList[i];
                ackList.RemoveAt(i);
                ack.Invoke(packet.json);
                return;
            }
        }

        #endregion
    }
}
