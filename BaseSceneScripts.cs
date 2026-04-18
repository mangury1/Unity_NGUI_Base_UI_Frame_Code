using System.Collections;

/// <summary>
/// 씬 스크립트 기본 클래스. 각 씬에서 상속받아 사용한다.
/// SceneChangeManager에서 Dictionary로 관리된다.
public abstract class BaseSceneScripts
{
    protected string _sceneName = string.Empty;
    protected bool _checkInit = false;

    public string SceneName { get { return _sceneName; } }

    /// <summary>
    /// 해당 씬 진입 시 터치 이펙트를 활성화할지 여부.
    /// </summary>
    public virtual bool TouchEffectEnabled { get { return true; } }

    /// <summary>
    /// 해당 씬의 로딩 완료 후 UI 처리 타입.
    /// 기본값은 Default(일반 로딩 종료).
    /// </summary>
    public virtual SceneLoadingType LoadingType { get { return SceneLoadingType.Default; } }

    public virtual void Initialze(string sceneName)
    {
        _sceneName = sceneName;
        _checkInit = true;
    }

    public virtual void Release() { }

    /// <summary>
    /// 해당 씬 입장 시 호출
    /// </summary>
    public virtual IEnumerator OnEntry() { yield return null; }

    /// <summary>
    /// 해당 씬 탈출 시 호출
    /// </summary>
    public virtual IEnumerator OnEscape() { yield return null; }

    /// <summary>
    /// 업데이트 코루틴
    /// </summary>
    public virtual IEnumerator Update() { yield return null; }
}

/// <summary>
/// 씬 진입 후 로딩 UI 처리 타입
/// </summary>
public enum SceneLoadingType
{
    /// <summary> 일반 로딩 종료 (EndLoading) </summary>
    Default,

    /// <summary> 로딩 UI를 끄지 않음 (인게임 씬 등) </summary>
    None,

    /// <summary> 매치 로딩 UI 표시 </summary>
    MatchLoading,

    /// <summary> 일반 로딩 UI 표시 </summary>
    NormalLoading,
}
