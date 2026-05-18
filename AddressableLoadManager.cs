//using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System;
//using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class AddressableLoadManager
{
    private static string LABELS_NAME { get { return "Asset"; } }
    public static long DownloadSize { get; private set; }

    public static bool CheckAssetDownload { get { return 0 < DownloadSize; } }

    //public static bool isInitialze = false; 

    private static AsyncOperationHandle<IList<IResourceLocation>> _loadHandle;
    private static List<IGrouping<int, IResourceLocation>> _loadResourceList = null;// _loadHandle.Result.GroupBy(x => x.DependencyHashCode).ToList();
    public static void ClearDependencyCache()
    {
        Addressables.ClearDependencyCacheAsync(LABELS_NAME);
    }

    public static void Initialze(Action action)
    {
        if(true == _loadHandle.IsValid())
            Addressables.Release(_loadHandle);

        AsyncOperationHandle<IResourceLocator> initializeHandle = Addressables.InitializeAsync();
        initializeHandle.Completed += (initialize) =>
        {
            _loadHandle = Addressables.LoadResourceLocationsAsync(LABELS_NAME);
            _loadHandle.Completed += (load) =>
            {
                if(null != action)
                    action();
            };
        };
    }

    #region -------- IEnumerator Load

    public static IEnumerator Initialze()
    {
        if(true == _loadHandle.IsValid())
            Addressables.Release(_loadHandle);

        AsyncOperationHandle<IResourceLocator> initializeHandle = Addressables.InitializeAsync();

        yield return initializeHandle;

        _loadHandle = Addressables.LoadResourceLocationsAsync(LABELS_NAME);

        yield return _loadHandle;

        if(null != _loadResourceList)
            _loadResourceList.Clear();

        _loadResourceList = _loadHandle.Result.GroupBy(x => x.DependencyHashCode).ToList();

        yield break;
    }

    /// <summary>
    /// 해당 어셋이 다운 받을것이 있는지 체크
    /// </summary>
    public static IEnumerator DownloadSizeAsync(Action next)
    {
        if(true == CheckLoadResourceList())
        {
            DownloadSize = 0;

            int loopCount = _loadResourceList.Count - 1;
            while(0 <= loopCount)
            {
                IGrouping<int, IResourceLocation> groupedLocations = _loadResourceList[loopCount];
                AsyncOperationHandle<long> downloadSizeHandle = Addressables.GetDownloadSizeAsync(groupedLocations.ToList());

                yield return downloadSizeHandle;

                bool checkComplete = false;
                downloadSizeHandle.Completed += (sizeHandle) =>
                {
                    DownloadSize = DownloadSize + sizeHandle.Result;
                    checkComplete = true;
                };

                yield return (false == checkComplete);
                loopCount = loopCount - 1;
            }
        }

        if(null != next)
            next();

        yield break;
    }

    public static IEnumerator DownloadAssetBundles(ClientAssetUI clientAssetUI, Action next)
    {//1

        if(true == CheckLoadResourceList())
        {
            clientAssetUI.LoadingStart();
            int downloadTotalCount = _loadResourceList.Count();
            int downloadCount = 0;
            int loopCount = _loadResourceList.Count - 1;
            int backUpLoopCount = 0;

            string descText = SystemMessageDataManager.Instance.GetStringByIdx(300201);
            string progressValueText = "{0}/{1} ({2})";
            float progressValue = 0.0f;

            clientAssetUI.DescText(descText);

            while(0 <= loopCount)
            {
                if(false == ClientNetworkManager.Instance.CheckNetwork())
                {
                    if(backUpLoopCount != loopCount && _loadResourceList.Count - 1 > loopCount)
                    {
                        loopCount = loopCount + 1;
                        backUpLoopCount = loopCount;
                    }
                    yield return null;
                }
                else
                {
                    IGrouping<int, IResourceLocation> groupedLocations = _loadResourceList[loopCount];
                    AsyncOperationHandle downloadhandle = Addressables.DownloadDependenciesAsync(groupedLocations.ToList());

                    yield return (false == downloadhandle.IsDone);

                    yield return downloadhandle;

                    Addressables.Release(downloadhandle);
                    loopCount = loopCount - 1;

                    downloadCount = downloadCount + 1;
                    progressValue = (float)downloadCount / (float)downloadTotalCount;

                    clientAssetUI.LoadingValue(progressValue);
                    clientAssetUI.ValueText(string.Format(progressValueText, downloadCount.ToString(), downloadTotalCount.ToString(), (progressValue * 100f).ToSecondPointPersent()));
                }
            }
        }


        Addressables.Release(_loadHandle);

        clientAssetUI.LoadingEnd();
        DownloadSize = 0;

        if(null != next)
            next();

        yield break;
    }//1

    #endregion

    #region -------- async Load

    public static async UniTask Async_Initialze()
    {
        try
        {
            if(true == _loadHandle.IsValid())
                Addressables.Release(_loadHandle);

            AsyncOperationHandle<IResourceLocator> initializeHandle = Addressables.InitializeAsync();

            await initializeHandle.ToUniTask();

            _loadHandle = Addressables.LoadResourceLocationsAsync(LABELS_NAME);

            await _loadHandle.ToUniTask();

            if(null != _loadResourceList)
                _loadResourceList.Clear();

            _loadResourceList = _loadHandle.Result.GroupBy(x => x.DependencyHashCode).ToList();

        }
        catch(Exception ex)
        {
            Debug.LogError($"Exception: {ex.Message}");

        }

        return;
    }

    public static async UniTask Async_DownloadSizeAsync(Action next)
    {
        try
        {
            if(true == CheckLoadResourceList())
            {
                DownloadSize = 0;

                int loopCount = _loadResourceList.Count - 1;
                while(0 <= loopCount)
                {
                    IGrouping<int, IResourceLocation> groupedLocations = _loadResourceList[loopCount];
                    AsyncOperationHandle<long> downloadSizeHandle = Addressables.GetDownloadSizeAsync(groupedLocations.ToList());
                    long size = await downloadSizeHandle.ToUniTask();

                    DownloadSize += size;
                    loopCount = loopCount - 1;
                }
            }

            if(null != next)
                next();
        }
        catch(Exception ex)
        {
            Debug.LogError($"Exception: {ex.Message}");
        }
    }

    public static async UniTask Async_DownloadAssetBundles(ClientAssetUI clientAssetUI, Action next)
    {//1
        try
        {
            if(true == CheckLoadResourceList())
            {
                clientAssetUI.LoadingStart();
                int downloadTotalCount = _loadResourceList.Count();
                int downloadCount = 0;
                int loopCount = _loadResourceList.Count - 1;
                int backUpLoopCount = 0;

                string descText = SystemMessageDataManager.Instance.GetStringByIdx(300201);
                string progressValueText = "{0}/{1} ({2})";
                float totalProgressValue = 0.0f;

                clientAssetUI.DescText(descText);

                while(0 <= loopCount)
                {
                    if(false == ClientNetworkManager.Instance.CheckNetwork())
                    {
                        if(backUpLoopCount != loopCount && _loadResourceList.Count - 1 > loopCount)
                        {
                            loopCount = loopCount + 1;
                            backUpLoopCount = loopCount;
                        }
                        await UniTask.Yield();
                    }
                    else
                    {
                        IGrouping<int, IResourceLocation> groupedLocations = _loadResourceList[loopCount];
                        AsyncOperationHandle downloadhandle = Addressables.DownloadDependenciesAsync(groupedLocations.ToList());

                        float progressValue = 0.0f;
                        while(false == downloadhandle.IsDone)
                        {
                            await UniTask.Yield();
                            progressValue = downloadhandle.PercentComplete; //(downloadhandle.PercentComplete + downloadCount) / (float)downloadTotalCount;
                            clientAssetUI.LoadingValue(progressValue);
                        }

                        clientAssetUI.LoadingValue(progressValue);
                        await downloadhandle.ToUniTask();

                        Addressables.Release(downloadhandle);
                        loopCount = loopCount - 1;

                        downloadCount = downloadCount + 1;

                        totalProgressValue = (float)downloadCount / (float)downloadTotalCount;
                        //clientAssetUI.LoadingValue(progressValue);

                        clientAssetUI.ValueText(string.Format(progressValueText, downloadCount.ToString(), downloadTotalCount.ToString(), (totalProgressValue * 100f).ToSecondPointPersent()));
                    }
                }
            }

            Addressables.Release(_loadHandle);

            clientAssetUI.LoadingEnd();
            DownloadSize = 0;

            if(null != next)
                next();
        }
        catch(Exception ex)
        {
            Debug.LogError($"Exception: {ex.Message}");
        }
    }//1

    #endregion

    public static T GetAsset<T>(Action<T> callback, params string[] assetPathNames) where T : UnityEngine.Object
    {
        T returnAsset = null;
        AsyncOperationHandle handle = Addressables.LoadAssetsAsync<T>(assetPathNames, (obj) =>
        {
            if(null != callback)
                callback(obj);

            returnAsset = obj;
        });

        Addressables.Release(handle);

        return returnAsset;
    }

    public static T GetAsset<T>(params string[] assetPathNames) where T : UnityEngine.Object
    {
        T returnAsset = null;
        AsyncOperationHandle handle = Addressables.LoadAssetsAsync<T>(assetPathNames, (obj) =>
        {
            returnAsset = obj;
        });

        object wait = handle.WaitForCompletion();
        Addressables.Release(handle);

        return returnAsset;
    }

    public static string GetCSVData(Action<string, bool> callback, params string[] assetPathNames)
    {
        string assetText = null;
        bool checkLoad = false;
        string assetName = Path.Combine(assetPathNames);

        AsyncOperationHandle handle = Addressables.LoadAssetAsync<TextAsset>(assetName);

        handle.Completed += (completed) =>
        {
            if(completed.Result == null)
            {
                Debug.Log($"GetCSVAsset() not found file - {assetName}");
            }
            else
            {
                TextAsset textAsset = completed.Result as TextAsset;
                assetText = textAsset.text;
                checkLoad = true;
            }
            callback(assetText, checkLoad);
        };

        return assetText;
    }

    public static string GetCSVData(params string[] assetPathNames)
    {
        TextAsset textAsset = null;
        string assetName = Path.Combine(assetPathNames);
        AsyncOperationHandle handle = Addressables.LoadAssetsAsync<TextAsset>(assetName, (asset) =>
        {
            if(null != asset)
            {
                Debug.Log($"GetCSVAsset() success - {assetName}");
                textAsset = asset;
            }
        });
        object wait = handle.WaitForCompletion();
        if(wait == null)
        {
            Debug.LogError($"GetCSVAsset() failed - {assetName}");
            return null;
        }
        return textAsset.text;
    }

    /// <summary>
    /// Addressables 에서 데이터를 로드한다.
    /// </summary>
    /// <param name="assetPathName">경로를 포함한 파일명.</param>
    /// <param name="callbackArg">콜백함수에 전달받을 데이터.</param>
    /// <param name="completedCallback">Completed 되었을 때 호출될 콜백함수.</param>
    public static void GetAssetAsync<T>(string assetPathName, Action<AsyncOperationHandle<T>, object[]> completeCallback, params object[] callbackArg)
    {
        var handle = Addressables.LoadAssetAsync<T>(assetPathName);
        handle.Completed += h =>
        {
            completeCallback.Invoke(h, callbackArg);
        };
    }

    public static void LoadResourceLocations(string copyPath, Action<IResourceLocation, bool> completeCallback)
    {
        Initialze(() =>
        {
            IResourceLocation returnValue = null;
            bool isSuccessful = false;
            for(int i = 0; i < _loadHandle.Result.Count(); ++i)
            {
                //Debug.LogError("__ PrimaryKey = " + handle.Result[i].PrimaryKey);

                if(string.Equals(copyPath, _loadHandle.Result[i].PrimaryKey))
                {

                    returnValue = _loadHandle.Result[i];
                    isSuccessful = true;
                    break;
                }
            }
            completeCallback.Invoke(returnValue, isSuccessful);

            Addressables.Release(_loadHandle);
        });

        //Addressables.LoadResourceLocationsAsync(LABELS_NAME).Completed +=
        //(handle) =>
        //{
        //    IResourceLocation returnValue = null;
        //    if(null != handle.Result && 0 < handle.Result.Count())
        //    {
        //        Debug.LogError("__ copyPath = " + copyPath);
        //        for(int i = 0; i < handle.Result.Count(); ++i)
        //        {
        //            //Debug.LogError("__ PrimaryKey = " + handle.Result[i].PrimaryKey);

        //            if(string.Equals(copyPath, handle.Result[i].PrimaryKey))
        //            {
        //                returnValue = handle.Result[i];
        //                Debug.LogError("__ PrimaryKey있네 ?? = " + copyPath + " /returnValue = " + returnValue);
        //                break;
        //            }
        //        }
        //        completeCallback.Invoke(returnValue);
        //    }
        //    else
        //    {
        //        Debug.LogError("null = handle.Result");
        //    }
        //};
    }

    public static bool CheckLoadHandle()
    {
        return (null != _loadHandle.Result && 0 < _loadHandle.Result.Count);
    }

    public static bool CheckLoadResourceList()
    {
        return (null != _loadResourceList && 0 < _loadResourceList.Count());
    }
}