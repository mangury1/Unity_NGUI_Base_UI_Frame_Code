using System;
using UnityEngine;

/// <summary>
/// 윈도우 기본 클래스 콘텐츠 작업할때 직접적으로 쓰일일은 없다.
/// </summary>
public class WindowUIFormBase : MonoBehaviour
{//WindowUIFormBase

    public event Action<WindowUIFormBase> OnClosed;

    protected UIPanel _uiPlanel = null;

    protected WindowBaseLockBackGroung _baseLockBg = null;

    protected Parameter _openParameter = null;

    /// <summary>
    /// 오픈했는지 체크
    /// </summary>
    protected bool _checkOpen = false;

    /// <summary>
    /// 활성화 상태인지 체크 
    /// </summary>
    protected bool _checkActive = false;

    /// <summary>
    /// 해당 윈도우 타입
    /// </summary>
    public virtual Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.BASE; }
    }

    public GameObject GetGameObject
    {
        get { return this.gameObject; }
    }

    public UIPanel Panel
    {
        get { return _uiPlanel; }
    }

    public bool CheckOpen
    {
        get { return _checkOpen; }
    }

    public bool CheckActive
    {
        get { return _checkActive; }
    }

    public virtual int Depth
    {
        get { return _uiPlanel.depth;  }
        set { _uiPlanel.depth = value; }
    }

    public virtual string SortingLayerName
    {
        get { return _uiPlanel.sortingLayerName; }
        set { _uiPlanel.sortingLayerName = value; }
    }

    public virtual int SortingOrder
    {
        get { return _uiPlanel.sortingOrder; }
        set { _uiPlanel.sortingOrder = value; }
    }

    public virtual int StartingRenderQueue
    {
        get { return _uiPlanel.startingRenderQueue; }
        set { _uiPlanel.startingRenderQueue = value; }
    }

    /// <summary>
    /// 이닛했는지 체크
    /// </summary>
    public bool CheckInitialized
    {
        get;
        protected set;
    }

    /// <summary>
    /// 뒤로가기 버튼으로 윈도우 닫기 중지
    /// </summary>
    public bool CheckEscCloseReturn
    {
        get;
        set;
    }

    protected virtual void Reset()
    {
        SetPlanel();
    }

    private void SetPlanel()
    {
        if(null == _uiPlanel)
            _uiPlanel = this.gameObject.GetComponent<UIPanel>();
    }

    public void SetDepth(int depth, int startingRenderQueue, int sortingOrder)
    {
        Depth = depth;
        StartingRenderQueue = startingRenderQueue;
        SortingOrder = sortingOrder;
    }

    /// <summary>
    /// 이닛 함수 : 여기서 UI 초기 셋팅을 해준다
    /// </summary>
    public virtual void Initialze()
    {
        SetPlanel();
        
        if(null != this.gameObject)
        {
            if(UICamera.mainCamera != null)
            {
                UIAnchor[] uIAnchors = GetComponentsInChildren<UIAnchor>(true);
                for(int i = 0; i < uIAnchors.Length; ++i)
                {
                    if(uIAnchors[i].enabled == false)
                        continue;
                    uIAnchors[i].transform.position = CommonUtil.GetAnchorWorldPos(uIAnchors[i].side);
                }
            }

            CheckEscCloseReturn = true;
            CheckInitialized = true;
        }
        else
        {
            Debug.LogError("__ !!!! Err Null Window GameObject");
        }
    }

    public virtual void Open(Parameter par = null)
    {
        if(null != par)
        {
            if(null != _openParameter)
            {
                _openParameter.Clear();
                _openParameter = null;
            }

            _openParameter = par;
        }

        _checkOpen = true;
        _checkActive = true;

        this.gameObject.SetActive(true);
    }

    public void ClickClose(GameObject gameObject)
    {
        Close();
    }

    public virtual void Close()
    {
        if(null != _openParameter)
        {
            _openParameter.Clear();
            _openParameter = null;
        }

        _checkOpen = false;
        _checkActive = false;

        this.gameObject.SetActive(false);
        OnClosed?.Invoke(this);
        //GameUIManager.instance.PreWindowOpen(this.gameObject.name);
    }

    /// <summary>
    /// 강제로 모든 윈도우가 닫힐때 사용하는 함수 : 초기화나 삭제가 필요한 데이터들만 처리 해준다.
    /// </summary>
    public virtual void ForciblyClose()
    {
        if (null != _openParameter)
        {
            _openParameter.Clear();
            _openParameter = null;
        }

        _checkOpen = false;
        _checkActive = false;

        this.gameObject.SetActive(false);
    }

    /// <summary>
    ///  뒤로가기 버튼으로 UI를 닫을 경우 Close 함수 호출로는 별개로 처리해야할 코드가 있을때 사용 
    /// </summary>
    public virtual void EscapeClose()
    {

    }

    public virtual void Release()
    {
        if(null != _uiPlanel)
            _uiPlanel = null;

        if(null != _openParameter)
        {
            _openParameter.Clear();
            _openParameter = null;
        }

        if(null != _baseLockBg)
        {
            _baseLockBg.Release();
            GameObject.Destroy(_baseLockBg);
            _baseLockBg = null;
            OnClosed = null;
        }
    }

    /// <summary>
    /// 윈도우가 활성화 될때
    /// </summary>
    protected virtual void ActivateOn() { _checkActive = true; }

    /// <summary>
    /// 윈도우가 비활성화 될때
    /// </summary>
    protected virtual void ActivateOff() { _checkActive = false; }

    public void Activate(bool isOn)
    {
        if(true == isOn)
        {
            ActivateOn();
        }
        else
        {
            ActivateOff();
        }
    }

    protected void BaseLockBgSetActive(bool state)
    {
        if(null != _baseLockBg /*&& state != _baseLockBg.activeInHierarchy*/)
        {
            _baseLockBg.Activate(state);
        }
    }

    public void DestroyGameObject()
    {
        if(true == Application.isPlaying)
        {
            Destroy(this.gameObject);
        }
        else
        {
            DestroyImmediate(this.gameObject);
        }
    }

    public bool OpenParContainsKey(string key)
    {
        if(null != _openParameter)
            return _openParameter.ContainsKey(key);
        else
            return false;
    }
    
    protected void CreateBaseLockBg(bool checkWindow)
    {
        if(null == _baseLockBg)
        {
            GameObject obj = Instantiate(Resources.Load<GameObject>(string.Format("{0}{1}", Constant.WINDOW_PREFAD_PATH, UICommon.OBJ_LOCK_BACK_GROUND)));
            obj.transform.SetParent(GetGameObject.transform);
            obj.name = string.Format("{0}_BaseLockBg", this.gameObject.name);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;

            _baseLockBg = obj.GetComponent<WindowBaseLockBackGroung>();
            _baseLockBg.Initialze(checkWindow);
        }
    }

    /// <summary>
    /// 배경 활성화 함수
    /// </summary>
    protected void ActiveBaseLockBg_MoveBg(bool setBaseActive, bool moveBgActive = true)
    {
        if(null != _baseLockBg)
        {
            _baseLockBg.ActiveBg(setBaseActive);
            _baseLockBg.ActiveMoveBg(moveBgActive);
        }
    }


    /// <summary>
    /// 버튼 딜리게이트 : 개인적으로 이 버튼 이벤트를 선호함 안쓰이면 삭제
    /// </summary>
    /// <param name="obj">버튼이 되는 오브젝트</param>
    /// <param name="click">실행 함수</param>
    /// <param name="par">인자값</param>
    protected void OnClickEventListener(GameObject obj, UIEventListener.VoidDelegate click, object par = null)
    {
        UIEventListener.Get(obj).soundType = Constant.BUTTON_SOUND_TYPE.BASE;
        UIEventListener.Get(obj).onClick = click;
        UIEventListener.Get(obj).parameter = par;
    }

    protected void OnClickEventListener(UIEventListener obj, UIEventListener.VoidDelegate click, object par = null)
    {
        obj.onClick = click;
        obj.parameter = par;
    }

    /// <summary>
    /// 버튼 딜리게이트 : 사운드 타입 추가
    /// </summary>
    protected void OnClickEventListener(Constant.BUTTON_SOUND_TYPE soundType, GameObject obj, UIEventListener.VoidDelegate click, object par = null)
    {
        UIEventListener.Get(obj).soundType = soundType;
        UIEventListener.Get(obj).onClick = click;
        UIEventListener.Get(obj).parameter = par;
    }

    protected void OnClickEventListener(Constant.BUTTON_SOUND_TYPE soundType, UIEventListener obj, UIEventListener.VoidDelegate click, object par = null)
    {
        obj.soundType = soundType;
        obj.onClick = click;
        obj.parameter = par;
    }

    protected void OnDragEventListener(GameObject obj, UIEventListener.VectorDelegate drag)
    {
        UIEventListener.Get(obj).onDrag = drag;
    }

    protected void OnPressEventListener(GameObject obj, UIEventListener.BoolDelegate press)
    {
        UIEventListener.Get(obj).onPress = press;
    }

    protected void OnPressEventListener(UIEventListener obj, UIEventListener.BoolDelegate press)
    {
        obj.onPress = press;
    }

    protected void OnHoverEventListener(GameObject obj, UIEventListener.BoolDelegate press)
    {
        UIEventListener.Get(obj).onHover = press;
    }

    protected void OnHoverEventListener(UIEventListener obj, UIEventListener.BoolDelegate press)
    {
        obj.onHover = press;
    }

    protected void SetBtnParameter(GameObject obj, object setPar)
    {
        UIEventListener.Get(obj).parameter = setPar;
    }

    protected object GetBtnParameter(GameObject obj)
    {
        return UIEventListener.Get(obj).parameter;
    }

    /// <summary>
    /// 업데이트 준비중 팝업.
    /// </summary>
    protected void UpdatePopupMessage()
    {
        Parameter openData = new Parameter();
        openData["PopUpType"] = Constant.MESSAGE_POPUP_TYPE.ONE_BTN;
        openData["YesButtonText"] = MessageDataManager.Instance.GetStringByIdx(300043);
        openData["MessageText"] = MessageDataManager.Instance.GetStringByIdx(10004);
        GameUIManager.instance.OpenWindow(UICommon.W_COMMON_MESSAGE, openData);
    }
}//WindowUIFormBase

/// <summary>
/// 튜토리얼 윈도우.
/// </summary>
public class TutorialWindowUIForm : WindowUIFormBase
{//TutorialWindowUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.TUTORIAL_WINDOW; }
    }

    protected override void ActivateOn()
    {
        gameObject.SetActive(true);
        base.ActivateOn();
    }

    protected override void ActivateOff()
    {
        gameObject.SetActive(false);
        base.ActivateOff();
    }

    protected override void Reset()
    {
        base.Reset();
    }

    public override void Close()
    {
        base.Close();
    }
}//TutorialWindowUIForm

/// <summary>
/// 고정 윈도우
/// </summary>
public class FittedWindowUIForm : WindowUIFormBase
{//FittedWindowUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.FITTED_WINDOW; }
    }

    protected override void ActivateOn()
    {
        this.gameObject.SetActive(true);
        base.ActivateOn();
    }

    protected override void ActivateOff()
    {
        this.gameObject.SetActive(false);
        base.ActivateOff();
    }

    public virtual void UpdateWindow()
    {

    }
}//FittedWindowUIForm

/// <summary>
/// 일반적인 윈도우
/// </summary>
public class WindowUIForm : WindowUIFormBase
{//WindowUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.WINDOW; }
    }

    public override void Initialze()
    {
        CreateBaseLockBg(true);
        base.Initialze();
    }

    public override void Close()
    {
        base.Close();
    }

    protected override void ActivateOn()
    {
        this.gameObject.SetActive(true);
        base.ActivateOn();
    }

    protected override void ActivateOff()
    {
        this.gameObject.SetActive(false);
        base.ActivateOff();
    }
}//WindowUIForm

/// <summary>
/// 팝업
/// </summary>
public class PopUpUIForm : WindowUIFormBase
{//PopUpUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.POPUP_WINDOW; }
    }

    public override void Initialze()
    {
        CreateBaseLockBg(false);
        base.Initialze();
    }

    public override void Close()
    {
        this.gameObject.SetActive(false);
        base.Close();
    }
}//PopUpUIForm

/// <summary>
/// 다른 팝업이 열려도 그대로 오픈 상태를 유지 하는 팝업
/// </summary>
public class ActiveOnPopUpUIForm : WindowUIFormBase
{//ActiveOnPopUpUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.ACTIVEON_POPUP_WINDOW; }
    }

    public override void Initialze()
    {
        CreateBaseLockBg(false);
        base.Initialze();
    }

    public override void Close()
    {
        this.gameObject.SetActive(false);
        base.Close();
    }
}//ActiveOnPopUpUIForm

/// <summary>
/// 툴팁 
/// </summary>
public class ToolTipUIForm : WindowUIFormBase
{//ToolTipUIForm

    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.TOOLTIP_WINDOW; }
    }

    /// <summary>
    /// 특정상황에서 터치로 닫기를 막는 변수
    /// </summary>
    protected bool _checkTouchClose = true;

    /// <summary>
    /// 툴팁을 닫을때 실행해야할 이벤트 등록
    /// </summary>
    protected Action _closeAction = null;

    public override void Open(Parameter par = null)
    {
        _checkTouchClose = true;
        base.Open(par);

        if(null != _openParameter)
        {
            if(true == _openParameter.ContainsKey("CloseAction"))
                _closeAction = _openParameter["CloseAction"] as Action;
        }

        UICamera.onClick += OnClickClose;
    }

    public override void Close()
    {
        UICamera.onClick -= OnClickClose;

        if(null != _closeAction)
        {
            _closeAction();
            _closeAction = null;
        }

        base.Close();

        //this.gameObject.SetActive(false);
        //GameUIManager.instance.OpenWindowListRemove(this.gameObject.name);
    }

    //protected void LateUpdate()
    //{
    //    try
    //    {
    //        TouchClose();
    //    }
    //    catch(System.Exception e)
    //    {
    //        Debug.LogError(e.ToString());
    //    }
    //}

    /// <summary>
    /// 터치 이벤트 발생시 UI 닫기
    /// </summary>
//    protected void TouchClose()
//    {
//        if(_checkOpen && _checkActive && CheckInitialized)
//        {
//            if(true == _checkTouchClose)
//            {
//#if UNITY_EDITOR || UNITY_STANDALONE
//                if(Input.GetMouseButton(0) || Input.GetMouseButton(1))
//                {
//                    Close();
//                }
//#else
//            if(Input.touchCount > 0)
//            {
//                Close();
//            }
//#endif
//            }
//        }
//    }

    protected void OnClickClose(GameObject obj)
    {
       if(_checkOpen && _checkActive && _checkTouchClose)
            Close();
    }
}//ToolTipUIForm

/// <summary>
/// 일반 윈도우 안에 있는 UI오브젝트에 WindowUIFormBase 기능을 쓰기 위한 클래스 : 타입도 지정해 주지 않음
/// </summary>
public class SubWindowUIForm : WindowUIFormBase
{//SubWindowUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.NONE; }
    }

    protected int _depth = 0;
    protected string _sortingLayerName = string.Empty;
    protected int _sortingOrder = 0;
    protected int _startingRenderQueue = 0;

    public override int Depth
    {
        get { return null != _uiPlanel ? _uiPlanel.depth : _depth; }
        set { if(null != _uiPlanel) _uiPlanel.depth = value; else _depth = value; }
    }

    public override string SortingLayerName
    {
        get { return null != _uiPlanel ? _uiPlanel.sortingLayerName : _sortingLayerName; }
        set { if(null != _uiPlanel) _uiPlanel.sortingLayerName = value; else _sortingLayerName = value; }
    }

    public override int SortingOrder
    {
        get { return null != _uiPlanel ? _uiPlanel.sortingOrder : _sortingOrder; }
        set { if(null != _uiPlanel) _uiPlanel.sortingOrder = value; else _sortingOrder = value; }
    }

    public override int StartingRenderQueue
    {
        get { return null != _uiPlanel ? _uiPlanel.startingRenderQueue : _startingRenderQueue; }
        set { if(null != _uiPlanel) _uiPlanel.startingRenderQueue = value; else _startingRenderQueue = value; }
    }

    public override void Close()
    {
        if(null != _openParameter)
        {
            _openParameter.Clear();
            _openParameter = null;
        }

        _checkOpen = false;
        _checkActive = false;

        this.gameObject.SetActive(false);
    }

    protected override void ActivateOn()
    {
        this.gameObject.SetActive(true);
        base.ActivateOn();
    }

    protected override void ActivateOff()
    {
        this.gameObject.SetActive(false);
        base.ActivateOff();
    }

    public override void Open(Parameter par = null)
    {
        if(null != par)
        {
            if(null != _openParameter)
            {
                _openParameter.Clear();
                _openParameter = null;
            }

            _openParameter = par;
        }

        _checkOpen = true;
        _checkActive = true;

        this.gameObject.SetActive(true);
    }

    public virtual void SubWIndClose()
    {
        if(null != _openParameter)
        {
            _openParameter.Clear();
            _openParameter = null;
        }

        _checkOpen = false;

        this.gameObject.SetActive(false);
    }

    public virtual void UpdateWindow()
    {

    }
}//SubWindowUIForm
