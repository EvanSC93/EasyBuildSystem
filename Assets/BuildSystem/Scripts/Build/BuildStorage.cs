using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BuildStorage : MonoBehaviour
{
    public static BuildStorage Instance;
    
    [SerializeField] private bool m_AutoSave = false;
    [SerializeField] private bool m_LoadAndWaitEndFrame;
    [SerializeField] private bool m_SavePrefabs = true;
    [SerializeField] private bool m_LoadPrefabs = true;

    [SerializeField] private float m_AutoSaveInterval = 60f;
    
    [SerializeField] private string m_StorageOutputFile;

    private bool LoadedFile = false;
    private bool m_FileIsCorrupted;
    
    private float TimerAutoSave;
    
    private List<PieceBehaviour> m_PrefabsLoaded = new List<PieceBehaviour>();
    
    #region Methods

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
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
                //Debug.LogError("<b>Easy Build System</b> : Saving of " + FindObjectsOfType<PieceBehaviour>().Length + " Part(s) ...");

                SaveStorageFile();

                //Debug.LogError("<b>Easy Build System</b> : Saved with successfuly !");

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
        if (pause)
        {
            if (!m_SavePrefabs)
            {
                return;
            }

            SaveStorageFile();
        }
    }

    private void OnApplicationQuit()
    {
        if (!m_SavePrefabs)
        {
            return;
        }

        SaveStorageFile();
    }
    
    public string GetOutPutPath()
    {
        if (m_StorageOutputFile == "")
        {
            string path = Application.persistentDataPath + "/cache";
            string fileName = "/data.dat";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            m_StorageOutputFile = path + fileName;
        }

        return m_StorageOutputFile;
    }
    
    /// <summary>
    /// (Editor) This method allows to load a storage file in Editor scene.
    /// </summary>
    public void LoadInEditor(string path)
    {
        int PrefabLoaded = 0;

        m_PrefabsLoaded = new List<PieceBehaviour>();

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
            Debug.LogError("<b>Easy Build System</b> : The file is empty or the data are corrupted.");

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

                    m_PrefabsLoaded.Add(PlacedPrefab);

                    PrefabLoaded++;
                }
                else
                {
                    Debug.LogError("<b>Easy Build System</b> : The Prefab (" + Serializer.Pieces[i].Id + ") does not exists in the Build Manager.");
                }
            }
        }

        Stream.Close();

        Debug.LogError("<b>Easy Build System</b> : Data file loaded " + PrefabLoaded + " Prefab(s) loaded in " + Time.realtimeSinceStartup.ToString("#.##") + " ms in the Editor scene.");

        m_PrefabsLoaded.Clear();
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
        return File.Exists(GetOutPutPath());
    }

    private IEnumerator LoadDataFile()
    {
        if (GetOutPutPath() == string.Empty || Directory.Exists(GetOutPutPath()))
        {
            Debug.LogError("<b>Easy Build System</b> : Please define output path.");

            yield break;
        }

        int prefabLoaded = 0;

        m_PrefabsLoaded = new List<PieceBehaviour>();

        bool result = ExistsStorageFile();

        if (result)
        {
            //Debug.LogError("<b>Easy Build System</b> : Loading data file ...");

            FileStream Stream = null;

            Stream = File.Open(GetOutPutPath(), FileMode.Open);

            PieceData serializer = null;

            try
            {
                using (StreamReader Reader = new StreamReader(Stream))
                {
                    serializer = JsonUtility.FromJson<PieceData>(Reader.ReadToEnd());
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
                    PieceBehaviour temp = BuildManager.instance.GetPieceById(serializer.Pieces[i].Id);

                    if (temp != null)
                    {
                        PieceBehaviour placedPiece = BuildManager.instance.PlacePrefab(temp,
                            PieceData.ParseToVector3(serializer.Pieces[i].Position),
                            PieceData.ParseToVector3(serializer.Pieces[i].Rotation),
                            PieceData.ParseToVector3(serializer.Pieces[i].Scale));

                        placedPiece.name = serializer.Pieces[i].Name;
                        placedPiece.transform.position = PieceData.ParseToVector3(serializer.Pieces[i].Position);
                        placedPiece.transform.eulerAngles = PieceData.ParseToVector3(serializer.Pieces[i].Rotation);
                        placedPiece.transform.localScale = PieceData.ParseToVector3(serializer.Pieces[i].Scale);

                        m_PrefabsLoaded.Add(placedPiece);

                        prefabLoaded++;

                        if (m_LoadAndWaitEndFrame)
                        {
                            yield return new WaitForEndOfFrame();
                        }
                    }
                    else
                    {
                        Debug.LogError("<b>Easy Build System</b> : The prefab (" + serializer.Pieces[i].Id + ") does not exists in the list of Build Manager.");
                    }
                }
            }

            if (Stream != null)
            {
                Stream.Close();
            }

            if (!m_LoadAndWaitEndFrame)
            {
                //Debug.LogError("<b>Easy Build System</b> : Data file loaded " + PrefabLoaded + " prefab(s) loaded in " + Time.realtimeSinceStartup.ToString("#.##") + " ms.");
            }
            else
            {
                //Debug.LogError("<b>Easy Build System</b> : Data file loaded " + PrefabLoaded + " prefab(s).");
            }

            LoadedFile = true;

            BuildEvent.instance.OnStorageLoadingResult.Invoke(m_PrefabsLoaded.ToArray());

            yield break;
        }
        else
        {
            //Debug.LogError("<b>Easy Build System</b> : No file");
            BuildEvent.instance.OnStorageLoadingResult.Invoke(null);
        }

        yield break;
    }

    private IEnumerator SaveDataFile()
    {
        if (m_FileIsCorrupted)
        {
            Debug.LogError("<b>Easy Build System</b> : The file is corrupted, the Prefabs could not be saved.");

            yield break;
        }

        if (GetOutPutPath() == string.Empty || Directory.Exists(GetOutPutPath()))
        {
            Debug.LogError("<b>Easy Build System</b> : Please define out file path.");

            yield break;
        }

        int SavedCount = 0;

        if (ExistsStorageFile())
        {
            File.Delete(GetOutPutPath());
        }
        else
        {
            BuildEvent.instance.OnStorageSavingResult.Invoke(null);
        }

        if (BuildManager.instance.CachedParts.Count > 0)
        {
            //Debug.LogError("<b>Easy Build System</b> : Saving data file ...");

            FileStream Stream = null;

            Stream = File.Create(GetOutPutPath());

            PieceData Data = new PieceData();

            PieceBehaviour[] PartsAtSave = BuildManager.instance.CachedParts.ToArray();

            for (int i = 0; i < PartsAtSave.Length; i++)
            {
                if (PartsAtSave[i] != null)
                {
                    if (PartsAtSave[i].CurrentState == StateType.Placed)
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

            using (StreamWriter Writer = new StreamWriter(Stream))
            {
                Writer.Write(JsonUtility.ToJson(Data));
            }

            Stream.Close();

            //Debug.LogError("<b>Easy Build System</b> : Data file saved " + SavedCount + " Prefab(s).");

            if (BuildEvent.instance != null)
            {
                BuildEvent.instance.OnStorageSavingResult.Invoke(m_PrefabsLoaded.ToArray());
            }

            yield break;
        }
    }

    private IEnumerator DeleteDataFile()
    {
        if (GetOutPutPath() == string.Empty || Directory.Exists(GetOutPutPath()))
        {
            Debug.LogError("<b>Easy Build System</b> : Please define out file path.");

            yield break;
        }

        if (File.Exists(GetOutPutPath()) == true)
        {
            for (int i = 0; i < m_PrefabsLoaded.Count; i++)
            {
                Destroy(m_PrefabsLoaded[i].gameObject);
            }

            File.Delete(GetOutPutPath());

            Debug.LogError("<b>Easy Build System</b> : The storage file has been removed.");
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