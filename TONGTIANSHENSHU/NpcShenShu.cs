using UnityEngine;
using System.Collections;

public class NpcShenShu : NpcBase 
{
    protected int m_nEffectID = 0;
#if UNITY_EDITOR
    public override void ResetCommonValues()
    {
        SetRegionAsDistance(15.0f);
        m_ActionType = ActionType.Just;
    }
#endif
    //public GameObject m_effObj;              
    #region        liuxin---增加
    public GameObject[] m_effObjs=new GameObject[4];
    #endregion
    protected void OnEnable()
    {
        CEEffect.RegDelegate(EFFECT_DELEGATE_MSG.TONGTIANSHENSHU, EffectDisappear);
    }
    protected void OnDisable()
    {
        CEEffect.RmvDelegate(EFFECT_DELEGATE_MSG.TONGTIANSHENSHU, EffectDisappear);
        
    }
    protected override void PlayNpcAction()
    {
        base.PlayNpcAction();
        #region   liuxin---修改
        showEffect();
        #endregion
        //m_nEffectID = CEEffect.DoEffect(m_effObj, m_Target.transform);  
        MainChPlayer.sendShenshuState(transform.GetComponent<CENode>().m_nodeName);
    }
    public bool EffectDisappear(EFFECT_DELEGATE_TYPE f_type, int _id)
    {
        if (_id == m_nEffectID)
        {
            m_nEffectID = 0;
        }
        return true;
    }

    // 특정NPC들이 클릭할수 있는가를 판정하는 재정의가능한 함수.
    // 일반동작을 한다면 재정의 할 필요가 없고 특수한 조건판단을 해야 하는 경우 재정의한다.
    protected override bool CheckCanClick()
    {
        if (m_nEffectID != 0)
            return false;
        
        return base.CheckCanClick();
    }

    public void showEffect()
    {
        switch (transform.GetComponent<CENode>().m_nodeName)
        {
            case "ShenshuNode_Left_1":
                m_nEffectID = CEEffect.DoEffect(m_effObjs[0], m_Target.transform);
                break;
            case "ShenshuNode_Right_1":
                m_nEffectID = CEEffect.DoEffect(m_effObjs[1], m_Target.transform);
                break;
            case "ShenshuNode_2":
                m_nEffectID = CEEffect.DoEffect(m_effObjs[2], m_Target.transform);
                break;
            case "ShenshuNode_3":
                m_nEffectID = CEEffect.DoEffect(m_effObjs[3], m_Target.transform);
                break;
            default:
                break;
        }
    }
}
