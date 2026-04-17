using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum CHANGE_GAME_SCENE_TYPE
{
    WAITING_ROOM, //대기방으로 이동
    PLAY_GAME //게임으로 바로 이동
}

public class SceneChangeManager : MonoBehaviour
{
    protected readonly WaitForSeconds SLEEP_TIME = new WaitForSeconds(1);

    private IEnumerator process = null;

    private AsyncOperation async = null;
    private AsyncOperation _loadingSceneAsync = null;

    private string nextSceneName = string.Empty;
    //현재 있는 씬이름
    public string CurrentSceneName { get; private set; }

    //유니티에서 제공하는 현재 있는 씬이름
    public string UnityActiveSceneName
    {
        get { return SceneManager.GetActiveScene().name; }
    }

    private static SceneChangeManager _instance;
    public static SceneChangeManager Instance
    {
        get
        {
            if(null == _instance)
            {
                GameObject obj;
                obj = GameObject.Find(typeof(SceneChangeManager).Name);
                if(null == obj)
                {
                    obj = new GameObject(typeof(SceneChangeManager).Name);
                    _instance = obj.AddComponent<SceneChangeManager>();
                }
                else
                {
                    _instance = obj.GetComponent<SceneChangeManager>();
                }

                _instance.SencesScriptsListInit();
                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
    }

    public AsyncOperation GetLoadingSceneAsync { get { return _loadingSceneAsync; } }


    /// <summary>
    /// 씬 스크립트 리스트
    /// </summary>
    private Dictionary<string, BaseSceneScripts> _scenceScriptsList = null;

    private Coroutine _checkLoadScene = null;

    /// <summary>
    /// 진입할 게임 씬 타입
    /// </summary>
    public CHANGE_GAME_SCENE_TYPE ChangeGameSceneType { get; private set; }

    /// <summary>
    /// 메치 게임인지 체크
    /// </summary>
    /// <returns></returns>
    public bool CheckMatchGame()
    {
        return ChangeGameSceneType == CHANGE_GAME_SCENE_TYPE.PLAY_GAME;
    }

    public bool CheckScene(string sceneName)
    {
        return string.Equals(CurrentSceneName, sceneName);
    }

    /// <summary>
    /// 아웃게임 씬에 있는지 체크
    /// </summary>
    public bool CheckOutGameScene()
    {
        return CheckScene(Constant.S_MAIN_ROOM_NAME) || CheckScene(Constant.S_CLIENT_ASSET_NAME) || CheckScene(Constant.S_CLIENT_NAME);
    }

    private void OnDestroy()
    {
        DestroyScenceScriptsList();

        if(null != _checkLoadScene)
        {
            StopCoroutine(_checkLoadScene);
            _checkLoadScene = null;
        }
    }

    /// <summary>
    /// 진입할 게임 씬타입 
    /// </summary>
    public void SetChangeGameSceneType(CHANGE_GAME_SCENE_TYPE type)
    {
        ChangeGameSceneType = type;
    }

    private void SencesScriptsListInit()
    {
        if(null == _scenceScriptsList)
        {
            _scenceScriptsList = new Dictionary<string, BaseSceneScripts>();
            _scenceScriptsList.Clear();
        }
        else
        {
            DestroyScenceScriptsList();

            _scenceScriptsList = new Dictionary<string, BaseSceneScripts>();
        }
    }

    private void DestroyScenceScriptsList()
    {
        var senceScriptsDic = _scenceScriptsList.GetEnumerator();
        while(true == senceScriptsDic.MoveNext())
        {
            BaseSceneScripts senceScripts = senceScriptsDic.Current.Value;
            senceScripts.Release();
            senceScripts = null;
        }

        _scenceScriptsList.Clear();
        _scenceScriptsList = null;
    }

    public void SceneChange(string sceneName, string scriptsName = null)
    {
        //디버그 확인용 
        if(!string.IsNullOrEmpty(scriptsName))
            Debug.LogWarning("__ !! SceneChange ScriptsName = " + scriptsName);

        if(string.IsNullOrEmpty(sceneName))
            return;

        if(!string.IsNullOrEmpty(nextSceneName))
            nextSceneName = string.Empty;

        nextSceneName = sceneName;

        GameUIManager.instance.GetWindowSceneLoading.StartLoading(true);
        GameUIManager.instance.DestroyWindow();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        if(null == process)
        {
            process = LoadEmptyScene();
            StartCoroutine(process);
        }
    }

    public AsyncOperation SceneChange2(string sceneName)
    {
        _loadingSceneAsync = SceneManager.LoadSceneAsync(sceneName);

        GameUIManager.instance.GetWindowSceneLoading.StartLoading(true);
        GameUIManager.instance.DestroyWindow();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        if(null != _checkLoadScene)
        {
            StopCoroutine(_checkLoadScene);
            _checkLoadScene = null;
        }

        _checkLoadScene = StartCoroutine(CheckLoadScene(sceneName));

        return _loadingSceneAsync;
    }

    IEnumerator LoadEmptyScene()
    {
        //현재 씬에서 처리 할것이 있다면 다 처리 할때까지 대기 
        string nowSceneName = SceneManager.GetActiveScene().name;
        if(true == _scenceScriptsList.ContainsKey(nowSceneName))
        {
            yield return StartCoroutine(_scenceScriptsList[nowSceneName].OnEscape());
        }

        async = SceneManager.LoadSceneAsync("EmptyScene");

        while(null != async && !async.isDone)
        {
            yield return null;
        }

        LoadNextScene();
    }

    #region -----------------------------  ClientAsset Sene 

    public void ClientAssetSceneChange(string sceneName)
    {
        if(string.IsNullOrEmpty(sceneName))
            return;

        if(!string.IsNullOrEmpty(nextSceneName))
            nextSceneName = string.Empty;

        nextSceneName = sceneName;

        GameUIManager.instance.DestroyWindow();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        //MouseTouchManager.Instance.SetActiveEffect(true);

        LoadNextScene();
    }

    #endregion

    private void LoadNextScene()
    {
        if(null != async)
            async = null;

        if(null != process)
        {
            StopCoroutine(process);
            process = null;
        }

        if(string.IsNullOrEmpty(nextSceneName))
            return;

        // SceneManager.LoadScene(nextSceneName);

        if(null != _checkLoadScene)
        {
            StopCoroutine(_checkLoadScene);
            _checkLoadScene = null;
        }

        _checkLoadScene = StartCoroutine(CheckLoadScene(nextSceneName));
    }

    public void SceneChangeAdditive(string sceneName)
    {
        if(null == SceneManager.GetSceneByName(sceneName))
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    public void SceneChangeUnload(string sceneName)
    {
        if(null != SceneManager.GetSceneByName(sceneName))
            SceneManager.UnloadSceneAsync(sceneName);
    }

    /// <summary>
    /// 최종 씬에 도착이 되었는지 체크
    /// </summary>
    private IEnumerator CheckLoadScene(string sceneName)
    {
        //이 두개 씬은 로딩을 위한 씬으로 두개 씬 진입중일 경우 대기 처리한다.
        if(Constant.S_FIELD_LOADING_SCENE_NAME == sceneName || Constant.S_MAIN_ROOM_LOADING_NAME == sceneName || Constant.S_EMPTY_SCENE_NAME == sceneName)
        {
            SceneManager.LoadScene(nextSceneName);

            while(true)
            {
                //Debug.LogError("__ CheckLoadScene Hold ScemeName = " + sceneName);
                yield return null;
            }
        }

        _loadingSceneAsync = SceneManager.LoadSceneAsync(sceneName);

        while(null != _loadingSceneAsync && !_loadingSceneAsync.isDone)
        {
            yield return null;
        }

        AddScript(sceneName);

        //처리가 남아있을지 몰라 1초정도 더 대기 
        yield return SLEEP_TIME;

        if(true == _scenceScriptsList.ContainsKey(sceneName))
        {
            //해당 씬에서 처리 할것이 있다면 더 대기
            yield return StartCoroutine(_scenceScriptsList[sceneName].OnEntry());
        }

        // 화면 터치 이펙트 On/Off
        if(Constant.S_NETWORK_GAMEROOM_NAME == sceneName
            || Constant.S_GUILD_LOBBY_NAME == sceneName
            || Constant.S_TRAINING_NAME == sceneName
            || Constant.S_STORY_NAME == sceneName
            || Constant.S_OPERATION_NAME == sceneName
            || Constant.S_SINGLE_GAMEROOM_NAME == sceneName
            || Constant.S_AI_TUTORIAL_GAMEROOM_NAME == sceneName)
            MouseTouchManager.Instance.SetActiveEffect(false);
        else
            MouseTouchManager.Instance.SetActiveEffect(true);

        CurrentSceneName = sceneName;

        if(sceneName == Constant.S_NETWORK_LOBBYROOM_NAME)
        {
            Constant.GAME_MATCH_TYPE matchType = Constant.GAME_MATCH_TYPE.NONE;
            if(null != ClientNetworkManager.Instance.GetClientNetworkLobbyInfo)
                matchType = ClientNetworkManager.Instance.GetClientNetworkLobbyInfo.MatchType;

            Scene scene = SceneManager.GetActiveScene();

            //게임중에 재접속시 네트워크룸 씬에 갔다가 인게임씬으로 가야 하는데 먼저 인게임씬으로 선 진입하는 경우가 있다.
            if(string.Equals(scene.name, Constant.S_NETWORK_GAMEROOM_NAME))
            {
                GameUIManager.instance.GetWindowSceneLoading.StartLoading();
            }
            //else if(true == CheckMatchGame())
            else if(matchType == Constant.GAME_MATCH_TYPE.NORMAL_MATCH ||
                matchType == Constant.GAME_MATCH_TYPE.COOPERATIVE_MATCH)
            {
                GameUIManager.instance.GetWindowSceneLoading.StartMatchLoading();
            }
            else
            {
                GameUIManager.instance.GetWindowSceneLoading.StartLoading();
            }
        }
        else if(sceneName != Constant.S_SINGLE_GAMEROOM_NAME
            && sceneName != Constant.S_AI_TUTORIAL_GAMEROOM_NAME)
        {
            //로딩 UI 꺼준다
            GameUIManager.instance.GetWindowSceneLoading.EndLoading(true);
        }

        _loadingSceneAsync = null;

        //StopCoroutine(_checkLoadScene);
        //_checkLoadScene = null;
    }

    /// <summary>
    /// 스크립트 추가
    /// </summary>
    public void AddScript(string sceneName)
    {//1
        if(false == _scenceScriptsList.ContainsKey(sceneName))
        {//2
            switch(sceneName)
            {
                case Constant.S_CLIENT_NAME:
                {
                    ClientSceneScripts addScript = new ClientSceneScripts();
                    addScript.Initialze(sceneName);
                    _scenceScriptsList.Add(sceneName, addScript);
                }
                break;

                case Constant.S_MAIN_ROOM_NAME:
                {
                    MainRoomSceneScripts addScript = new MainRoomSceneScripts();
                    addScript.Initialze(sceneName);
                    _scenceScriptsList.Add(sceneName, addScript);
                }
                break;

                case Constant.S_FISHING_GROUND_NAME:
                {
                    FishingGroundSceneScripts addScript = new FishingGroundSceneScripts();
                    addScript.Initialze(sceneName);
                    _scenceScriptsList.Add(sceneName, addScript);
                }
                break;
            }
        }//2
    }//1


    public S GetSceneScripts<S>(string sceneName) where S : BaseSceneScripts
    {
        S sceneScripts = null;

        if(null != _scenceScriptsList && true == _scenceScriptsList.ContainsKey(sceneName))
            sceneScripts = _scenceScriptsList[sceneName] as S;

        return sceneScripts;
    }

}
