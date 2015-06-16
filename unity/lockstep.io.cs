using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SocketIO;
using System;
using System.Text;

public class LockstepIO : MonoBehaviour 
{
	private SocketIOComponent Socket;
	private List<long> SyncOffsets;
	private List<long> SyncRoundTrips;
	private Dictionary<long, JSONObject> CommandQueue;
	private float SyncRateSec = 1f / 60f;
	private int SyncPoolSize = 10;
	private DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	public delegate void ExecuteCommandSignature (JSONObject Command);
	public ExecuteCommandSignature ExecuteCommandFunction;
	public string LastLockstepReadyString;
	public long LastServerNow;
	public long LastLocalNow;
	public long LastSyncOffset;
	public long LastSyncRoundTrip;
	public bool LastSyncStatus;
	public long CommandDelay;
	public long LocalNow 
	{
		get
		{
			return (long)Time.frameCount;
		}
	}
	public long LockStepTime
	{
		get 
		{
			return LocalNow - LastSyncOffset;
		}
	}
	public long SyncOffset
	{
		get 
		{
			long sum = 0;
			for (int i = 0; i < SyncOffsets.Count; i++)
				sum += SyncOffsets[i];
			sum /= SyncOffsets.Count;
			return sum;
		}
	}
	public long SyncRoundTrip
	{
		get 
		{
			long sum = 0;
			for (int i = 0; i < SyncRoundTrips.Count; i++)
				sum += SyncRoundTrips[i];
			sum /= SyncRoundTrips.Count;
			return sum;
		}
	}
	public bool IsSynched
	{
		get
		{
			if (SyncOffsets.Count >= SyncPoolSize)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}

	
	public void Sync(ExecuteCommandSignature executeCommandFunction)
	{
		ExecuteCommandFunction = executeCommandFunction;
		SyncOffsets = new List<long>();
		SyncRoundTrips = new List<long>();
		CommandQueue = new Dictionary<long, JSONObject>();
		Socket = GetComponent<SocketIOComponent>();
		Socket.On ("lockstep.io:seed", OnLockstepSeed);
		Socket.On ("lockstep.io:sync", OnLockstepSync);
		Socket.On ("lockstep.io:ready", OnLockstepReady);
		Socket.On ("lockstep.io:cmd:issue", OnCommandIssue);
		InvokeRepeating("LockstepSync", 0f, SyncRateSec);
	}
	
	private void OnLockstepSeed(SocketIOEvent evt)
	{
		int randomSeed = (int)evt.data.GetField("randomSeed").n;
		UnityEngine.Random.seed = randomSeed;
		Debug.Log ("Seed: " + randomSeed); 
	}
	
	private void LockstepSync ()
	{
		JSONObject ntp = JSONObject.Create(JSONObject.Type.OBJECT);
		ntp.AddField("t0", (double)LocalNow);
		Socket.Emit("lockstep.io:sync", ntp);
		Socket.Emit("lockstep.io:seed", new JSONObject());
	}
	
	private void OnLockstepSync(SocketIOEvent evt)
	{
		LastLocalNow = LocalNow;
		long t0 = (long)evt.data.GetField("t0").n;
		long t1 = (long)evt.data.GetField("t1").n;
		long diff = LastLocalNow - t1 - ((LastLocalNow - t0) / 2);
		long syncRoundTrip = LastLocalNow - t0;
		SyncOffsets.Insert(0, diff);
		if (SyncOffsets.Count > SyncPoolSize)
		{
			SyncOffsets.RemoveAt(SyncOffsets.Count - 1);
		}
		SyncRoundTrips.Insert(0, syncRoundTrip);
		if (SyncRoundTrips.Count > SyncPoolSize)
		{
			SyncRoundTrips.RemoveAt(SyncRoundTrips.Count - 1);
		}
		LastSyncOffset = SyncOffset;
		LastSyncRoundTrip = SyncRoundTrip;
		LastServerNow = t1;
		LastSyncStatus = IsSynched;
		if (LastSyncStatus)
		{
			JSONObject ready = new JSONObject();
			ready.AddField("localNow", (double)LastLocalNow);
			ready.AddField("offset", (double)LastSyncOffset);
			ready.AddField("roundTrip", (double)LastSyncRoundTrip);
			ready.AddField("lockstep", (double)LockStepTime);
			Socket.Emit("lockstep.io:ready", ready);
		}
	}
	
	private void OnLockstepReady(SocketIOEvent evt)
	{
		string format = "000000000000";
		int formatLength = format.Length + 1;
		
		string debugText = "ID".PadLeft(formatLength)        + " "    +
			               "OFFSET".PadLeft(formatLength)    + " "    + 
				           "ROUNDTRIP".PadLeft(formatLength) + " "    +
				           "LOCKSTEP".PadLeft(formatLength)  + "\n\r";
		JSONObject clients = evt.data.GetField("clients");
		CommandDelay = (long)evt.data.GetField("commandDelay").n;
		for (int key = 0; key < clients.keys.Count; key++)
		{
			debugText += clients.keys[key].Substring(0, 8).PadLeft(formatLength)                                   + " "    +
				         clients[clients.keys[key]].GetField("offset").n.ToString(format).PadLeft(formatLength)    + " "    +
					     clients[clients.keys[key]].GetField("roundTrip").n.ToString(format).PadLeft(formatLength) + " "    +
					     clients[clients.keys[key]].GetField("lockstep").n.ToString(format).PadLeft(formatLength)  + "\n\r";
			
		}
		LastLockstepReadyString = debugText;
	}

	private void OnCommandIssue(SocketIOEvent evt)
	{
		long atLockstep = (long)evt.data.GetField("atLockstep").n;
		long delay = (atLockstep - LockStepTime);
		if (delay < 0)
		{
			throw new Exception("Missed Event (LAG)");
		}
		float lockstepDelaySec = (float)delay / 1000f;
		CommandQueue.Add(atLockstep, evt.data);
		Invoke("OnCommandExecute", lockstepDelaySec);
	}
	
	private void OnCommandExecute () 
	{
		long now = LockStepTime;
		long closest = -1;
		long closest_delta = -1;
		foreach(KeyValuePair<long, JSONObject> command in CommandQueue)
		{
			long current_delta = now - command.Key;
			if (closest == -1 || current_delta < closest_delta)
			{
				closest = command.Key;
				closest_delta = current_delta;
			}
		}
		
		JSONObject closestCommand = CommandQueue[closest];
		CommandQueue.Remove(closest);
		ExecuteCommandFunction(closestCommand);
	}
	
	public void IssueCommand(JSONObject Command)
	{
		Command.AddField("atLockstep", (double)(LockStepTime + CommandDelay));
		Socket.Emit("lockstep.io:cmd:issue", Command);
	}
}