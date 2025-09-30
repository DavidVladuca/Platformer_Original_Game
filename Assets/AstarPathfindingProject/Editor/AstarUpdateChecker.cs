using UnityEngine;
using UnityEditor;
#if UNITY_2018_1_OR_NEWER
using UnityEngine.Networking;
#endif
using System.Collections.Generic;
using System.Linq;

namespace Pathfinding
{
    /// <summary>Handles update checking for the A* Pathfinding Project</summary>
    [InitializeOnLoad]
    public static class AstarUpdateChecker
    {
#if UNITY_2018_1_OR_NEWER
        /// <summary>Used for downloading new version information</summary>
        static UnityWebRequest updateCheckDownload;
#else
        /// <summary>Used for downloading new version information</summary>
        static WWW updateCheckDownload;
#endif

        static System.DateTime _lastUpdateCheck;
        static bool _lastUpdateCheckRead;

        static System.Version _latestVersion;
        static System.Version _latestBetaVersion;

        /// <summary>Description of the latest update of the A* Pathfinding Project</summary>
        static string _latestVersionDescription;

        static bool hasParsedServerMessage;

        /// <summary>Number of days between update checks</summary>
        const double updateCheckRate = 1F;

        /// <summary>URL to the version file containing the latest version number.</summary>
        const string updateURL = "https://www.arongranberg.com/astar/version.php";

        /// <summary>Last time an update check was made</summary>
        public static System.DateTime lastUpdateCheck
        {
            get
            {
                try
                {
                    if (_lastUpdateCheckRead) return _lastUpdateCheck;

                    _lastUpdateCheck = System.DateTime.Parse(EditorPrefs.GetString("AstarLastUpdateCheck", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
                    _lastUpdateCheckRead = true;
                }
                catch (System.FormatException)
                {
                    lastUpdateCheck = System.DateTime.UtcNow;
                    Debug.LogWarning("Invalid DateTime string encountered when loading from preferences");
                }
                return _lastUpdateCheck;
            }
            private set
            {
                _lastUpdateCheck = value;
                EditorPrefs.SetString("AstarLastUpdateCheck", _lastUpdateCheck.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        /// <summary>Latest version of the A* Pathfinding Project</summary>
        public static System.Version latestVersion
        {
            get
            {
                RefreshServerMessage();
                return _latestVersion ?? AstarPath.Version;
            }
            private set
            {
                _latestVersion = value;
            }
        }

        /// <summary>Latest beta version of the A* Pathfinding Project</summary>
        public static System.Version latestBetaVersion
        {
            get
            {
                RefreshServerMessage();
                return _latestBetaVersion ?? AstarPath.Version;
            }
            private set
            {
                _latestBetaVersion = value;
            }
        }

        /// <summary>Summary of the latest update</summary>
        public static string latestVersionDescription
        {
            get
            {
                RefreshServerMessage();
                return _latestVersionDescription ?? "";
            }
            private set
            {
                _latestVersionDescription = value;
            }
        }

        /// <summary>
        /// Holds various URLs and text for the editor.
        /// </summary>
        static Dictionary<string, string> astarServerData = new Dictionary<string, string> {
            { "URL:modifiers", "http://www.arongranberg.com/astar/docs/modifiers.php" },
            { "URL:astarpro", "http://arongranberg.com/unity/a-pathfinding/astarpro/" },
            { "URL:documentation", "http://arongranberg.com/astar/docs/" },
            { "URL:findoutmore", "http://arongranberg.com/astar" },
            { "URL:download", "http://arongranberg.com/unity/a-pathfinding/download" },
            { "URL:changelog", "http://arongranberg.com/astar/docs/changelog.php" },
            { "URL:tags", "http://arongranberg.com/astar/docs/tags.php" },
            { "URL:homepage", "http://arongranberg.com/astar/" }
        };

        static AstarUpdateChecker()
        {
            EditorApplication.update += UpdateCheckLoop;
            EditorBase.getDocumentationURL = () => GetURL("documentation");
        }

        static void RefreshServerMessage()
        {
            if (!hasParsedServerMessage)
            {
                var serverMessage = EditorPrefs.GetString("AstarServerMessage");

                if (!string.IsNullOrEmpty(serverMessage))
                {
                    ParseServerMessage(serverMessage);
                    ShowUpdateWindowIfRelevant();
                }
            }
        }

        public static string GetURL(string tag)
        {
            RefreshServerMessage();
            string url;
            astarServerData.TryGetValue("URL:" + tag, out url);
            return url ?? "";
        }

        public static void CheckForUpdatesNow()
        {
            lastUpdateCheck = System.DateTime.UtcNow.AddDays(-5);
            EditorApplication.update -= UpdateCheckLoop;
            EditorApplication.update += UpdateCheckLoop;
        }

        static void UpdateCheckLoop()
        {
            if (!CheckForUpdates())
            {
                EditorApplication.update -= UpdateCheckLoop;
            }
        }

        static bool CheckForUpdates()
        {
#if UNITY_2018_1_OR_NEWER
            if (updateCheckDownload != null)
            {
#if UNITY_2020_1_OR_NEWER
                if (updateCheckDownload.isDone)
                {
                    if (updateCheckDownload.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning("Error checking for updates to A* Pathfinding Project\n" + updateCheckDownload.error);
                        updateCheckDownload.Dispose();
                        updateCheckDownload = null;
                        return false;
                    }
                    UpdateCheckCompleted(updateCheckDownload.downloadHandler.text);
                    updateCheckDownload.Dispose();
                    updateCheckDownload = null;
                }
#else
                if (updateCheckDownload.isDone) {
                    if (updateCheckDownload.isNetworkError || updateCheckDownload.isHttpError) {
                        Debug.LogWarning("Error checking for updates to A* Pathfinding Project\n" + updateCheckDownload.error);
                        updateCheckDownload.Dispose();
                        updateCheckDownload = null;
                        return false;
                    }
                    UpdateCheckCompleted(updateCheckDownload.downloadHandler.text);
                    updateCheckDownload.Dispose();
                    updateCheckDownload = null;
                }
#endif
            }
#else
            if (updateCheckDownload != null && updateCheckDownload.isDone) {
                if (!string.IsNullOrEmpty(updateCheckDownload.error)) {
                    Debug.LogWarning("Error checking for updates to A* Pathfinding Project\n" + updateCheckDownload.error);
                    updateCheckDownload = null;
                    return false;
                }
                UpdateCheckCompleted(updateCheckDownload.text);
                updateCheckDownload = null;
            }
#endif

            var offsetMinutes = (Application.isPlaying && Time.time > 60) || AstarPath.active != null ? -20 : 20;
            var minutesUntilUpdate = lastUpdateCheck.AddDays(updateCheckRate).AddMinutes(offsetMinutes).Subtract(System.DateTime.UtcNow).TotalMinutes;
            if (minutesUntilUpdate < 0)
            {
                DownloadVersionInfo();
            }

            return updateCheckDownload != null || minutesUntilUpdate < 10;
        }

        static void DownloadVersionInfo()
        {
            var script = AstarPath.active != null ? AstarPath.active : GameObject.FindObjectOfType(typeof(AstarPath)) as AstarPath;

            if (script != null)
            {
                script.ConfigureReferencesInternal();
                if ((!Application.isPlaying && (script.data.graphs == null || script.data.graphs.Length == 0)) || script.data.graphs == null)
                {
                    script.data.DeserializeGraphs();
                }
            }

            bool mecanim = GameObject.FindObjectOfType(typeof(Animator)) != null;

#if UNITY_2018_1_OR_NEWER
            string v = UnityWebRequest.EscapeURL(AstarPath.Version.ToString());
            string branch = UnityWebRequest.EscapeURL(AstarPath.Branch);
            string unityVersion = UnityWebRequest.EscapeURL(Application.unityVersion);
            string targetPlatform = UnityWebRequest.EscapeURL(EditorUserBuildSettings.activeBuildTarget.ToString());
#else
            string v = AstarPath.Version.ToString();
            string branch = AstarPath.Branch;
            string unityVersion = Application.unityVersion;
            string targetPlatform = EditorUserBuildSettings.activeBuildTarget.ToString();
#endif

            string query = updateURL +
                           "?v=" + v +
                           "&pro=0" +
                           "&check=" + updateCheckRate + "&distr=" + AstarPath.Distribution +
                           "&unitypro=" + (Application.HasProLicense() ? "1" : "0") +
                           "&inscene=" + (script != null ? "1" : "0") +
                           "&targetplatform=" + targetPlatform +
                           "&devplatform=" + Application.platform +
                           "&mecanim=" + (mecanim ? "1" : "0") +
                           "&hasNavmesh=" + (script != null && script.data.graphs.Any(g => g.GetType().Name == "NavMeshGraph") ? 1 : 0) +
                           "&hasPoint=" + (script != null && script.data.graphs.Any(g => g.GetType().Name == "PointGraph") ? 1 : 0) +
                           "&hasGrid=" + (script != null && script.data.graphs.Any(g => g.GetType().Name == "GridGraph") ? 1 : 0) +
                           "&hasLayered=" + (script != null && script.data.graphs.Any(g => g.GetType().Name == "LayerGridGraph") ? 1 : 0) +
                           "&hasRecast=" + (script != null && script.data.graphs.Any(g => g.GetType().Name == "RecastGraph") ? 1 : 0) +
                           "&hasCustom=" + (script != null && script.data.graphs.Any(g => g != null && !g.GetType().FullName.Contains("Pathfinding.")) ? 1 : 0) +
                           "&graphCount=" + (script != null ? script.data.graphs.Count(g => g != null) : 0) +
                           "&unityversion=" + unityVersion +
                           "&branch=" + branch;

#if UNITY_2018_1_OR_NEWER
            updateCheckDownload = UnityWebRequest.Get(query);
            updateCheckDownload.SendWebRequest();
#else
            updateCheckDownload = new WWW(query);
#endif
            lastUpdateCheck = System.DateTime.UtcNow;
        }

        static void UpdateCheckCompleted(string result)
        {
            EditorPrefs.SetString("AstarServerMessage", result);
            ParseServerMessage(result);
            ShowUpdateWindowIfRelevant();
        }

        static void ParseServerMessage(string result)
        {
            if (string.IsNullOrEmpty(result)) return;

            hasParsedServerMessage = true;
            string[] splits = result.Split('|');
            latestVersionDescription = splits.Length > 1 ? splits[1] : "";

            if (splits.Length > 4)
            {
                var fields = splits.Skip(4).ToArray();
                for (int i = 0; i < (fields.Length / 2) * 2; i += 2)
                {
                    string key = fields[i];
                    string val = fields[i + 1];
                    astarServerData[key] = val;
                }
            }

            try
            {
                if (astarServerData.ContainsKey("VERSION:branch") && !string.IsNullOrEmpty(astarServerData["VERSION:branch"]))
                {
                    latestVersion = new System.Version(astarServerData["VERSION:branch"]);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Could not parse latest version\n" + ex);
            }

            try
            {
                if (astarServerData.ContainsKey("VERSION:beta") && !string.IsNullOrEmpty(astarServerData["VERSION:beta"]))
                {
                    latestBetaVersion = new System.Version(astarServerData["VERSION:beta"]);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Could not parse latest beta version\n" + ex);
            }
        }

        static void ShowUpdateWindowIfRelevant()
        {
#if !ASTAR_ATAVISM
            try
            {
                System.DateTime remindDate;
                var remindVersion = new System.Version(EditorPrefs.GetString("AstarRemindUpdateVersion", "0.0.0.0"));
                if (latestVersion == remindVersion && System.DateTime.TryParse(EditorPrefs.GetString("AstarRemindUpdateDate", "1/1/1971 00:00:01"), out remindDate))
                {
                    if (System.DateTime.UtcNow < remindDate) return;
                }
                else
                {
                    EditorPrefs.DeleteKey("AstarRemindUpdateDate");
                    EditorPrefs.DeleteKey("AstarRemindUpdateVersion");
                }
            }
            catch
            {
                Debug.LogError("Invalid AstarRemindUpdateVersion or AstarRemindUpdateDate");
            }

            var skipVersion = new System.Version(EditorPrefs.GetString("AstarSkipUpToVersion", AstarPath.Version.ToString()));

            if (AstarPathEditor.FullyDefinedVersion(latestVersion) != AstarPathEditor.FullyDefinedVersion(skipVersion) &&
                AstarPathEditor.FullyDefinedVersion(latestVersion) > AstarPathEditor.FullyDefinedVersion(AstarPath.Version))
            {
                EditorPrefs.DeleteKey("AstarSkipUpToVersion");
                EditorPrefs.DeleteKey("AstarRemindUpdateDate");
                EditorPrefs.DeleteKey("AstarRemindUpdateVersion");

                AstarUpdateWindow.Init(latestVersion, latestVersionDescription);
            }
#endif
        }
    }
}
