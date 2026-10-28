using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Storyborn.Editor
{
    // The sample scene explicitly owns the game; unrelated scenes stay untouched.
    public static class PrototypeSetup
    {
        [MenuItem("Storyborn/Open Prototype")]
        public static void Open()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Prototype.unity");
        }
        [MenuItem("Storyborn/Play Prototype")]
        public static void Play()
        {
            if(EditorApplication.isPlaying) return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Prototype.unity");
            EditorApplication.isPlaying=true;
        }
        [MenuItem("Storyborn/Run Combat Rule Checks")]
        public static void CheckRules()
        {
            var chain = new DashChain();
            Require(chain.TryStart(0), "First dash");
            Require(chain.TryStart(.2f), "Second dash");
            Require(!chain.TryStart(.3f), "Chain locks third dash");
            Require(chain.TryStart(.71f), "Dash resumes after lock");
            chain.Reset();
            Require(chain.TryStart(0) && chain.TryStart(.5f) && chain.Count == 1, "Spaced dashes reset chain");
            Require(CombatRules.InAttack(0,0,1,1.5f,0,2), "Forward hit");
            Require(!CombatRules.InAttack(0,0,1,-1.5f,0,2), "No backward hit");
            Require(!CombatRules.InAttack(0,0,1,1,3,2), "No hit through excessive height");
            Require(CombatRules.Overlap(0,0,1,1,.9f,0,1,1), "Body overlap");
            Require(!CombatRules.Overlap(0,0,1,1,2,0,1,1), "Separate bodies");
            Debug.Log("Storyborn: all 10 combat rule checks passed.");
        }
        static void Require(bool condition, string name)
        { if(!condition) throw new System.Exception("Combat rule failed: "+name); }
        [MenuItem("Storyborn/Build Windows x64")]
        public static void BuildWindows()
        {
            string folder = EditorUtility.OpenFolderPanel("Build destination", "", "");
            if(string.IsNullOrEmpty(folder)) return;
            BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/Prototype.unity"},folder+"/Storyborn.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
        }
    }
}
