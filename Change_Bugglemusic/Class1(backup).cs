using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;


namespace Peak_Project
{
    [BepInPlugin("Change_Bugglemusic_SlimeYuri", "Change_Bugglemusic", "0.1")]
    //[BepInDependency("com.bepines.plugin.important")]
    [BepInProcess("PEAK.exe")]
    public class Peak_Project : BaseUnityPlugin
    {
        private void Awake()
        {
            base.Logger.LogInfo("[Change_Bugglemusic] 启动中");
            Logger.LogInfo("BepInEx:HelloWorld");
            Harmony harmony = new Harmony("Change_Bugglemusic_SlimeYuri");
            Type type = AccessTools.TypeByName("BugleSFX");
            if (type == null)
            {
                base.Logger.LogError("[Change_Bugglemusic] 没有找到音频文件SFX!");
                return;
            }
            MethodInfo method = type.GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo methodInfo = AccessTools.Method(typeof(Peak_Project), "UpdatePostfix", null, null);
            if (method == null || methodInfo == null)
            {
                base.Logger.LogError("[Change_Bugglemusic] 没有找到补丁运行方法");
                return;
            }
            harmony.Patch(method, null, new HarmonyMethod(methodInfo), null, null, null);
            base.Logger.LogInfo("[Change_Bugglemusic] Harmony patch 成功运行");
            LoadCustomClips(); // 启动异步加载
        }

        private void LoadCustomClips()
        {
            string dirPath = Path.Combine(Paths.PluginPath, "Change_Bugglemusic");
            var files = Directory.GetFiles(dirPath);
            int index = 0;
            foreach (var filePath in files)
            {
                string ext = Path.GetExtension(filePath).ToLower();
                if (ext == ".wav" || ext == ".mp3" || ext == ".ogg")
                {
                    StartCoroutine(LoadAudioClip(index, filePath));
                    index++;
                }
            }
        }

        private IEnumerator LoadAudioClip(int index, string filePath)
        {
            string uri = "file://" + filePath;
            string extension = Path.GetExtension(filePath).ToLower();

            AudioType audioType;
            switch (extension)
            {
                case ".mp3":
                    audioType = AudioType.MPEG; // MP3
                    break;
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
                // 其他支持格式，可以继续添加
                default:
                    base.Logger.LogWarning($"[Change_Bugglemusic] 不支持的文件格式: {extension}");
                    yield break;
            }

            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                    uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    base.Logger.LogError($"[Change_Bugglemusic] 加载失败: {filePath}, Error: {uwr.error}");
                }
                else
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
                    if (clip != null)
                    {
                        customBugleClips[index] = clip;
                        base.Logger.LogInfo($"[Change_Bugglemusic] 成功加载: {filePath}");
                    }
                    else
                    {
                        base.Logger.LogError($"[Change_Bugglemusic] 音频解码失败: {filePath}");
                    }
                }
            }
        }

        private static void UpdatePostfix(object __instance)
        {
            // 保持原有的补丁处理逻辑不变
            Type type = __instance.GetType();
            FieldInfo fieldInfo = AccessTools.Field(type, "hold");
            FieldInfo left = AccessTools.Field(type, "bugle");
            FieldInfo fieldInfo2 = AccessTools.Field(type, "buglePlayer");
            FieldInfo fieldInfo3 = AccessTools.Field(type, "currentClip");
            if (fieldInfo == null || left == null || fieldInfo2 == null || fieldInfo3 == null)
            {
                return;
            }
            bool flag = (bool)fieldInfo.GetValue(__instance);
            AudioSource audioSource = (AudioSource)fieldInfo2.GetValue(__instance);
            int key = (int)fieldInfo3.GetValue(__instance);
            AudioClip audioClip;
            if (flag && audioSource != null && Peak_Project.customBugleClips.TryGetValue(key, out audioClip) && audioSource.clip != audioClip)
            {
                audioSource.clip = audioClip;
                audioSource.Play();
            }
        }

        private static readonly Dictionary<int, AudioClip> customBugleClips = new Dictionary<int, AudioClip>();
    }
}
