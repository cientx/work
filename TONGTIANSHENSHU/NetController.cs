#if UNITY_STANDALONE_WIN || UNITY_EDITOR
#define _WINDOWS_
#elif UNITY_ANDROID
#define _MOBILE_
#endif

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class NetController : BaseController
{
	const int NETPLAYER_CHECK_TIME = 500;
	const int TM_SEND_LUCK_REQ_MIN_CYCLE = 300;
	protected ChPlayer m_ChPlayer = null;
// 
// 	private bool m_bReceivedStopPacket = false;
	
	private float m_MoveReceivedTime;
	private float m_MoveDistance;
	private Vector3 m_MoveInitPos;
	private ulong m_LastSendTime = 0;
    private bool m_moveByLocal = false;

	protected override void Awake()
	{
		base.Awake();

		m_ChPlayer = (ChPlayer)m_BasePlayer;
	}

	protected override void Update()
	{
		base.Update();

        //if (ch_state == ChState.Breath && IsReached())
		//	return;
			
		if (m_ChPlayer.MainCh)
			return;

        if (IsMoveControlledByLocal())
            return;

		if (m_controlType == ControlType.NetMove)
		{
            //if (ch_state == ChState.Turning)
            Vector3 position = transform.position;
            Quaternion rotate = transform.rotation;
            if (m_rotating)
            {                
                if (Mathf.Abs(rotate.eulerAngles.y - m_TargetRotate.eulerAngles.y) > 1.0f)
                    transform.rotation = Quaternion.Slerp(rotate, m_TargetRotate, m_RotateSpeed * Time.deltaTime);
                else
                {
                    transform.rotation = m_TargetRotate;

                    //MoveStart = false;
                    m_rotating = false;
                    return;
                }
            }

            //if (ch_state == ChState.Run || ch_state == ChState.Walk || ch_state == ChState.Back || ch_state == ChState.Breath)
            //if (MoveStart == true)
            {
                if (!IsReached())
                {
                    MoveStart = true;    
                    m_rotating = false;
                    //if (ch_state == ChState.Breath)
                    //    ;//ch_state = m_netPreState;

                    Vector3 pos = m_TargetPos;
                    
                    pos.y = position.y;

                    Quaternion rot = rotate;

                    if (!pos.Equals(position))
                    {
                        if (MoveType == ChMoveType.Up)
                            rot = rotate;
                        else if (MoveType != ChMoveType.Back)
                            rot = Quaternion.LookRotation(pos - position);
                        else
                            rot = Quaternion.LookRotation(position - pos);
                    }

                    if (rotate != rot)
                        transform.rotation = Quaternion.Slerp(rotate, rot, m_RotateSpeed * Time.deltaTime);

                    float distCovered = (Time.time - m_MoveReceivedTime) * m_MoveSpeed;
                    float fracJourney = distCovered / m_MoveDistance;
                    
                    transform.position = Vector3.Lerp(m_MoveInitPos, m_TargetPos, fracJourney);
                }
                else if (MoveStart == true)
                {
                    if (m_netState == ChState.Breath)
                    {
                        MoveStart = false;
                    }                    
                    transform.position = m_TargetPos;
                }
                else
                {
                    transform.position = m_TargetPos;
                    if (Mathf.Abs(rotate.eulerAngles.y - m_TargetRotate.eulerAngles.y) > 1.0f)
                        m_rotating = true;
                    else if (m_netState == ChState.Breath)
                    {

                    }
                }
            }
            m_beforePos = position;
		}
	}


	/*public virtual void NetState(Vector3 _pos, float _dir, string _animation, uint _aniState)
	{
		m_controlType = ControlType.NetMove;

		if (!string.IsNullOrEmpty(_animation))
		{
			if (_animation == m_NestStates[(int)ChState.Run])
			{
				m_bReceivedStopPacket = false;
				state = ChState.Run;
				//SetNetState(_animation);
			}
			else if (_animation == m_NestStates[(int)ChState.Walk])
			{
				m_bReceivedStopPacket = false;
				state = ChState.Walk;
			}
			else if (_animation == m_NestStates[(int)ChState.Back])
			{
				m_bReceivedStopPacket = false;
				state = ChState.Back;
			}
			else if (_animation == m_NestStates[(int)ChState.Breath])
			{
				m_bReceivedStopPacket = true;
				m_MoveStopRotate = _dir;
				NetMove(_pos, _dir);
			}
			else if (_animation == m_NestStates[(int)ChState.Turning])
			{
				NetRotate(_pos, _dir);
			}
			else if (_animation == m_NestStates[(int)ChState.Flying])
			{
				transform.position = m_TargetPos = _pos;
				transform.rotation = Quaternion.Euler(new Vector3(0, _dir, 0));

				m_ChPlayer.ChActor.pos_info.pos = transform.position;
				m_ChPlayer.ChActor.pos_info.dir = transform.rotation.eulerAngles.y;
			}
		}
		else
		{
			NetMove(_pos, _dir);
		}
	}*/

	public void NetMove(Vector3 _pos, float _dir = 0.0f)
	{
		m_controlType = ControlType.NetMove;

        switch (m_netState)
        {
            case ChState.Walk:
            case ChState.Run:
            case ChState.Back:                
                //MoveStart = true;                
                break;
            case ChState.Put:
			    transform.position = m_TargetPos = _pos;
			    transform.rotation = Quaternion.Euler(new Vector3(0, _dir, 0));

			    SetActorInfo(transform.position, transform.rotation.eulerAngles.y, m_NetStates[(int)m_netState]);
                break;            
        }

        Vector3 curPos = transform.position;
        Vector3 delta = _pos - curPos;
        float distSq = delta.magnitude;
        m_TargetPos = _pos;
        m_TargetRotate = Quaternion.Euler(new Vector3(0, _dir, 0));

        if (distSq < m_StoppingDistance * m_MoveSpeed)
        {
            transform.position = _pos;
            m_rotating = true;
        }
        else
        {
            m_MoveDistance = Vector3.Distance(_pos, curPos);
            m_MoveReceivedTime = Time.time;
            m_MoveInitPos = curPos;
        }
	}

	public void NetRotate(Vector3 _pos, float _rotate = 0.0f)
	{
		//ch_state = ChState.Turning;
		m_TargetRotate = Quaternion.Euler(new Vector3(0, _rotate, 0));
		m_MoveStopRotate = _rotate;
	}	

	public bool IsMoveControlledByLocal()
	{
		//if (m_controlType == ControlType.NetMove || m_controlType == ControlType.None)
        return m_moveByLocal;

		//return true;
	}	

    public void SetMoveControlledByLocal(bool enable)
    {
        m_moveByLocal = enable;
    }

    // ksh update
    // update content : change the public function to override function
	public override void SendChState()
	{
		if (m_ChPlayer.IsLockSend() || !m_ChPlayer.MainCh)
			return;

		NetLogic.Singleton.sendChState(m_ChPlayer.ChActor.id, 
			m_ChPlayer.ChActor.pos_info.pos.x, 
			m_ChPlayer.ChActor.pos_info.pos.y,
			m_ChPlayer.ChActor.pos_info.pos.z, 
			m_ChPlayer.ChActor.pos_info.dir,
			m_ChPlayer.ChActor.animation,// m_NestStates[(int)state], 			
			m_ChPlayer.ChActor.ani_state,
			m_ChPlayer.ChActor.hold_item);

		m_LastSendTime = (ulong)Common._TM_CPU();
	}

    public void sendShenshuState(string action_name)
    {
        NetLogic.Singleton.sendChState(m_ChPlayer.ChActor.id,
            m_ChPlayer.ChActor.pos_info.pos.x,
            m_ChPlayer.ChActor.pos_info.pos.y,
            m_ChPlayer.ChActor.pos_info.pos.z,
            m_ChPlayer.ChActor.pos_info.dir,
            action_name, 0, 0);
    }

    protected virtual void SendMove(Vector3 _pos)
	{
		if (m_ChPlayer.IsLockSend() || !m_ChPlayer.MainCh)
			return;

		//if (state == ChState.Run || state == ChState.Walk || state == ChState.Flying || state == ChState.Back)
		{
			ulong curTime = (ulong)Common._TM_CPU();
			if (curTime - m_LastSendTime < NETPLAYER_CHECK_TIME)
				return;

            bool isPlayingAnimation = m_ChPlayer.isPlayingAnimationExactly("Idle");
            if (isPlayingAnimation == true && m_ChPlayer.ChActor.animation != m_NetStates[(int)ChState.Breath])
            {
                SetActorInfo(transform.position, transform.rotation.eulerAngles.y, m_NetStates[(int)ChState.Breath]);
                SendChState();
            }

            if (_pos != m_ChPlayer.ChActor.pos_info.pos)
            {
                NetLogic.Singleton.sendMove(m_ChPlayer.ChActor.id, _pos.x, _pos.y, _pos.z);
                m_ChPlayer.ChActor.pos_info.pos = _pos;

                m_LastSendTime = (ulong)Common._TM_CPU();
            }			
		}
	}

	int SetNetState(string _animation)
	{
		//ch_state = ChState.Breath;
        //MoveStart = false;

		for (int i = 0; i < m_NetStates.Length; ++i)
		{
			if (m_NetStates[i] == _animation)
			{
				//ch_state = (ChState)i;
				return i;
			}
		}

		return 0;
	}

	public override void CancelMove()
	{
		base.CancelMove();
// 		m_bReceivedStopPacket = true;
	}

	public override void SetActorInfo(Vector3 pos, float dir, string animation)
	{
		base.SetActorInfo(pos, dir, animation);

		m_ChPlayer.ChActor.pos_info.pos = pos;
		m_ChPlayer.ChActor.pos_info.dir = dir;
		m_ChPlayer.ChActor.animation = animation;
	}

    public override ChMoveType MoveType
    {
        get
        {
            return base.MoveType;
        }
        set
        {
            base.MoveType = value;

            if (m_BasePlayer != null)
                m_BasePlayer.SetAnimatorMoveType(m_moveType);
        }
    }
}
