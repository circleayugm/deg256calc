#define DEBUG


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class MainSceneCtrl : MonoBehaviour
{
	[SerializeField]
	public RectTransform ROOT_CANVAS;
	[SerializeField]
	public InputField INP_OUTPUT;
	[SerializeField]
	public InputField INP_OBJECT;
	[SerializeField]
	public InputField INP_ANGLE;
	[SerializeField]
	public InputField INP_SPEED;
	[SerializeField]
	public InputField INP_DELAY;
	[SerializeField]
	public InputField INP_CHARACTER;
	[SerializeField]
	public InputField INP_FLIP;
	[Space]
	[SerializeField]
	ObjectManager MANAGE;


	List<ObjectData> OBJECT_LIST = new List<ObjectData>();
	public int count = 0;
	bool sw_move = false;

	// Start is called before the first frame update
	void Start()
	{
		Application.targetFrameRate = 60;
		MANAGE = GameObject.Find("root_main").GetComponent<ObjectManager>();
		OBJECT_LIST.Clear();
#if true

		if (MANAGE != null)
		{
			while (MANAGE.SW_BOOT != true)
			{
				// 起動待ち時間
				Debug.Log("ObjectManager起動待ち:" + count);
				count++;
			}
		}
#endif
		count = 0;
		//Application.runInBackground = false;

	}

	// Update is called once per frame
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}


#if DEBUG
		if (Input.GetKeyDown(KeyCode.Space) == true)
		{
			MANAGE.Set(ObjectManager.TYPE.MYSHIP, 0, 0, Vector3.zero, 64, 1);
		}
#endif


		count++;
	}




	// ボタン類
	public void Press256UpButton()
	{
		INP_ANGLE.text = "" + ((int.Parse(INP_ANGLE.text) + 1) % 256);
	}
	public void Press256DownButton()
	{
		INP_ANGLE.text = "" + ((int.Parse(INP_ANGLE.text) - 1) % 256);
		if (int.Parse(INP_ANGLE.text) == -1)
		{
			INP_ANGLE.text = "" + 255;
		}
	}

	public void PressMoveStartButton()
	{
		sw_move = true;
		for (int i = 0; i < OBJECT_LIST.Count; i++)
		{
			switch (OBJECT_LIST[i].style)
			{
				case 0:
					MANAGE.Set(ObjectManager.TYPE.MYSHIP, OBJECT_LIST[i].flip, OBJECT_LIST[i].delay, new Vector3(0, 0, 0), OBJECT_LIST[i].angle, OBJECT_LIST[i].speed);
					break;
				case 1:
					MANAGE.Set(ObjectManager.TYPE.BULLET, OBJECT_LIST[i].flip, OBJECT_LIST[i].delay, new Vector3(0, 0, 0), OBJECT_LIST[i].angle, OBJECT_LIST[i].speed);
					break;
			}
		}

		string st = "\r★キャラクタを動かしています。\r";
		INP_OUTPUT.text += st;
	}
	public void PressMoveEndButton()
	{
		sw_move = false;
		string st = "\r★キャラクタを停止しました。\r";
		INP_OUTPUT.text += st;

	}

	public void PressObjectCallButton()
	{
		string st = "";
		if (OBJECT_LIST.Count < int.Parse(INP_OBJECT.text))
		{
			st = "\rパターン" + INP_OBJECT.text + "は存在しません。\r";
			INP_OUTPUT.text += st;
			return;
		}
		int pt = int.Parse(INP_OBJECT.text);
		INP_ANGLE.text = "" + OBJECT_LIST[pt].angle;
		INP_SPEED.text = "" + OBJECT_LIST[pt].speed;
		INP_DELAY.text = "" + OBJECT_LIST[pt].delay;
		INP_CHARACTER.text = "" + OBJECT_LIST[pt].style;
		INP_FLIP.text = "" + OBJECT_LIST[pt].flip;
		st = "\rパターン" + pt + "を呼び出しました。\r";
		INP_OUTPUT.text += st;
	}
	public void PressObjectSetButton()
	{
		ObjectData obj = new ObjectData();
		int num=OBJECT_LIST.Count;
		if (OBJECT_LIST.Count > int.Parse(INP_OBJECT.text))
		{
			num = int.Parse(INP_OBJECT.text);
			if (num < 0)
			{
				num = 0;
			}
		}
		obj=obj.Set(num,
			int.Parse(INP_ANGLE.text),
			int.Parse(INP_SPEED.text),
			int.Parse(INP_DELAY.text),
			int.Parse(INP_CHARACTER.text),
			int.Parse(INP_FLIP.text));
		string st = "\rパターン" + num + "を追加/変更しました。\r";
		INP_OUTPUT.text += st;
		if (OBJECT_LIST.Count > int.Parse(INP_OBJECT.text))
		{
			OBJECT_LIST[int.Parse(INP_OBJECT.text)].obj = obj.obj;
			OBJECT_LIST[int.Parse(INP_OBJECT.text)].angle = obj.angle % 256;
			OBJECT_LIST[int.Parse(INP_OBJECT.text)].speed = obj.speed;
			OBJECT_LIST[int.Parse(INP_OBJECT.text)].delay = obj.delay;
			OBJECT_LIST[int.Parse(INP_OBJECT.text)].style = obj.style;
			OBJECT_LIST[int.Parse(INP_OBJECT.text)].flip = obj.flip;
					}
		else
		{
			OBJECT_LIST.Add(obj);
		}
		INP_OBJECT.text = "" + num;
		INP_ANGLE.text = "" + obj.angle;
		INP_SPEED.text = "" + obj.speed;
		INP_DELAY.text = "" + obj.speed;
		INP_CHARACTER.text = "" + obj.style;
		INP_FLIP.text = "" + obj.flip;
	}
	public void PressResetButton()
	{
		MANAGE.ResetAll();
		OBJECT_LIST.Clear();
		INP_OBJECT.text = "" + 0;
		INP_ANGLE.text = "" + 0;
		INP_SPEED.text = "" + 1;
		INP_DELAY.text = "" + 0;
		INP_CHARACTER.text = "" + 0;
		INP_FLIP.text = "" + 0;
		INP_OUTPUT.text = "パターン含め初期化完了。";
	}
}
