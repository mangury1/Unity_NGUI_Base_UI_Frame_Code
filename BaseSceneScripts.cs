using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

/// <summary>
/// 해당 씬에서 사용할 씬 스크립트 해당씬에서 상속 받아서 사용한다. SceneChangeManager 에서 리스트로 관리한다.
/// </summary>
public abstract class BaseSceneScripts
{
    protected string _sceneName = string.Empty;
    protected bool _checkInit = false;
    
    public string SceneName { get { return _sceneName; } }

    //public IEnumerator GetEnumerator() { yield return null; }

    public virtual void Initialze(string sceneName )
    {
        _sceneName = sceneName;
        _checkInit = true;
    }

    public virtual void Release() { }

    /// <summary>
    /// 해당씬 입장시 호출
    /// </summary>
    public virtual IEnumerator OnEntry() { yield return null; }

    /// <summary>
    /// 해당씬 탈출시 호출
    /// </summary>
    public virtual IEnumerator OnEscape() { yield return null; }

    /// <summary>
    /// 업데이트 코루틴
    /// </summary>
    public virtual IEnumerator Update() { yield return null; }
}
