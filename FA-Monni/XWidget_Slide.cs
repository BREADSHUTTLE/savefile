using UnityEngine;
using System.Collections;
using System;

public class XWidget_Slide : MonoBehaviour {

    public enum SlideState { InCallBack, In, SlideIn, OutCallBack, Out, SlideOut }

    private AnimationCurve slideXCurve;
    //public AnimationCurve slideYCurve;

    private SlideState m_curState = SlideState.Out;
    public SlideState CurState {  get { return m_curState; } }

    public Transform slideRoot;

    public float slideStartPos;
    public float slideEndPos;
    public float slideTime;

    private float m_curTime = 0.0f;

    public Action OutStartCallBack = null;
    public Action OutCallBack = null;

    public Action InStartCallBack = null;
    public Action InCallBack = null;

    public GameObject chatIcon = null;
    public GameObject chatIconActive = null;

    public GameObject backIcon = null;
    public GameObject backIconActive = null;


    void Awake()
    {
        if (chatIcon != null)
            chatIcon.SetActive(true);

        if (chatIconActive != null)
            chatIconActive.SetActive(true);

        if (backIcon != null)
            backIcon.SetActive(false);

        if (backIconActive != null)
            backIconActive.SetActive(false);

        BuildSlideCurve();
    }

    void OnDisable()
    {
        if (slideRoot == null)
            slideRoot = gameObject.transform;
        XUnityHelper.SetLocalPositionX(slideRoot, slideXCurve.Evaluate(0f));
        Build(false);
        m_curState = SlideState.Out;
        m_curTime = 0f;
    }

    public void SetSlidePos(float startPos, float endPos)
    {
        slideStartPos = startPos;
        slideEndPos = endPos;
        BuildSlideCurve();
        if (m_curState == SlideState.In)
            XUnityHelper.SetLocalPositionX(slideRoot, slideXCurve.Evaluate(1f));
        else if(m_curState == SlideState.Out)
            XUnityHelper.SetLocalPositionX(slideRoot, slideXCurve.Evaluate(0f));
    }

    public void SetOutStartCallback(Action outStartCallBack)
    {
        OutStartCallBack = outStartCallBack;
    }
    public void SetOutCallback(Action outCallBack)
    {
        OutCallBack = outCallBack;
    }
    public void SetInStartCallback(Action inStartCallBack)
    {
        InStartCallBack = inStartCallBack;
    }
    public void SetInCallback(Action inCallBack)
    {
        InCallBack = inCallBack;
    }

    public void Build(bool slideIn)
    {
        if (chatIcon != null)
            chatIcon.SetActive(!slideIn);
        if (chatIconActive != null)
            chatIconActive.SetActive(!slideIn);

        if (backIcon != null)
            backIcon.SetActive(slideIn);
        if (backIconActive != null)
            backIconActive.SetActive(slideIn);

        if (slideIn)
        {
            m_curState = SlideState.SlideIn;
        }
        else
        {
            m_curState = SlideState.SlideOut;
        }
    }

    private void BuildSlideCurve()
    {
        float interval = slideTime /6;
        AnimationCurve curve = new AnimationCurve(new Keyframe(0f, slideStartPos), new Keyframe(0f+ interval, slideStartPos), new Keyframe(slideTime, slideEndPos), new Keyframe(slideTime + interval, slideEndPos));

        slideXCurve = curve;
    }

    void Update()
    {
        CheckSlide();
    }

    protected void CheckSlide()
    {
        if (m_curState == SlideState.OutCallBack)
        {
            if (OutCallBack != null)
                OutCallBack();

            m_curState = SlideState.Out;
            return;
        }

        if (m_curState == SlideState.InCallBack)
        {
            if (InCallBack != null)
                InCallBack();

            m_curState = SlideState.In;
            return;
        }


        if (m_curState == SlideState.In
            || m_curState == SlideState.Out)
            return;

        

        float lastTime = slideXCurve.keys[slideXCurve.keys.Length - 1].time;
        if (m_curState == SlideState.SlideIn)
        {
            m_curTime += RealTime.deltaTime;
            if (m_curTime > lastTime)
            {
                m_curState = SlideState.InCallBack;
            }
        }
        else if (m_curState == SlideState.SlideOut)
        {
            m_curTime -= RealTime.deltaTime;
            if (m_curTime <= 0.0f)
            {
                m_curState = SlideState.OutCallBack;
            }

        }

        XUnityHelper.SetLocalPositionX(slideRoot, slideXCurve.Evaluate(m_curTime));
    }

    public bool IsOuting()
    {
        return (CurState == SlideState.Out || CurState == SlideState.SlideOut || CurState == SlideState.OutCallBack);
    }


    public bool IsInning()
    {
        return (CurState == SlideState.In || CurState == SlideState.SlideIn || CurState == SlideState.InCallBack);
    }

    public void ToggleSlide()
    {
        if (IsOuting())
        {
            Build(true);

            if (InStartCallBack != null)
                InStartCallBack();
        }
        else if (IsInning())
        {
            Build(false);

            if (OutStartCallBack != null)
                OutStartCallBack();
        }
    }

}
