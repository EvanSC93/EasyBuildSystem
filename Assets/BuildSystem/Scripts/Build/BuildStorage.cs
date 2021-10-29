using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BuildStorage : MonoBehaviour
{
    public static BuildStorage Instance;

    [SerializeField] private StorageType m_StorageType;
    
    [SerializeField] private bool m_AutoDefineInDataPath = true;
    [SerializeField] private bool m_AutoSave = false;
    [SerializeField] private bool m_LoadAndWaitEndFrame;
    [SerializeField] private bool m_SavePrefabs = true;
    [SerializeField] private bool m_LoadPrefabs = true;

    [SerializeField] private float m_AutoSaveInterval = 60f;
    
    [SerializeField] private string m_StorageOutputFile;
    
    private bool LoadedFile = false;
    private bool m_FileIsCorrupted;
    
    private float TimerAutoSave;
    
    private List<PieceBehaviour> PrefabsLoaded = new List<PieceBehaviour>();
    
    #region Methods

    /// <summary>
    /// (Editor) This method allows to load a storage file in Editor scene.
    /// </summary>
    public void LoadInEditor(string path)
    {
        int PrefabLoaded = 0;

        PrefabsLoaded = new List<PieceBehaviour>();

        BuildManager Manager = FindObjectOfType<BuildManager>();

        if (Manager == null)
        {
            Debug.LogError("<b>Easy Build System</b> : The BuildManager is not in the scene, please add it to load a file.");

            return;
        }

        FileStream Stream = File.Open(path, FileMode.Open);

        PieceData Serializer = null;

        try
        {
            using (StreamReader Reader = new StreamReader(Stream))
            {
                Serializer = JsonUtility.FromJson<PieceData>(Reader.ReadToEnd());
            }
        }
        catch
        {
            Stream.Close();

            Debug.LogError("<b>Easy Build System</b> : Please check that the file extension to load is correct.");

            return;
        }

        if (Serializer == null || Serializer.Pieces == null)
        {
            Debug.Log("<b>Easy Build System</b> : The file is empty or the data are corrupted.");

            return;
        }

        for (int i = 0; i < Serializer.Pieces.Count; i++)
        {
            if (Serializer.Pieces[i] != null)
            {
                PieceBehaviour Prefab = Manager.GetPieceById(Serializer.Pieces[i].Id);

                if (Prefab != null)
                {
                    PieceBehaviour PlacedPrefab = Manager.PlacePrefab(Prefab,
                        PieceData.ParseToVector3(Serializer.Pieces[i].Position),
                        PieceData.ParseToVector3(Serializer.Pieces[i].Rotation),
                        PieceData.ParseToVector3(Serializer.Pieces[i].Scale));

                    PlacedPrefab.transform.position = PieceData.ParseToVector3(Serializer.Pieces[i].Position);
                    PlacedPrefab.transform.eulerAngles = PieceData.ParseToVector3(Serializer.Pieces[i].Rotation);
                    PlacedPrefab.transform.localScale = PieceData.ParseToVector3(Serializer.Pieces[i].Scale);

                    PrefabsLoaded.Add(PlacedPrefab);

                    PrefabLoaded++;
                }
                else
                {
                    Debug.Log("<b>Easy Build System</b> : The Prefab (" + Serializer.Pieces[i].Id + ") does not exists in the Build Manager.");
                }
            }
        }

        Stream.Close();

        Debug.Log("<b>Easy Build System</b> : Data file loaded " + PrefabLoaded + " Prefab(s) loaded in " + Time.realtimeSinceStartup.ToString("#.##") + " ms in the Editor scene.");

        PrefabsLoaded.Clear();
    }

    /// <summary>
    /// This method allows to load the storage file.
    /// </summary>
    public void LoadStorageFile()
    {
        StartCoroutine(LoadDataFile());
    }

    /// <summary>
    /// This method allows to save the storage file.
    /// </summary>
    public void SaveStorageFile()
    {
        StartCoroutine(SaveDataFile());
    }

    /// <summary>
    /// This method allows to delete the storage file.
    /// </summary>
    public void DeleteStorageFile()
    {
        StartCoroutine(DeleteDataFile());
    }

    /// <summary>
    /// This method allows to check if the storage file.
    /// </summary>
    public bool ExistsStorageFile()
    {
        return File.Exists(m_StorageOutputFile);
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (m_AutoDefineInDataPath)
        {
            string path = Application.persistentDataPath + "/cache";
            string fileName = "/data.dat";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            m_StorageOutputFile = path + fileName;
        }

        if (m_LoadPrefabs)
        {
            StartCoroutine(LoadDataFile());
        }

        if (m_AutoSave)
        {
            TimerAutoSave = m_AutoSaveInterval;
        }
    }

    private void Update()
    {
        if (m_AutoSave)
        {
            if (TimerAutoSave <= 0)
            {
                Debug.Log("<b>Easy Build System</b> : Saving of " + FindObjectsOfType<PieceBehaviour>().Length + " Part(s) ...");

                SaveStorageFile();

                Debug.Log("<b>Easy Build System</b> : Saved with successfuly !");

                TimerAutoSave = m_AutoSaveInterval;
            }
            else
            {
                TimerAutoSave -= Time.deltaTime;
            }
        }
    }

    private void OnApplicationPause(bool pause)
    {

#if UNITY_EDITOR

#else
            if (m_StorageType == StorageType.Android)
            {
                if (!m_SavePrefabs)
                {
                    return;
                }

                SaveStorageFile();
            }
#endif
    }

    private void OnApplicationQuit()
    {
        if (!m_SavePrefabs)
        {
            return;
        }

        SaveStorageFile();
    }

    private IEnumerator LoadDataFile()
    {
        if (m_StorageType == StorageType.Desktop)
        {
            if (m_StorageOutputFile == string.Empty || Directory.Exists(m_StorageOutputFile))
            {
                Debug.LogError("<b>Easy Build System</b> : Please define output path.");

                yield break;
            }
        }

        int PrefabLoaded = 0;

        PrefabsLoaded = new List<PieceBehaviour>();

        if (ExistsStorageFile() || m_StorageType == StorageType.Android)
        {
            Debug.Log("<b>Easy Build System</b> : Loading data file ...");

            FileStream Stream = null;

            if (m_StorageType == StorageType.Desktop)
            {
                Stream = File.Open(m_StorageOutputFile, FileMode.Open);
            }

            PieceData serializer = null;

            try
            {
                if (m_StorageType == StorageType.Desktop)
                {
                    using (StreamReader Reader = new StreamReader(Stream))
                    {
                        serializer = JsonUtility.FromJson<PieceData>(Reader.ReadToEnd());
                    }
                }
                else
                {
                    serializer = JsonUtility.FromJson<PieceData>(PlayerPrefs.GetString("EBS_Storage"));
                }
            }
            catch (Exception ex)
            {
                Stream.Close();

                m_FileIsCorrupted = true;

                Debug.LogError("<b>Easy Build System</b> : " + ex);

                BuildEvent.instance.OnStorageLoadingResult.Invoke(null);
                yield break;
            }

            if (serializer == null)
            {
                BuildEvent.instance.OnStorageLoadingResult.Invoke(null);
                yield break;
            }

            for (int i = 0; i < serializer.Pieces.Count; i++)
            {
                if (serializer.Pieces[i] != null)
                {
                    PieceBehaviour Prefab = BuildManager.instance.GetPieceById(serializer.Pieces[i].Id);

                    if (Prefab != null)
                    {
                        PieceBehaviour placedPrefab = BuildManager.instance.PlacePrefab(Prefab,
                            PieceData.ParseToVector3(serializer.Pieces[i].Position),
                            PieceData.ParseToVector3(serializer.Pieces[i].Rotation),
                            PieceData.ParseToVector3(serializer.Pieces[i].Scale));

                        placedPrefab.name = serializer.Pieces[i].Name;
                        placedPrefab.transform.position = PieceData.ParseToVector3(serializer.Pieces[i].Position);
                        placedPrefab.transform.eulerAngles = PieceData.ParseToVector3(serializer.Pieces[i].Rotation);
                        placedPrefab.transform.localScale = PieceData.ParseToVector3(serializer.Pieces[i].Scale);

                        PrefabsLoaded.Add(placedPrefab);

                        PrefabLoaded++;

                        if (m_LoadAndWaitEndFrame)
                        {
                            yield return new WaitForEndOfFrame();
                        }
                    }
                    else
                    {
                        Debug.Log("<b>Easy Build System</b> : The prefab (" + serializer.Pieces[i].Id + ") does not exists in the list of Build Manager.");
                    }
                }
            }

            if (Stream != null)
            {
                Stream.Close();
            }

            if (!m_LoadAndWaitEndFrame)
            {
                Debug.Log("<b>Easy Build System</b> : Data file loaded " + PrefabLoaded + " prefab(s) loaded in " + Time.realtimeSinceStartup.ToString("#.##") + " ms.");
            }
            else
            {
                Debug.Log("<b>Easy Build System</b> : Data file loaded " + PrefabLoaded + " prefab(s).");
            }

            LoadedFile = true;

            BuildEvent.instance.OnStorageLoadingResult.Invoke(PrefabsLoaded.ToArray());

            yield break;
        }
        else
        {
            BuildEvent.instance.OnStorageLoadingResult.Invoke(null);
        }

        yield break;
    }

    private IEnumerator SaveDataFile()
    {
        if (m_FileIsCorrupted)
        {
            Debug.LogWarning("<b>Easy Build System</b> : The file is corrupted, the Prefabs could not be saved.");

            yield break;
        }

        if (m_StorageOutputFile == string.Empty || Directory.Exists(m_StorageOutputFile))
        {
            Debug.LogError("<b>Easy Build System</b> : Please define out file path.");

            yield break;
        }

        int SavedCount = 0;

        if (ExistsStorageFile())
        {
            File.Delete(m_StorageOutputFile);
        }
        else
        {
            BuildEvent.instance.OnStorageSavingResult.Invoke(null);
        }

        if (BuildManager.instance.CachedParts.Count > 0)
        {
            Debug.Log("<b>Easy Build System</b> : Saving data file ...");

            FileStream Stream = null;

            if (m_StorageType == StorageType.Desktop)
            {
                Stream = File.Create(m_StorageOutputFile);
            }

            PieceData Data = new PieceData();

            PieceBehaviour[] PartsAtSave = BuildManager.instance.CachedParts.ToArray();

            for (int i = 0; i < PartsAtSave.Length; i++)
            {
                if (PartsAtSave[i] != null)
                {
                    if (PartsAtSave[i].CurrentState == StateType.Placed || PartsAtSave[i].CurrentState == StateType.Remove)
                    {
                        PieceData.SerializedPiece DataTemp = new PieceData.SerializedPiece
                        {
                            Id = PartsAtSave[i].ID,
                            Name = PartsAtSave[i].name,
                            Position = PieceData.ParseToSerializedVector3(PartsAtSave[i].transform.position),
                            Rotation = PieceData.ParseToSerializedVector3(PartsAtSave[i].transform.eulerAngles),
                            Scale = PieceData.ParseToSerializedVector3(PartsAtSave[i].transform.localScale),
                        };

                        Data.Pieces.Add(DataTemp);

                        SavedCount++;
                    }
                }
            }

            if (m_StorageType == StorageType.Desktop)
            {
                using (StreamWriter Writer = new StreamWriter(Stream))
                {
                    Writer.Write(JsonUtility.ToJson(Data));
                }

                Stream.Close();
            }
            else
            {
                PlayerPrefs.SetString("EBS_Storage", JsonUtility.ToJson(Data));

                PlayerPrefs.Save();
            }

            Debug.Log("<b>Easy Build System</b> : Data file saved " + SavedCount + " Prefab(s).");

            if (BuildEvent.instance != null)
            {
                BuildEvent.instance.OnStorageSavingResult.Invoke(PrefabsLoaded.ToArray());
            }

            yield break;
        }
    }

    private IEnumerator DeleteDataFile()
    {
        if (m_StorageOutputFile == string.Empty || Directory.Exists(m_StorageOutputFile))
        {
            Debug.LogError("<b>Easy Build System</b> : Please define out file path.");

            yield break;
        }

        if (File.Exists(m_StorageOutputFile) == true)
        {
            for (int i = 0; i < PrefabsLoaded.Count; i++)
            {
                Destroy(PrefabsLoaded[i].gameObject);
            }

            File.Delete(m_StorageOutputFile);

            Debug.Log("<b>Easy Build System</b> : The storage file has been removed.");
        }
        else
        {
            if (BuildEvent.instance != null)
            {
                BuildEvent.instance.OnStorageSavingResult.Invoke(null);
            }
        }
    }

    #endregion Methods
}