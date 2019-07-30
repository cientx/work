#if UNITY_STANDALONE_WIN || UNITY_EDITOR
#define _WINDOWS_
#elif UNITY_ANDROID
#define _MOBILE_
#endif

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ChAnimator))]
[RequireComponent(typeof(ChController))]
//[RequireComponent(typeof(LODGroup))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CapsuleCollider))]
public class ChPlayer : NetPlayer
{

    #region private member
    ClickAgent m_clickAgent = null;
    #endregion

	#region Override MonoBehavior
    protected override void Awake()
    {
        if (m_hideDistRatio < 0.0f)
            m_hideDistRatio = 0.03f; // 반드시 base.Awake()전에 값을 설정해야 함. BasePlayer.Awake()에서 이 값에 의해 LOD를 재설정하기 때문

        base.Awake();
    }

	// Update is called once per frame
	protected override void Update()
	{
		base.Update();

		if (MainCh)
        {
            PlayRestAni(Time.deltaTime);


        }
		//m_Effects.Update();

		// test code for character rotate
		//if (IsRotating)
		//	CELog.Log(IsRotating.ToString());

		//if (isPlayingAnimation("Fish_Feed"))
		//	CELog.Log("Animation");

		// test code end

	}
    bool CheckOtherPlayerClickable()
    {
        if (MainCh)
            return false;

        ChPlayer ctrlPlayer = GameData.Singleton.getMyChPlayer();
        float dist = Vector3.Distance(ctrlPlayer.transform.position, transform.position);

        bool bClickable = dist < Define.VALID_SELECT_DISTANCE;
        m_clickAgent.Transparent = !bClickable;

        return bClickable;
    }
    void ClickedPlayer(Collider collider)
    {
        if (LogicHeaven.Singleton.ActivityState == GDActivityState.GDAS_ACTING
            && CEScriptLogic.Singleton.SendNotifyToScript("isPlayingPlayerAction") == "0")
            return;

        Player player = GameData.Singleton.getPlayer(FamilyID);
        if (player.bDelegate)
            return;

        GameData.Singleton.getMyChPlayer().CancelMove();
        CEMsgManager.PostMsg(CEMsg.EventClickPlayer, new CEMsgParam().Ints((int)FamilyID));
    }

    void RightClickedPlayer(Collider collider)
    {
        if (LogicHeaven.Singleton.ActivityState == GDActivityState.GDAS_ACTING
            && CEScriptLogic.Singleton.SendNotifyToScript("isPlayingPlayerAction") == "0")
            return;

        Player player = GameData.Singleton.getPlayer(FamilyID);
        if (player.bDelegate)
            return;

        FamilyPopup.ShowFamilyPopup(FamilyID, Input.mousePosition, true, true);
    }
    #endregion Override MonoBehavior

    public void InitPlayer(bool _bMain, Player _player)
	{
		base.Init(_player.nFamilyID);

        m_forceShow = false;

		m_ChController = GetComponent<ChController>();

		MainCh = _bMain;
		Player = _player;

        m_clickAgent = ClickAgent.GetClickAgent(gameObject, null, CursorMgr.CursorType.NPC_Pointer);
        m_clickAgent.checkClick = CheckOtherPlayerClickable;
        m_clickAgent.leftClick = ClickedPlayer;
		m_clickAgent.rightClick = RightClickedPlayer;
        if (MainCh)
            m_clickAgent.Transparent = true;

        uint nModelID = 0;

		if (_player != null)
		{
			//foreach (KeyValuePair<uint, Actor> pair in _player.chs)
			Dictionary<uint, Actor>.Enumerator enumerator = _player.chs.GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<uint, Actor> pair = enumerator.Current;

				ChActor = pair.Value;
                nModelID = pair.Value.nModelID;
				break;
			}

            // inserted by ksh
            string fullName = string.Empty;
            if (Player.bDelegate)
            {
                fullName = GameData.Singleton.getFamilyUIName(Player.nDelegatedID);
                fullName += MasterData.Text(CETextID.TXTID_ACT_DELEGATE_POSTFIX);
            }
            else if (Player.nCloneFamilyID != 0)
                fullName = GameData.Singleton.getFamilyUIName(Player.nCloneFamilyID);
            else
                fullName = GameData.Singleton.getFamilyUIName(Player.nFamilyID);

            if (Player.nFamilyID == GameData.Singleton.getMyFamilyID())
            {
                if (GameData.Singleton.getMyFamily().isSysUser)
                    DrawText(0, fullName, new Color(0.5f, 0.5f, 1.0f, 1.0f));
                else
                    DrawText(0, fullName, Common.GetPlayerLevelColor(Player.level));

                DrawTextLabelIcon(0, Player.level);
            }
            else
            {
                uint family_id = Player.bDelegate ? Player.nDelegatedID : Player.nFamilyID;
                if (GameData.Singleton.isSysFamily(family_id))
                    DrawText(0, fullName, new Color(0.5f, 0.5f, 1.0f, 1.0f));
                else
                    DrawText(0, fullName, Common.GetPlayerLevelColor(Player.level));

                DrawTextLabelIcon(0, Player.level);
            }
            // end

            if (MainCh)
                m_ChController.InitMoveSound(MasterData.Singleton.m_dicMoveSounds[nModelID]);
		}
		//else
		//	CELog.Log("ChPlayer.InitPlayer() player is null.", LogType.Error);
		

		if (_bMain)
        {
            SetJoystick();
            m_ChController.InitMoveMark();
            m_ChController.MoveType = ChMoveType.Run;

            HideDistRatio = 0.0f;
        }

		EnableCharacterCtrl(_bMain);
		EnableCapsuleCollider(!_bMain);
	}

    public override void DrawText(int _index, string _text, Color _color)
    {
        base.DrawText(_index, _text, _color);

        CE3DtextLabel text = m_textLabels[_index];
        if (text != null)
        {
            text.SetChTextType();
            text.SetTextH(_text);
            //text.SetFont(ResourceManager.Singleton.GetFont());
            text.SetTextColor(_color);
        }
    }

    protected override void _updateTextOnCh(SHOW_TEXT_CH_STATE _showState = SHOW_TEXT_CH_STATE.STC_NONE)
    {
        if (m_textPoint == null || m_objForPos == null || m_textLabels.Count == 0)
            return;

        // update position state
        float offset = m_textPoint.position.y - m_objForPos.position.y;
        if (offset < 0 || offset > m_offset)
            m_textLabels[0].transform.position = m_objForPos.position;
        else
            m_textLabels[0].transform.position = m_textPoint.position;

        // update show state
        GameObject obj = null;
        if (m_textPoint != null)
            obj = m_textPoint.gameObject;
        else
            obj = m_textLabels[0].gameObject;

        bool show = m_bShow;
        switch (_showState)
        {
            case SHOW_TEXT_CH_STATE.STC_ALL_HIDE:
                show = false; break;
            case SHOW_TEXT_CH_STATE.STC_ONLY_OWN_SHOW:
                show = m_MainCh; break;
        }

        if (obj.activeSelf != show)
            ShowText(show);
    }

    public override void MovePlayer(Vector3 _targetPos, ChMoveType _moveType)
	{
        if (_moveType == ChMoveType.Flying)
            m_ChController.EnableGravity(false);
		base.MovePlayer(_targetPos, _moveType);
	}

    public override void MovePlayerToDeltaPos(Vector3 _deltaPos, ChMoveType _moveType)
    {
        base.MovePlayerToDeltaPos(_deltaPos, _moveType);
    }

    public override uint getHoldItemID()
    {
        return m_Actor.hold_item;
    }
    public override MdsEffect getEffect(uint _effectID)
    {
        MasterData master_data = MasterData.Singleton;

        MdtMapEffect effects = master_data.GetChStateEffect(ModelType.MT_HERO);
        if (effects != null && effects.ContainsKey((CH_EFFECT)_effectID))
        {
            return effects[(CH_EFFECT)_effectID];
        }
        return null;
    }

    public void DoCameraTour(Vector3 _targetPos, Vector3 _rotation,
        Common.CallBackFunc _OutCallBack = null, Common.CallBackFunc _InCallBack = null, bool bNotFade = false)
    {
        m_ChController.DoCameraTour(_targetPos, _rotation, _OutCallBack, _InCallBack, bNotFade);
    }

	public override void MovePlayerAndRotate(Vector3 _targetPos, Vector3 _rotation, ChMoveType _moveType)
	{
        base.MovePlayerAndRotate(_targetPos, _rotation, _moveType);
	}

    public override void MovePlayerAndRotate(Vector3[] _targetPos, Vector3 _rotation,
        ChMoveType _moveType, Common.CallBackFunc _callBack = null)
    {
        base.MovePlayerAndRotate(_targetPos, _rotation, _moveType, _callBack);
    }

    public override void MovePlayerAndRotateAtOnce(Vector3 _targetPos, Vector3 _rotation)
    {
        base.MovePlayerAndRotateAtOnce(_targetPos, _rotation);
    }

	// Move by NavPathFinding...
	public void MoveByNavPathFinding(Vector3 _pos)
	{
		m_ChController.MoveByNavPathFinding(_pos);
	}

	public Vector3[] GetCalculatePath(Vector3 _targetPos)
	{
		return m_ChController.GetCalculatePath(_targetPos);
	}
	public void JoystickMove(Vector3 delta)
	{        
	    m_ChController.JoystickMove(delta);
	}

    public void ShowMoveMark(Vector3 position)
    {
        m_ChController.ShowMoveMark(position);
    }

    public void ClickOtherCollider(Vector3 target)
    {
        m_ChController.ClickOtherCollider(target);
    }

    public void ChangeFromCameraCross(Vector3 _target, Vector3 _rotate)
    {
        m_ChController.MovePlayerAndRotateAtOnce(_target, _rotate);
    }

    

	public void StartJoystickMove()
	{
   
        m_ChController.StartJoystickMove();
	}

	public void StopJoystickMove()
	{
        if (m_ChController.MoveStart)
		    m_ChController.StopJoystickMove();
	}

    public override void CancelMove()
    {
        if (m_ChController.MoveStart)
            m_ChController.CancelMove();
    }
	public void LockSendPacket(bool _lock)
	{
		if (MainCh)
			m_lockSend = _lock;
	}

    public void SetMoveControlledByLocal(bool enable)
    {
        m_ChController.SetMoveControlledByLocal(enable);
    }

	public bool IsLockSend()
	{
		if (!GameData.Singleton.IsInGameState(CEMacro.GS_MASK_HEAVEN))
			return true;

		return m_lockSend;
	}

    public override void PlayAnimation(string _animation, bool bNeedCrossFade = false, float crossTime = 0.15f, int layer = 0)
	{
        base.PlayAnimation(_animation, bNeedCrossFade, crossTime, layer);
				
		m_Actor.animation = _animation;
		if (m_ChController)
			m_ChController.SendChState();
	}

	public void PlayEffect(string _animation)
	{


	}

	public override List<GameObject> AttachItem(uint _itemId)
	{
		List<GameObject> attachedItems = base.AttachItem(_itemId);

		m_Actor.hold_item = _itemId;
		m_Actor.animation = "";
		m_Actor.ani_state = 1;

		if (m_ChController)
			m_ChController.SendChState();

        if (MainCh)
            m_Actor.ani_state = m_ChAnimator.IsEnableEvent() ? (uint)0 : (uint)1;

		return attachedItems;
	}	

	private bool SetJoystick()
	{
		TouchManager.Singleton.SetPlayer(this);

		return true;
	}	
    
	public void EnableCharacterCtrl(bool _enable)
	{
		if (m_ChController)
			m_ChController.EnableCharacterCtrl(_enable);
	}

	public void EnableCapsuleCollider(bool _enable)
	{
		if (m_ChController)
			m_ChController.EnableCapsuleCollider(_enable);
	}

    public void EnableGravity(bool _enable)
    {
        if (m_ChController)
            m_ChController.EnableGravity(_enable);
    }

    public override void EnableEvent(bool _enable)
    {
        base.EnableEvent(_enable);

        //m_ChAnimator.EnableEvent(_enable);
        ChActor.ani_state = _enable ? (uint)0 : (uint)1;
    }

	public Player Player
	{
		get { return m_Player; }
		set { m_Player = value; }
	}

	public Actor ChActor
	{
		get { return m_Actor; }
		set { m_Actor = value; }
	}

    public void PutItem()
    {
		if (m_isNpcPlayer)
		{
			CEScriptLogic.Singleton.SendNotifyToScript("PutNpcPlayerItem," + FamilyID.ToString() + "," + m_npcName);
		}
		else
			CEScriptLogic.Singleton.SendNotifyToScript("PutItem," + m_Player.nFamilyID.ToString() + "," + m_Actor.hold_item.ToString());
    }

    public void SetForceShow(bool _show)
    {
        m_forceShow = _show;
    }

    public void RotateTo(Vector3 rot)
    {
        m_ChController.RotateTo(rot);
    }

    public void SendChState()
    {
        m_ChController.SendChState();
    }

    public void sendShenshuState(string action_name)
    {
        m_ChController.sendShenshuState(action_name);
    }
    public void SendMoveForce()
    {
        if (m_ChController)
            m_ChController.SendMoveForce();
    }

    public bool EnableClickMove
    {
        get
        {
            return m_ChController.EnableClickMove;
        }

        set
        {
            m_ChController.EnableClickMove = value;
        }
    }
}
