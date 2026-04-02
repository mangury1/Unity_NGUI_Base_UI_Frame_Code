using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 윈도우 관리 메니저 해당 씬에 상속 받아서 쓴다.
/// </summary>
public abstract class BaseUIManager<T> : MonoBehaviour where T : BaseUIManager<T>
{
    private static T _instance;

    /// <summary>
    /// The static reference to the instance
    /// </summary>
    public static T instance
    {
        get
        {
            if(null != _instance)
            {
                if(null == _instance._loadPrefadsList)
                    _instance._loadPrefadsList = new Dictionary<string, GameObject>();
                if(null == _instance._windowList)
                    _instance._windowList = new Dictionary<string, WindowUIFormBase>();
                if(null == _instance._openWindowList)
                    _instance._openWindowList = new List<string>();
            }
            
            return _instance;
        }
        protected set
        {
            _instance = value;
        }
    }

    /// <summary>
    /// Gets whether an instance of this singleton exists
    /// </summary>
    public static bool InstanceExists { get { return instance != null; } }

    public static event Action InstanceSet;

    [SerializeField]
    protected UIRoot _uiRoot = null;

    /// <summary>
    /// UI 카메라 배열
    /// </summary>
    [SerializeField]
    protected Camera[] _uiCameraArry = null;

    /// <summary>
    /// 불러온 프리팹 리스트
    /// </summary>
    protected Dictionary<string, GameObject> _loadPrefadsList = new Dictionary<string, GameObject>();

    /// <summary>
    /// 윈도우 리스트
    /// </summary>
    protected Dictionary<string, WindowUIFormBase> _windowList = new Dictionary<string, WindowUIFormBase>();

    /// <summary>
    /// 현재 열려있는 윈도우 이름
    /// </summary>
    protected List<string> _openWindowList = new List<string>();
    public Dictionary<string, WindowUIFormBase> GetWindowList
    {
        get { return this._windowList; }
    }

    public int OpenWindowListCount
    {
        get
        {
            if(null != _openWindowList)
                return _openWindowList.Count;
            else
                return 0;
        }
    }

    /// <summary>
    /// 뎁스 OffSet 값.
    /// </summary>
    private const int DEPTH_OFFSET = 100;

    // 튜토리얼 진행중 확인.
    protected bool _isTutorial = false;

    protected virtual void Awake()
    {
        try
        {
            if(instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = (T)this;
                if(InstanceSet != null)
                {
                    InstanceSet();
                }
            }
        }
        catch(System.Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    protected virtual void Start()
    {
        try
        {
            Resources.UnloadUnusedAssets();
        }
        catch(System.Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    protected virtual void OnDestroy()
    {
        ClearWindow();
    }

    protected virtual void LateUpdate()
    {

    }

    /// <summary>
    /// 게임 종료 확인팝업.
    /// </summary>
    protected void PopupAppQuit()
    {
        Parameter openData = new Parameter();
        openData["PopUpType"] = Constant.MESSAGE_POPUP_TYPE.TWO_BTN;
        openData["MessageText"] = MessageDataManager.Instance.GetStringByIdx(1700018);
        openData["NoButtonText"] = MessageDataManager.Instance.GetStringByIdx(300045);
        openData["YesButtonText"] = MessageDataManager.Instance.GetStringByIdx(300043);
        openData["RightButtonAction"] = new System.Action(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
        OpenWindow(UICommon.W_COMMON_MESSAGE, openData);
    }

    /// <summary>
    /// 데이터 삭제
    /// </summary>
    public virtual void ClearWindow()
    {
        DestroyWindow();
        this._loadPrefadsList.Clear();
        this._loadPrefadsList = null;

        this._windowList = null;
        this._openWindowList = null;
    }

    /// <summary>
    /// 로드한 클론 오브젝트 삭제
    /// </summary>
    public virtual void DestroyWindow()
    {
        if(null != this._windowList)
        {
            var windowDic = this._windowList.GetEnumerator();
            while(true == windowDic.MoveNext())
            {
                WindowUIFormBase uiForm = windowDic.Current.Value;
                if(uiForm.WindowType == Constant.WINDOW_TYPE.TUTORIAL_WINDOW)
                    continue;

                uiForm.Release();
                uiForm.DestroyGameObject();
                uiForm = null;
            }

            this._windowList.Clear();
        }

        if(null != this._openWindowList)
            this._openWindowList.Clear();
    }

    public void ActiveFittedWindowUIForm(bool isActive)
    {
        var windowDic = _openWindowList.GetEnumerator();
        for(int i = 0; i < _openWindowList.Count; ++i)
        {
            string name = _openWindowList[i];
            if(_windowList[name] is FittedWindowUIForm)
                _windowList[name].Activate(isActive);
        }
        ActiveFittedWindowUIFormAdd(isActive);
    }

    /// <summary>
    /// 고정UI 끌때 같이 끌 UI
    /// </summary>
    /// <param name="isActive"></param>
    protected abstract void ActiveFittedWindowUIFormAdd(bool isActive);
    /// <summary>
    /// 프리팹 불러오기
    /// </summary>
    /// <param name="prdfadName"></param>
    /// <returns></returns>
    protected GameObject LoadPrefad(string prefadName)
    {//1
        GameObject loadPrdfad = null;

        if(false == this._loadPrefadsList.TryGetValue(prefadName, out loadPrdfad))
        {//2
            string loadRout = string.Format("{0}{1}", Constant.WINDOW_PREFAD_PATH, prefadName);

            loadPrdfad = Resources.Load<GameObject>(loadRout);

            if(null == loadPrdfad)
            {
                Debug.LogError("__ !!! Err Not Find Prefad Name = " + prefadName);
            }
        }//2
        return loadPrdfad;
    }//1

    /// <summary>
    /// 프리팹 생성
    /// </summary>
    /// <param name="prefadName"></param>
    /// <param name="cam"></param>
    /// <returns></returns>
    protected GameObject CreatePrefad(Camera cam, string prefadName)
    {
        GameObject loadPrefad = LoadPrefad(prefadName);
        GameObject createPrefad = null;
        if(null != loadPrefad)
        {
            createPrefad = Instantiate(loadPrefad);

            createPrefad.name = prefadName;
            createPrefad.transform.parent = cam.transform;
            createPrefad.transform.localPosition = Vector3.zero;
            createPrefad.transform.localRotation = Quaternion.identity;
            createPrefad.transform.localScale = Vector3.one;

            Transform[] children = createPrefad.GetComponentsInChildren<Transform>();
            foreach(Transform child in children)
            {
                if(null != child.GetComponent<UIAnchor>())
                {
                    child.GetComponent<UIAnchor>().uiCamera = cam;
                }
            }
        }
        if(null == createPrefad)
        {
            Debug.LogError("__ !!! Err Not Create Prefad Name = " + prefadName);
        }

        return createPrefad;
    }

    /// <summary>
    /// 해당 윈도우 찾기
    /// </summary>
    /// <param name="windowName"></param> 
    /// <returns></returns>
    public WindowUIFormBase GetWindow(string windowName)
    {
        WindowUIFormBase openWindow = null;

        if(null != _windowList && true == this._windowList.ContainsKey(windowName))
        {
            //Debug.LogError("GetWindow windowName = " + windowName);
            openWindow = this._windowList[windowName];
        }
        return openWindow;
    }

    /// <summary>
    /// 해당 윈도우 찾기 
    /// </summary>
    /// <param name="windowName"></param> 
    /// <returns></returns>
    public C GetWindow<C>(string windowName) where C : WindowUIFormBase
    {
        C openWindow = null;

        if(null != _windowList && true == this._windowList.ContainsKey(windowName))
        {
            //Debug.LogError("GetWindow windowName = " + windowName);
            openWindow = this._windowList[windowName] as C;
        }
        return openWindow;
    }

    /// <summary>
    /// 윈도우 뎁스 변경. (다른 윈도우 위로 올라오거나 내려갈때 사용 ex.매칭 윈도우)
    /// </summary>
    /// <param name="window"></param>
    /// <param name="depth"></param>
    public void ChangeWindowDepth(WindowUIFormBase window, int depth)
    {
        if(null == window)
            return;

        if(window.CheckActive == false || window.CheckOpen == false)
        {
            window.Activate(true);
        }

        CommonUtil.AdjustDepth2(window.gameObject, depth);
    }

    /// <summary>
    /// 윈도우 오픈
    /// </summary>
    /// <param name="windowName">프리팹 이름(키값이 되어 나중에 이 값으로 윈도우 검색)</param>
    /// <param name="par">오픈시 필요한 데이터</param>
    /// <param name="cameraNumber">붙여줄 카메라 넘버</param>
    /// <returns></returns>
    public WindowUIFormBase OpenWindow(string windowName, Parameter par = null, Constant.UI_CAMERA_NUMBER cameraNumber = Constant.UI_CAMERA_NUMBER.ONE)
    {
        return OpenWindow(_uiCameraArry[(int)cameraNumber], windowName, par);
    }

    public WindowUIFormBase OpenWindow(Camera cam, string windowName, Parameter par)
    {//1
        WindowUIFormBase openWindow = GetWindow(windowName);

        if(null == openWindow)
        {
            GameObject createObj = CreatePrefad(cam, windowName);

            if(null == createObj)
            {
                Debug.LogError("__ !!! Err Null createObj. windowName=" + windowName + " cam=" + cam + " par=" + par);
                return null;
            }
            else
            {
                openWindow = createObj.GetComponent<WindowUIFormBase>();
            }

            if(null == openWindow)
            {
                Debug.LogError("__ !!! Err Null window. windowName=" + windowName + " cam=" + cam + " par=" + par);
                if(null != createObj)
                {
                    Destroy(createObj);
                    createObj = null;
                }
                return null;
            }
        }

        switch(openWindow.WindowType)
        {
            case Constant.WINDOW_TYPE.NONE:
            case Constant.WINDOW_TYPE.BASE:
            {
                Debug.LogError("__ !! Err 잘못된 타입의 윈도우가 요청을하고 있다 !! WindowName = " + windowName);
            }
            return null;
        }


        //WindowUIFormBase curTopWindow = null;
        WindowUIFormBase tutorialWindow = null;

        int curTopDepth = 0;
        bool ActiveOnPopup = false;

        if(0 < this._openWindowList.Count)
        {//2-6

            //이미 열려 있는 윈도우면 기존 이름 삭제
            bool checkOpenWind = this._openWindowList.Contains(windowName);
            if(true == checkOpenWind)
            {
                this._openWindowList.Remove(windowName);
            }

            if(0 < this._openWindowList.Count)
            {
                //curTopWindow = this._windowList[this._openWindowList.Last()];

                for(int i = (this._openWindowList.Count - 1); i >= 0; --i)
                {//2-6-1

                    string windName = this._openWindowList[i];

                    WindowUIFormBase checkWindow = this._windowList[windName];
                    if(checkWindow.WindowType == Constant.WINDOW_TYPE.ACTIVEON_POPUP_WINDOW)
                        ActiveOnPopup = true;
                    else if(checkWindow.WindowType == Constant.WINDOW_TYPE.TUTORIAL_WINDOW)
                        tutorialWindow = checkWindow;

                    if(null != checkWindow && openWindow.name != checkWindow.name)
                    {//2-6-2

                        switch(checkWindow.WindowType)
                        {
                            case Constant.WINDOW_TYPE.TUTORIAL_WINDOW:
                            {
                                // 튜토리얼.
                                
                            }
                            break;

                            case Constant.WINDOW_TYPE.WINDOW:
                            {
                                //오픈 할려는 창이 일반 창일 경우에만 이전 창 비활성화 처리
                                if(openWindow.WindowType == Constant.WINDOW_TYPE.WINDOW)
                                {
                                    // 매칭 윈도우는 닫지 않고 진행합니다.
                                    if(checkWindow.name.Equals(UICommon.W_LOBBY_MATCHING))
                                    {

                                    }
                                    else if(true == checkWindow.CheckOpen && true == checkWindow.CheckActive)
                                    {
                                        checkWindow.Activate(false);
                                    }
                                }
                            }
                            break;

                            case Constant.WINDOW_TYPE.POPUP_WINDOW:
                            case Constant.WINDOW_TYPE.TOOLTIP_WINDOW:
                            {
                                //팝업이면 그냥 닫아버린다.
                                if(true == checkWindow.CheckOpen && true == checkWindow.CheckActive)
                                    checkWindow.Close();
                            }
                            break;

                            case Constant.WINDOW_TYPE.ACTIVEON_POPUP_WINDOW:
                            {
                                //오픈 할려는 창이 일반 창일 경우에만 팝업을 닫는다.
                                if(Constant.WINDOW_TYPE.WINDOW == openWindow.WindowType)
                                {
                                    if(true == checkWindow.CheckOpen && true == checkWindow.CheckActive)
                                        checkWindow.Close();
                                }
                            }
                            break;

                            //case Constant.WINDOW_TYPE.FITTED_WINDOW:
                            //{
                            //    Debug.LogError("__ !!! OpenWindow FITTED_WINDOW UpdateWindow");
                            //    (checkWindow as FittedWindowUIForm).UpdateWindow();
                            //}
                            //break;

                            default:
                            {
                                //지금은 아무 처리도 하지 않는다.
                                //Debug.LogError("__ !! Err 잘못된 윈도우 타입이 들어 왔다. !! = " + checkWindow.WindowType);
                            }
                            break;
                        }
                    }//2-6-2

                    if(curTopDepth < checkWindow.Depth)
                        curTopDepth = checkWindow.Depth;

                }//2-6-1
            }
        }//2-6

        switch(openWindow.WindowType)
        {
            case Constant.WINDOW_TYPE.WINDOW:
            case Constant.WINDOW_TYPE.POPUP_WINDOW:
            case Constant.WINDOW_TYPE.ACTIVEON_POPUP_WINDOW:
            case Constant.WINDOW_TYPE.TOOLTIP_WINDOW:
                {
                    //뎁스 설정 start
                    int curDepth = 0 < curTopDepth ? curTopDepth / DEPTH_OFFSET : 0;
                    int nextDepth = 0 < curDepth ? (curDepth + 1) * DEPTH_OFFSET : DEPTH_OFFSET;

                    //int curDepth = null != curTopWindow ? curTopWindow.Depth / DEPTH_OFFSET : 0;
                    //int nextDepth = null != curTopWindow ? (curDepth + 1) * DEPTH_OFFSET : DEPTH_OFFSET;

                    if(ActiveOnPopup)
                        nextDepth += DEPTH_OFFSET;

                    if(true == openWindow.CheckInitialized)
                    {
                        openWindow.Depth = 1;
                    }

                    CommonUtil.AdjustDepth2(openWindow.GetGameObject, nextDepth);

                    if(null != tutorialWindow)
                    {
                        CommonUtil.AdjustDepth2(tutorialWindow.gameObject, nextDepth + DEPTH_OFFSET);
                    }
                }
                break;

            case Constant.WINDOW_TYPE.TUTORIAL_WINDOW:
                {
                    // 튜토리얼 뎁스는 현제 최상단.
                    CommonUtil.AdjustDepth2(openWindow.GetGameObject, curTopDepth + DEPTH_OFFSET);

                    _isTutorial = true;
                }
                break;
        }

        //if(Constant.WINDOW_TYPE.FITTED_WINDOW != openWindow.WindowType &&
        //    Constant.WINDOW_TYPE.TUTORIAL_WINDOW != openWindow.WindowType)
        //{

        //}
        //else if(openWindow.WindowType == Constant.WINDOW_TYPE.TUTORIAL_WINDOW)
        //{

        //}

        openWindow.GetGameObject.SetActive(true);
        
        //뎁스 설정 end

        if(false == openWindow.CheckInitialized)
        {
            openWindow.Initialze();
            CloseEventWindowsAdd(openWindow);
        }

        //Debug.LogError("___ OpenWindow Depth = " + openWindow.Depth);

        //윈도우 리스트에 등록
        if(true == this._windowList.ContainsKey(windowName))
        {//2-3
            this._windowList[windowName] = openWindow;
        }//2-3
        else
        {//2-2
            this._windowList.Add(windowName, openWindow);
        }//2-2

        //오픈한 윈도우 리스트에 등록
        this._openWindowList.Add(windowName);

        //오픈
        openWindow.Open(par);

        return openWindow;
    }//1

    /// <summary>
    /// 윈도우 닫기 
    /// </summary>
    /// <param name="windowName">닫을려는 윈도우 키값</param>
    public void CloseWindow(string windowName)
    {//1
        if(null != this._openWindowList && 0 < this._openWindowList.Count)
        {//2
            bool checkOpenWind = this._openWindowList.Contains(windowName);
            if(true == checkOpenWind)
            {//3
                WindowUIFormBase closeWindow = GetWindow(windowName);
                if(null != closeWindow)
                {
                    //if (Constant.WINDOW_TYPE.FITTED_WINDOW != closeWindow.WindowType)
                    {
                        closeWindow.ForciblyClose();
                        this._openWindowList.Remove(windowName);
                    }
                }
                else
                {
                    Debug.LogError("__ !! Err 등록이 안된 윈도우  = " + windowName);
                    this._openWindowList.Remove(windowName);
                }
            }//3
        }//2
    }//1

    /// <summary>
    /// 고정형 윈도우를 제외한 모든 창 닫기 (튜토리얼 제외)
    /// </summary>
    public void CloseWindows()
    {//1
        if(null != this._openWindowList && 0 < this._openWindowList.Count)
        {//2

            for(int i = (this._openWindowList.Count - 1); i > 0; --i)
            {
                string windowName = this._openWindowList[i];
                WindowUIFormBase closeWindow = GetWindow(windowName);

                if(null != closeWindow)
                {
                    if(Constant.WINDOW_TYPE.FITTED_WINDOW != closeWindow.WindowType &&
                        Constant.WINDOW_TYPE.TUTORIAL_WINDOW != closeWindow.WindowType)
                    {
                        closeWindow.ForciblyClose();
                        this._openWindowList.Remove(windowName);
                    }
                }
            }
        }//2
    }//1

    /// <summary>
    /// 입력한 윈도우를 제외한 모든 창 닫기 (고정형, 튜토리얼 제외)
    /// </summary>
    public void CloseWindows(string activeOnWindName)
    {
        if(null != this._openWindowList && 0 < this._openWindowList.Count)
        {//2

            for(int i = (this._openWindowList.Count - 1); i > 0; --i)
            {
                string windowName = this._openWindowList[i];
                WindowUIFormBase closeWindow = GetWindow(windowName);

                if(null != closeWindow && false == string.Equals(activeOnWindName, windowName))
                {
                    if(Constant.WINDOW_TYPE.FITTED_WINDOW != closeWindow.WindowType &&
                        Constant.WINDOW_TYPE.TUTORIAL_WINDOW != closeWindow.WindowType)
                    {
                        closeWindow.ForciblyClose();
                        this._openWindowList.Remove(windowName);
                    }
                }
            }
        }//2
    }

    /// <summary>
    /// 닫을때 실행할 이벤트 등록
    /// </summary>
    protected void CloseEventWindowsAdd(WindowUIFormBase wind)
    {
        if(null != wind)
            wind.OnClosed += PreWindowOpen;
    }

    /// <summary>
    /// 모든 윈도우 닫기
    /// </summary>
    public void AllCloseWindows()
    {//1
        if(null != this._openWindowList && 0 < this._openWindowList.Count)
        {//2
            for(int i = 0; i < this._openWindowList.Count; i++)
            {
                string windowName = this._openWindowList[i];
                WindowUIFormBase closeWindow = GetWindow(windowName);
                // 튜토리얼 창 예외
                if(null != closeWindow && closeWindow.WindowType == Constant.WINDOW_TYPE.TUTORIAL_WINDOW)
                {
                    continue;
                }
                closeWindow.ForciblyClose();
            }
            _openWindowList.Clear();
        }//2
    }//1

    /// <summary>
    /// 오픈 윈도우 리스트에 추가.
    /// </summary>
    /// <param name="windowName"></param>
    public void OpenWindowListAdd(string windowName, WindowUIFormBase window)
    {
        if(null != this._openWindowList && 0 < this._openWindowList.Count)
        {
            if(false == this._openWindowList.Contains(windowName))
                this._openWindowList.Add(windowName);
        }

        if(false == _windowList.ContainsKey(windowName))
            _windowList.Add(windowName, window);
        else
            _windowList[windowName] = window;
    }

    /// <summary>
    /// 오픈 윈도우 리스트에서 삭제
    /// </summary>
    public void OpenWindowListRemove(string windowName)
    {
        if(null != this._openWindowList && 0 < this._openWindowList.Count)
        {
            this._openWindowList.Remove(windowName);
        }
    }

    /// <summary>
    /// 이전 윈도우 열기 
    /// </summary>
    /// <param name="windowName"></param>
    /// <param name="checkEsc"></param>
    public void PreWindowOpen(string windowName, bool clickEsc = false)
    {//1
        WindowUIFormBase closeWindow = GetWindow(windowName);

        if(null == closeWindow)
        {
            Debug.Log($"Close Window Is Null :: {windowName}");
            return;
        }

        //닫히는 창이 고정형이면 닫지 않는다.
        if(Constant.WINDOW_TYPE.FITTED_WINDOW == closeWindow.WindowType)
        {
            return;
        }
        else if(Constant.WINDOW_TYPE.TUTORIAL_WINDOW == closeWindow.WindowType)
        {
            _isTutorial = false;
        }

        if(true == clickEsc)
        {
            closeWindow.Close();
        }

        //오픈 리스트에서 삭제
        for(int i = (this._openWindowList.Count - 1); i >= 0; --i)
        {//2-2
            if(string.Equals(windowName, this._openWindowList[i]))
            {
                this._openWindowList.RemoveAt(i);
                //break;
            }
        }//2-2

        UpdatePreWindow();
    }//1

    /// <summary>
    /// 이전 윈도우 열기 
    /// </summary>
    public void PreWindowOpen(WindowUIFormBase closeWindow)
    {//1
        if(null == closeWindow)
            return;

        string windName = closeWindow.gameObject.name;
        Constant.WINDOW_TYPE windowType = closeWindow.WindowType;
        bool checkUpdatePreWindow = true;

        switch(windowType)
        {
            //툴팁일 경우 이전 윈도우UI를 갱신 하지 않는다.
            case Constant.WINDOW_TYPE.TOOLTIP_WINDOW:
                checkUpdatePreWindow = false;
                break;

            //닫히는 창이 고정형이면 닫지 않는다.
            case Constant.WINDOW_TYPE.FITTED_WINDOW:
                return;

            case Constant.WINDOW_TYPE.TUTORIAL_WINDOW:
                _isTutorial = false;
                break;
        }

        //closeWindow.Close();

        //오픈 리스트에서 삭제
        for(int i = (this._openWindowList.Count - 1); i >= 0; --i)
        {//2-2
            if(string.Equals(windName, this._openWindowList[i]))
            {
                this._openWindowList.RemoveAt(i);
                //break;
            }
        }//2-2

        if(true == checkUpdatePreWindow)
            UpdatePreWindow();
    }//1

    /// <summary>
    /// 현재 열려있는 윈도우 UI 갱신
    /// </summary>
    public void UpdatePreWindow()
    {//1
        //이전 창 활성화
        if(null != this._openWindowList && 0 < this._openWindowList.Count)
        {//2
            string activateOnWindowName = this._openWindowList.Last();

            WindowUIFormBase activateWindow = GetWindow(activateOnWindowName);

            if(null != activateWindow)
            {//3-2
                switch(activateWindow.WindowType)
                {
                    case Constant.WINDOW_TYPE.WINDOW:
                    case Constant.WINDOW_TYPE.ACTIVEON_POPUP_WINDOW:
                    {
                        activateWindow.Activate(true);
                    }
                    break;

                    case Constant.WINDOW_TYPE.FITTED_WINDOW:
                    {
                        (activateWindow as FittedWindowUIForm).UpdateWindow();
                    }
                    break;

                    default:
                    {
                        //다른 타입에 대한 예외 처리가 필요하면 여기에 ~
                    }
                    break;
                }
            }//3-2
            else
            {//3-1
                Debug.LogError("__ !! Err  Null activateWindow Name = " + activateOnWindowName);
            }//3-1
        }//2
    }//1
}
