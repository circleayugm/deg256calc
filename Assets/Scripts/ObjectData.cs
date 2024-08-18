using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectData
{
	public int obj { get; set; }
	public int angle { get; set; }
	public int speed { get; set; }
	public int delay { get; set; }
	public int style { get; set; }
	public int flip { get; set; }

	public ObjectData()
	{
		obj = -1;
		angle = 0;
		speed = 1;
		delay = 0;
		style = 0;
		flip = 0;
	}

	public ObjectData Set(int o,int a,int s,int d,int st,int f)
	{
		ObjectData ret = new ObjectData();
		ret.obj = o;
		ret.angle = a;
		ret.speed = s;
		ret.delay = d;
		ret.style = st;
		ret.flip = f;
		return ret;
	}
}
