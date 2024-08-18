/*
 * 
 *	ObjectCtrl.cs
 *		オブジェクトの固有動作の管理
 * 
 * 
 * 
 * 
 * 
 *		20221211	WSc101用に再構成
 *		20240715	3Dobjを扱うために大幅書き換え
 *		
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class ObjectCtrl : MonoBehaviour
{
	[Space]
	[SerializeField]
	public int OBJcnt = 0;
	[Space]
	[SerializeField]
	public GameObject MainModel;    // メイン3Dモデル
	[SerializeField]
	public BoxCollider Main3DHit;   // 3Dボックスヒット
	//[SerializeField]
	//GeometryWireFrameCtrl WIREFRAME_CTRL;	// 色変化コントローラー
	[SerializeField]
	public Transform ModelRoll;     // ロール(横回転)
	[SerializeField]
	public Transform ModelPitch;    // ピッチ(上下首振り)
	[SerializeField]
	public Transform ModelYaw;		// ヨー(左右首振り)

	[SerializeField]
	public SpriteRenderer MainPic;      // メイン画像.(プライオリティ前面)
	[SerializeField]
	public Transform MainPos;           // 座標・回転関連.
	[SerializeField]
	public BoxCollider2D MainHit;   // 当たり判定.
	[SerializeField]
	public SpriteRenderer EmotePic;		// エモートアイコン
	[Space]

	public int LIFE = 0;        // 耐久力.
	public bool NOHIT = false;  // 当たり判定の有無.
	[Space]
	public Vector3 target = new Vector3(0, 0, 0);
	public int speed = 0;                       // 移動速度.
	public int angle = 0;                       // 移動角度(360度を256段階で指定).
	public int oldangle = 0;                    // 1int前の角度
	public int type = 0;                        // キャラクタタイプ(同じキャラクタだけど動きが違うなどの振り分け).
	public int mode = 0;                        // 動作モード(キャラクタによって意味が違う).
	public int power = 0;                       // 相手に与えるダメージ量.
	public int count = 0;                       // 動作カウンタ.
	public int[] param = new int[4];            // パラメータ4個
	public Vector3[] parampos = new Vector3[4]; // テンポラリ座標4個
	public Vector3 vect = Vector3.zero;         // 移動量
	public int interval = 0;                    // 0で自爆しない・任意の数値を入れるとカウント到達時に自爆


	public int ship_energy_charge = 0;



	[Space]

	readonly Color COLOR_NORMAL = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	readonly Color COLOR_DAMAGE = new Color(1.0f, 0.0f, 0.0f, 1.0f);
	readonly Color COLOR_ERASE = new Color(0.0f, 0.0f, 0.0f, 0.0f);

	static readonly float[] MOVE_LIMIT_X = new float[] { 227f, 575f, 735f };	// 壁判定・追い出し先
	static readonly float[] MOVE_LIMIT_Y = new float[] { 39f, 127f, 227f };
	static readonly float[] VECT_LIMIT_X = new float[] { 16f, -16f, 16f };	// 壁判定・チェック用ベクトル
	static readonly float[] VECT_LIMIT_Y = new float[] { 16f, 16f, 16f };

	const float SHIP_MOVE_SPEED = 0.10f;
	const float WATER_MOVE_SPEED = 0.54f;

	const float WATER_PERCENTAGE = 0.001f;

	const float OFFSCREEN_MIN_X = -6.00f;
	const float OFFSCREEN_MAX_X = 6.00f;
	const float OFFSCREEN_MIN_Y = -4.00f;
	const float OFFSCREEN_MAX_Y = 4.00f;

	const float HITSIZE_MYSHIP = 0.16f;
	const float HITSIZE_MYSHOT = 0.64f;
	const float HITSIZE_ENEMY = 0.32f;

	public Vector3 myinp;

	public ObjectManager.MODE obj_mode = ObjectManager.MODE.NOUSE;  // キャラクタの管理状態.
	public ObjectManager.TYPE obj_type = ObjectManager.TYPE.NOUSE;  // キャラクタの分類(当たり判定時に必要).

	MainSceneCtrl MAIN;
	ObjectManager MANAGE;

	void Awake()
	{
		MAIN = GameObject.Find("root_main").GetComponent<MainSceneCtrl>();
		MANAGE = GameObject.Find("root_main").GetComponent<ObjectManager>();

		//MainHit.enabled = false;
	}


	// Use this for initialization
	void Start()
	{
		for (int i = 0; i < param.Length; i++)
		{
			param[i] = 0;
			Application.targetFrameRate = 60;
		}
	}
	// Update is called once per frame
	void Update()
	{
		Vector3 pos = Vector3.zero;

		if (obj_mode == ObjectManager.MODE.NOUSE)
		{
			return;
		}
#if false
		if (ModeManager.mode == ModeManager.MODE.GAME_PAUSE)
		{
			return;
		}
#endif

		switch (obj_mode)
		{
			case ObjectManager.MODE.NOUSE:
				return;
			case ObjectManager.MODE.INIT:
				//MainPic.enabled = true;
				count = 0;
				break;
			case ObjectManager.MODE.HIT:
				//MainHit.enabled = true;
				break;
			case ObjectManager.MODE.NOHIT:
				//MainHit.enabled = false;
				break;
			case ObjectManager.MODE.FINISH:
				MANAGE.Return(this);
				break;
		}
		switch (obj_type)
		{
			// 自機
			case ObjectManager.TYPE.MYSHIP:
				if (mode > 0)
				{
					mode--;
					return;
				}
				else if (mode == 0)
				{
					count = -1;
					mode = -1;
				}

				switch (count)	// 自機の状態に応じて移動・攻撃を行う
				{
					case 0: // 初期化
						{
							obj_mode = ObjectManager.MODE.HIT;
							obj_type = ObjectManager.TYPE.MYSHIP;
							//MainHit.enabled = true;
							//MainHit.size = new Vector2(32, 32);
							NOHIT = false;

							Main3DHit.enabled = true;
							Main3DHit.size = new Vector3(0.1f, 0.1f, 0.1f);
							MainModel = Instantiate(MANAGE.OBJ_MYSHIP);
							MainModel.transform.SetParent(ModelRoll.transform, false);
							MainModel.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
							MainModel.transform.localRotation = Quaternion.identity;
							MainModel.transform.localEulerAngles = new Vector3(180, 0, -90);

							//MainPic.color = COLOR_NORMAL;
							//MainPic.sortingOrder = 0;
							//MainPic.sprite = MANAGE.SPR_GIRL[0];
							param[0] = type;
							param[1] = 0;
							param[2] = 0;
							param[3] = 0;
							power = 1;
							LIFE = 1;
							this.transform.localPosition = new Vector3(0, 0, 0);
							this.transform.localEulerAngles = new Vector3(0, 0, MANAGE.AngleToRotation(angle));
							vect = MANAGE.AngleToVector3(angle, speed);
						}
						break;
					default: // 通常
						this.transform.localEulerAngles = new Vector3(0, 0, MANAGE.AngleToRotation(angle));
						this.transform.localPosition += (vect * 0.05f);
						Vector3 dist=this.transform.localPosition;	// Vector3(0,0,0)からの距離ざっくり
						float xx = dist.x * dist.x;
						float yy = dist.y * dist.y;
						double dist2 = Mathf.Sqrt(xx + yy);
						if (dist2 > 4.00d)		// 距離リミット
						{
							if (param[0] > 0)	// 何らかのスイッチが入っていた場合は正反対に戻ってくる
							{
								angle = (angle + 0x80) % 256;	// 方向反転
								vect = MANAGE.AngleToVector3(angle, speed);
							}
							else
							{
								GameObject.Destroy(MainModel);
								MANAGE.Return(this);
							}
						}
						break;
				}

				break;




			/*
				敵弾



			*/
			case ObjectManager.TYPE.BULLET:
				if (mode > 0)
				{
					mode--;
					return;
				}
				else if (mode == 0)
				{
					count = -1;
					mode = -1;
				}

				switch (count)  // 自機の状態に応じて移動・攻撃を行う
				{
					case 0: // 初期化
						{
							obj_mode = ObjectManager.MODE.HIT;
							obj_type = ObjectManager.TYPE.MYSHIP;
							//MainHit.enabled = true;
							//MainHit.size = new Vector2(32, 32);
							NOHIT = false;

							Main3DHit.enabled = true;
							Main3DHit.size = new Vector3(0.1f, 0.1f, 0.1f);
							MainModel = Instantiate(MANAGE.OBJ_BULLET);
							MainModel.transform.SetParent(ModelRoll.transform, false);
							MainModel.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
							MainModel.transform.localRotation = Quaternion.identity;
							MainModel.transform.localEulerAngles = new Vector3(180, 0, -90);

							//MainPic.color = COLOR_NORMAL;
							//MainPic.sortingOrder = 0;
							//MainPic.sprite = MANAGE.SPR_GIRL[0];
							param[0] = type;
							param[1] = 0;
							param[2] = 0;
							param[3] = 0;
							power = 1;
							LIFE = 1;
							this.transform.localPosition = new Vector3(0, 0, 0);
							this.transform.localEulerAngles = new Vector3(0, 0, MANAGE.AngleToRotation(angle));
							vect = MANAGE.AngleToVector3(angle, speed);
						}
						break;
					default: // 通常
						this.transform.localEulerAngles = new Vector3(0, 0, MANAGE.AngleToRotation(angle));
						this.transform.localPosition += (vect * 0.05f);
						Vector3 dist = this.transform.localPosition;    // Vector3(0,0,0)からの距離ざっくり
						float xx = dist.x * dist.x;
						float yy = dist.y * dist.y;
						double dist2 = Mathf.Sqrt(xx + yy);
						if (dist2 > 4.00d)      // 距離リミット
						{
							if (param[0] > 0)   // 何らかのスイッチが入っていた場合は正反対に戻ってくる
							{
								angle = (angle + 0x80) % 256;   // 方向反転
								vect = MANAGE.AngleToVector3(angle, speed);
							}
							else
							{
								GameObject.Destroy(MainModel);
								MANAGE.Return(this);
							}
						}
						break;
				}

				break;



#if false

			/*
			壁



			*/
			case ObjectManager.TYPE.WALL:
				if (count == 0)
				{
					switch (mode)
					{
						case 0:
							obj_mode = ObjectManager.MODE.HIT;
							MainPic.sprite = MANAGE.SPR_WALL[0];
							MainHit.enabled = true;
							MainHit.size = new Vector2(16, 16);
							NOHIT = false;
							break;
						case 1:
							obj_mode = ObjectManager.MODE.NOHIT;
							MainPic.sprite = MANAGE.SPR_WALL[1];
							MainHit.enabled = false;
							NOHIT = true;
							break;
					}
					MainPic.color = COLOR_NORMAL;
					MainPic.sortingOrder = -10;
					power = 1;
					LIFE = 1;
				}
				break;




			/*

			 敵(モブ男・魔法使い)






			 */

			case ObjectManager.TYPE.ENEMY:
				if (count == 0)
				{
					obj_mode = ObjectManager.MODE.HIT;
					MainHit.enabled = true;
					MainHit.size = new Vector2(32, 32);
					NOHIT = false;
					mode = 0;       // 移動カテゴリ
					//type = 0;		// 自分のタイプ(Inspectorから指定・普遍)
					EmotePic.sprite = null;
					EmotePic.enabled = false;
					MainPic.color = COLOR_NORMAL;
					MainPic.sortingOrder = 0;
					MainPic.sprite = MANAGE.SPR_ENEMY[(type * 12)];
					angle = 0;
					param[0] = 0;   // 衝突があった場合1以上
					param[1] = 0;   // ターゲット到着後待ち時間
					param[2] = 0;   // エモートアイコンの種別
					param[3] = 0;   // エモートアイコン表示カウンタ
					parampos[0] = new Vector3(0, 0, 0); // 1フレーム前の座標
					parampos[1] = new Vector3(0, 0, 0); // 移動用ベクトル
					parampos[2] = new Vector3(0, 0, 0); // 目標座標
					power = 1;
					LIFE = 1;
					switch(type)
					{
						case 0:
							this.transform.localPosition = new Vector3(-150, 150, 0);
							break;
						case 1:
							this.transform.localPosition = new Vector3(150, 150, 0);
							break;
						case 2:
							this.transform.localPosition = new Vector3(-150, -150, 0);
							break;
						case 3:
							this.transform.localPosition = new Vector3(150, -150, 0); 
							break;
					}
				}
				else
				{
					parampos[0] = this.transform.localPosition;
					switch (mode)
					{
						case 0: // 移動開始待ち
							switch (type)
							{
								case 0:	// モブ男0
									{
										if (count == 1)
										{
											if (Random.Range(0, 1000) < 200)
											{
												param[2] = 2;   // エモートアイコン・汗
												param[3] = 0;
												parampos[1] = new Vector3(Random.Range(-220, 220), Random.Range(-220, 220), parampos[0].z);
												int ang = (MANAGE.RotationToAngle(MANAGE.RadToRotation(MANAGE.GetRad(this.transform.localPosition, parampos[1]))) + 64) % 256;
												//MANAGE.Set(ObjectManager.TYPE.BED, 0, 0, parampos[1], 0, 0);
												parampos[2] = MANAGE.SetVector(this.transform.localPosition, parampos[1], (float)speed / 1.0f, Random.Range(-5, 5), false);
											}
											else
											{
												param[2] = 3;   // エモートアイコン・よだれ
												param[3] = 0;
												parampos[1] = MANAGE.GIRL_POS;
												int ang = (MANAGE.RotationToAngle(MANAGE.RadToRotation(MANAGE.GetRad(this.transform.localPosition, parampos[1]))) + 64) % 256;
												parampos[2] = MANAGE.SetVector(this.transform.localPosition, parampos[1], speed<<1, 0, false);
											}
										}
									}
									break;
								case 1:	// モブ男1
									{
										if (count == 1)
										{
											if (Random.Range(0, 1000) < 200)
											{
												param[2] = 2;   // エモートアイコン・汗
												param[3] = 0;
												parampos[1] = new Vector3(Random.Range(50, 220), Random.Range(50, 220), parampos[0].z);
												int ang = (MANAGE.RotationToAngle(MANAGE.RadToRotation(MANAGE.GetRad(this.transform.localPosition, parampos[1]))) + 64) % 256;
												//MANAGE.Set(ObjectManager.TYPE.BED, 0, 0, parampos[1], 0, 0);
												parampos[2] = MANAGE.SetVector(this.transform.localPosition, parampos[1], (float)speed / 2.0f, Random.Range(-5, 5), false);
											}
											else
											{
												param[2] = 3;   // エモートアイコン・よだれ
												param[3] = 0;
												parampos[1] = MANAGE.GIRL_POS;
												int ang = (MANAGE.RotationToAngle(MANAGE.RadToRotation(MANAGE.GetRad(this.transform.localPosition, parampos[1]))) + 64) % 256;
												parampos[2] = MANAGE.SetVector(this.transform.localPosition, parampos[1], speed, 0, false);
											}
										}
									}
									break;
								case 2:	// モブ男2
									{
										if (count == 1)
										{
											if (Random.Range(0, 1000) < 200)
											{
												param[2] = 2;   // エモートアイコン・汗
												param[3] = 0;
												parampos[1] = new Vector3(Random.Range(-220, -50), Random.Range(-220, -50), parampos[0].z);
												int ang = (MANAGE.RotationToAngle(MANAGE.RadToRotation(MANAGE.GetRad(this.transform.localPosition, parampos[1]))) + 64) % 256;
												//MANAGE.Set(ObjectManager.TYPE.BED, 0, 0, parampos[1], 0, 0);
												parampos[2] = MANAGE.SetVector(this.transform.localPosition, parampos[1], (float)speed / 1.0f, Random.Range(-5, 5), false);
											}
											else
											{
												param[2] = 3;   // エモートアイコン・よだれ
												param[3] = 0;
												parampos[1] = MANAGE.GIRL_POS;
												int ang = (MANAGE.RotationToAngle(MANAGE.RadToRotation(MANAGE.GetRad(this.transform.localPosition, parampos[1]))) + 64) % 256;
												parampos[2] = MANAGE.SetVector(this.transform.localPosition, parampos[1], speed<<2, 0, false);
											}
										}
									}
									break;
								case 3:
									{
										if (count == 1)
										{
											if (Random.Range(0, 1000) < 900)
											{
												param[2] = 2;   // エモートアイコン・汗
												param[3] = 0;
												parampos[1] = new Vector3(Random.Range(50, 220), Random.Range(-220, -50), parampos[0].z);
												int ang = (MANAGE.RotationToAngle(MANAGE.RadToRotation(MANAGE.GetRad(this.transform.localPosition, parampos[1]))) + 64) % 256;
												//MANAGE.Set(ObjectManager.TYPE.BED, 0, 0, parampos[1], 0, 0);
												parampos[2] = MANAGE.SetVector(this.transform.localPosition, parampos[1], (float)speed / 1.5f, Random.Range(-5, 5), false);
											}
											else
											{
												param[2] = 3;   // エモートアイコン・よだれ
												param[3] = 0;
												parampos[1] = MANAGE.GIRL_POS;
												int ang = (MANAGE.RotationToAngle(MANAGE.RadToRotation(MANAGE.GetRad(this.transform.localPosition, parampos[1]))) + 64) % 256;
												parampos[2] = MANAGE.SetVector(this.transform.localPosition, parampos[1], speed<<1, 0, false);
											}
										}
									}
									break;
							}
							param[1]++;
							if (param[1] > Random.Range(180, 300))
							{
								mode = 1;
							}
							break;
						case 1: // 目標に向けて移動・途中で疲れるまで
							{
								this.transform.localPosition += parampos[2];
								if (count > Random.Range(300,400))
								{
									count = 0;
									mode = 0;
								}
							}
							break;
						case 2: // 壁以外の目標に衝突
							break;
						default:
							break;
					}
				}
				
				if (param[3] >= 0)
				{
					EmotePic.enabled = true;
					EmotePic.sprite = MANAGE.SPR_EMOTE_ICON[(param[2] *3)+ (count >> 3) % 3];
					EmotePic.sortingOrder = MainPic.sortingOrder + 1;
					switch (param[3])
					{
						case 0:
						case 1:
						case 2:
						case 3:
						case 4:
						case 5:
						case 6:
						case 7:
						case 8:
						case 9:
						case 10:
						case 11:
						case 12:
						case 13:
						case 14:
						case 15:
						case 16:
							EmotePic.transform.localPosition = new Vector3(0, param[3] * 3, 0);
							break;
						case 150:
							EmotePic.enabled = false;   // エモートアイコン表示消去
							param[3] = -2;
							break;
						default:
							EmotePic.transform.localPosition = new Vector3(0, 48, 0);
							break;
					}
					param[3]++;
				}
				else
				{
					EmotePic.enabled = false;
				}


				switch (count)  // 自機の状態に応じて移動・攻撃を行う
				{
					case 0: // 初期化
						{
							obj_mode = ObjectManager.MODE.HIT;
							MainHit.enabled = true;
							MainHit.size = new Vector2(32, 32);
							NOHIT = false;
							mode = 1;		// 移動カテゴリ
							EmotePic.sprite=null;
							EmotePic.enabled = false;
							MainPic.color = COLOR_NORMAL;
							MainPic.sortingOrder = 0;
							MainPic.sprite = MANAGE.SPR_ENEMY[(mode * 12)];
							angle = 0;
							param[0] = 0;	// 衝突があった場合1以上
							param[1] = 0;	// ターゲット到着後待ち時間
							param[2] = 0;   // エモートアイコンの種別
							param[3] = 0;   // エモートアイコン表示カウンタ
							parampos[0] = new Vector3(0, 0, 3);	// 1フレーム前の座標
							parampos[1] = new Vector3(0, 0, 3); // 移動用ベクトル
							parampos[2] = new Vector3(0, 0, 0);	// 目標座標
							power = 1;
							LIFE = 1;
						}
						break;
					default: // 通常
						{
						switch(mode)	// 移動モード分岐
						{
								case 0:	// ターゲットに向かって移動中
								{
										if (param[0] == 0)
										{
											param[0]++;
											parampos[1] = new Vector3(Random.Range(-220, 220),  Random.Range(-220, 220), parampos[0].z);
											int ang = (MANAGE.RotationToAngle(MANAGE.RadToRotation(MANAGE.GetRad(this.transform.localPosition,parampos[1]))) + 64) % 256;
											//Debug.Log("ang=" + ang);
											if (count == 1)
											{
												MANAGE.Set(ObjectManager.TYPE.BED, 0, parampos[1], 0, 0);
											}
											parampos[2] = MANAGE.SetVector(this.transform.localPosition,parampos[1], speed, Random.Range(-5,5), false);
											
										}
										else if (count>Random.Range(120,180))
										{
											// 追いかけ疲れ・待ち時間
											parampos[2] = MANAGE.SetVector(this.transform.localPosition, parampos[1], speed, Random.Range(3,-3), false);
											count = 10;
										}
										//else
										{
											this.transform.localPosition += (parampos[2] / 4);
										}
										//param[1]++;
										//if (param[1] > 60)
										{
											param[1] = 0;
											parampos[2] = MANAGE.SetVector(this.transform.localPosition,parampos[1], speed, 0, false);
										}
								}
									break;
								case 1: // ターゲット到着・待ち時間
									param[2]++;
									if (param[2] > Random.Range(120, 180))
									{
										param[0] = 0;
										param[2] = 0;
										mode = 0;
										count = -1;
									}
									break;
								case 2: // ガール追い掛け
									if (count==0)
									{
										
									}
									break;
								case 3: // ガール捕まえた・待ち時間
									break;
						}
							if (param[1] > 0)
							{
								// ふとんに当たった時
								parampos[2] = new Vector3(0, 0, 0);
								mode = 0;
								//Debug.Log("ふとん衝突；localPosition=" + this.transform.localPosition + " / parampos[0]=" + parampos[0]);

							}
							parampos[0] = this.transform.localPosition;
							this.transform.localPosition += parampos[2];
						}
						break;
				}

				// 姿勢アニメ
				if (parampos[2].y <= 0.0f)
				{
					MainPic.sprite = MANAGE.SPR_ENEMY[0 + ((type * 12) + (Mathf.Abs(count) >> 3) % 3)];
				}
				else
				{
					MainPic.sprite = MANAGE.SPR_ENEMY[9 + ((type * 12) + (Mathf.Abs(count) >> 3) % 3)];
				}
				if (parampos[2].x > 0.4f)
				{
					MainPic.sprite = MANAGE.SPR_ENEMY[6 + ((type * 12) + (Mathf.Abs(count) >> 3) % 3)];
				}
				else if (vect.x < -0.4f)
				{
					MainPic.sprite = MANAGE.SPR_ENEMY[3 + ((type * 12) + (Mathf.Abs(count) >> 3) % 3)];
				}
				break;
#endif







			/******************************************************
			 * 
			 * 
			 * 
			 * 
			 * ここからエフェクトなど
			 * 
			 * 
			 *
			 ******************************************************
			 */
			case ObjectManager.TYPE.NOHIT_EFFECT:
				if (count == 0)
				{
					obj_mode = ObjectManager.MODE.NOHIT;
					MainHit.enabled = false;
					MainPic.enabled = true;
					LIFE = 1;
					vect = MANAGE.AngleToVector3(angle, speed * 0.05f);
					MainPic.sprite = MANAGE.SPR_CRUSH[0];
					MainPic.sortingOrder = 5;
				}
				else if (count >= 16)
				{
					MANAGE.Return(this);
				}
				else
				{
					this.transform.localPosition += vect;
					this.transform.localScale = this.transform.localScale * 1.1f;
					MainPic.sprite = MANAGE.SPR_CRUSH[count >> 1];
				}
				break;
		}

		// 自前衝突判定を使う場合
		//MANAGE.CheckHit(this);

		if (LIFE <= 0)	// 死亡確認
		{
			Dead();
		}

		count++;
	}


#if false

	/// <summary>
	/// 当たり判定部・スプライト同士が衝突した時に走る
	/// </summary>
	/// <param name="collider">衝突したスプライト当たり情報</param>
	void OnTriggerEnter(Collider collider)
	{
		if (obj_mode == ObjectManager.MODE.NOHIT)
		{
			return;
		}
		ObjectCtrl other = collider.gameObject.GetComponent<ObjectCtrl>();
		if (other == null)
		{
			return;
		}
		if (other.obj_mode == ObjectManager.MODE.NOHIT)
		{
			return;
		}
		if (NOHIT == true)
		{
			return;
		}
		if (other.NOHIT == true)
		{
			return;
		}
		switch (other.obj_type)
		{
			case ObjectManager.TYPE.BULLET:
				{
					if (obj_type == ObjectManager.TYPE.MYSHIP)
					{
						other.mode = 2;
					}
				}
				break;
		}

		{
			case ObjectManager.TYPE.WALL:
				{
					if (obj_type == ObjectManager.TYPE.MYSHIP)
					{
						if (other.obj_mode == ObjectManager.MODE.HIT)
						{
							param[0] = 1;                               // 衝突があったスイッチ
							Vector3 newpos = new Vector3(0, 0, 0);      // 最終的な移動先
							if (other.parampos[0].x != 0.0f)        // 壁マップチップに移動先を問い合わせる		
							{
								newpos.x = other.parampos[0].x;
							}
							if (other.parampos[0].y != 0.0f)
							{
								newpos.y = other.parampos[0].y;
							}
							if ((newpos.x != 0.0f) && (newpos.y != 0.0f))   // 両方に値が入った場合
							{
								float xx = Mathf.Abs(parampos[0].x - newpos.x); // 横座標優先で移動先を決める計算
								float yy = Mathf.Abs(parampos[0].y - newpos.y);
								if (xx < yy)
								{
									newpos.y = parampos[0].y;
								}
								else
								{
									newpos.x = parampos[0].x;
								}
							}
							if (newpos.x == 0.0f)               // 1フレーム前の座標を書き戻す
							{
								newpos.x = parampos[0].x;
							}
							if (newpos.y == 0.0f)
							{
								newpos.y = parampos[0].y;
							}

							parampos[1] = newpos;   // 新しい(補正された)座標
							this.transform.localPosition = newpos;
						}
					}

					else if (obj_type == ObjectManager.TYPE.ENEMY)
					{
						if (other.obj_mode == ObjectManager.MODE.HIT)
						{
							Vector3 newpos = new Vector3(0, 0, 0);      // 最終的な移動先
							if (other.parampos[0].x != 0.0f)        // 壁マップチップに移動先を問い合わせる		
							{
								newpos.x = other.parampos[0].x;
							}
							if (other.parampos[0].y != 0.0f)
							{
								newpos.y = other.parampos[0].y;
							}
							if ((newpos.x != 0.0f) && (newpos.y != 0.0f))   // 両方に値が入った場合
							{
								float xx = Mathf.Abs(parampos[0].x - newpos.x); // 横座標優先で移動先を決める計算
								float yy = Mathf.Abs(parampos[0].y - newpos.y);
								if (xx < yy)
								{
									newpos.y = parampos[0].y;
								}
								else
								{
									newpos.x = parampos[0].x;
								}
							}
							if (newpos.x == 0.0f)               // 1フレーム前の座標を書き戻す
							{
								newpos.x = parampos[0].x;
							}
							if (newpos.y == 0.0f)
							{
								newpos.y = parampos[0].y;
							}
							parampos[1] = newpos;   // 新しい(補正された)座標
							this.transform.localPosition = newpos;
						}
					}

				}
		break;
			case ObjectManager.TYPE.ENEMY:
				if (obj_type == ObjectManager.TYPE.FLOOR)
				{
					if (other.parampos[0].z == this.parampos[0].z)
					{
						// 待ち時間設定・次の目的地設定
						other.mode = 2;
						other.param[2] = 1;
						//other.parampos[2] = new Vector3(Random.Range(20, 200), 0 - Random.Range(20, 200), 0);
						//other.param[1] = Random.Range(120, 300);
						MANAGE.Return(this);    // 古いふとん消滅
					}
				}
				else if (obj_type == ObjectManager.TYPE.MYSHIP)
				{
					// 襲い設定・画面切り替え・AVGパートに移行
//#if false
					MAIN.sw_novel_mode = true;
					MAIN.cnt_character = other.type;
//#endif
				}
				else if (obj_type == ObjectManager.TYPE.WALL)
				{
					// 壁に当たった時・めり込まないように座標を戻してあげる
					other.transform.localPosition = other.parampos[0];
				}
				break;
		}
	}
#endif

#if false
	/// <summary>
	///		接触から離れた場合・最終的にはフロアを全部塗ればクリア
	/// </summary>
	/// <param name="collider"></param>
	private void OnTriggerExit(Collider collider)
	{
		GameObject other2 = null;
		ObjectCtrl other = collider.gameObject.GetComponent<ObjectCtrl>();
		if (other == null)
		{
			other2 = collider.gameObject.GetComponent<GameObject>();
		}
		if (other2 == null)
		{
			return;
		}
        switch (other2.gameObject.name)
		{
			case "limit_circle":
				{
					if (obj_type == ObjectManager.TYPE.MYSHIP)
					{
						if (param[0] == 1)
						{
							angle = (angle + 0x80) % 256;
						}
						else
						{
							MANAGE.Return(this);
						}
					}
				}
				break;
			default:
				break;
		}
	}
#endif


#if false
	/// <summary>
	/// 当たり判定部・スプライト同士が衝突した時に走る
	/// </summary>
	/// <param name="collider">衝突したスプライト当たり情報</param>
	void OnTriggerEnter2D(Collider2D collider)
	{
		if (obj_mode == ObjectManager.MODE.NOHIT)
		{
			return;
		}
		ObjectCtrl other = collider.gameObject.GetComponent<ObjectCtrl>();
		if (other.obj_mode == ObjectManager.MODE.NOHIT)
		{
			return;
		}
		if (NOHIT == true)
		{
			return;
		}
		if (other.NOHIT == true)
		{
			return;
		}
		switch (other.obj_type)
		{
			case ObjectManager.TYPE.WALL:
				{
					if (obj_type == ObjectManager.TYPE.MYSHIP)
					{
						if (other.obj_mode == ObjectManager.MODE.HIT)
						{
							this.param[0] = 1;
							Vector3 newpos = this.transform.localPosition;
							if (
									(this.transform.localPosition.x < this.parampos[0].x)
								|| (this.transform.localPosition.x > this.parampos[0].x)
								)
							{
								this.vect.x = 0.0f;
								newpos.x = this.parampos[0].x;
								newpos.y = this.transform.localPosition.y;
							}
							if (
									(this.transform.localPosition.y < this.parampos[0].y)
								|| (this.transform.localPosition.y > this.parampos[0].y)
								)
							{
								this.vect.y = 0.0f;
								newpos.x = this.transform.localPosition.x;
								newpos.y = this.parampos[0].y;
							}
							this.transform.localPosition = newpos;
						}
						Debug.Log("wall check:oldpos=" + parampos[0] + " / newpos=" + transform.localPosition + " / vect=" + other.vect);
					}
				}
				break;
		}
	}
#endif

	/// <summary>
	/// ダメージ与える
	/// </summary>
	/// <param name="damage">ダメージ量</param>
	public void Damage(int damage)
	{
		LIFE -= damage;
			if (LIFE <= 0)
			{
				Dead();	// リプレイある時はダメージ関数で死亡処理を行わない
			}
	}

	/// <summary>
	///		死んだ時の処理全般
	/// </summary>
	public void Dead()
	{
		obj_mode = ObjectManager.MODE.NOHIT;
		switch (obj_type)
		{
			default:
				break;
		}
		//MainPic.color = COLOR_NORMAL;
		count = 0;
	}


	public void DisplayOff()
	{
		//MainModel.gameObject.SetActive(false);
		//Main3DHit.gameObject.SetActive(false);
		//MainPic.enabled = false;
		//MainHit.enabled = false;
		//MainPic.color = COLOR_NORMAL;
		//MainPic.sprite = null;
	}


}
