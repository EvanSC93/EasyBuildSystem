using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class PlayerPrefTools
{
    [MenuItem("Tools/GameData/Clear All Saved Data")]
    public static void ClearSavedData()
    {
        PlayerPrefs.DeleteAll();

        bool isHaveData = Directory.Exists(Application.persistentDataPath + "/cache");
        if (isHaveData)
        {
            Directory.Delete(Application.persistentDataPath + "/cache", true);
        }
    }
}
