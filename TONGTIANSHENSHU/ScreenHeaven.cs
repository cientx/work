using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ScreenHeaven : ScreenBase
{
	#region Constant
	#endregion Constant

	#region protected variables
	//action memo 관련
	/// <summary>
	///  현재 진행되고 있는 행사의 형태이다.
	///  
	/// </summary>
	protected TActionType m_curActType = TActionType.AT_SIMPLERITE;
    public TActionType CurrentActionType
    {
        get
        {
            return m_curActType;
        }

        set
        {
            m_curActType = value;
        }
    }

    protected bool m_bRite = false;
    public bool IsInRite {
        get {
            return m_bRite;
        }
        set
        {
            m_bRite = value;
        }
    }
    protected int m_nCurPrayIndex = -1;
    public int CurPrayIndex
    {
        get
        {
            return m_nCurPrayIndex;
        }
        set
        {
            m_nCurPrayIndex = value;
        }
    }
    protected byte m_nCurPrayPos = 0;
    public byte CurPrayActPos
    {
        get
        {
            return m_nCurPrayPos;
        }
        set
        {
            m_nCurPrayPos = value;
        }
    }
    #endregion

    public static new ScreenHeaven Singleton
    {
        get
        {
            return (ScreenBase.Singleton) as ScreenHeaven;
        }
    }
    public GameObject returnDialog;

    protected NetError m_ResultEnterRoom = NetError.Success;
    protected bool m_showAllPlayers = true;
    protected bool m_showOtherPlayers = true;
    private bool m_bShowMainCtrl = true;
    private uint m_gotoWhere = 0;
    private string m_actRegionName;
	private bool m_isMapCameraTour = false;

	protected ScnTouXiangResultMgr m_touxiangResultMgr = new ScnTouXiangResultMgr();

	protected GameObject m_HuaRain = null;

	protected ChatBalloon m_chatBalloon = null;

    #region virtual methods
    /// <summary>
    /// 
    /// </summary>
    /// <param name="_waitActPos"></param>
    /// <param name="_newActPos"></param>
    /// <param name="host_msg"></param>
    /// <param name="guest_msg"></param>
    /// <returns>   
    /// true : _newActPos 에서 행사를 할수 없습니다. _waitActPos에서 대기중인 행사를 취소할수 없습니다. 
    /// false : _newActPos에서 행사를 시작 할수 있습니다. _waitActPos에서 대기중인 행사를 취소할수 있습니다.
    /// </returns>
    public virtual bool getWaitingStateToCancel(ref byte _waitActPos, ref byte _newActPos, ref string host_msg, ref string guest_msg)
    {
        return false;
    }
    #endregion
    // Use this for initialization
    protected override void InitData()
    {
        m_touxiangResultMgr.onEnterMap();
        ScnEnvAnimalMgr.Singleton.onEnterMap();

        IMEManager.Singleton.ActivateIME(true);

		ScnLanternMgr.Singleton.onEnterMap();
		m_chatBalloon = new ChatBalloon();
        base.InitData();
    }
    protected override void DeInitData()
    {
        m_touxiangResultMgr.onExitMap();
        ScnEnvAnimalMgr.Singleton.onExitMap();
        CursorMgr.Singleton.AutoHide = false;
		ScnLanternMgr.Singleton.onExitMap();
		m_actResultMgr = null;

        base.DeInitData();
    }

    protected override void InitWindow()
    {
        HideReturnDialog();

        base.InitWindow();

        if (GameData.Singleton.ShowChat == false)
            WndManager.Singleton.ShowChatWnd(false);

        if (GameData.Singleton.ShowChit == false)
            WndManager.Singleton.ShowChit(false);
    }

    protected override void Init3DScene()
    {
        if (Terrain.activeTerrain)
        {
            Terrain.activeTerrain.castShadows = false;
            //Terrain.activeTerrain.heightmapMaximumLOD = 1;
            //Terrain.activeTerrain.heightmapPixelError = 200;
        }

        base.Init3DScene();
    }

    protected override void DeInit3DScene()
    {
        base.DeInit3DScene();
    }
    protected override void LateInit()
    {
        base.LateInit();
    }

    protected override void onEscape()
    {
        if (!WndManager.Singleton.EscapeWindows() && !WndManager.Singleton.IsDoModal())
            base.onEscape();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    /// <summary>
    ///  행사 대기 상태를 검사 한다.
    /// </summary>
    protected virtual void updateRiteWaitStatus()
    {
		
        int remain_count = 0;
		

		if (GameData.Singleton.IsInGameState(CEMacro.GS_MASK_HEAVEN) && !IsInRite)
        {
            MultiRiteInfo multi_info = GameData.Singleton.multiRiteInfo;
            if (multi_info != null && multi_info.nHostFID == GameData.Singleton.getMyFamilyID() && GameData.Singleton.WaitingActPos == multi_info.actPos)
            {
                if (multi_info.nRemainCount <= 0)
                {
                    int secs = (int)GameData.Singleton.multiRiteInfo.receive_time / 1000;
                    int count_down = GameData.Singleton.multiRiteInfo.nCountDown;
                    int cur_time = (int)Common._TM() / 1000;
                    remain_count = (count_down - cur_time + secs);
                    if ( remain_count <= 0 && !NetLogic.Singleton.IsCurrentActPos((byte)multi_info.actPos))
                    {
                        NetLogic.Singleton.sendMultiActGo((byte)multi_info.actPos);
                        CEMsgManager.PostMsg(CEMsg.EventTimeToMultirite);
                    }
                }
                
            }
        }
    }

    protected override void Update()
    {
		if (m_isMapCameraTour && !AutoCameraManager.Singleton.IsCameraTouring())
		{
			m_isMapCameraTour = false;
            if (!CEScriptLogic.Singleton.IsNianfoIdle())
            {
                CEScriptLogic.Singleton.SendNotifyToScript("stopNianFo");
            }
		}

		if (m_ResultEnterRoom != NetError.Success)
        {
            if (m_ResultEnterRoom != NetError.Disconnected)
                ForceExitRoom((int)m_ResultEnterRoom);
        }

		if (m_HuaRain != null && m_HuaRain.activeSelf)
		{
			m_HuaRain.transform.parent = AutoCameraManager.Singleton.GetActivCameraPos();
			m_HuaRain.transform.localPosition = Vector3.zero;
			m_HuaRain.transform.localRotation = Quaternion.identity;
		}
        updateRiteWaitStatus();

		m_chatBalloon.OnUpdate(Time.deltaTime);
        base.Update();
    }

    protected override void onEnterRoom()
    {
        IsInRite = false;

        if (GameData.Singleton.isChangeChannel())
            m_ResultEnterRoom = NetLogic.Singleton.SendLeaveChannel();
        else
            StartCoroutine(SendEnterRoom());

        base.onEnterRoom();
    }

    protected override void RegisterMsgHandler()
    {
        CEMsgManager.AddMsgDelegate(CEMsg.NetAddPlayer, CmNetAddPlayer);
        CEMsgManager.AddMsgDelegate(CEMsg.NetDelPlayer, CmNetDelPlayer);
        CEMsgManager.AddMsgDelegate(CEMsg.NetMove, CmNetMove);
        CEMsgManager.AddMsgDelegate(CEMsg.NetChState, CmNetChState);

        //Exit Room
        CEMsgManager.AddMsgDelegate(CEMsg.EventExitRoom, CmEventExitRoom);
        CEMsgManager.AddMsgDelegate(CEMsg.EventEnterStation, CmEventEnterStation);
        CEMsgManager.AddMsgDelegate(CEMsg.NetLuck, CmNetLuck);

        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvChangedTouxiangResult, CmRecvChangedTouxiangResult);
        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvDecYishiResults, CmNetRecvDecYishiResult);
		CEMsgManager.AddMsgDelegate(CEMsg.EventShowHuaRain, CmEventShowHuaRain);


        // Rite
        CEMsgManager.AddMsgDelegate(CEMsg.EventReleaseAct, CmReleaseRite);
        CEMsgManager.AddMsgDelegate(CEMsg.EventStartRite, CmEventStartRite);
        CEMsgManager.AddMsgDelegate(CEMsg.EventFinishRite, CmEventFinishRite);
        CEMsgManager.AddMsgDelegate(CEMsg.EventFinishRiteWithoutPos, CmEventFinishRiteWithoutPos);
        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvActStart, CmRevActStart);
        
        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvActWait, CmNetActWait);
        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvActRelease, CmNetRecvActRelease);
        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvActStep, CmNetRecvActStep);
        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvActNowStart, CmNetRecvActNowStart);
        //MultiRite
        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvMultiRiteState, CmNetMultiRiteState);
        CEMsgManager.AddMsgDelegate(CEMsg.EventMultiToSingleRite, CmEventMultiToSingleRite);
        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvMultiRiteGo, CmNetMultiRiteGo);
        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvMultiRiteStart, CmNetMultiRiteStart);
        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvMultiRiteCommand, CmNetMultiRiteCommand);
        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvMultiRiteRelease, CmNetMultiRiteRelease);

        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvCurLargeAct, CmNetCurLargeAct);

        CEMsgManager.AddMsgDelegate(CEMsg.NetRecvDaXingYishiStart, CmNetDaXingYishiStart);

        CEMsgManager.AddMsgDelegate(CEMsg.NetChangedDress, CmNetChangedDress);
		CEMsgManager.AddMsgDelegate(CEMsg.EventMapCameraTour, CmEventMapCameraTour);

        CEMsgManager.AddMsgDelegate(CEMsg.EventStartUserGuide, CmStartUserGuide);
        CEMsgManager.AddMsgDelegate(CEMsg.EventEndUserGuide, CmEndUserGuide);


		base.RegisterMsgHandler();
    }
    /// From : NetLogic::onMultiActStarted(NetRecvMultiRiteStarted)
    /// To : Screen::OnStopRiteForOther, To : Logic::CmNetMultiRiteRelease
    /// Params :   Uints[0] : host faimily id, Uint[1] : act pos, Uint[2] : is sucess 
    /// Old Version : notifyRecvMultiRiteRelease   
    protected override void UnRegisterMsgHandler()
    {
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetAddPlayer, CmNetAddPlayer);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetDelPlayer, CmNetDelPlayer);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetMove, CmNetMove);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetChState, CmNetChState);

        //Exit Room
        CEMsgManager.RemoveMsgDelegate(CEMsg.EventExitRoom, CmEventExitRoom);
        CEMsgManager.RemoveMsgDelegate(CEMsg.EventEnterStation, CmEventEnterStation);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetLuck, CmNetLuck);

        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvChangedTouxiangResult, CmRecvChangedTouxiangResult);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvDecYishiResults, CmNetRecvDecYishiResult);


        // Rite
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvActWait, CmNetActWait);
        CEMsgManager.RemoveMsgDelegate(CEMsg.EventReleaseAct, CmReleaseRite);
        CEMsgManager.RemoveMsgDelegate(CEMsg.EventStartRite, CmEventStartRite);
        CEMsgManager.RemoveMsgDelegate(CEMsg.EventFinishRite, CmEventFinishRite);

        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvActStart, CmRevActStart);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvActNowStart, CmNetRecvActNowStart);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvActStep, CmNetRecvActStep);
        CEMsgManager.RemoveMsgDelegate(CEMsg.EventFinishRiteWithoutPos, CmEventFinishRiteWithoutPos);
        //MultiRite
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvMultiRiteState, CmNetMultiRiteState);
        CEMsgManager.RemoveMsgDelegate(CEMsg.EventMultiToSingleRite, CmEventMultiToSingleRite);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvMultiRiteGo, CmNetMultiRiteGo);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvMultiRiteStart, CmNetMultiRiteStart);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvMultiRiteCommand, CmNetMultiRiteCommand);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvMultiRiteRelease, CmNetMultiRiteRelease);
        // Only use in Development mode
        //CEMsgManager.RemoveMsgDelegate(CEMsg.EventUseViewDistanceByLayer, CmUseViewDistanceByLayer);

        //CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvMultiRiteRelease, CmActRelease);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvActRelease, CmNetRecvActRelease);

        CEMsgManager.RemoveMsgDelegate(CEMsg.EventShowHuaRain, CmEventShowHuaRain);

        CEMsgManager.RemoveMsgDelegate(CEMsg.NetChangedDress, CmNetChangedDress);
		CEMsgManager.RemoveMsgDelegate(CEMsg.EventMapCameraTour, CmEventMapCameraTour);
        CEMsgManager.RemoveMsgDelegate(CEMsg.NetRecvDaXingYishiStart, CmNetDaXingYishiStart);

        CEMsgManager.RemoveMsgDelegate(CEMsg.EventStartUserGuide, CmStartUserGuide);
        CEMsgManager.RemoveMsgDelegate(CEMsg.EventEndUserGuide, CmEndUserGuide);

		base.UnRegisterMsgHandler();
    }

    protected override void OnInitLogic()
    {
        base.OnInitLogic();
    }

    protected override void InitCameras()
    {
        base.InitCameras();
    }
    protected virtual void CmRevActStart(CEMsgParam _mp)
    {
        uint familyId = _mp.UIntVal[0];
        uint actPos = _mp.UIntVal[1];
        uint myFamilyID = GameData.Singleton.getMyFamilyID();

        uint subPos = 0;
        if (_mp.UIntCount > 3)
            subPos = _mp.UIntVal[3];

        if (myFamilyID != familyId)
        {
			if (actPos == (uint)(ENUM_ACTPOS.ACTPOS_RUYIBEI))
            {
                CEScriptLogic.Singleton.SendNotifyToScript("hideXiangItem,1");
            }
            else
			{
				if ((uint)GameData.Singleton.getPlayingRitePos(myFamilyID) != actPos)
					MoveEnfocelyOwnChOutOfActRange(actPos);
				GameData.Singleton.startRite((byte)actPos, familyId, "", (uint)subPos);
			}
		}
    }
    protected virtual void CmNetRecvActNowStart(CEMsgParam _mp)
    {
        CEMsgManager.PostMsg(CEMsg.NotifyStartRiteForOther, _mp);
    }
    protected virtual void CmNetRecvActStep(CEMsgParam _mp)
    {
        CEMsgManager.PostMsg(CEMsg.NotifyOtherActStep, _mp);
    }
    protected virtual void CmNetRecvActRelease(CEMsgParam _mp)
    {
       // uint family_id = _mp.UIntVal[0];
        uint act_pos = _mp.UIntVal[1];
        // Script Logic에 행사를 취소하라는 명령을 내린다.
        CEMsgManager.PostMsg(CEMsg.NotifyStopRiteForOther, _mp);
        SetActRegion(act_pos, false);
    }
    // PC - OnNotifyEvent - notifyRecvActWait
    // CEMsg.NetActWait
    // int pram0 : remain count, 
    // Uint Param0 : act pos,
    // Uint param1 : pray index / sub pos
    public virtual void CmNetActWait(CEMsgParam _mp)
    {
        byte act_pos = (byte)_mp.UIntVal[0];
        uint sub_pos = (uint)_mp.UIntVal[1];
        int remain_count = _mp.IntVal[0];

        byte waiting_act_pos = GameData.Singleton.WaitingActPos;
        CurPrayIndex = (int)sub_pos;                            //this is for tianshou rite

        // 현재 대기중인 act_pos와 써버로부터 내려온 act pos가 같을때
        // 그에 대한 대기 처리를 진행한다.
        if (waiting_act_pos == act_pos ||
            ((waiting_act_pos == (byte)ENUM_ACTPOS.ACTPOS_BLESS_LAMP &&
                ((byte)ENUM_ACTPOS.ACTPOS_BLESS_LAMP <= act_pos) &&
                    (act_pos < (byte)ENUM_ACTPOS.ACTPOS_BLESS_LAMP + Macro.MAX_BLESS_LAMP_COUNT))))
        {
            deleteWaitingMsgWnd();
            GameData.Singleton.ActivityState = GDActivityState.GDAS_WAITING_TURN;
            _actWaitNormal(act_pos, remain_count, CurPrayIndex);
        }
    }


    public virtual void CmEventStartRite(CEMsgParam _mp)
    {
        byte f_act_pos = (byte)_mp.IntVal[0];
        startRite(f_act_pos);
    }

    // PC :: param1 : f_act_pos, param2 : f_succeed = true;
    public virtual void CmReleaseRite(CEMsgParam _mp)
    {
        byte f_act_pos = (byte)_mp.IntVal[0];
        bool f_succeed = _mp.BoolVal[0];

        releaseRite(f_act_pos, f_succeed);
    }
    /// <summary>
    /// 이전 PC판본에서 notifyEvent RiteFinish에 대응된다.
    /// </summary>
    /// <param name="_mp"></param>
    protected virtual void CmEventFinishRite(CEMsgParam _mp)
    {
        if (_mp.UIntCount < 2)
            return;
        ENUM_ACTPOS rite_act_pos = (ENUM_ACTPOS)_mp.UIntVal[1];

        releaseRite((byte)rite_act_pos);
        GameData.Singleton.ActivityState = GDActivityState.GDAS_IDLE;
    }
    protected virtual void CmEventFinishRiteWithoutPos(CEMsgParam _mp)
    {
        if (_mp.UIntCount < 1)
            return;
        GDRiteKind riteKind = (GDRiteKind)_mp.UIntVal[0];
        //        uint familyID = _mp.UIntVal[1];

        switch (riteKind)
        {
            case GDRiteKind.GDRK_FEED_FISH:
                //NetLogic.Singleton.sendFishFood
                break;
        }

        GameData.Singleton.ActivityState = GDActivityState.GDAS_IDLE;
    }

    #region Multi-Rite-Related-Message-Delegates
    /// From : NetLogic
    /// To : Screen::CmNetMultiRiteState
    /// Params :  Uints[0] : act pos, Uints[1] : family id, Uints[2] : remain count, 
    /// Old Version : notifyRecvMultiRiteState
    public virtual void CmNetMultiRiteState(CEMsgParam _mp)
    {
        uint actPos = 0, remainCount = 0;

        if (_mp.UIntCount > 0)
            actPos = _mp.UIntVal[0];

        if (_mp.UIntCount > 2)
            remainCount = _mp.UIntVal[2];

        if (actPos != 0 && GameData.Singleton.multiRiteInfo.nHostFID != Define.INVALID_ID)
            GameData.Singleton.waitRite((byte)actPos, (int)remainCount, 1);

        CEMsgManager.PostMsg(CEMsg.NotifyMultiRiteStateUpdated);
    }

    public virtual void CmEventMultiToSingleRite(CEMsgParam _mp)
    {
        byte actPos = (byte)ENUM_ACTPOS.ACTPOS_NONE;

        if (_mp.IntCount > 0)
            actPos = (byte)_mp.IntVal[0];
        byte sub_pos = 0;
        if (_mp.IntCount > 1)
            sub_pos = (byte)_mp.IntVal[1];

		startRite(actPos, sub_pos);
		IsInRite = true;
	}
    /// From : NetLogic::onMultiActGo(M_SC_MULTIACT_GO)
    /// To : Screen::CmNetMultiRiteGo
    /// Params :   Uints[0] : act pos, Uint[1] : act type, Multirite type(common multi, xdg, xdg_sub)
    /// Old Version : notifyRecvMultiRiteGo
    /// ScreenHeaven:notifyRecvMultiRiteGo
    /// 써버로부터 단체행사를 시작하라는 통보를 받아서 처리하는 부분
    public virtual void CmNetMultiRiteGo(CEMsgParam _mp)
    {
        
        uint actPos = _mp.UIntVal[0];
        uint jrt_type = _mp.UIntVal[1];
        uint sub_pos = 0;
        if (_mp.UIntCount > 2)
            sub_pos = _mp.UIntVal[2];
        /// 단체행사에 참가한 유저들은 무조건 보이게 하고
        /// 숨쉬기와 같은 잔동작들을 하지 않도록 하는 처리이다.
        /// 유니티 판본에서 필요한지는 모르겠음
        _setMultiRitePlayerShow(true);
        /// 아이템 설정창, 대기창문과 같이 행사설정과 관련된 모든 창문들을 닫는다.
        _closeAllRiteRelatedWindows();
        WndManager.CloseAllWnds();

        YishiSettings settings = GameData.Singleton.getRiteSettings(GDRiteKind.GDRK_SIMPLERITE) as YishiSettings;
        settings.m_actPos = GameData.Singleton.multiRiteInfo.actPos;

        ///actType는 써버로부터 내려받은 단체행사 형태이다.
        
        GameData.Singleton.JingXianRiteType = (JingXian_Rite_Type)jrt_type;
        ///Script Logic에 행사를 시작할데 대한 지령을 날린다.
        CEMsgManager.PostMsg(CEMsg.NotifyStartRite, new CEMsgParam().UInts(GameData.Singleton.getMyFamilyID(), (uint)actPos, (uint)CurrentActionType, sub_pos).Ints((int)jrt_type));
        // 현재 Screen의 상태를 행사중인 상태로 설정한다. 행사도중 일련의(방탈퇴와 같은) 례외처리에서 리용된다.
        IsInRite = true;


	}
    /// From : NetLogic::onActNowStart(M_SC_ACT_NOW_START)
    /// To : Screen::CmNetMultiRiteStart
    /// Params :   Uints[0] : act pos, Uint[1] : family id, Uint[2] : desk no, sub pos
    /// Old Version : notifyRecvMultiRiteStart
    public virtual void CmNetMultiRiteStart(CEMsgParam _mp)
    {
        uint actPos = _mp.UIntVal[0];
        uint familyID = _mp.UIntVal[1];
        CurPrayActPos = (byte)actPos;
        string sRegionName = GetScopeNameFromActPos((byte)actPos); 
        WndManager.CloseAllWnds();
        _startMultiRite();
        GameData.Singleton.startRite((byte)actPos, familyID, sRegionName);
    }
    /// From : NetLogic::onMultiActRelease(NetRecvMultiRiteRelease)
    /// To : Screen::CmNetMultiRiteRelease, To : Logic::OnStopRiteForOther
    /// Params :   Uints[0] : host faimily id, Uint[1] : act pos, Uint[2] : is sucess , sub pos
    /// Old Version : notifyRecvMultiRiteRelease      
    public virtual void CmNetMultiRiteRelease(CEMsgParam _mp)
    {
        CELog.Log("enter in ScreenHeaven::CmNetMultiRiteRelease ");

        // uint hostFID = 0;
        // uint actPos = 0;
        // uint subPos = 0;

        // if (_mp.UIntCount > 0)
        //     hostFID = _mp.UIntVal[0];
        // if (_mp.UIntCount > 1)
        //     actPos = _mp.UIntVal[1];
        // if (_mp.UIntCount > 2)
        //     subPos = _mp.UIntVal[2];
        bool is_success = false;
        if (_mp.BoolCount > 0)
            is_success = _mp.BoolVal[0];
        
        MultiRiteInfo info = GameData.Singleton.multiRiteInfo;

        CEMsgManager.PostMsg(CEMsg.NotifyStopMultiRite, _mp);
        CELog.Log("ScreenHeaven::CmNetMultiRiteRelease parameter have not fault");

        // 진행중인 행사가 있는가?
        if (!IsInRite) // 진행중인 행사가 없다.
        {
            // did not attend to rite
            CELog.Log("ScreenHeaven::CmNetMultiRiteRelease There are no rite to cancel, m_bRite = false");
            NetLogic.Singleton.sendActWaitCancel((uint)info.actPos, false, NetLogic.SendPostClosing.SPC_SEND);
        }
        else // 진행중인 행사가 있다.
        {
            GDRiteKind rite_kind = GameData.Singleton.getRiteKindFromActPos((byte)info.actPos);
            if (rite_kind != GDRiteKind.GDRK_NONE) 
            {
                // 행사가 성공적으로(정상적으로) 끝났는가?
                if (is_success) // 예
                {
                    TSelectedItems items = new TSelectedItems();
                    string pray = "";
                    bool secret = false;
                    GameData.Singleton.getRiteSettingItems(rite_kind, ref pray, ref secret, ref items);
                    CELog.Log("Clear Setting , because yishi finished successfully");
                    // 행사정보를 초기화한다.
                    GameData.Singleton.ClearSetting(rite_kind);

                    // 인벤상태를 갱신한다. 행사중에 리용한 아이템들을 인벤에서 없앤다.
                    if (items.Count > 0) 
                    {
                        TInventory inven = GameData.Singleton.getInventory();

                        for (int i = 0; i < items.Count; i++)
                        {
                            SelectedItem sel_item = items[i];
                            if (sel_item != null)
                            {
                                if (sel_item.inven_pos >= 0)
                                {
                                    if (inven.ContainsKey(sel_item.inven_pos))
                                    {
                                        InvenItem it_inven = inven[sel_item.inven_pos];
                                        it_inven.count -= sel_item.item_count;
                                        string item_name = GameData.Singleton.getItemName(it_inven.id);
                                        string str = string.Format(MasterData.Text(CETextID.TXTID_MAIL_DEL_ITEMS), sel_item.item_count, item_name ); ;
                                        Common.printSysText(str);
                                        if (it_inven.count == 0)
                                            inven.Remove(sel_item.inven_pos);
                                    }
                                }
                            }
                        }
                        CEMsgManager.PostMsg(CEMsg.NetChangedInven);
                    }
                }
                else // 정상적으로 끝나지 않았다.
                {
                    // 행사 정보만 초기화한다.
                    GameData.Singleton.ClearSetting(rite_kind);
                }
                _stopMultiRite(_mp);
            }
            else
            {
                CELog.Log("ScreenRoom::CmNetMultiRiteRelease rite kind is none");
                //GameData.Singleton.ClearSetting(rite_kind);
            }
        }
        GameData.Singleton.lockInventory(false);
    }
	public virtual void CmNetMultiRiteCommand(CEMsgParam _mp)
    {
		/*
		MultiRite_Command cmd = (MultiRite_Command)_mp.UIntVal[0];
		uint param = _mp.UIntVal[1];

		switch (cmd)
		{
			case MultiRite_Command.MR_VOICE:
				CEScriptLogic.Singleton.SendNotifyToScript("SetVoiceID," + param.ToString());
				break;
			case MultiRite_Command.MR_BOWTYPE:
				CEScriptLogic.Singleton.SendNotifyToScript("SetBowType," + param.ToString());
				break;
			case MultiRite_Command.MR_XDG_START_DANCE:
				CEScriptLogic.Singleton.SendNotifyToScript("SetStartDance," + param.ToString());
				break;
			case MultiRite_Command.MR_FINISH_SUBMAP_XDG:
				CEScriptLogic.Singleton.SendNotifyToScript("DoFinishSubmapXDG," + param.ToString());
				break;
		}
		 * */
    }
    #endregion //Multi-Rite-Related-Message-Delegates

    #region Large Rite Related Meesage Delegates
    /// From : NetLogic::onCurLargeAct(M_SC_LARGEACT_CURRENT),NetLogic::onReqLargeAct(M_SC_LARGEACT_REQUEST), onLargeActCurReqNum(M_SC_LARGEACT_USERNUM)
    /// To : ScreenPublic, ScreenJisi
    /// Params : uints[0] : 갱신된 내용이 무엇인가를 알려준다. LARGE_ACT_UPDATE_TYPE
    ///         FULL_INFO_UPDATE - 써버로부터 현재 행사 정보 전체를 내려 받았다. (M_SC_LARGEACT_CURRENT)
    ///         JOINER_COUNT_UPDATE - 행사 참가자수가 변했다는것을 써버로부터 내려받는다. (M_SC_LARGEACT_USERNUM)
    ///         REQUEST_RESPONSE - 행사 참가 요청에 대한 응답 (M_SC_LARGEACT_REQUEST)
    /// Old Version : notifyRecvCurLargeAct
    /// 현재 대형행사정보가 갱신되였다는것을 알려준다.
    protected virtual void CmNetCurLargeAct(CEMsgParam _mp)
    {
        LARGE_ACT_UPDATE_TYPE type = (LARGE_ACT_UPDATE_TYPE)_mp.UIntVal[0];
        if(type == LARGE_ACT_UPDATE_TYPE.REQUEST_RESPONSE)
        {
            GameMessage.messageBox(MasterData.Text(CETextID.TXTID_REQSUC_LARGEACT), WndMessageType.WMT_OK);
        }
    }
    protected virtual void CmNetDaXingYishiStart(CEMsgParam _mp)
    {
        return;
    }
    #endregion
    #region Character-Player-Related-Message-Delegates
    void CmNetAddPlayer(CEMsgParam _mp)
    {
        uint familyId = (uint)_mp.IntVal[0];
        if (familyId == 0)
        {
            addAllPlayers();

            OpenPlace openPlace = GameData.Singleton.GetOpenPlace();
            if (openPlace != null && openPlace.getRoomDataStatus(RoomDataKind.RDK_ALLPLAYERSINROOM) == RoomDataStatus.RDS_RECEIVED)
                openPlace.setRoomDataStatus(RoomDataKind.RDK_ALLPLAYERSINROOM, RoomDataStatus.RDS_INITIALIZED);
        }
        else
        {
            Player player = GameData.Singleton.getPlayer(familyId);
            addFamilyPlayers(player, true);
            GameData.Singleton.InitSceneFinished = true;
        }
		CEMsgManager.PostMsg(CEMsg.NetShowChLuckName);
	}

    void CmNetDelPlayer(CEMsgParam _mp)
    {
        delFamilyPlayers((uint)_mp.IntVal[0]);
    }

    void CmNetMove(CEMsgParam _mp)
    {
        uint familyId = (uint)_mp.IntVal[0];

        if (familyId == GameData.Singleton.getMyFamilyID())
            return;

        Player player = GameData.Singleton.getPlayer(familyId);
        if (player == null || GameData.Singleton.getChPlayer(familyId) == null)
            return;

        Actor actor = player.getActor((uint)_mp.IntVal[1]);
        if (actor == null)
            return;

        actor.pos_info.pos = new Vector3(_mp.FloatVal[0], _mp.FloatVal[1], _mp.FloatVal[2]);

        ChPlayer ctrl = GameData.Singleton.getChPlayer(familyId);
        if (ctrl != null)
            ctrl.NetMove(actor.pos_info.pos, actor.pos_info.dir);
    }

    void CmNetChState(CEMsgParam _mp)
    {
        uint familyId = (uint)_mp.IntVal[0];

        if (GameData.Singleton.getMyFamilyID() == familyId)
            return;

        Player player = GameData.Singleton.getPlayer(familyId);
        if (player == null)
            return;

        Actor actor = player.getActor((uint)_mp.IntVal[1]);
        if (actor == null)
            return;

        actor.pos_info.dir = _mp.FloatVal[0];
        actor.pos_info.pos = new Vector3(_mp.FloatVal[1], _mp.FloatVal[2], _mp.FloatVal[3]);
        actor.tm_hold_item = (uint)_mp.IntVal[2];
        uint aniState = (uint)_mp.IntVal[3];
        string animation = _mp.StringCount > 0 ? _mp.StringVal[0] : string.Empty;

        if (!string.IsNullOrEmpty(animation) && animation.Split('_')[0] == "ShenshuNode")
        {
            NpcShenShu ShenShu = CENodeManager.getNode(animation).GetComponent<NpcShenShu>();

            if (ShenShu)
                ShenShu.showEffect();
        }

        ChPlayer ctrl = GameData.Singleton.getChPlayer(familyId);
        if (ctrl != null)
            ctrl.NetState(actor.pos_info.pos, actor.pos_info.dir, animation, aniState, actor.tm_hold_item);
    }
    void CmEventExitRoom(CEMsgParam _mp)
    {
        if (_mp.UIntVal[0] != 0)
        {
            if (_mp.UIntCount == 2)
                m_gotoWhere = _mp.UIntVal[1];
            GameData.Singleton.SetPromotionIDInRoom(_mp.UIntVal[0]);
            CETextID txtID = _mp.UIntVal[0] == Define.INVALID_UIDX ? CETextID.TXTID_ROOMBUY_MSG : CETextID.TXTID_ROOMPROMOTION_MSG;
            GameMessage.messageBox(txtID, WndMessageType.WMT_OK_CANCEL, delegateExitRoom, "", MasterData.Text(CETextID.TXTID_ROOMPROMOTION_HUIDATING));
        }
        else
        {
            m_gotoWhere = _mp.UIntVal[1];
            string confirm_str = "";

			if(m_gotoWhere == 0)
				GameData.Singleton.getWaitingStateOnQuit(GameData.QuitType.QT_TO_LOGIN, ref confirm_str);
			else
				GameData.Singleton.getWaitingStateOnQuit(GameData.QuitType.QT_TO_HALL, ref confirm_str);

			//GameData.Singleton.getWaitingStateOnQuit(GameData.QuitType.QT_TO_HALL, ref confirm_str);
            GameMessage.messageBox(confirm_str, WndMessageType.WMT_OK_CANCEL | WndMessageType.WMT_ICON_INFORMATION, delegateExitRoom);
        }
    }
    void CmEventEnterStation(CEMsgParam _mp)
    {
        if (GameData.Singleton.IsInGameState(CEMacro.GS_MASK_HEAVEN))
            _processExitRoom(true);
    }
    void CmEventShowHuaRain(CEMsgParam _mp)
	{
		if (m_HuaRain == null)
			m_HuaRain = CENodeManager.getNode("HuaRain");

		bool show = _mp.BoolVal[0];
		m_HuaRain.SetActive(show);

		if (!show)
			m_HuaRain.transform.parent = null;
	}

    #endregion //Character-Player-Related-Message-Delegates
    void CmNetLuck(CEMsgParam _mp)
    {
        if (_mp.IntCount == 0)
            return;
        if (_mp.IntVal[0] == 0/* || _mp.IntVal[0] == 3*/)
        {
            uint luck_id = (uint)_mp.IntVal[1];
            uint family_id = (uint)_mp.IntVal[2];
            uint roomId = (uint)_mp.IntVal[3];

            if (GameData.Singleton.IsInGameState(CEMacro.GS_MASK_HEAVEN))
                ScnLuckMgr.onReceiveLuck(family_id, luck_id, roomId);

            if (luck_id == 0)
                return;

            MdtMapLuck lucks = MasterData.Singleton.m_lucks;
            if (!lucks.ContainsKey(luck_id))
                return;
            //MdsLuck luckInfo = MasterData.Singleton.GetLuckInfo(luck_id);
            MdsLuck luckInfo = lucks[luck_id];
            switch (luckInfo.luckLevel)
            {
                case 4:
                    {
                        if (family_id == GameData.Singleton.getMyFamilyID())
                            ScnLuckMgr.ModifyBlessCard();
                    }
                    break;
            }
        }
        else if (_mp.IntVal[0] == 1)
        {
            if (GameData.Singleton.IsInGameState(CEMacro.GS_MASK_HEAVEN))
                ScnLuckMgr.OnReceiveEarlierLuck((uint)_mp.IntVal[1], (uint)_mp.IntVal[2]);
        }

        CEMsgManager.PostMsg(CEMsg.NetShowChLuckName);
    }

    void CmRecvChangedTouxiangResult(CEMsgParam _mp)
    {
        m_touxiangResultMgr.update();
        OpenPlace open_place = GameData.Singleton.GetOpenPlace();
        if (open_place != null && open_place.getRoomDataStatus(RoomDataKind.RDK_TOUXIANGRESULT) == RoomDataStatus.RDS_RECEIVED)
            open_place.setRoomDataStatus(RoomDataKind.RDK_TOUXIANGRESULT, RoomDataStatus.RDS_INITIALIZED);
    }

    void CmNetRecvDecYishiResult(CEMsgParam _params)
    {
        if (_params.UIntCount < 1)
            return;

        if (m_actResultMgr != null)
            m_actResultMgr.OnDecedentYishiResult((byte)_params.UIntVal[0]);

        OpenPlace open_place = GameData.Singleton.GetOpenPlace();
        if (open_place != null && open_place.getRoomDataStatus(RoomDataKind.RDK_YISHIRESULT) == RoomDataStatus.RDS_RECEIVED)
            open_place.setRoomDataStatus(RoomDataKind.RDK_YISHIRESULT, RoomDataStatus.RDS_INITIALIZED);
    }

    void CmNetChangedDress(CEMsgParam _mp)
    {
        uint fId = _mp.UIntVal[0];

        CharacterBase chInfo = null;

        Player player = GameData.Singleton.getPlayer(fId);
        if (player != null && player.chs != null)
        {
            Dictionary<uint, Actor>.Enumerator enumerator = player.chs.GetEnumerator();
            enumerator.MoveNext();
            chInfo = enumerator.Current.Value;
        }
        else
            CELog.Log("Can not find " + fId.ToString() + " family in ScreenBase::ChangeDress.", LogType.Error);


        ChPlayer chplayer = GameData.Singleton.getChPlayer(fId);
        if (player == null)
            return;

        // change main player's dress
        Common.initChNodeFromChInfo(chplayer, chInfo);

        // change ui's dress
        ChangeDress(fId);
    }
	void CmEventMapCameraTour(CEMsgParam _mp)
	{
		m_isMapCameraTour = true;
	}

    void CmStartUserGuide(CEMsgParam _mp)
    {
        ScreenBase.LoadScreen(CEMacro.GS_GUIDE_HALL);
        //UserGuideMgr.Singleton.StartUserGuide();
    }

    void CmEndUserGuide(CEMsgParam _mp)
    {
        UserGuideMgr.Singleton.EndUserGuide();
        ScreenBase.LoadScreen(CEMacro.GS_HALL);
    }

	public void ShowReturnDialog()
    {
        if (returnDialog)
            returnDialog.SetActive(true);
    }

    public void HideReturnDialog()
    {
        if (returnDialog)
            returnDialog.SetActive(false);
    }

    public void ShowMainCtrls(bool bshow)
    {
        m_bShowMainCtrl = bshow;
        CEMsgManager.PostMsg(CEMsg.EventChitsUpdated);
    }
    public bool GetShowMainCtrl()
    {
        return m_bShowMainCtrl;
    }

    public void ShowAllPlayers(bool _show)
    {
        m_showAllPlayers = _show;

        // uint myFamilyID = GameData.Singleton.getMyFamilyID();

        Dictionary<uint, ChPlayer> players = new Dictionary<uint, ChPlayer>();
        GameData.Singleton.getChPlayers(ref players);

        Dictionary<uint, ChPlayer>.Enumerator enumer = players.GetEnumerator();
        while (enumer.MoveNext())
        {
            ChPlayer player = enumer.Current.Value;
            player.ShowPlayer(_show);
        }
    }

    public void ShowOtherPlayers(bool _show)
    {
        m_showOtherPlayers = _show;

        uint myFamilyID = GameData.Singleton.getMyFamilyID();

        Dictionary<uint, ChPlayer> players = new Dictionary<uint, ChPlayer>();
        GameData.Singleton.getChPlayers(ref players);
        uint myActPos = GameData.Singleton.getPlayingRitePos(myFamilyID);

        foreach (uint familyID in players.Keys)
        {
            if (familyID == myFamilyID)
                continue;

            if (_show)
                players[familyID].ShowPlayer(_show);
            else
            {
                if ((myActPos == (uint)ENUM_ACTPOS.ACTPOS_NONE || GameData.Singleton.getPlayingRitePos(familyID) != myActPos))
                    players[familyID].ShowPlayer(_show);
            }
        }
        CEMsgManager.PostMsg(CEMsg.EventChitsUpdated);
    }

    public bool GetShowOtherPlayer()
    {
        return m_showOtherPlayers && m_showAllPlayers;
    }


    private void addAllPlayers()
    {
        Dictionary<uint, Player> players = new Dictionary<uint, Player>();
        if (!GameData.Singleton.getPlayers(ref players))
            return;

        //foreach(KeyValuePair<uint, Player> pair in players)
        Dictionary<uint, Player>.Enumerator enumerator = players.GetEnumerator();
        while (enumerator.MoveNext())
        {
            KeyValuePair<uint, Player> pair = enumerator.Current;

            if (pair.Key != GameData.Singleton.getMyFamilyID())
                addFamilyPlayers(pair.Value, true);
        }
    }

    private void addFamilyPlayers(Player _player, bool bEffect)
    {
        if (_player == null)
            return;

        if (_player.nFamilyID == GameData.Singleton.getMyFamilyID())
        {

            if (BirthPos[0] == null)
                return;

            // if autorite and liangenyindao, effect disable.
            bool bPlayerEffect = bEffect;
            if (GameData.Singleton.IsAutoRite())
            {
                DefaultRiteSettings setting = GameData.Singleton.getRiteSettings(GDRiteKind.GDRK_LIANGEN_INTRO);
                if (setting != null && setting.isConfigured())
                    bPlayerEffect = false;
            }

            if (!addFamilyPlayer(_player, bPlayerEffect, true))
                return;

            GameData.Singleton.getChPlayer(_player.nFamilyID).gameObject.tag = Define.MainPlayerTag;

            // Check is auto rite.
            if (GameData.Singleton.IsAutoRite())
                CEMsgManager.PostMsg(CEMsg.EventAutoRiteStart);
            if (GameData.Singleton.IsFJAutoRite())
                CEMsgManager.PostMsg(CEMsg.EventFJAutoRiteStart);
            CheckMultiRite();
        }
        else
        {
            if (GameData.Singleton.getChPlayer(_player.nFamilyID) != null)
                delFamilyPlayers(_player.nFamilyID);

            if (!addFamilyPlayer(_player, bEffect, false))
                return;
        }
    }

    private bool addFamilyPlayer(Player _player, bool doEffect, bool _main = true)
    {
        Transform trans = _main ? BirthPos[0] : BirthPos[1];
        // CESpace birthSpace = CEScopeManager.Singleton.GetRandSpace("Pos_Birth0");
        //Vector3 birthPos = birthSpace.GetRandPos(true);
        string strModelName = "";
        Dictionary<uint, Actor>.Enumerator enumerator = _player.chs.GetEnumerator();
        while (enumerator.MoveNext())
        {
            KeyValuePair<uint, Actor> pair = enumerator.Current;

            MdsModel model = (MdsModel)MasterData.Singleton.GetModelInfo(pair.Value.nModelID);
            if (model != null)
                strModelName = model.prefab;
        }

        GameObject instance = (GameObject)GameObject.Instantiate(ResourceManager.Singleton.GetModel(strModelName), Vector3.zero, trans.rotation);

        if (instance == null)
            return false;

        instance.transform.SetParent(trans);
        Util.ChangeLayersRecursively(instance.transform, _main ? Define.MainPlayerLayer : Define.OtherPlayerLayer);

        //instance.AddComponent<ChController>();
        ChPlayer chPlayer = instance.GetComponent<ChPlayer>();

        if (chPlayer == null)
            return false;

        chPlayer.InitPlayer(_main, _player);

        GameData.Singleton.addChPlayer(_player.nFamilyID, chPlayer);
        Common.initChNodeFromChInfo(chPlayer, chPlayer.ChActor);
        chPlayer.WorldPos = chPlayer.ChActor.pos_info.pos;
        if (_main)
        {
            //MainCameraController camCtrl = Camera.main.gameObject.GetComponent<MainCameraController>();
            CameraController camCtrl = Camera.main.gameObject.GetComponent<CameraController>();
            // CESoundManager.Singleton.OnCreateMainPlayer(instance);             // for sound
            if (camCtrl != null)
            {
                camCtrl.SetPlayer(instance);
                camCtrl.MainCamera = Camera.main;
            }
        }
        else
        {
            chPlayer.WorldPos = chPlayer.TargetPos = chPlayer.ChActor.pos_info.pos;
            chPlayer.WorldRot = chPlayer.TargetRot = Quaternion.Euler(0, chPlayer.ChActor.pos_info.dir, 0);

            string ani = chPlayer.ChActor.animation;

            // 3자가 아이템을 붙인 상태에서 움직이면 아이템이 붙지 않은것으로 형상된다. 따라서 이부분이 따로 필요함.
            if (chPlayer.ChActor.hold_item != 0 && !string.IsNullOrEmpty(chPlayer.ChActor.animation))
                chPlayer.AttachItem(chPlayer.ChActor.hold_item);

            if (!string.IsNullOrEmpty(ani) && !ani.Equals("flying"))
            {
                switch (chPlayer.ChActor.hold_item)
                {
                    case 1000020:  // nianfo 중이면
                        ani = "M_SitFloorPray";
                        break;
                    case 1000018:  // 련꽃가야금치기
                        ani = "M_GuqinHaoyun";
                        break;
                }
            }

            chPlayer.NetState(chPlayer.ChActor.pos_info.pos, chPlayer.ChActor.pos_info.dir, ani,
                chPlayer.ChActor.ani_state, chPlayer.ChActor.hold_item);//.PlayAnimation(chPlayer.ChActor.animation);

            chPlayer.ShowPlayer(m_showOtherPlayers && m_showAllPlayers);
        }

        bool bShow = true;
        if (!_main && chPlayer.ChActor.show_prop == (byte)_CHARACTERSHOWTYPE.CHSHOW_HIDE)
            bShow = false;

//         if (doEffect)
//             playernode->appear(bShow, CH_SHOWHIDE_TIME);
//         else
//             playernode->show(bShow, CH_SHOWHIDE_TIME);
        if (doEffect && bShow)
            chPlayer.DoEffect((uint)CH_EFFECT.EFX_BIRTH);
        
        if (_player.nFamilyID == GameData.Singleton.getMyFamilyID())
        {
            if (m_MainRTPlayer == null)
                CreateMainRTPlayer(ResourceManager.Singleton.GetModel(strModelName), BirthPos.Length > 2 ? BirthPos[2] : BirthPos[1]);
        }

        return true;

    }

    public bool CloneFamily(uint _familyId, uint _newFamilyId)
    {
        //string familyName;
        Player player = GameData.Singleton.getPlayer(_familyId);

        if (player == null)
            return false;

        Actor mainActor = null;

        Dictionary<uint, Actor>.Enumerator enumerator = player.chs.GetEnumerator();
        while (enumerator.MoveNext())
        {
            KeyValuePair<uint, Actor> it = enumerator.Current;
            mainActor = it.Value;
            break;
        }
        if (mainActor == null)
            return false;

        //familyName = mainActor.sName;

        //bool nameUpdated = GameData.Singleton.setFamilyName(_newFamilyId, familyName);

        Actor ch = new Actor();
        ch.id = _newFamilyId;
        ch.nModelID = mainActor.nModelID;
        ch.nDefaultGarb = mainActor.nDefaultGarb;
        ch.nAddin_Garb = mainActor.nAddin_Garb;
        ch.nFaceId = mainActor.nFaceId;
        ch.nHairId = mainActor.nHairId;
        ch.sName = mainActor.sName;
        ch.ani_state = mainActor.ani_state;
        ch.pos_info = mainActor.pos_info.Clone();

        Player newPlayer = new Player();
        newPlayer.nFamilyID = _newFamilyId;
        newPlayer.nCloneFamilyID = _familyId;
        newPlayer.chs.Add(ch.id, ch);
        newPlayer.level = player.level;

        GameData.Singleton.addPlayer(newPlayer);

        addFamilyPlayer(newPlayer, false, false);

        ChPlayer chPlayer = GameData.Singleton.getChPlayer(_newFamilyId);
        if (chPlayer != null)
            chPlayer.WorldPos = new Vector3(1000, 2000, 1000);
        // Screen::postNotifyReceive(INotifyRecv::notifyRecvAddPlayer, new_family_id, 1);
        return true;
    }

    public void delFamilyPlayers(uint _familyId)
    {
        if (_familyId != GameData.Singleton.getMyFamilyID())
        {
            ChPlayer chPlayer = GameData.Singleton.getChPlayer(_familyId);
            if (chPlayer)
            {
				ScnLanternMgr.Singleton.deletePlayer(_familyId);
				chPlayer.DoEffect((uint)CH_EFFECT.EFX_EXIT);
                chPlayer.DoEffect("CE_ep_taozi_01");
                chPlayer.DoEffect("CE_ep_taozi_01");
                chPlayer.DoEffect("CE_ep_taozi_01");
                chPlayer.DoEffect("CE_ep_taozi_01");
                //chPlayer.gameObject.SetActive(false);
                Destroy(chPlayer.gameObject);
                GameData.Singleton.delPlayer(_familyId);
                GameData.Singleton.delChPlayer(_familyId);
           }
        }
        else
        {
            GameObject playerObj = GameData.Singleton.getChPlayer(_familyId).gameObject;
            if (playerObj == null)
                return;

            // CESoundManager.Singleton.OnDeleteMainPlayer(playerObj);

            Destroy(playerObj);
            playerObj = null;
            GameData.Singleton.delPlayer(_familyId);
            GameData.Singleton.delChPlayer(_familyId);

            CameraController ctrl = Camera.main.gameObject.GetComponent<CameraController>();
            if (ctrl != null)
            {
                ctrl.MainCamera = null;
                ctrl.SetPlayer(null);
            }

        }
    }

    private void CheckMultiRite()
    {
        MultiRiteInfo info = GameData.Singleton.multiRiteInfo;
        if (info != null)
        {
            if (!info.bInThisRoom)
            {
                //OpenPrepareJoinWnd((uint)info.actPos, info.RoomID);
                WndManager.OpenWnd(WndType.WndRiteJoinerWait, new CEMsgParam().UInts((uint)info.actPos, info.RoomID));
                NetLogic.Singleton.sendMultiActReady(info.actPos);
                info.bInThisRoom = true;
            }
        }
    }

    protected IEnumerator SendEnterRoom()
    {
        if (GameData.Singleton.isChangingShard())
        {
            GameData.Singleton.setChangingShard(false);
            yield return StartCoroutine(NetLogic.Singleton.sendLeaveLobby(value => m_ResultEnterRoom = value));
        }
        else
            yield return StartCoroutine(NetLogic.Singleton.SendLeaveRoom(value => m_ResultEnterRoom = value));

        if (m_ResultEnterRoom == NetError.Success)
        {
            GameData.Singleton.LastSendLeaveTime = Common._TM_PROCESS();
            loadScreen(CEMacro.GS_NONE, GameData.Singleton.GetSceneIDToEnter());
            base.onEnterRoom();
        }
        else
            GameData.Singleton.LastSendLeaveTime = 0;
    }

    protected IEnumerator SendEnterRoom(uint _roomId, uint _channelId, uint _chId, PosInfo _pos)
    {
        yield return StartCoroutine(NetLogic.Singleton.sendEnterRoom(value => m_ResultEnterRoom = value, _roomId, _channelId, _chId, _pos));
    }

    private IEnumerator SendLeaveRoom(bool _enterStation = false, bool _forceExit = false, bool _forceHall = false)
    {
        if (!_forceExit)
        {
            yield return StartCoroutine(NetLogic.Singleton.SendLeaveRoom(value => m_ResultEnterRoom = value));

            if (m_ResultEnterRoom == NetError.Success)
            {
                GameData.Singleton.LastSendLeaveTime = Common._TM_PROCESS();
                if (_enterStation)
                {
                    GameData.Singleton.FirstToWorld = false;
                    loadScreen(CEMacro.GS_STATION);
                }
                else
                {
                    if (_forceHall)
                        loadScreen(CEMacro.GS_HALL);
                    else
                    {
                        if (m_gotoWhere == 1)
                            loadScreen(CEMacro.GS_HALL);
                        if (m_gotoWhere == 0)
                            loadScreen(CEMacro.GS_LOGIN);
                    }
                    //base.onEnterRoom();
                }
            }
            else
                GameData.Singleton.LastSendLeaveTime = 0;
        }
        else
        {
            GameData.Singleton.setNetEntering(true);

            GameData.Singleton.LastSendLeaveTime = Common._TM_PROCESS();
            loadScreen(CEMacro.GS_HALL);
            //base.onEnterRoom();
        }
    }

    protected void _processExitRoom(bool _enterStation = false, bool _forceExit = false, bool _forceHall = false)
    {
        // cancel rite
        CELog.Log("ScreenRoom _processExitRoom ----------> sendActWaitCancel");
        NetLogic.Singleton.sendActWaitCancel();
        GameData.Singleton.cancelRiteWaiting();
        GameData.Singleton.DelChitReqMultiPray();

        IsInRite = false;

        // cancel auto-rite
        if (GameData.Singleton.IsAutoRite() || GameData.Singleton.IsFJAutoRite())
            GameData.Singleton.ClearSetting(GDRiteKind.GDRK_NONE, GDRiteAttrib.GDRA_AUTO);


        StartCoroutine(SendLeaveRoom(_enterStation, _forceExit, _forceHall));
    }

    bool delegateExitRoom(System.Object msg)
    {
        WndMsgEvent result = msg as WndMsgEvent;
        if (result.m_result == WndMessageResult.WMR_OK)
        {
            if (GameData.Singleton.IsInGameState(CEMacro.GS_PUBLIC))
            {
                bool isInThisRoom = GameData.Singleton.IsInPlace((uint)GameData.Singleton.getReqLargeChannelID(), GameData.Singleton.getLargeActRequestRoomID());
                if (isInThisRoom)
                {
                    if (GameData.Singleton.isEqualLargeActState(LargeAct_State.LAS_PREPARE))
                    {
                        NetLogic.Singleton.sendCancelLargeAct();
                        GameData.Singleton.delChit(_CHITTYPE.CHITTYPE_LARGEACT_PREPARE);
                    }
                    else if (GameData.Singleton.isEqualLargeActState(LargeAct_State.LAS_START))
                        GameData.Singleton.setLargeActState(LargeAct_State.LAS_NONE);
                }
            }

            _processExitRoom();
        }
        else
            GameData.Singleton.SetPromotionIDInRoom(0);

        return true;
    }

    private void ForceExitRoom(int _error)
    {
        GameMessage.errorBox(_error, ForceExitRoomDelegate);
    }
    protected bool ForceExitRoomDelegate(System.Object _msg)
    {
        _processExitRoom(false, true);
        return true;
    }

    /*
    /// <summary>
    /// playerList에 등록된 캐랙터들에 active속성을 설정한다.
    /// 캐랙터 _bShowHide에 따라
    /// </summary>
    /// <param name="_bShowHide">드</param>
    /// <param name="_playerList">보여주기를 설정할 캐랙터 패밀리 아이디 리스트</param>
    /// <param name="_exceptList">보여주기 설정에서 제외되여야 할 캐랙터 리스트</param>
    protected void _showPlayers(bool _bShowHide = true, List<uint> _playerList = null, List<uint> _exceptList = null)
    {
        Dictionary<uint, ChPlayer> char_list = null;
        GameData.Singleton.getChPlayers(ref char_list);
        uint player_id = 0;
        if (char_list == null)
            return;

        /// 자신을 제외한 플레이어들 숨기기
        uint my_family_id = GameData.Singleton.getMyFamilyID();
        if (_playerList != null)
        {
            for (int i = 0; i < _playerList.Count; i++)
            {
                player_id = _playerList[i];
                if (char_list.ContainsKey(player_id) &&
                    _exceptList != null &&
                    !_exceptList.Exists(id => id == player_id))
                    char_list[player_id].gameObject.SetActive(_bShowHide);
            }
        }
        else
        {
            Dictionary<uint, ChPlayer>.Enumerator enumerator = char_list.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<uint, ChPlayer> pair = enumerator.Current;
                if (((ChPlayer)pair.Value).FamilyID == my_family_id)
                {
                    continue;
                    //pair.Value.gameObject.SetActive(_bShowHide);
                }
                if (_exceptList != null && _exceptList.Exists(id => id == ((ChPlayer)pair.Value).FamilyID))
                    continue;
                pair.Value.gameObject.SetActive(_bShowHide);
            }
        }

    }


    /// <summary>
    /// 주인공 캐랙터들에 대한 보이기/숨기기를 설정한다.
    /// pc판에서는 m_isShowOtehrPlys라는 기발 변수를 설정하고
    /// character node의 update고리에서 보
    /// </summary>
    /// <param name="_bShowHide"></param>
    /// <param name="_exceptList"></param>
    protected void _showAllPlayers(bool _bShowHide = true, List<uint> _exceptList = null)
    {
        Dictionary<uint, ChPlayer> char_list = null;
        GameData.Singleton.getChPlayers(ref char_list);


        /// 자신을 제외한 플레이어들 숨기기
        uint my_family_id = GameData.Singleton.getMyFamilyID();
        Dictionary<uint, ChPlayer>.Enumerator enumerator = char_list.GetEnumerator();
        while (enumerator.MoveNext())
        {
            KeyValuePair<uint, ChPlayer> pair = enumerator.Current;
            if (((ChPlayer)pair.Value).FamilyID == my_family_id)
            {
                continue;
                //pair.Value.gameObject.SetActive(_bShowHide);
            }
            if (_exceptList != null && _exceptList.Exists(id => id == ((ChPlayer)pair.Value).FamilyID))
                continue;
            pair.Value.gameObject.SetActive(_bShowHide);
        }
    }
    */

    public void MoveEnfocelyOwnChOutOfActRange(string actRegionName, string outRegionName)
    {
        ChPlayer chPlayer = GameData.Singleton.getChPlayer(GameData.Singleton.getMyFamilyID());

        CEScope scope = CEScopeManager.Singleton.GetScope(actRegionName);
        if (scope == null)
            return;

        if (!scope.ContainPoint(chPlayer.WorldPos))
        {
            SetActRegion(actRegionName, true);
            return;
        }

        chPlayer.CancelMove();
        Vector3 randPos = CEScopeManager.Singleton.GetRandPostion(outRegionName, true);

        Vector3 rot = CEScopeManager.Singleton.GetPointRotation(outRegionName);
        m_actRegionName = actRegionName;
        chPlayer.DoCameraTour(randPos, rot, OnCameraTourFinished);
    }
    public void MoveEnfocelyOwnChOutOfActRange(uint actPos)
    {
        string actRegionName = "";
        string outRegionName = "";

        bool success = MasterData.Singleton.GetActRegionName(GameData.Singleton.GetCurSceneID(), actPos, out actRegionName, out outRegionName);
        if (success == false)
            return;

        MoveEnfocelyOwnChOutOfActRange(actRegionName, outRegionName);
    }

    public bool OnCameraTourFinished(System.Object obj)
    {
        SetActRegion(m_actRegionName, true);
        m_actRegionName = "";
        return true;
    }

    public void SetActRegion(string actRegionName, bool enable)
    {
        CEScope scope = CEScopeManager.Singleton.GetScope(actRegionName);
        int spaceCount = scope.m_spaces.Count;

        for (int i = 0; i < spaceCount; i++)
        {
            CESpace space = scope.GetSpace(i);
            if (space == null)
                return;

            if (space.m_type == SPACE_TYPE.Point)
                continue;

            space.gameObject.layer = LayerMask.NameToLayer(Define.ZDLayer);

            NavMeshObstacle obstacle = space.gameObject.GetComponent<NavMeshObstacle>();
            if (obstacle)
                obstacle.enabled = enable;

            Collider collider = space.gameObject.GetComponent<Collider>();
            if (collider)
                collider.enabled = enable;
        }
    }
    public void SetActRegion(uint actPos, bool enable)
    {
        string actRegionName = "";
        string outRegionName = "";

        bool success = MasterData.Singleton.GetActRegionName(GameData.Singleton.GetCurSceneID(), actPos, out actRegionName, out outRegionName);
        if (success == false)
            return;

        SetActRegion(actRegionName, enable);
    }

    public virtual void OnClickActResult(int idx, uint npcId)
    {

    }

    protected virtual void _actWaitNormal(byte _enumActPos, int _nRemainCount, int _nPrayIdx)
    {
        // 현재 게임 상태를 의식 대기상태로 설정한다.
        GameData.Singleton.ActivityState = GDActivityState.GDAS_WAITING_TURN;

        int old_remain_count = GameData.Singleton.RemainCount;
        // 아직 자기 순서가 돼지 않았을 경우
        if (_nRemainCount > 0)
        {
            // 대기렬에 처음으로 끼여들거나 기다림 순서가 바뀌였을때
            if ((old_remain_count < 0) || (_nRemainCount < old_remain_count))
            {
                // 대기렬 정보를 갱신하여 준다.
                GameData.Singleton.waitRite(_enumActPos, _nRemainCount);
                string sMsg = "";
                if (_nRemainCount == 1)
                {
                    sMsg = MasterData.Text(CETextID.TXTID_ACTIVITY_NOW_YOUR_TURN);
                }
                else
                {
                    sMsg = Util.ToFormattedString(MasterData.Text(CETextID.TXTID_ACTIVITY_WAIT_PROMPT), _nRemainCount - 1);
                }
                // 갱신된 대기렬 정보를 유저에게 알려준다.
                GameMessage.messageBox(sMsg, WndMessageType.WMT_YES_NO, NotifyActivityWait, "", "等 待", "取 消", "取 消");
            }
        }
        else if (old_remain_count < 0) // 무조건 행사를 시작해야 할 경우 - _nRemainCount <= 0
        {
            startRite(_enumActPos, (uint)_nPrayIdx);
        }
        else // 자기순서가 되였을 때
        {
            /// 대기 순서를 0으로 설정하고
            GameData.Singleton.waitRite(_enumActPos, 0);
            string sMsg = Util.ToFormattedString(MasterData.Text(CETextID.TXTID_ACTIVITY_NOTIFY_READY), _nRemainCount - 1);
            /// 행사 시작 통보문을 유저에게 보여준다.
            // open a message box , ask to user whether wait order or not.
            GameMessage.messageBox(sMsg, WndMessageType.WMT_YES_NO, NotifyActivityReady, "", "", "", "", 5);
        }
    }

    public bool NotifyActivityWait(System.Object obj)
    {
        deleteWaitingMsgWnd(false);

        GameData.Singleton.delChit(GameData.Singleton.getMyFamilyID(), _CHITTYPE.CHITTYPE_WAIT);
        WndMsgEvent msg = (WndMsgEvent)obj;
        if (msg.m_result == WndMessageResult.WMR_YES || msg.m_result == WndMessageResult.WMR_AUTO)
        {

            Chit chit = new Chit();
            chit.type = _CHITTYPE.CHITTYPE_WAIT;
            chit.button_type = TChitButtonType.CHIT_YES_NO;
            chit.nSourceID = GameData.Singleton.getMyFamilyID();
            GameData.Singleton.ActivityState = GDActivityState.GDAS_WAITING_TURN;
            GameData.Singleton.addChit(chit);
        }
        else
        {
            CELog.Log("ScreenRoom NotifyActivityOrder ---------> sendActWaitCancel");
            NetLogic.Singleton.sendActWaitCancel();
            GameData.Singleton.cancelRiteWaiting();
            GameData.Singleton.lockInventory(false);
        }
        return true;
    }
    //pc - screenheaven::m_delegateActivityReady, handleActivityReady
    protected virtual void startRite(byte f_act_pos, uint f_act_subpos = 0)
    {
        CurPrayActPos = f_act_pos;
    }

    protected virtual void releaseRite(byte f_act_pos, bool f_succeed = true)
    {
        NetLogic.Singleton.sendActRelease();
        GameData.Singleton.releaseRite(GameData.Singleton.getMyFamilyID());

        switch (f_act_pos)
        {
            default:
                if (f_act_pos >= (byte)ENUM_ACTPOS_R0.ACTPOS_R0_XIANG_INDOOR_1 && f_act_pos < (byte)ENUM_ACTPOS_R0.ACTPOS_R0_XIANG_OUTDOOR_7)
                    Common.printSysText(MasterData.Text(CETextID.TXTID_WORSHIP_XIANG_END));
                break;
        }

    }
    protected virtual void _setRitTypeToLogic(JingXian_Rite_Type _jrtType)
    {
        switch (_jrtType)
        {
            case JingXian_Rite_Type.JRT_Common_None:
                LogicHeaven.Singleton.CurRiteType = LogicHeaven.RITE_TYPE.NONE;
                break;
            case JingXian_Rite_Type.JRT_Common_Rite:
                LogicHeaven.Singleton.CurRiteType = LogicHeaven.RITE_TYPE.RITE;
                break;
            case JingXian_Rite_Type.JRT_Common_MultiRite:
                LogicHeaven.Singleton.CurRiteType = LogicHeaven.RITE_TYPE.MULTI_RITE;
                break;
            case JingXian_Rite_Type.JRT_XDG_MultiRite:
                LogicHeaven.Singleton.CurRiteType = LogicHeaven.RITE_TYPE.MULTI_XDG_RITE;
                break;
            case JingXian_Rite_Type.JRT_MUSIC_RITE:
                LogicHeaven.Singleton.CurRiteType = LogicHeaven.RITE_TYPE.NONE;
                break;
            case JingXian_Rite_Type.JRT_XDG_MultiRite_SubMap:
                LogicHeaven.Singleton.CurRiteType = LogicHeaven.RITE_TYPE.MULTI_XDG_SUB_RITE;
                break;
            case JingXian_Rite_Type.JRT_XDG_Rite:
                LogicHeaven.Singleton.CurRiteType = LogicHeaven.RITE_TYPE.XDG_RITE;
                break;
            case JingXian_Rite_Type.JRT_XDG_Rite_SubMap:
                LogicHeaven.Singleton.CurRiteType = LogicHeaven.RITE_TYPE.XDG_SUB_RITE;
                break;
            default:
                LogicRoom.Singleton.CurRiteType = LogicHeaven.RITE_TYPE.NONE;
                break;
        }
    }

    public virtual CETextID getRegionName(byte _actPos)
    {
        return CETextID.TXTID_INVALID;
    }
    // ActPos 로부터 ScopeName을 얻어낸다.
    // old : CEScriptLogic.Singleton.SendNotifyToScript("getActRegion", f_act_pos.ToString())
    public virtual string GetScopeNameFromActPos(byte _actPos)
    {
        // CurPrayIndex를 리용해서 region을 갈라내야 한다.
        return string.Empty;
    }
    /// <summary>
    ///  행사 설정 관련 창문들을 닫는다.
    ///  행사 설정 창문이 켜진 상태에서 행사가 시작되는 경우(대기 혹은 집체 행사)
    ///  행사 설정 창문들을 닫는다.
    /// </summary>
    protected void _escapeWindows()
    {

    }

    #region Rite-Related-Internal-Methods
    protected virtual void _closeAllRiteRelatedWindows()
    {
        WndManager.CloseWnd(WndType.WndRiteHostWait);
        WndManager.CloseWnd(WndType.WndRiteJoinerWait);
        //postNotifyEvent(eventChitVisible);
        //postNotifyEvent(eventCloseDialog, 0, 0, WndType::PrepareMultiRite);
    }
    protected void _setMultiRitePlayerShow(bool bShow)
    {
        MultiRiteInfo info = GameData.Singleton.getMultiRiteInfo();

        TJoinInfoForMultiRite.Enumerator itr = info.JoinInfo.GetEnumerator();
        while (itr.MoveNext())
        {
            JoinInfoForMultiRite ji = itr.Current.Value;
            _setPlayerJoinToYiShi(ji.nFID, bShow);
        }

        _setPlayerJoinToYiShi(info.nHostFID, bShow);
    }
    //----------------------------------------------------------------------------------------------------------------------------
    protected void _setPlayerJoinToYiShi(uint f_family_id, bool f_join)
    {
        // 	    uint aid = Common.ConvertFIDToAID(f_family_id);
        // 	    if (aid != 0)
        // 	    {
        ChPlayer player = GameData.Singleton.getChPlayer(f_family_id);
        if (player != null)
        {
            player.SetForceShow(f_join);
            player.EnableRestAni(!f_join);
        }
        // 	    }
    }
    /// <summary>
    /// 행사 시작에 필요한 파라메터들을 설정한다.
    /// </summary>
    protected virtual JingXian_Rite_Type _setYishiSettings()
    {
        YishiSettings yishi_setting = GameData.Singleton.getRiteSettings(GDRiteKind.GDRK_SIMPLERITE) as YishiSettings;
        yishi_setting.m_actIndex = CurPrayIndex;
        {
            // to do - 행사 진행하는 동안 행사 npc가 고인을 향해 돌아서 있는 형상
        }

        /// 왜 여기서 lock해야 하는지 모르겠음
        //GameData.Singleton.lockInventory();
        /// act_pos가 YISHI로 공통인데
        /// 그안에서 진행가능한 행사가 여러가지이다.(일반 천수의식, XianDaGong, 불교구역의식, XianDaGong_subMap, 하늘/땅, 청신대의식)
        /// 그 행사들을 구분하여 알려주어야 한다.
        JingXian_Rite_Type jrt_type = JingXian_Rite_Type.JRT_Common_None;
        if (yishi_setting.m_set.bValid)
        {
            MdsItem md_item = MasterData.Singleton.GetItem(yishi_setting.m_set.id);
            if (md_item != null && (md_item.sub_type == (uint)SACRIF_TYPE.SACRFTYPE_TAOCAN_ROOM_XDG))
            {
                jrt_type = GameData.Singleton.JingXianRiteType;
                if (jrt_type == JingXian_Rite_Type.JRT_Common_None)
                {
                    jrt_type = JingXian_Rite_Type.JRT_XDG_Rite;
                    bool is_sub_xdg = LogicHeaven.Singleton.IsSubMapXDG();
                    if (is_sub_xdg)
                    {
                        jrt_type = JingXian_Rite_Type.JRT_XDG_Rite_SubMap;
                    }
                }
            }
        }
        if (yishi_setting.m_isMusicRite)
        {
            // to do for music rite
            jrt_type = JingXian_Rite_Type.JRT_MUSIC_RITE;
        }
        else if (jrt_type == JingXian_Rite_Type.JRT_Common_None)
        {
            jrt_type = JingXian_Rite_Type.JRT_Common_Rite;
        }

        // set that the names of act results is not visible.
        SetResultNameVisible(false);
        
        IsInRite = true;

        return jrt_type;
    }

    protected virtual string _getYishPrayText()
    {
        string pray_text = "";
        MultiRiteInfo info = GameData.Singleton.multiRiteInfo;

        string sUIFName = GameData.Singleton.getFamilyUIName(info.nHostFID);

        if (GameData.Singleton.getMyFamilyID() == info.nHostFID)
        {
            if (info.sPray.Length > 0)
            {
                pray_text = sUIFName + ":\n";
                pray_text += info.sPray;
                pray_text += "\n\n";
            }
        }
        else
        {
            if (!info.bSecret && info.sPray.Length > 0)
            {
                pray_text = sUIFName + ":\n";
                pray_text += info.sPray;
                pray_text += "\n\n";
            }
        }


        TJoinInfoForMultiRite.Enumerator en = info.JoinInfo.GetEnumerator();
        while (en.MoveNext())
        {
            KeyValuePair<uint, JoinInfoForMultiRite> itr = en.Current;
            if ((itr.Value.bSecret && itr.Value.nFID != GameData.Singleton.getMyFamilyID()) || itr.Value.sPray.Length <= 0)
                continue;
            pray_text += GameData.Singleton.getFamilyUIName(itr.Value.nFID) + ":\n";
            pray_text += itr.Value.sPray;
            pray_text += "\n\n";
        }

        return pray_text;
    }
    protected virtual void _startMultiRite()
    {
        GameData.Singleton.lockInventory();
        MultiRiteInfo info = GameData.Singleton.multiRiteInfo;
        YishiSettings setting = (YishiSettings)(GameData.Singleton.getRiteSettings(GDRiteKind.GDRK_SIMPLERITE));
        setting.m_prayText = _getYishPrayText();
        /// ScriptLogic에  현재 MultiRite의 상태를 알려준다.
        string sParam = "SetMultiRiteState,RiteStart," + info.nHostFID.ToString();
        CEScriptLogic.Singleton.SendNotifyToScript(sParam);
        

		//CEMsgManager.PostMsg(CEMsg.EventSetMultiRiteState, new CEMsgParam().Ints((int)MULTIRITE_STATE.RiteStart).UInts(info.nHostFID));
        //((SceneMain*)m_pMainScene)->setResultNameVisible(false);
        SetResultNameVisible(false);
    }
    protected virtual void _stopYishi()
    {
        releaseRite(m_nCurPrayPos);
        SetResultNameVisible(true);

        IsInRite = false;
    }
    protected virtual void _printStopMultiriteSysText()
    {

    }
    protected virtual void _stopMultiRite(CEMsgParam _mp)
    {
        if (!IsInRite)
            return;

        MultiRiteInfo info = GameData.Singleton.multiRiteInfo;
        _printStopMultiriteSysText();

        TJoinInfoForMultiRite.Enumerator en = info.JoinInfo.GetEnumerator();
        while (en.MoveNext())
        {
            KeyValuePair<uint, JoinInfoForMultiRite> it = en.Current;
            GameData.Singleton.releaseRite(it.Value.nFID);
        }

        SetResultNameVisible(true);

        if (GameData.Singleton.isMultiRiteHost())
            NetLogic.Singleton.sendMultiActRelease();

        GameData.Singleton.releaseRite(GameData.Singleton.getMyFamilyID());
        _setMultiRitePlayerShow(false);
        info.clear();
        IsInRite = false;
    }

    #endregion
    bool NotifyActivityReady(System.Object obj)
    {

        GameData.Singleton.delChit(GameData.Singleton.getMyFamilyID(), _CHITTYPE.CHITTYPE_WAIT);
        WndMsgEvent msg = (WndMsgEvent)obj;
        if (msg.m_result == WndMessageResult.WMR_YES || msg.m_result == WndMessageResult.WMR_AUTO)
        {
            startRite(GameData.Singleton.WaitingActPos, (uint)CurPrayIndex);
        }
        else
        {
            CELog.Log("ScreenRoom NotifyActivityReady ---------> sendActWaitCancel");
            NetLogic.Singleton.sendActWaitCancel();
            GameData.Singleton.cancelRiteWaiting();
        }
        return true;
    }

	public void AddChatBalloon(string msg, uint fid, TChatType type)
	{
		m_chatBalloon.AddChatBalloonMsg(msg, fid, type);
	}
}
