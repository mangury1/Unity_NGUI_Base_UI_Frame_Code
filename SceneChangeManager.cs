using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum CHANGE_GAME_SCENE_TYPE
{
    WAITING_ROOM, // 대기방으로 이동
    PLAY_GAME     // 게임으로 바로 이동
}

/// <summary>
/// [개선사항 요약]
///
/// 1. AddScript의 switch-case 하드코딩 제거
///    → RegisterSceneScript<T>() 제네릭 메서드로 교체.
///      씬 추가 시 이 클래스를 수정하지 않아도 됨 (OCP 준수).
///
/// 2. CheckLoadScene 내부 하드코딩 조건문 제거
///    → 터치 이펙트, 로딩 UI 처리를 각 씬 스크립트의 속성값으로 위임.
///      (BaseSceneScripts.TouchEffectEnabled / LoadingType)
///
/// 3. Awake()에서 초기화
///    → 기존 Instance getter 안에서 초기화하던 방식을 Awake()로 이동.
///      초기화 타이밍 문제 방지.
///
/// 4. SceneChange / SceneChange2 이원화 정리
///    → SceneChange()로 일원화. 내부적으로 AsyncOperation 반환이 필요한 경우
///      GetLoadingSceneAsync 프로퍼티로 접근.
/// </summary>
public class SceneChangeManager : MonoBehaviour
{
    protected readonly WaitForSeconds SLEEP_TIME = new WaitForSeconds(1);

    private IEnumerator _process = null;
    private AsyncOperation _async = null;
    private AsyncOperation _loadingSceneAsync = null;

    private string _nextSceneName = string.Empty;

    /// <summary>현재 씬 이름 (직접 추적)</summary>
    public string CurrentSceneName { get; private set; }

    /// <summary>Unity가 인식하는 현재 씬 이름</summary>
    public string UnityActiveSceneName
    {
        get { return SceneManager.GetActiveScene().name; }
    }

    public AsyncOperation GetLoadingSceneAsync { get { return _loadingSceneAsync; } }

    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    private static SceneChangeManager _instance;
    public static SceneChangeManager Instance
    {
        get
        {
            if (null == _instance)
            {
                GameObject obj = GameObject.Find(typeof(SceneChangeManager).Name);
                if (null == obj)
                {
                    obj = new GameObject(typeof(SceneChangeManager).Name);
                    _instance = obj.AddComponent<SceneChangeManager>();
                }
                else
                {
                    _instance = obj.GetComponent<SceneChangeManager>();
                }

                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
    }

    // ---------------------------------------------------------------
    // 씬 스크립트 레지스트리
    // ---------------------------------------------------------------

    private Dictionary<string, BaseSceneScripts> _sceneScriptsList = null;
    private Coroutine _checkLoadScene = null;

    /// <summary>진입할 게임 씬 타입</summary>
    public CHANGE_GAME_SCENE_TYPE ChangeGameSceneType { get; private set; }

    // ---------------------------------------------------------------
    // Unity 생명주기
    // ---------------------------------------------------------------

    private void Awake()
    {
        // 싱글턴 중복 방지
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 초기화를 Awake로 이동 (기존: Instance getter 내부에서 호출)
        InitSceneScriptsList();
    }

    private void OnDestroy()
    {
        DestroySceneScriptsList();

        if (null != _checkLoadScene)
        {
            StopCoroutine(_checkLoadScene);
            _checkLoadScene = null;
        }
    }

    // ---------------------------------------------------------------
    // 씬 스크립트 등록 (기존 AddScript switch-case 대체)
    // ---------------------------------------------------------------

    private void InitSceneScriptsList()
    {
        if (_sceneScriptsList != null)
            DestroySceneScriptsList();

        _sceneScriptsList = new Dictionary<string, BaseSceneScripts>();
    }

    private void DestroySceneScriptsList()
    {
        if (_sceneScriptsList == null) return;

        foreach (var pair in _sceneScriptsList)
        {
            pair.Value.Release();
        }

        _sceneScriptsList.Clear();
        _sceneScriptsList = null;
    }

    /// <summary>
    /// 씬 스크립트를 제네릭으로 등록한다.
    /// 씬 추가 시 이 클래스를 수정하지 않고, 외부(씬 초기화 코드 등)에서 호출하면 된다.
    ///
    /// 사용 예:
    ///   SceneChangeManager.Instance.RegisterSceneScript<MainRoomSceneScripts>(Constant.S_MAIN_ROOM_NAME);
    /// </summary>
    public void RegisterSceneScript<T>(string sceneName) where T : BaseSceneScripts, new()
    {
        if (_sceneScriptsList.ContainsKey(sceneName))
            return;

        T script = new T();
        script.Initialze(sceneName);
        _sceneScriptsList.Add(sceneName, script);
    }

    /// <summary>
    /// 씬 스크립트를 인스턴스로 직접 등록한다. (팩토리 패턴 등 외부 생성이 필요한 경우)
    /// </summary>
    public void RegisterSceneScript(string sceneName, BaseSceneScripts script)
    {
        if (_sceneScriptsList.ContainsKey(sceneName))
            return;

        script.Initialze(sceneName);
        _sceneScriptsList.Add(sceneName, script);
    }

    public S GetSceneScripts<S>(string sceneName) where S : BaseSceneScripts
    {
        if (_sceneScriptsList != null && _sceneScriptsList.ContainsKey(sceneName))
            return _sceneScriptsList[sceneName] as S;

        return null;
    }

    // ---------------------------------------------------------------
    // 씬 상태 조회
    // ---------------------------------------------------------------

    public void SetChangeGameSceneType(CHANGE_GAME_SCENE_TYPE type)
    {
        ChangeGameSceneType = type;
    }

    public bool CheckMatchGame()
    {
        return ChangeGameSceneType == CHANGE_GAME_SCENE_TYPE.PLAY_GAME;
    }

    public bool CheckScene(string sceneName)
    {
        return string.Equals(CurrentSceneName, sceneName);
    }

    public bool CheckOutGameScene()
    {
        return CheckScene(Constant.S_MAIN_ROOM_NAME)
            || CheckScene(Constant.S_CLIENT_ASSET_NAME)
            || CheckScene(Constant.S_CLIENT_NAME);
    }

    // ---------------------------------------------------------------
    // 씬 전환 (SceneChange / SceneChange2 일원화)
    // ---------------------------------------------------------------

    /// <summary>
    /// 일반 씬 전환. EmptyScene을 경유해 메모리를 정리한 후 nextSceneName으로 이동한다.
    /// 기존 SceneChange()와 동일한 흐름.
    /// AsyncOperation이 필요하면 GetLoadingSceneAsync 프로퍼티로 접근.
    /// </summary>
    public void SceneChange(string sceneName, string scriptsName = null)
    {
        if (!string.IsNullOrEmpty(scriptsName))
            Debug.LogWarning("__ !! SceneChange ScriptsName = " + scriptsName);

        if (string.IsNullOrEmpty(sceneName))
            return;

        _nextSceneName = sceneName;

        GameUIManager.instance.GetWindowSceneLoading.StartLoading(true);
        GameUIManager.instance.DestroyWindow();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        if (null == _process)
        {
            _process = LoadEmptyScene();
            StartCoroutine(_process);
        }
    }

    public void ClientAssetSceneChange(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        _nextSceneName = sceneName;

        GameUIManager.instance.DestroyWindow();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        LoadNextScene();
    }

    public void SceneChangeAdditive(string sceneName)
    {
        if (null == SceneManager.GetSceneByName(sceneName))
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    public void SceneChangeUnload(string sceneName)
    {
        if (null != SceneManager.GetSceneByName(sceneName))
            SceneManager.UnloadSceneAsync(sceneName);
    }

    // ---------------------------------------------------------------
    // 내부 씬 전환 흐름
    // ---------------------------------------------------------------

    private IEnumerator LoadEmptyScene()
    {
        string nowSceneName = SceneManager.GetActiveScene().name;
        if (_sceneScriptsList.ContainsKey(nowSceneName))
        {
            yield return StartCoroutine(_sceneScriptsList[nowSceneName].OnEscape());
        }

        _async = SceneManager.LoadSceneAsync(Constant.S_EMPTY_SCENE_NAME);

        while (null != _async && !_async.isDone)
            yield return null;

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        _async = null;

        if (null != _process)
        {
            StopCoroutine(_process);
            _process = null;
        }

        if (string.IsNullOrEmpty(_nextSceneName))
            return;

        if (null != _checkLoadScene)
        {
            StopCoroutine(_checkLoadScene);
            _checkLoadScene = null;
        }

        _checkLoadScene = StartCoroutine(CheckLoadScene(_nextSceneName));
    }

    /// <summary>
    /// 실제 씬 로드 및 진입 처리.
    /// 하드코딩 조건문 대신 각 씬 스크립트의 속성값(TouchEffectEnabled, LoadingType)을 읽어 처리한다.
    /// </summary>
    private IEnumerator CheckLoadScene(string sceneName)
    {
        // 로딩 전용 씬은 바로 전환 후 대기
        if (Constant.S_FIELD_LOADING_SCENE_NAME == sceneName
            || Constant.S_MAIN_ROOM_LOADING_NAME == sceneName
            || Constant.S_EMPTY_SCENE_NAME == sceneName)
        {
            SceneManager.LoadScene(_nextSceneName);

            while (true)
                yield return null;
        }

        _loadingSceneAsync = SceneManager.LoadSceneAsync(sceneName);

        while (null != _loadingSceneAsync && !_loadingSceneAsync.isDone)
            yield return null;

        // 씬 로드 후 스크립트가 없으면 등록 시도 (하위 호환용)
        // 권장: 씬 초기화 코드에서 RegisterSceneScript<T>()를 직접 호출할 것
        TryAutoRegisterScript(sceneName);

        // 처리가 남아있을 수 있어 1초 대기
        yield return SLEEP_TIME;

        if (_sceneScriptsList.ContainsKey(sceneName))
        {
            yield return StartCoroutine(_sceneScriptsList[sceneName].OnEntry());
        }

        // ✅ 기존: 하드코딩 if/else 조건문
        // ✅ 개선: 씬 스크립트 속성값을 읽어 처리 (매니저가 씬을 몰라도 됨)
        ApplySceneSettings(sceneName);

        CurrentSceneName = sceneName;

        // 로딩 UI 처리도 씬 스크립트 속성값 기반으로 처리
        ApplyLoadingUI(sceneName);

        _loadingSceneAsync = null;
    }

    /// <summary>
    /// 씬 스크립트의 TouchEffectEnabled 속성을 읽어 터치 이펙트를 설정한다.
    /// 씬 스크립트가 없으면 기본값(true)을 사용.
    /// </summary>
    private void ApplySceneSettings(string sceneName)
    {
        bool touchEffectEnabled = true;

        if (_sceneScriptsList.TryGetValue(sceneName, out BaseSceneScripts script))
        {
            touchEffectEnabled = script.TouchEffectEnabled;
        }

        MouseTouchManager.Instance.SetActiveEffect(touchEffectEnabled);
    }

    /// <summary>
    /// 씬 스크립트의 LoadingType 속성을 읽어 로딩 UI를 처리한다.
    /// 네트워크 로비처럼 별도 분기가 필요한 씬은 씬 스크립트에서 LoadingType을 오버라이드.
    /// </summary>
    private void ApplyLoadingUI(string sceneName)
    {
        SceneLoadingType loadingType = SceneLoadingType.Default;

        if (_sceneScriptsList.TryGetValue(sceneName, out BaseSceneScripts script))
        {
            loadingType = script.LoadingType;
        }

        switch (loadingType)
        {
            case SceneLoadingType.None:
                // 로딩 UI를 끄지 않음 (인게임 씬 등)
                break;

            case SceneLoadingType.MatchLoading:
                GameUIManager.instance.GetWindowSceneLoading.StartMatchLoading();
                break;

            case SceneLoadingType.NormalLoading:
                GameUIManager.instance.GetWindowSceneLoading.StartLoading();
                break;

            case SceneLoadingType.Default:
            default:
                GameUIManager.instance.GetWindowSceneLoading.EndLoading(true);
                break;
        }
    }

    /// <summary>
    /// 하위 호환용: 씬 로드 시 스크립트가 없을 경우 기존 switch-case 방식으로 폴백.
    /// 새로 추가하는 씬은 RegisterSceneScript<T>()를 씬 초기화 시점에 직접 호출할 것.
    /// 이 메서드는 기존 씬들의 동작을 유지하기 위한 임시 브릿지임.
    /// </summary>
    private void TryAutoRegisterScript(string sceneName)
    {
        if (_sceneScriptsList.ContainsKey(sceneName))
            return;

        switch (sceneName)
        {
            case Constant.S_CLIENT_NAME:
                RegisterSceneScript<ClientSceneScripts>(sceneName);
                break;

            case Constant.S_MAIN_ROOM_NAME:
                RegisterSceneScript<MainRoomSceneScripts>(sceneName);
                break;

            case Constant.S_FISHING_GROUND_NAME:
                RegisterSceneScript<FishingGroundSceneScripts>(sceneName);
                break;

            // ✅ 신규 씬 추가 시 여기 대신 씬 초기화 코드에서 아래처럼 호출하세요:
            // SceneChangeManager.Instance.RegisterSceneScript<NewSceneScripts>(Constant.S_NEW_SCENE_NAME);
        }
    }
}
