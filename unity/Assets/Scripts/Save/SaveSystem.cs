using System;
using System.IO;
using Politicmon.Core;
using UnityEngine;

namespace Politicmon.Save
{
    /// <summary>Saves the game as JSON in Application.persistentDataPath (atomic write through a temp file).</summary>
    public static class SaveSystem
    {
        public static string FilePath { get; set; }

        static string Path => FilePath ??= System.IO.Path.Combine(Application.persistentDataPath, "politicmon_save.json");

        public static bool HasSave => File.Exists(Path);

        public static bool Save()
        {
            try
            {
                GameState.Progress.savedAt = DateTime.Now.ToString("s");
                string json = JsonUtility.ToJson(GameState.Progress, true);
                string tmp = Path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(Path))
                    File.Delete(Path);
                File.Move(tmp, Path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Save failed: " + e.Message);
                return false;
            }
        }

        public static bool Load()
        {
            var progress = Read();
            if (progress == null)
                return false;
            GameState.Load(progress);
            return true;
        }

        public static GameProgress Read()
        {
            if (!HasSave)
                return null;
            try
            {
                var progress = JsonUtility.FromJson<GameProgress>(File.ReadAllText(Path));
                return progress != null && progress.version > 0 && progress.party != null ? progress : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void Delete()
        {
            if (HasSave)
                File.Delete(Path);
        }
    }
}
