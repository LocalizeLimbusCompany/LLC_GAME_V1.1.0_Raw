﻿using Addressable;
using BepInEx;
using HarmonyLib;
using Login;
using SimpleJSON;
using Steamworks;
using StorySystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtilityUI;
using static System.Diagnostics.ConfigurationManagerInternalFactory;
using static UI.Utility.EventTriggerEntryAttacher;

namespace LimbusLocalize
{
    [BepInPlugin("Bright.LimbusLocalizeMod", "LimbusLocalizeMod", VERSION)]
    [HarmonyPatch]
    public class LimbusLocalize : BaseUnityPlugin
    {
#if false
        public static bool UseCache;
        public static string CachePath;
        public static string CacheLang;
#endif
        public const string VERSION = "0.1.2";
        public static string path;
        public void Awake()
        {
            //禁止销毁对象,创建隐藏文件夹,创建机翻实例,检查缓存
            path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DontDestroyOnLoad(gameObject);
            gameObject.hideFlags |= HideFlags.HideAndDontSave;
            gameObject.layer = 5;
            if (!Directory.Exists(path + "/.hide"))
            {
                Directory.CreateDirectory(path + "/.hide");
                FileAttributes MyAttributes = File.GetAttributes(path + "/.hide");
                File.SetAttributes(path + "/.hide", MyAttributes | FileAttributes.Hidden);
            }
            gameObject.AddComponent<TranslateJSON>();
            gameObject.AddComponent<UpdateChecker>();
#if false
            if (!File.Exists(LimbusLocalize.path + "/Localize/Cache/HowToLoadCache"))
            {
                UseCache = false;
            }
            else
            {
                var HowToLoadCache = File.ReadAllText(LimbusLocalize.path + "/Localize/Cache/HowToLoadCache").Split(' ');
                var ver = HowToLoadCache[0].Split('.');
                var ver2 = VERSION.Split('.');
                bool isnew = false;
                for (int i = 0; i < ver2.Length; i++)
                {
                    if (int.Parse(ver2[i]) > int.Parse(ver[i]))
                    {
                        isnew = true;
                    }
                }
                if (isnew)
                {
                    Directory.Delete(LimbusLocalize.path + "/Localize/Cache");
                    UseCache = false;
                }
                else
                {
                    UseCache = true;
                    CachePath = LimbusLocalize.path + "/Localize/Cache/" + HowToLoadCache[1];
                    var lang = CachePath.Split('/');
                    CacheLang = lang[lang.Length - 1];
                }
            }
#endif
            //hook方法
            Harmony harmony = new Harmony("LimbusLocalizeMod");
            harmony.PatchAll(typeof(LimbusLocalize));
            //使用AssetBundle技术载入中文字库
            foreach (TMP_FontAsset fontAsset in AssetBundle.LoadFromFile(path + "/tmpchinesefont").LoadAllAssets<TMP_FontAsset>())
                TMP_FontAssets.Add(fontAsset);
        }
        public static List<TMP_FontAsset> TMP_FontAssets = new List<TMP_FontAsset>();
        [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.fontMaterial), MethodType.Setter)]
        [HarmonyPrefix]
        private static bool set_fontMaterial(TMP_Text __instance, Material value)
        {
            //防止字库变动
            value = __instance.font.material;
            //处理不正确大小
            if (__instance.fontSize >= 50f)
            {
                __instance.fontSize -= __instance.fontSize / 50f * 20f;
            }
            if (__instance.m_sharedMaterial != null && __instance.m_sharedMaterial.GetInstanceID() == value.GetInstanceID())
            {
                return false;
            }
            __instance.m_sharedMaterial = value;
            __instance.m_padding = __instance.GetPaddingForMaterial();
            __instance.m_havePropertiesChanged = true;
            __instance.SetVerticesDirty();
            __instance.SetMaterialDirty();
            return false;
        }
        [HarmonyPatch(typeof(TextMeshProLanguageSetter), nameof(TextMeshProLanguageSetter.UpdateTMP))]
        [HarmonyPrefix]
        private static bool UpdateTMP(TextMeshProLanguageSetter __instance, LOCALIZE_LANGUAGE lang)
        {
            //使用中文字库
            var fontAsset = TMP_FontAssets[0];
            __instance._text.font = fontAsset;
            __instance._text.fontMaterial = fontAsset.material;
            if (__instance._matSetter != null)
            {
                __instance._matSetter.defaultMat = fontAsset.material;
                __instance._matSetter.ResetMaterial();
                return false;
            }
            __instance.gameObject.TryGetComponent<TextMeshProMaterialSetter>(out TextMeshProMaterialSetter textMeshProMaterialSetter);
            if (textMeshProMaterialSetter != null)
            {
                textMeshProMaterialSetter.defaultMat = fontAsset.material;
                textMeshProMaterialSetter.ResetMaterial();
            }
            return false;
        }
        [HarmonyPatch(typeof(TextDataManager), nameof(TextDataManager.LoadRemote))]
        [HarmonyPrefix]
        private static bool LoadRemote(LOCALIZE_LANGUAGE lang)
        {
            //载入所有文本
            var tm = TextDataManager.Instance;
            tm._isLoadedRemote = true;
            TextDataManager.RomoteLocalizeFileList romoteLocalizeFileList = JsonUtility.FromJson<TextDataManager.RomoteLocalizeFileList>(SingletonBehavior<AddressableManager>.Instance.LoadAssetSync<TextAsset>("Assets/Resources_moved/Localize", "RemoteLocalizeFileList", null, null).Item1.ToString());
            tm._uiList.Init(romoteLocalizeFileList.UIFilePaths);
            tm._characterList.Init(romoteLocalizeFileList.CharacterFilePaths);
            tm._personalityList.Init(romoteLocalizeFileList.PersonalityFilePaths);
            tm._enemyList.Init(romoteLocalizeFileList.EnemyFilePaths);
            tm._egoList.Init(romoteLocalizeFileList.EgoFilePaths);
            tm._skillList.Init(romoteLocalizeFileList.SkillFilePaths);
            tm._passiveList.Init(romoteLocalizeFileList.PassiveFilePaths);
            tm._bufList.Init(romoteLocalizeFileList.BufFilePaths);
            tm._itemList.Init(romoteLocalizeFileList.ItemFilePaths);
            tm._keywordList.Init(romoteLocalizeFileList.keywordFilePaths);
            tm._skillTagList.Init(romoteLocalizeFileList.skillTagFilePaths);
            tm._abnormalityEventList.Init(romoteLocalizeFileList.abnormalityEventsFilePath);
            tm._attributeList.Init(romoteLocalizeFileList.attributeTextFilePath);
            tm._abnormalityCotentData.Init(romoteLocalizeFileList.abnormalityGuideContentFilePath);
            tm._keywordDictionary.Init(romoteLocalizeFileList.keywordDictionaryFilePath);
            tm._actionEvents.Init(romoteLocalizeFileList.actionEventsFilePath);
            tm._egoGiftData.Init(romoteLocalizeFileList.egoGiftFilePath);
            tm._stageChapter.Init(romoteLocalizeFileList.stageChapterPath);
            tm._stagePart.Init(romoteLocalizeFileList.stagePartPath);
            tm._stageNodeText.Init(romoteLocalizeFileList.stageNodeInfoPath);
            tm._dungeonNodeText.Init(romoteLocalizeFileList.dungeonNodeInfoPath);
            tm._storyDungeonNodeText.Init(romoteLocalizeFileList.storyDungeonNodeInfoPath);
            tm._quest.Init(romoteLocalizeFileList.Quest);
            tm._dungeonArea.Init(romoteLocalizeFileList.dungeonAreaPath);
            tm._battlePass.Init(romoteLocalizeFileList.BattlePassPath);
            tm._storyTheater.Init(romoteLocalizeFileList.StoryTheater);
            tm._announcer.Init(romoteLocalizeFileList.Announcer);
            tm._normalBattleResultHint.Init(romoteLocalizeFileList.NormalBattleHint);
            tm._abBattleResultHint.Init(romoteLocalizeFileList.AbBattleHint);
            tm._tutorialDesc.Init(romoteLocalizeFileList.TutorialDesc);
            tm._iapProductText.Init(romoteLocalizeFileList.IAPProduct);
            tm._userInfoBannerDesc.Init(romoteLocalizeFileList.UserInfoBannerDesc);
            tm._illustGetConditionText.Init(romoteLocalizeFileList.GetConditionText);
            tm._choiceEventResultDesc.Init(romoteLocalizeFileList.ChoiceEventResult);
            tm._battlePassMission.Init(romoteLocalizeFileList.BattlePassMission);
            tm._gachaTitle.Init(romoteLocalizeFileList.GachaTitle);
            tm._introduceCharacter.Init(romoteLocalizeFileList.IntroduceCharacter);
            tm._userBanner.Init(romoteLocalizeFileList.UserBanner);

            tm._abnormalityEventCharDlg.AbEventCharDlgRootInit(romoteLocalizeFileList.abnormalityCharDlgFilePath);
            tm._personalityVoiceText.PersonalityVoiceJsonDataListInit(romoteLocalizeFileList.PersonalityVoice);
            tm._announcerVoiceText.AnnouncerVoiceJsonDataListInit(romoteLocalizeFileList.AnnouncerVoice);
            tm._bgmLyricsText.BgmLyricsJsonDataListInit(romoteLocalizeFileList.BgmLyrics);
            tm._egoVoiceText.EGOVoiceJsonDataListInit(romoteLocalizeFileList.EGOVoice);
            return false;
        }
        public static bool isgameupdate;
        [HarmonyPatch(typeof(TextDataManager), nameof(TextDataManager.LoadLocal))]
        [HarmonyPrefix]
        private static bool LoadLocal(LOCALIZE_LANGUAGE lang)
        {
            var tm = TextDataManager.Instance;
            TextDataManager.LocalizeFileList localizeFileList = JsonUtility.FromJson<TextDataManager.LocalizeFileList>(Resources.Load<TextAsset>("Localize/LocalizeFileList").ToString());
            tm._loginUIList.Init(localizeFileList.LoginUIFilePaths);
            tm._fileDownloadDesc.Init(localizeFileList.FileDownloadDesc);
            tm._battleHint.Init(localizeFileList.BattleHint);
            return false;
        }
        [HarmonyPatch(typeof(StoryData), nameof(StoryData.Init))]
        [HarmonyPrefix]
        private static bool StoryDataInit(StoryData __instance)
        {
            //载入所有剧情
            ScenarioAssetDataList scenarioAssetDataList = JsonUtility.FromJson<ScenarioAssetDataList>(File.ReadAllText(LimbusLocalize.path + "/Localize/CN/CN_NickName.json"));
            __instance._modelAssetMap = new Dictionary<string, ScenarioAssetData>();
            __instance._standingAssetMap = new Dictionary<string, StandingAsset>();
            __instance._standingAssetPathMap = new Dictionary<string, string>();
            foreach (ScenarioAssetData scenarioAssetData in scenarioAssetDataList.assetData)
            {
                string name = scenarioAssetData.name;
                __instance._modelAssetMap.Add(name, scenarioAssetData);
                if (!string.IsNullOrEmpty(scenarioAssetData.fileName) && !__instance._standingAssetPathMap.ContainsKey(scenarioAssetData.fileName))
                    __instance._standingAssetPathMap.Add(scenarioAssetData.fileName, "Story_StandingModel" + scenarioAssetData.fileName);
            }
            ScenarioMapAssetDataList scenarioMapAssetDataList = JsonUtility.FromJson<ScenarioMapAssetDataList>(Resources.Load<TextAsset>("Story/ScenarioMapCode").ToString());
            __instance._mapAssetMap = new Dictionary<string, ScenarioMapAssetData>();
            foreach (ScenarioMapAssetData scenarioMapAssetData in scenarioMapAssetDataList.assetData)
                __instance._mapAssetMap.Add(scenarioMapAssetData.id, scenarioMapAssetData);
            __instance._emotionMap = new Dictionary<string, EmotionAsset>();
            for (int i = 0; i < __instance._emotions.Count; i++)
                __instance._emotionMap.Add(__instance._emotions[i].prefab.Name.ToLower(), __instance._emotions[i]);
            return false;
        }
        [HarmonyPatch(typeof(StoryData), nameof(StoryData.GetScenario))]
        [HarmonyPrefix]
        private static bool GetScenario(StoryData __instance, string scenarioID, LOCALIZE_LANGUAGE lang, ref Scenario __result)
        {
            //读取剧情
            string item = File.ReadAllText(LimbusLocalize.path + "/Localize/CN/CN_" + scenarioID + ".json");
            TextAsset textAsset = SingletonBehavior<AddressableManager>.Instance.LoadAssetSync<TextAsset>("Assets/Resources_moved/Story/Effect", scenarioID, null, null).Item1;
            if (textAsset == null)
            {
                textAsset = SingletonBehavior<AddressableManager>.Instance.LoadAssetSync<TextAsset>("Assets/Resources_moved/Story/Effect", "SDUMMY", null, null).Item1;
            }
            string text3 = item;
            string text4 = textAsset.ToString();
            Scenario scenario = new Scenario();
            scenario.ID = scenarioID;
            JSONArray jsonarray = (JSONArray)JSONNode.Parse(text3)["dataList"];
            JSONArray jsonarray2 = (JSONArray)JSONNode.Parse(text4)["dataList"];
            for (int i = 0; i < jsonarray.Count; i++)
            {
                int num = jsonarray[i]["id"];
                if (num >= 0)
                {
                    JSONNode jsonnode = new JSONObject();
                    if (jsonarray2[i]["id"] == num)
                    {
                        jsonnode = jsonarray2[i];
                    }
                    scenario.Scenarios.Add(new Dialog(num, jsonarray[i], jsonnode));
                }
            }
            __result = scenario;
            return false;
        }
        [HarmonyPatch(typeof(StoryData), nameof(StoryData.GetTellerTitle))]
        [HarmonyPrefix]
        private static bool GetTellerTitle(StoryData __instance, string name, LOCALIZE_LANGUAGE lang, ref string __result)
        {
            //剧情称号
            if (__instance._modelAssetMap.TryGetValue(name, out ScenarioAssetData scenarioAssetData))
                __result = scenarioAssetData.nickName;
            return false;
        }
        [HarmonyPatch(typeof(StoryData), nameof(StoryData.GetTellerName))]
        [HarmonyPrefix]
        private static bool GetTellerName(StoryData __instance, string name, LOCALIZE_LANGUAGE lang, ref string __result)
        {
            //剧情名字
            if (__instance._modelAssetMap.TryGetValue(name, out ScenarioAssetData scenarioAssetData))
                __result = scenarioAssetData.krname;
            return false;
        }
        [HarmonyPatch(typeof(LoginSceneManager), nameof(LoginSceneManager.SetLoginInfo))]
        [HarmonyPostfix]
        private static void SetLoginInfo(LoginSceneManager __instance)
        {
            string SteamID = SteamClient.SteamId.ToString();

            //在主页右下角增加一段文本，用于指示版本号和其他内容
            var fontAsset = TMP_FontAssets[0];
            __instance.tmp_loginAccount.font = fontAsset;
            __instance.tmp_loginAccount.fontMaterial = fontAsset.material;
            __instance.tmp_loginAccount.text = "LimbusLocalizeMod v." + VERSION;
            //增加首次使用弹窗，告知使用者不用花钱买/使用可能有封号概率等
            if (UpdateChecker.UpdateCall != null)
            {
                TranslateJSON.OpenGlobalPopup("模组更新已下载,点击确认将打开下载路径并退出游戏", default, default, "确认", delegate () { UpdateChecker.UpdateCall(); });
                return;
            }
            if (File.Exists(LimbusLocalize.path + "/.hide/checkisfirstuse"))
                if (File.ReadAllText(LimbusLocalize.path + "/.hide/checkisfirstuse") == SteamID + " true")
                    return;
            UserAgreementUI userAgreementUI = Instantiate(__instance._userAgreementUI, __instance._userAgreementUI.transform.parent);
            userAgreementUI.gameObject.SetActive(true);
            userAgreementUI.tmp_popupTitle.GetComponent<UITextDataLoader>().enabled = false;
            userAgreementUI.tmp_popupTitle.text = "首次使用提示";
            var textMeshProUGUI = userAgreementUI._userAgreementContent._agreementJP.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            userAgreementUI._userAgreementContent.Init(delegate (bool on)
            {
                if (userAgreementUI._userAgreementContent.Agreed())
                {
                    textMeshProUGUI.text = "模因封号触媒启动\r\n\r\n检测到存活迹象\r\n\r\n解开安全锁";
                    userAgreementUI._userAgreementContent.toggle_userAgreements.gameObject.SetActive(false);
                    userAgreementUI.btn_confirm.interactable = true;
                }
            });
            userAgreementUI._panel.closeEvent.AddListener(delegate ()
            {
                File.WriteAllText(LimbusLocalize.path + "/.hide/checkisfirstuse", SteamID + " true");
                userAgreementUI.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(userAgreementUI);
                UnityEngine.Object.Destroy(userAgreementUI.gameObject);
            });
            userAgreementUI.btn_cancel._onClick.AddListener(delegate ()
            {
                SteamClient.Shutdown();
                Application.Quit();
            });
            userAgreementUI.btn_confirm.interactable = false;
            userAgreementUI.btn_confirm._onClick.AddListener(new UnityAction(userAgreementUI.OnConfirmClicked));
            userAgreementUI._collectionOfPersonalityInfo.gameObject.SetActive(false);
            userAgreementUI._userAgreementContent._scrollRect.content = userAgreementUI._userAgreementContent._agreementJP;
            textMeshProUGUI.font = fontAsset;
            textMeshProUGUI.fontMaterial = fontAsset.material;
            textMeshProUGUI.text = "<link=\"https://github.com/Bright1192/LimbusLocalize\">点我进入Github链接</link>\n该mod完全免费\n零协会是唯一授权发布对象\n警告：使用模组会有微乎其微的封号概率(如果他们检测这个的话)\n你已经被警告过了";
            var textMeshProUGUI2 = userAgreementUI._userAgreementContent.toggle_userAgreements.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            textMeshProUGUI2.GetComponent<UITextDataLoader>().enabled = false;
            textMeshProUGUI2.font = fontAsset;
            textMeshProUGUI2.fontMaterial = fontAsset.material;
            textMeshProUGUI2.text = "点击进行身份认证";
            userAgreementUI._userAgreementContent.transform.localPosition = new Vector3(510f, 77f);
            userAgreementUI._userAgreementContent.toggle_userAgreements.gameObject.SetActive(true);
            userAgreementUI._userAgreementContent._agreementJP.gameObject.SetActive(true);
            userAgreementUI._userAgreementContent.img_titleBg.gameObject.SetActive(false);
            float preferredWidth = userAgreementUI._userAgreementContent.tmp_title.preferredWidth;
            Vector2 sizeDelta = userAgreementUI._userAgreementContent.img_titleBg.rectTransform.sizeDelta;
            sizeDelta.x = preferredWidth + 60f;
            userAgreementUI._userAgreementContent.img_titleBg.rectTransform.sizeDelta = sizeDelta;
            userAgreementUI._userAgreementContent._userAgreementsScrollbar.value = 1f;
            userAgreementUI._userAgreementContent._userAgreementsScrollbar.size = 0.3f;
        }

#if DEBUG
        [HarmonyPatch(typeof(AddressablePopup), nameof(AddressablePopup.OnDownloadingYes))]
        [HarmonyPrefix]
        private static bool OnDownloadingYes(AddressablePopup __instance)
        {
            TranslateJSON.OpenGlobalPopup("检测到游戏进行了热更新,是否机翻更新后变动/新增的文本?\n如果是将根据你是否挂梯子使用谷歌/有道翻译\n如果否将根据你是否挂梯子将更新后文本保留为韩文/英文", default, default, default, delegate ()
            {
                TranslateJSON.DoTranslate = true;
                TranslateJSON.TranslateCall = delegate ()
                {
                    __instance._updateMovieScreen.SetActive(false);
                    SingletonBehavior<AddressableManager>.Instance.InitLoad();
                    __instance.Close();
                };
                TranslateJSON.StartTranslate();
            }, delegate ()
            {
                TranslateJSON.DoTranslate = false;
                TranslateJSON.TranslateCall = delegate ()
                {
                    __instance._updateMovieScreen.SetActive(false);
                    SingletonBehavior<AddressableManager>.Instance.InitLoad();
                    __instance.Close();
                };
                TranslateJSON.StartTranslate();
            });
            return false;
        }
#endif
    }
}
