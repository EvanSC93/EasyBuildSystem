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
    
    private bool m_FileIsCorrupted;
    private float m_TimerAutoSave;
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
            m_TimerAutoSave = m_AutoSaveInterval;
        }
    }

    private void Update()
    {
        if (m_AutoSave)
        {
            if (m_TimerAutoSave <= 0)
            {
                SaveStorageFile();
                m_TimerAutoSave = m_AutoSaveInterval;
            }
            else
            {
                m_TimerAutoSave -= Time.deltaTime;
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
        int prefabLoaded = 0;

        m_PrefabsLoaded = new List<PieceBehaviour>();

        BuildManager manager = FindObjectOfType<BuildManager>();

        if (manager == null)
        {
            Debug.LogError("<b>Easy Build System</b> : The BuildManager is not in the scene, please add it to load a file.");

            return;
        }

        FileStream stream = File.Open(path, FileMode.Open);

        PieceData serializer = null;

        try
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                serializer = JsonUtility.FromJson<PieceData>(reader.ReadToEnd());
            }
        }
        catch
        {
            stream.Close();

            Debug.LogError("<b>Easy Build System</b> : Please check that the file extension to load is correct.");

            return;
        }

        if (serializer == null || serializer.Pieces == null)
        {
            Debug.LogError("<b>Easy Build System</b> : The file is empty or the data are corrupted.");

            return;
        }

        for (int i = 0; i < serializer.Pieces.Count; i++)
        {
            if (serializer.Pieces[i] != null)
            {
                PieceBehaviour prefab = manager.GetPieceById(serializer.Pieces[i].Id);

                if (prefab != null)
                {
                    PieceBehaviour placedPrefab = manager.PlacePrefab(prefab,
                        PieceData.ParseToVector3(serializer.Pieces[i].Position),
                        PieceData.ParseToVector3(serializer.Pieces[i].Rotation),
                        PieceData.ParseToVector3(serializer.Pieces[i].Scale));

                    placedPrefab.transform.position = PieceData.ParseToVector3(serializer.Pieces[i].Position);
                    placedPrefab.transform.eulerAngles = PieceData.ParseToVector3(serializer.Pieces[i].Rotation);
                    placedPrefab.transform.localScale = PieceData.ParseToVector3(serializer.Pieces[i].Scale);
                    placedPrefab.Model.transform.rotation = Quaternion.Euler(PieceData.ParseToVector3(serializer.Pieces[i].ModelRotation));
                    
                    m_PrefabsLoaded.Add(placedPrefab);

                    prefabLoaded++;
                }
                else
                {
                    Debug.LogError("<b>Easy Build System</b> : The Prefab (" + serializer.Pieces[i].Id + ") does not exists in the Build Manager.");
                }
            }
        }

        stream.Close();

        Debug.LogError("<b>Easy Build System</b> : Data file loaded " + prefabLoaded + " Prefab(s) loaded in " + Time.realtimeSinceStartup.ToString("#.##") + " ms in the Editor scene.");

        m_PrefabsLoaded.Clear();
    }

    /// <summary>
    /// This method allows to save the storage file.
    /// </summary>
    public void SaveStorageFile()
    {
        StartCoroutine(SaveDataFile());
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

            FileStream stream = null;

            stream = File.Open(GetOutPutPath(), FileMode.Open);

            PieceData serializer = null;

            try
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    serializer = JsonUtility.FromJson<PieceData>(reader.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                stream.Close();

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
                        placedPiece.Model.transform.rotation = Quaternion.Euler(PieceData.ParseToVector3(serializer.Pieces[i].ModelRotation));
                        
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

            if (stream != null)
            {
                stream.Close();
            }

            BuildEvent.instance.OnStorageLoadingResult.Invoke(m_PrefabsLoaded.ToArray());
        }
        else
        {
            //Debug.LogError("<b>Easy Build System</b> : No file");
            BuildEvent.instance.OnStorageLoadingResult.Invoke(null);
        }
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

        int savedCount = 0;

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

            FileStream stream = null;

            stream = File.Create(GetOutPutPath());

            PieceData data = new PieceData();

            PieceBehaviour[] partsAtSave = BuildManager.instance.CachedParts.ToArray();

            for (int i = 0; i < partsAtSave.Length; i++)
            {
                if (partsAtSave[i] != null)
                {
                    if (partsAtSave[i].CurrentState == StateType.Placed)
                    {
                        PieceData.SerializedPiece dataTemp = new PieceData.SerializedPiece
                        {
                            Id = partsAtSave[i].ID,
                            Name = partsAtSave[i].name,
                            Position = PieceData.ParseToSerializedVector3(partsAtSave[i].transform.position),
                            Rotation = PieceData.ParseToSerializedVector3(partsAtSave[i].transform.eulerAngles),
                            ModelRotation = PieceData.ParseToSerializedVector3(partsAtSave[i].Model.transform.eulerAngles),
                            Scale = PieceData.ParseToSerializedVector3(partsAtSave[i].transform.localScale),
                        };

                        data.Pieces.Add(dataTemp);

                        savedCount++;
                    }
                }
            }

            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(JsonUtility.ToJson(data));
            }

            stream.Close();

            //Debug.LogError("<b>Easy Build System</b> : Data file saved " + SavedCount + " Prefab(s).");

            if (BuildEvent.instance != null)
            {
                BuildEvent.instance.OnStorageSavingResult.Invoke(m_PrefabsLoaded.ToArray());
            }
        }
    }

    #endregion Methods
}