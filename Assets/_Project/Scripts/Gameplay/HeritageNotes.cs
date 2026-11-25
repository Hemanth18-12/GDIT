using System;
using System.Text;
using UnityEngine;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>Six short sourced notes, discovered by exploring the existing places.</summary>
    public sealed class HeritageNotes : MonoBehaviour
    {
        [Serializable] public class Note { public string id,title,fact,connection,source,url; public int place; }
        [Serializable] class FileData { public Note[] notes; }
        static Note[] notes;
        public static int Page;
        float next;
        public static Note[] All
        {
            get
            {
                if(notes==null) {var asset=Resources.Load<TextAsset>("Story/heritage_notes");notes=asset!=null?JsonUtility.FromJson<FileData>(asset.text).notes:Array.Empty<Note>();}
                return notes;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot()
        {
            var go=new GameObject("HeritageNotes");DontDestroyOnLoad(go);go.AddComponent<HeritageNotes>();
            StoryDialogueSystem.OnNewGame+=ResetNotes;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene,mode) => HideDeityDepiction();
            HideDeityDepiction();
        }
        static void HideDeityDepiction()
        {
            var original = GameObject.Find("LordJagannath_SacredDeityShrine");
            if(original != null) foreach(var renderer in original.GetComponentsInChildren<Renderer>(true)) renderer.enabled=false;
        }
        public static bool Known(Note note)=>PlayerPrefs.GetInt("heritage."+note.id,0)==1;
        public static int Discovered {get {int count=0;foreach(var n in All)if(Known(n))count++;return count;}}
        public static void ResetNotes(){foreach(var n in All)PlayerPrefs.DeleteKey("heritage."+n.id);Page=0;}
        public static void Discover(string id)
        {
            foreach(var n in All)if(n.id==id&&!Known(n))
            {PlayerPrefs.SetInt("heritage."+id,1);PlayerPrefs.Save();Core.AudioManager.Instance?.PlaySFX("sfx_codex",0.5f);Debug.Log("[Heritage] discovered "+id);return;}
        }
        void Update()
        {
            if(Time.timeScale<=0f||Time.unscaledTime<next)return;next=Time.unscaledTime+0.5f;
            var pc=ZoneKit.Player;if(pc==null)return;
            if(NewBazaar.Instance != null)
            {
                if(pc.transform.position.z > -6f) Discover("market_craft");
                if(NewBazaar.Instance.RouteFinished) Discover("market_transmission");
                return;
            }
            foreach(var note in All)
            {
                if(Known(note) || note.id.StartsWith("lore_z"))continue;var place=V3Places.Get(note.place);
                if(place.ok&&Vector3.Distance(pc.transform.position,place.at)<18f)Discover(note.id);
            }
        }
        public static string LoadingFact(int index)=>All.Length>0?All[(index&int.MaxValue)%All.Length].fact:"Observe carefully and protect shared heritage.";
        public static string JournalText()
        {
            if(All.Length==0)return "No heritage notes available.";
            Page=(Page%All.Length+All.Length)%All.Length;var note=All[Page];
            if(!Known(note))return $"<b>HERITAGE NOTES — {Discovered}/{All.Length} discovered</b>\n\nNote {Page+1}: not discovered. Explore {V3Places.LegacyNames[note.place]}.\n\n[Left / Right] Browse notes";
            return $"<b>HERITAGE NOTES — {Discovered}/{All.Length} discovered</b>\n\n<color={Core.ScreenText.Gold}><b>{note.title}</b></color>\n{note.fact}\n\n<b>In this game</b>\n{note.connection}\n\n<size=80%>Source: {note.source}\nHistorical and conservation context; fictional game setting.</size>\n\n[Left / Right] Note {Page+1}/{All.Length}";
        }
        public static string LearnedSummary()
        {
            var text=new StringBuilder($"<b>WHAT YOU LEARNED — {Discovered}/{All.Length} notes discovered</b>\n\n");
            foreach(var n in All)if(Known(n))text.Append("<b>").Append(n.title).Append("</b>\n").Append(n.fact).Append("\n\n");
            if(Discovered==0)text.Append("Explore the places and open Journal > Heritage Notes to discover facts.\n");
            text.Append("The investigation and puzzle mechanisms are fictional. These notes offer learning opportunities; no learning gain has been measured.");
            return text.ToString();
        }
    }
}
