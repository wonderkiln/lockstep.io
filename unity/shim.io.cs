using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SocketIO;
using System;

public class ShimIO : MonoBehaviour 
{
	public enum Dispatcher {
		stateSync,
		buildingManager,
		minionManager
	};
	public GameObject       BuildingManagerObject;
	public GameObject       MinionManagerObject;
	public GameObject       DebugTextObject;
	private BuildingManager buildingManager;
	private MinionManager   minionManager;
	private TextMesh        debugTextMesh;
	private LockstepIO      lockstepIO;

	void Start () 
	{
		debugTextMesh = DebugTextObject.GetComponent<TextMesh>();
		buildingManager = BuildingManagerObject.GetComponent<BuildingManager>();
		minionManager = MinionManagerObject.GetComponent<MinionManager>();
		lockstepIO = GetComponent<LockstepIO>();
		lockstepIO.Sync(ExecuteCommand);
	}
	
	void Update () 
	{
		debugTextMesh.text = lockstepIO.LastLockstepReadyString;
	}
	
	public void FixedUpdate ()
	{	
		StateSync();
	}
	
	private void StateSync ()
	{
		if (lockstepIO.IsSynched)
		{
			long lockstepTime = lockstepIO.LockStepTime;
			if (lockstepTime % 30 == 0)
			{
				JSONObject state = new JSONObject();
				state.AddField("dispatcher", (int)Dispatcher.stateSync);
				state.AddField("lockstepTime", lockstepTime);
				state.AddField("minionManger", minionManager.GetState());
				string hash = Checksum.Hash(state.ToString()).ToString();
				state.AddField("hash", hash);
				lockstepIO.IssueCommand(state);
			}
		}
	}
	
	public void IssueCommand(JSONObject Command)
	{
		lockstepIO.IssueCommand(Command);
	}

	public void ExecuteCommand(JSONObject Command)
	{
		int dispatcher = (int)Command["dispatcher"].n;
		switch ((Dispatcher)dispatcher)
		{
			case Dispatcher.stateSync:
				buildingManager.ExecuteCommand(Command);
				minionManager.ExecuteCommand(Command);
				break;
			case Dispatcher.buildingManager:
				buildingManager.ExecuteCommand(Command);
				break;
			case Dispatcher.minionManager:
				minionManager.ExecuteCommand(Command);
				break;
			default:
				throw new Exception("Unknown dispatcher " + dispatcher);
		}
	}
}