using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class JSONObject 
{
	public const int    MaxDepth = 100;
	public const string JSONInfinity = "\"INFINITY\"";
	public const string JSONNegativeInfinity = "\"NEGINFINITY\"";
	public const string JSONNotANumber = "\"NaN\"";
	public static readonly char[] JSONWhitespace = new[] { ' ', '\r', '\n', '\t' };
	public enum JSONType { NULL, STRING, NUMBER, OBJECT, ARRAY, BOOL, BAKED }
	public bool IsContainer { get { return (type == JSONType.ARRAY || type == JSONType.OBJECT); } }
	public JSONType type = JSONType.NULL;
	public int Count 
	{
		get 
		{
			if (list == null)
			{
				return -1;
			}
			return list.Count;
		}
	}
	public List<JSONObject> list;
	public List<string> keys;
	public string str;
	public double n;
	public bool b;
	public delegate void AddJSONConents(JSONObject self);

	public static JSONObject JSONNull { get { return Create(JSONType.NULL); } }	
	public static JSONObject obj { get { return Create(JSONType.OBJECT); } }
	public static JSONObject arr { get { return Create(JSONType.ARRAY); } }	

	public JSONObject(JSONType t) 
	{
		type = t;
		switch(t) 
		{
			case JSONType.ARRAY:
				list = new List<JSONObject>();
				break;
			case JSONType.OBJECT:
				list = new List<JSONObject>();
				keys = new List<string>();
				break;
		}
	}
	
	public JSONObject(bool b) 
	{
		type = JSONType.BOOL;
		this.b = b;
	}
	
	public JSONObject(double d) 
	{
		type = JSONType.NUMBER;
		n = d;
	}

	public JSONObject(Dictionary<string, string> dic) 
	{
		type = JSONType.OBJECT;
		keys = new List<string>();
		list = new List<JSONObject>();
		foreach (KeyValuePair<string, string> kvp in dic) 
		{
			keys.Add(kvp.Key);
			list.Add(CreateStringObject(kvp.Value));
		}
	}

	public JSONObject(Dictionary<string, JSONObject> dic) 
	{
		type = JSONType.OBJECT;
		keys = new List<string>();
		list = new List<JSONObject>();
		foreach(KeyValuePair<string, JSONObject> kvp in dic) 
		{
			keys.Add(kvp.Key);
			list.Add(kvp.Value);
		}
	}

	public JSONObject (AddJSONConents content) 
	{
		content.Invoke(this);
	}

	public JSONObject (JSONObject[] objs) 
	{
		type = JSONType.ARRAY;
		list = new List<JSONObject>(objs);
	}

	public static JSONObject StringObject (string val) 
	{ 
		return CreateStringObject(val); 
	}
	public void Absorb (JSONObject obj) 
	{
		list.AddRange(obj.list);
		keys.AddRange(obj.keys);
		str = obj.str;
		n = obj.n;
		b = obj.b;
		type = obj.type;
	}
	
	public static JSONObject Create() 
	{
		return new JSONObject();
	}

	public static JSONObject Create (JSONType t) 
	{
		JSONObject obj = Create();
		obj.type = t;
		switch (t) 
		{
			case JSONType.ARRAY:
				obj.list = new List<JSONObject>();
				break;
			case JSONType.OBJECT:
				obj.list = new List<JSONObject>();
				obj.keys = new List<string>();
				break;
		}
		return obj;
	}

	public static JSONObject Create (bool val) 
	{
		JSONObject obj = Create();
		obj.type = JSONType.BOOL;
		obj.b = val;
		return obj;
	}
	
	public static JSONObject Create (float val) 
	{
		JSONObject obj = Create();
		obj.type = JSONType.NUMBER;
		obj.n = val;
		return obj;
	}
	
	public static JSONObject Create (double val) 
	{
		JSONObject obj = Create();
		obj.type = JSONType.NUMBER;
		obj.n = val;
		return obj;
	}
	
	public static JSONObject Create (int val) 
	{
		JSONObject obj = Create();
		obj.type = JSONType.NUMBER;
		obj.n = val;
		return obj;
	}
	
	public static JSONObject CreateStringObject (string val) 
	{
		JSONObject obj = Create();
		obj.type = JSONType.STRING;
		obj.str = val;
		return obj;
	}

	public static JSONObject CreateBakedObject (string val) 
	{
		JSONObject bakedObject = Create();
		bakedObject.type = JSONType.BAKED;
		bakedObject.str = val;
		return bakedObject;
	}

	public static JSONObject Create (string val) 
	{ 
		return Create(val, -2, false, false); 
	}

	public static JSONObject Create (string val, int maxDepth) 
	{ 
		return Create(val, maxDepth, false, false); 
	}

	public static JSONObject Create (string val, int maxDepth, bool storeExcessLevels) 
	{ 
		return Create(val, maxDepth, storeExcessLevels, false); 
	}

	public static JSONObject Create (string val, int maxDepth, bool storeExcessLevels, bool strict) 
	{
		JSONObject obj = Create();
		obj.Parse(val, maxDepth, storeExcessLevels, strict);
		return obj;
	}

	public static JSONObject Create (AddJSONConents content) 
	{
		JSONObject obj = Create();
		content.Invoke(obj);
		return obj;
	}

	public static JSONObject Create (Dictionary<string, string> dic) 
	{
		JSONObject obj = Create();
		obj.type = JSONType.OBJECT;
		obj.keys = new List<string>();
		obj.list = new List<JSONObject>();
		foreach(KeyValuePair<string, string> kvp in dic) 
		{
			obj.keys.Add(kvp.Key);
			obj.list.Add(CreateStringObject(kvp.Value));
		}
		return obj;
	}

	public JSONObject () 
	{ 
	}


	public JSONObject (string str)
	{ 
		Parse(str, -2, false, false); 
	}

	public JSONObject (string str, int maxDepth, bool storeExcessLevels, bool strict) 
	{
		Parse(str, maxDepth, storeExcessLevels, strict);
	}

	void Parse (string str) 
	{ 
		Parse(str, -2, false, false); 
	}

	void Parse (string str, int maxDepth, bool storeExcessLevels, bool strict) 
	{
		if (!string.IsNullOrEmpty(str)) 
		{
			str = str.Trim(JSONWhitespace);
			if (strict) 
			{
				if(str[0] != '[' && str[0] != '{') 
				{
					type = JSONType.NULL;
					Debug.LogWarning("Improper (strict) JSON formatting.  First character must be [ or {");
					return;
				}
			}
			if (str.Length > 0) 
			{
				if (string.Compare(str, "true", true) == 0) 
				{
					type = JSONType.BOOL;
					b = true;
				} 
				else if (string.Compare(str, "false", true) == 0) 
				{
					type = JSONType.BOOL;
					b = false;
				} 
				else if (string.Compare(str, "null", true) == 0) 
				{
					type = JSONType.NULL;
				} 
				else if (str == JSONInfinity) 
				{
					type = JSONType.NUMBER;
					n = double.PositiveInfinity;
				} 
				else if (str == JSONNegativeInfinity) 
				{
					type = JSONType.NUMBER;
					n = double.NegativeInfinity;
				} 
				else if (str == JSONNotANumber) 
				{
					type = JSONType.NUMBER;
					n = double.NaN;
				} 
				else if (str[0] == '"') 
				{
					type = JSONType.STRING;
					this.str = str.Substring(1, str.Length - 2);
				} 
				else 
				{
					int tokenTmp = 1;
					int offset = 0;
					switch (str[offset]) 
					{
						case '{':
							type = JSONType.OBJECT;
							keys = new List<string>();
							list = new List<JSONObject>();
							break;
						case '[':
							type = JSONType.ARRAY;
							list = new List<JSONObject>();
							break;
						default:
							try 
							{
								n = System.Convert.ToDouble(str);
								type = JSONType.NUMBER;
							} 
							catch (System.FormatException) 
							{
								type = JSONType.NULL;
								Debug.LogWarning("improper JSON formatting:" + str);
							}
							return;
					}
					string propName = "";
					bool openQuote = false;
					bool inProp = false;
					int depth = 0;
					while (++offset < str.Length) 
					{
						if (System.Array.IndexOf(JSONWhitespace, str[offset]) > -1)
						{
							continue;
						}
						if (str[offset] == '\\') 
						{
							offset += 1;
							continue;
						}
						if (str[offset] == '"') 
						{
							if (openQuote) 
							{
								if (!inProp && depth == 0 && type == JSONType.OBJECT)
								{
									propName = str.Substring(tokenTmp + 1, offset - tokenTmp - 1);
								}
								openQuote = false;
							} 
							else 
							{
								if(depth == 0 && type == JSONType.OBJECT) 
								{
									tokenTmp = offset;
								}
								openQuote = true;
							}
						}
						if (openQuote)
						{
							continue;
						}
						if (type == JSONType.OBJECT && depth == 0) 
						{
							if(str[offset] == ':') 
							{
								tokenTmp = offset + 1;
								inProp = true;
							}
						}

						if(str[offset] == '[' || str[offset] == '{') 
						{
							depth++;
						} 
						else if (str[offset] == ']' || str[offset] == '}') 
						{
							depth--;
						}
						if ((str[offset] == ',' && depth == 0) || depth < 0) 
						{
							inProp = false;
							string inner = str.Substring(tokenTmp, offset - tokenTmp).Trim(JSONWhitespace);
							if(inner.Length > 0) 
							{
								if(type == JSONType.OBJECT)
								{
									keys.Add(propName);
								}
								if(maxDepth != -1)	
								{
									list.Add(Create(inner, (maxDepth < -1) ? -2 : maxDepth - 1));
								}
								else if (storeExcessLevels)
								{
									list.Add(CreateBakedObject(inner));
								}
							}
							tokenTmp = offset + 1;
						}
					}
				}
			} 
			else 
			{
				type = JSONType.NULL;
			}
		} 
		else 
		{
			type = JSONType.NULL;
		}
	}

	public bool IsNumber { get { return type == JSONType.NUMBER; } }
	public bool IsNull   { get { return type == JSONType.NULL;   } }
	public bool IsString { get { return type == JSONType.STRING; } }
	public bool IsBool   { get { return type == JSONType.BOOL;   } }
	public bool IsArray  { get { return type == JSONType.ARRAY;  } }
	public bool IsObject { get { return type == JSONType.OBJECT; } }
	
	public void Add (bool val) 
	{
		Add(Create(val));
	}

	public void Add (float val) 
	{
		Add(Create(val));
	}

	public void Add (int val) 
	{
		Add(Create(val));
	}

	public void Add (string str) 
	{
		Add(CreateStringObject(str));
	}

	public void Add (AddJSONConents content) 
	{
		Add(Create(content));
	}

	public void Add (JSONObject obj) 
	{
		if (obj) 
		{
			if (type != JSONType.ARRAY) 
			{
				type = JSONType.ARRAY;
				if (list == null)
				{
					list = new List<JSONObject>();
				}
			}
			list.Add(obj);
		}
	}

	public void AddField (string name, bool val) 
	{
		AddField(name, Create(val));
	}

	public void AddField (string name, float val) 
	{
		AddField(name, Create(val));
	}

	public void AddField (string name, double val) 
	{
		AddField(name, Create(val));
	}

	public void AddField (string name, int val) 
	{
		AddField(name, Create(val));
	}

	public void AddField (string name, AddJSONConents content) 
	{
		AddField(name, Create(content));
	}

	public void AddField (string name, string val) 
	{
		AddField(name, CreateStringObject(val));
	}

	public void AddField (string name, JSONObject obj) 
	{
		if (obj) 
		{
			if (type != JSONType.OBJECT) 
			{
				if (keys == null)
				{
					keys = new List<string>();
				}
				if (type == JSONType.ARRAY) 
				{
					for (int i = 0; i < list.Count; i++)
					{
						keys.Add(i + "");
					}
				} 
				else if (list == null)
				{
					list = new List<JSONObject>();
				}
				type = JSONType.OBJECT;
			}
			keys.Add(name);
			list.Add(obj);
		}
	}

	public void SetField (string name, bool val) 
	{ 
		SetField(name, Create(val)); 
	}
	public void SetField (string name, float val) 
	{ 
		SetField(name, Create(val)); 
	}
	public void SetField (string name, int val) 
	{ 
		SetField(name, Create(val)); 
	}
	public void SetField (string name, JSONObject obj) 
	{
		if (HasField(name)) 
		{
			list.Remove(this[name]);
			keys.Remove(name);
		}
		AddField(name, obj);
	}
	public void RemoveField (string name) 
	{
		if (keys.IndexOf(name) > -1) 
		{
			list.RemoveAt(keys.IndexOf(name));
			keys.Remove(name);
		}
	}
	public delegate void FieldNotFound (string name);
	public delegate void GetFieldResponse (JSONObject obj);

	public void GetField (ref bool field, string name) 
	{ 
		GetField(ref field, name, null); 
	}

	public void GetField (ref bool field, string name, FieldNotFound fail) 
	{
		if (type == JSONType.OBJECT) 
		{
			int index = keys.IndexOf(name);
			if (index >= 0) 
			{
				field = list[index].b;
				return;
			}
		}
		if (fail != null) 
		{
			fail.Invoke(name);
		}
	}

	public void GetField (ref double field, string name) 
	{ 
		GetField(ref field, name, null); 
	}
	
	public void GetField (ref double field, string name, FieldNotFound fail) 
	{
		if (type == JSONType.OBJECT) 
		{
			int index = keys.IndexOf(name);
			if (index >= 0) 
			{
				field = list[index].n;
				return;
			}
		}
		if (fail != null) 
		{
			fail.Invoke(name);
		}
	}

	public void GetField (ref int field, string name) 
	{ 
		GetField(ref field, name, null); 
	}
	
	public void GetField (ref int field, string name, FieldNotFound fail) 
	{
		if (type == JSONType.OBJECT) 
		{
			int index = keys.IndexOf(name);
			if(index >= 0) 
			{
				field = (int)list[index].n;
				return;
			}
		}
		if (fail != null) 
		{
			fail.Invoke(name);
		}
	}
	
	public void GetField (ref uint field, string name) 
	{ 
		GetField(ref field, name, null); 

	}

	public void GetField (ref uint field, string name, FieldNotFound fail) 
	{
		if (type == JSONType.OBJECT) 
		{
			int index = keys.IndexOf(name);
			if(index >= 0) 
			{
				field = (uint)list[index].n;
				return;
			}
		}
		if (fail != null) 
		{
			fail.Invoke(name);
		}
	}
	
	public void GetField (ref string field, string name) 
	{ 
		GetField(ref field, name, null); 
	}

	public void GetField (ref string field, string name, FieldNotFound fail) 
	{
		if (type == JSONType.OBJECT) 
		{
			int index = keys.IndexOf(name);
			if (index >= 0) 
			{
				field = list[index].str;
				return;
			}
		}
		if (fail != null) 
		{
			fail.Invoke(name);
		}
	}
	
	public void GetField (string name, GetFieldResponse response) 
	{ 
		GetField(name, response, null); 
	}

	public void GetField (string name, GetFieldResponse response, FieldNotFound fail) 
	{
		if (response != null && type == JSONType.OBJECT) 
		{
			int index = keys.IndexOf(name);
			if (index >= 0) 
			{
				response.Invoke(list[index]);
				return;
			}
		}
		if(fail != null) 
		{
			fail.Invoke(name);
		}
	}

	public JSONObject GetField (string name) 
	{
		if (type == JSONType.OBJECT)
		{
			for (int i = 0; i < keys.Count; i++)
			{
				if (keys[i] == name)
				{
					return list[i];
				}
			}
		}
		return null;
	}

	public bool HasFields (string[] names) 
	{
		for (int i = 0; i < names.Length; i++)
		{
			if (!keys.Contains(names[i]))
			{
				return false;
			}
		}
		return true;
	}

	public bool HasField (string name) 
	{
		if (type == JSONType.OBJECT) 
		{
			for(int i = 0; i < keys.Count; i++) 
			{
				if(keys[i] == name) 
				{
					return true;
				}
			}
		}
		return false;
	}

	public void Clear() 
	{
		type = JSONType.NULL;
		if(list != null)
		{
			list.Clear();
		}
		if(keys != null)
		{
			keys.Clear();
		}
		str = "";
		n = 0;
		b = false;
	}

	public JSONObject Copy () 
	{
		return Create(Print());
	}

	public void Bake() 
	{
		if (type != JSONType.BAKED) 
		{
			str = Print();
			type = JSONType.BAKED;
		}
	}
	public IEnumerable BakeAsync () 
	{
		if (type != JSONType.BAKED) 
		{
			foreach (string s in PrintAsync()) 
			{
				if (s == null)
				{
					yield return s;
				}
				else 
				{
					str = s;
				}
			}
			type = JSONType.BAKED;
		}
	}
	
	public string Print () 
	{ 
		return Print(false); 
	}

	public string Print (bool pretty) 
	{
		StringBuilder builder = new StringBuilder();
		Stringify(0, builder, pretty);
		return builder.ToString();
	}

	public IEnumerable<string> PrintAsync () 
	{ 
		return PrintAsync(false); 
	}

	public IEnumerable<string> PrintAsync (bool pretty) 
	{
		StringBuilder builder = new StringBuilder();
		foreach (IEnumerable e in StringifyAsync(0, builder, pretty)) 
		{
			yield return null;
		}
		yield return builder.ToString();
	}

	const float maxFrameTime = 0.008f;

	IEnumerable StringifyAsync (int depth, StringBuilder builder) 
	{	
		return StringifyAsync(depth, builder, false); 
	}

	IEnumerable StringifyAsync (int depth, StringBuilder builder, bool pretty) 
	{	
		if (depth++ > MaxDepth) 
		{
			Debug.Log("reached max depth!");
			yield break;
		}
		switch (type) 
		{
			case JSONType.BAKED:
				builder.Append(str);
				break;
			case JSONType.STRING:
				builder.AppendFormat("\"{0}\"", str);
				break;
			case JSONType.NUMBER:
				if(double.IsInfinity(n))
				{
					builder.Append(JSONInfinity);
				}
				else if(double.IsNegativeInfinity(n))
				{
					builder.Append(JSONNegativeInfinity);
				}
				else if(double.IsNaN(n))
				{
					builder.Append(JSONNotANumber);
				}
				else
				{
					builder.Append(n.ToString());
				}
				break;
			case JSONType.OBJECT:
				builder.Append("{");
				if(list.Count > 0) 
				{
					if (pretty)
					{
						builder.Append("\n");
					}
					for (int i = 0; i < list.Count; i++) 
					{
						string key = keys[i];
						JSONObject obj = list[i];
						if (obj) 
						{
							if (pretty)
							{
								for (int j = 0; j < depth; j++)
								{
									builder.Append("\t"); 
								}
							}
							builder.AppendFormat("\"{0}\":", key);
							foreach (IEnumerable e in obj.StringifyAsync(depth, builder, pretty))
							{
								yield return e;
							}
							builder.Append(",");
							if (pretty)
							{
								builder.Append("\n");
							}
						}
					}
					if (pretty)
					{
						builder.Length -= 2;
					}
					else
					{
						builder.Length--;
					}
				}
				if (pretty && list.Count > 0) 
				{
					builder.Append("\n");
					for (int j = 0; j < depth - 1; j++)
					{
						builder.Append("\t");
					}
				}
				builder.Append("}");
				break;
			case JSONType.ARRAY:
				builder.Append("[");
				if (list.Count > 0) 
				{
					if (pretty)
					{
						builder.Append("\n");
					}
					for (int i = 0; i < list.Count; i++) 
					{
						if (list[i]) 
						{
							if (pretty)
							{
								for(int j = 0; j < depth; j++)
								{
									builder.Append("\t");
								}
							}
							foreach (IEnumerable e in list[i].StringifyAsync(depth, builder, pretty))
							{
								yield return e;
							}
							builder.Append(",");
							if (pretty)
							{
								builder.Append("\n");
							}
						}
					}
					if (pretty)
					{
						builder.Length -= 2;
					}
					else
					{
						builder.Length--;
					}
				}
				if (pretty && list.Count > 0) 
				{
					builder.Append("\n");
					for (int j = 0; j < depth - 1; j++)
					{
						builder.Append("\t");
					}
				}
				builder.Append("]");
				break;
			case JSONType.BOOL:
				if (b)
				{
					builder.Append("true");
				}
				else
				{
					builder.Append("false");
				}
				break;
			case JSONType.NULL:
				builder.Append("null");
				break;
		}
	}

	void Stringify (int depth, StringBuilder builder)
	{ 
		Stringify(depth, builder, false); 
	}
	
	void Stringify (int depth, StringBuilder builder, bool pretty) 
	{
		if (depth++ > MaxDepth) 
		{
			Debug.Log("reached max depth!");
			return;
		}
		switch (type) 
		{
			case JSONType.BAKED:
				builder.Append(str);
				break;
			case JSONType.STRING:
				builder.AppendFormat("\"{0}\"", str);
				break;
			case JSONType.NUMBER:
				if (double.IsInfinity(n))
				{
					builder.Append(JSONInfinity);
				}
				else if (double.IsNegativeInfinity(n))
				{
					builder.Append(JSONNegativeInfinity);
				}
				else if (double.IsNaN(n))
				{
					builder.Append(JSONNotANumber);
				}
				else
				{
					builder.Append(n.ToString());
				}
				break;
			case JSONType.OBJECT:
				builder.Append("{");
				if(list.Count > 0) 
				{
					if (pretty)
					{
						builder.Append("\n");
					}
					for (int i = 0; i < list.Count; i++) 
					{
						string key = keys[i];
						JSONObject obj = list[i];
						if (obj) 
						{
							if (pretty)
							{
								for (int j = 0; j < depth; j++)
								{
									builder.Append("\t"); 
								}
							}
							builder.AppendFormat("\"{0}\":", key);
							obj.Stringify(depth, builder, pretty);
							builder.Append(",");
							if (pretty)
							{
								builder.Append("\n");
							}
						}
					}
					if (pretty)
					{
						builder.Length -= 2;
					}
					else
					{
						builder.Length--;
					}
				}
				if (pretty && list.Count > 0) 
				{
					builder.Append("\n");
					for (int j = 0; j < depth - 1; j++)
					{
						builder.Append("\t");
					}
				}
				builder.Append("}");
				break;
			case JSONType.ARRAY:
				builder.Append("[");
				if (list.Count > 0) 
				{
					if (pretty)
					{
						builder.Append("\n");
					}
					for (int i = 0; i < list.Count; i++) 
					{
						if (list[i]) 
						{
							if (pretty)
							{
								for(int j = 0; j < depth; j++)
								{
									builder.Append("\t");
								}
							}
							list[i].Stringify(depth, builder, pretty);
							builder.Append(",");
							if (pretty)
							{
								builder.Append("\n");
							}
						}
					}
					if (pretty)
					{
						builder.Length -= 2;
					}
					else
					{
						builder.Length--;
					}
				}
				if (pretty && list.Count > 0) 
				{
					builder.Append("\n");
					for (int j = 0; j < depth - 1; j++)
					{
						builder.Append("\t");
					}
				}
				builder.Append("]");
				break;
			case JSONType.BOOL:
				if (b)
				{
					builder.Append("true");
				}
				else
				{
					builder.Append("false");
				}
				break;
			case JSONType.NULL:
				builder.Append("null");
				break;
		}
	}

	public static implicit operator WWWForm (JSONObject obj) 
	{
		WWWForm form = new WWWForm();
		for (int i = 0; i < obj.list.Count; i++) 
		{
			string key = i + "";
			if (obj.type == JSONType.OBJECT)
			{
				key = obj.keys[i];
			}
			string val = obj.list[i].ToString();
			if (obj.list[i].type == JSONType.STRING)
			{
				val = val.Replace("\"", "");
			}
			form.AddField(key, val);
		}
		return form;
	}

	public JSONObject this[int index] 
	{
		get 
		{
			if(list.Count > index) 
			{
				return list[index];
			}
			else 
			{
				return null;
			}
		}
		set 
		{
			if (list.Count > index)
			{
				list[index] = value;
			}
		}
	}

	public JSONObject this[string index] 
	{
		get 
		{
			return GetField(index);
		}
		set 
		{
			SetField(index, value);
		}
	}

	public override string ToString () 
	{
		return Print();
	}

	public string ToString (bool pretty) 
	{
		return Print(pretty);
	}

	public Dictionary<string, string> ToDictionary () 
	{
		if (type == JSONType.OBJECT) 
		{
			Dictionary<string, string> result = new Dictionary<string, string>();
			for (int i = 0; i < list.Count; i++) 
			{
				JSONObject val = list[i];
				switch (val.type) 
				{
					case JSONType.STRING: result.Add(keys[i], val.str);    break;
					case JSONType.NUMBER: result.Add(keys[i], val.n + ""); break;
					case JSONType.BOOL:   result.Add(keys[i], val.b + ""); break;
					default: Debug.LogWarning("Omitting object: " + keys[i] + " in dictionary conversion"); break;
				}
			}
			return result;
		}
		Debug.LogWarning("Tried to turn non-Object JSONObject into a dictionary");
		return null;
	}
	
	public static implicit operator bool (JSONObject o) 
	{
		return o != null;
	}
}