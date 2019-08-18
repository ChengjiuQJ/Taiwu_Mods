﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony12;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using UnityEngine.Networking;

namespace RobTomb
{

    public class Settings : UnityModManager.ModSettings
    {
        public bool daomu;
        public int paixu;
        public int search;
        public string amount;
        public bool noPoisonItem;
        public bool autoCheckUpdate;
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }

    public class ModDate
    {
        public static Dictionary<int, Dictionary<int, string>> actorsDate = new Dictionary<int, Dictionary<int, string>>();
        public static Dictionary<int, SortedDictionary<int, int[]>> actorsGongFas = new Dictionary<int, SortedDictionary<int, int[]>>();
        public static List<int> actorsDateKeys=new List<int> {79 };
        public static List<int> actorsGongFasKeys=new List<int> { 20409 };
    }


    public static class Main
    {
        public static bool enabled;
        public static Settings settings;
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static int round = 0;                 //盗墓轮数
        public static int dieActorId = 0;          //墓主人id
        public static List<int> normalActors=new List<int>();          //当前格的人的集合
        public static int gongFaId = 0;                     //古冢遗刻功法id
        public static List<int> treasure = new List<int>();             //天材地宝itemid集合
        public static bool isinGang = false;                                    //在门派驻地唯一一格
        public static bool hasWaived = false;                       //放弃修习
        public static bool haveOtherWay = false;                //被发现时选择了除“束手就擒”的选项
        public static int enemyValueId = 0;                         //敌人
        public static int safeitemId = 0;                               //藏匿物品的id
        public static int basejilv = 0;                                   //盗墓未被发现的基础概率
        public static int nextjilv = 0;                                   //下轮被发现的概率（用于显示）
        public static bool baolu = false;                              //由于事情分支而被门派发现且仍未逃走
        public static bool hasKill = false;                            //死斗中选择杀死对面
        public static int button = 2;                                     //设置中盗墓手札的开关
        public static int debtTime = 0;                                //古冢遗刻的剩余时间
        public static List<int> bixieWeapon = new List<int>   //对僵尸宝具
        {40701,
        40702,
        40703,
        40704,
        40705,
        40706,
        40707,
        40708,
        40709,
        40801,
        40802,
        40803,
        40804,
        40805,
        40806,
        40807,
        40808,
        40809,
        60207, //九灵辟邪匣
        62209,//神鬼踏歌
        81605,//天香伏邪手
        81607,//降魔神木臂
        81704,//缚妖五指束
        63104,//降魔杵
        63109,//三界降服
        52609,//轩辕夏禹剑
        52706,//却邪
        82405,//斩魔雌雄剑
        82506,//辟邪神木剑
        52809,//斩龙铡
        82706,//辟邪神木刀
        82708,//太一伏魔刀
        53808,//镇狱碑
        63909,//神骇
        };
        public static Dictionary<int, int> getItem = new Dictionary<int, int>();                       //获得物品
        public static Dictionary<int, int> getItemCache = new Dictionary<int, int>();             //获得物品缓存
        public static int[] getRecourse = { 0, 0, 0, 0, 0, 0 };                                                        //获得资源
        public static int[] getRecourseCache = { 0, 0, 0, 0, 0, 0 };                                              //获得资源缓存
        public static int baseGongId = 0;                                                                                        //当前地点归属，0：野外，1-15：各大门派，其余：村庄
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            settings = Settings.Load<Settings>(modEntry);

            Logger = modEntry.Logger;
            string resdir = System.IO.Path.Combine(modEntry.Path, "Data");
            Logger.Log(" resdir :" + resdir);
            RobTomb_LoadData.resdir = resdir;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            if (settings.autoCheckUpdate)
                AutoUpdate.AutoUpdate.CheckUpdate(modEntry);
            return true;
        }

        
        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.daomu = GUILayout.Toggle(settings.daomu, "开启盗墓玩法");
            GUILayout.Label("坟墓排序方式：");
            settings.paixu = GUILayout.Toolbar(settings.paixu, new string[] { "默认（按死亡顺序）", "按地位由高到低","按地位由低到高"});
            GUILayout.Label("坟墓筛选方式：");
            settings.search = GUILayout.Toolbar(settings.search, new string[] { "无", "只显示被已挖过的", "只显示尚未挖过的","只显示有粽子的"});
            GUILayout.BeginHorizontal();
            GUILayout.Label("最多同时显示的坟墓数量（0代表全部显示）：");
            settings.amount = GUILayout.TextField(settings.amount,4,GUILayout.Width(200) ,GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            settings.noPoisonItem = GUILayout.Toggle(settings.noPoisonItem, "过滤带毒物品(非装备）");
            if (GUILayout.Button("盗墓手札",GUILayout.MaxWidth(200)))
                button = (button+1)%4;
            if (button==1)
            {
                GUIStyle myStyle = new GUIStyle();
                myStyle.fontSize = 22;
                GUILayout.Label("<color=#E4504DFF>沙雕版\n一、选择目标篇\n1.孤家寡人，莫得朋友\n2.地处偏远，莫得看守\n3.门派驻地，勿要靠近\n二、成功率篇\n1.深思熟虑，三思而行\n2.聪颖冷静，多多益善\n3.见好就收，方能无恙\n三、收获篇\n1.细腻之人，自无遗漏\n2.天材地宝，福者得之\n四、遇敌篇\n1.三十六计，走为上计\n2.有舍有得，多多变通\n五、鬼怪篇\n1.善事利器，无所不催\n2.以己之长，攻彼之短</color>",myStyle);
            }
            else if(button==3)
            {
                GUIStyle myStyle = new GUIStyle();
                myStyle.fontSize = 22;
                GUILayout.Label("<color=#E4504DFF>正常版\n设定集：\n1.基础成功率由你谋划所花时间与人物聪颖程度决定\n2.门派驻地内的墓受到保护，墓主人生前地位越高则保护越严密\n3.同格内墓主人的友人越多，越有可能盗墓失败\n4.坚毅能够提升进入疲惫状态前最大的盗墓次数，以及提供中毒和受伤的减免\n5.细腻越高越容易在墓中找到墓主人的物品和资源\n6.水性越高，越容易找到珍稀物品；福源越高，获得的珍稀物品的品级越高\n7.还有些隐藏设定就暂且不表了~</color>", myStyle);
            }
            AutoUpdate.AutoUpdate.OnGUI(modEntry,ref settings.autoCheckUpdate);
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        public static int SortList1(int a, int b) //a b表示列表中的元素

        {

            if (Math.Abs(int.Parse(DateFile.instance.GetActorDate(a,20,false))) < Math.Abs(int.Parse(DateFile.instance.GetActorDate(b, 20, false)))) 

            {
                return -1;

            }

            else if (Math.Abs(int.Parse(DateFile.instance.GetActorDate(a, 20, false))) > Math.Abs(int.Parse(DateFile.instance.GetActorDate(b, 20, false))))

            {

                return 1;

            }

            return 0;
        }
        public static int SortList2(int a, int b) //a b表示列表中的元素

        {

            if (Math.Abs(int.Parse(DateFile.instance.GetActorDate(a, 20, false))) < Math.Abs(int.Parse(DateFile.instance.GetActorDate(b, 20, false))))

            {
                return 1;

            }

            else if (Math.Abs(int.Parse(DateFile.instance.GetActorDate(a, 20, false))) > Math.Abs(int.Parse(DateFile.instance.GetActorDate(b, 20, false))))

            {

                return -1;

            }

            return 0;
        }

        public static void Finish()
        {
            switch (MassageWindow.instance.eventValue[1])
            {
                case 0: //默认结局
                    {
                        int actorId = DateFile.instance.mianActorId;
                        int moodchange = int.Parse(DateFile.instance.goodnessDate[DateFile.instance.GetActorGoodness(actorId)][102]);
                        DateFile.instance.SetActorMood(actorId, moodchange, 100, true);
                        break;
                    }
                case 1:   //逃离
                    {
                        int actorId = DateFile.instance.mianActorId;
                        DateFile.instance.SetActorMood(actorId, -5, 100, false);
                        break;
                    }

                case 2:   //流言蜚语
                    {
                        int actorId = DateFile.instance.mianActorId;
                        DateFile.instance.SetActorMood(actorId, -5, 100, false);
                        DateFile.instance.SetActorFameList(actorId, 401, 1, 0);
                        break;
                    }

                case 3://被抓到
                    {
                        int actorId = DateFile.instance.mianActorId;
                        int level = enemyValueId % 10;
                        int id = 0;
                        int level2 = Mathf.Clamp(level - 1, 1, 9);
                        bool flag = false;
                        List<int> gangActors = new List<int>(DateFile.instance.GetGangActor(baseGongId, level2));
                        X:
                        if (gangActors.Count > 0)
                        {
                            id = gangActors[UnityEngine.Random.Range(0, gangActors.Count)];
                        }
                        else
                        {
                            if (level2 == 1 || flag)
                            {
                                level2 += 1;
                                flag = true;
                            }
                            else
                            {
                                level2 -= 1;
                            }
                            gangActors.AddRange(DateFile.instance.GetGangActor(baseGongId, level2));
                            if (level2 == 9) { return; }
                            goto X;
                        }
                        List<int> itemIds = new List<int>(getItem.Keys);
                        string text = "";
                        if(itemIds.Count>0)
                        {
                            for(int i=0;i<itemIds.Count;i++)
                            {
                                DateFile.instance.ChangeTwoActorItem(actorId, id, itemIds[i], getItem[itemIds[i]]);
                                //要不要加个经历显示呢，比如xxx在xx缴获了太吾盗掘的赃物ORZ  但可能有坏档风险  PeopleLifeAI.instance.AISetMassage()

                                text += DateFile.instance.GetActorName(actorId)+"失去了盗墓所得......";
                            }
                        }
                        Logger.Log("成功失去物品");
                        for (int i = 0; i < 6; i++)
                        {
                            UIDate.instance.ChangeTwoActorResource(actorId, id, i, Main.getRecourse[i], true);
                        }
                        Main.Logger.Log("成功失去资源");
                        DateFile.instance.SetActorMood(actorId, -10, 100, false);
                        DateFile.instance.SetActorFameList(actorId, 104, 1, 0);
                        MassageWindow.instance.SetBadSocial(dieActorId, actorId, 401);
                        if (text != "")
                            text += "\n";
                        text += "墓主人亲友与" + DateFile.instance.GetActorName(actorId) + "结下了仇怨......";
                        TipsWindow.instance.SetTips(0, new string[] { text }, 200);
                        if (Main.baseGongId > 0 && Main.baseGongId <= 5)
                        {
                            List<int> list = new List<int>();
                            for (int i = 1; i <= 9; i++)
                            {
                                list.AddRange(DateFile.instance.GetGangActor(Main.baseGongId, i));
                            }
                            Main.Logger.Log("成功添加门派人物");
                            for (int i = 0; i < list.Count; i++)
                            {
                                int favor = int.Parse(DateFile.instance.GetActorDate(list[i], 3, false));
                                if (favor != -1)
                                    DateFile.instance.ChangeFavor(list[i], -2000, false, false);
                            }
                            Main.Logger.Log("成功改变好感");
                        }
                        else if (baseGongId <= 10)
                        {
                            DateFile.instance.SetGangValue(int.Parse(DateFile.instance.GetGangDate(baseGongId, 11)), int.Parse(DateFile.instance.GetGangDate(baseGongId, 3)), -10);
                            Main.Logger.Log("成功失去恩义");
                        }
                        else if (baseGongId <= 15)
                        {
                            List<int> list = new List<int>();
                            for (int i = 1; i <= 9; i++)
                            {
                                list.AddRange(DateFile.instance.GetGangActor(Main.baseGongId, i));
                            }
                            for (int i = 0; i < list.Count; i++)
                            {
                                if (int.Parse(DateFile.instance.GetActorDate(list[i], 47, false)) > 0)
                                {
                                    int favor = int.Parse(DateFile.instance.GetActorDate(list[i], 3, false));
                                    if (favor != -1)
                                        DateFile.instance.ChangeFavor(list[i], -12000, false, true);
                                }
                            }
                            Main.Logger.Log("成功下降支持");
                        }
                        else
                        {
                            DateFile.instance.SetGangValue(int.Parse(DateFile.instance.GetGangDate(baseGongId, 11)), int.Parse(DateFile.instance.GetGangDate(baseGongId, 3)), -10);
                            Main.Logger.Log("成功失去恩义");
                        }
                        int goodness = int.Parse(DateFile.instance.GetGangDate(baseGongId, 13));
                        if (goodness >= 875)
                        {
                            DateFile.instance.MakeRandInjury(actorId, (UnityEngine.Random.Range(0, 100) >= 75) ? 10 : 0, UnityEngine.Random.Range(100, 1000));
                        }
                        else if (goodness >= 625)
                        {
                            List<int> list = new List<int>(DateFile.instance.actorItemsDate[actorId].Keys);
                            if (list.Count > 0)
                            {
                                int itemId = list[UnityEngine.Random.Range(0, list.Count)];
                                DateFile.instance.ChangeTwoActorItem(actorId, id, itemId, 1, -1);
                            }
                        }
                        else if (goodness >= 375)
                        {
                            UIDate.instance.ChangeResource(actorId, 5, -(10 - level) * (10 - level) * (10 - level)*100, true);
                        }
                        else if (goodness <= 125&& goodness >= 0)
                        {
                            UIDate.instance.ChangeTime(true, 10);
                        }
                        break;
                    }
                case 4:  //学功法技艺结果
                    {
                        int actorId = DateFile.instance.mianActorId;
                        DateFile.instance.SetActorMood(actorId, +10, 100, false);
                        break;
                    }
                case 5: //寻获宝物结果
                    {
                        int actorId = DateFile.instance.mianActorId;
                        DateFile.instance.SetActorMood(actorId, +5, 100, false);
                        break;
                    }
                case 6://一无所获
                    {
                        int actorId = DateFile.instance.mianActorId;
                        DateFile.instance.SetActorMood(actorId, -5, 100, false);
                        break;
                    }

            }
        }
        /// <summary>
        /// 走为上计
        /// </summary>
        public static void DoFlee()
        {
            Main.haveOtherWay = true;
            int actorId = DateFile.instance.mianActorId;
            int jilv = 0;
            int actorSpeed = BattleVaule.instance.GetMoveSpeed(true, actorId, false);
            if (actorSpeed <= 150)
            {
                jilv = actorSpeed / 5;
            }
            else if (actorSpeed <= 300)
            {
                jilv = 30 + (actorSpeed - 150) / 10;
            }
            else
            {
                jilv = 45 + (actorSpeed - 300) / 20;
            }
            if(MassageWindow.instance.eventValue[1]==1)
            {
                int battleEnemyId = int.Parse(DateFile.instance.presetGangGroupDateValue[enemyValueId][301].Split(new char[] { '|' })[0]);
                jilv -= BattleVaule.instance.GetMoveSpeed(false, battleEnemyId, false, 0)/10;
            }
            if (UnityEngine.Random.Range(0, 100) < jilv)
            {
                EndToEvent(199801301);
            }
            else
            {
                if (MassageWindow.instance.eventValue[1] !=0)
                    EndToEvent(199802412);
                else
                    EndToEvent(199801302);
            }
        }

        /// <summary>
        /// 调整输入框数据类型
        /// </summary>
        public static void InputFix()
        {
            MassageWindow.instance.inputTextField.contentType = InputField.ContentType.IntegerNumber;
        }

        /// <summary>
        /// 贿赂
        /// </summary>
        public static void Bribe(int num)
        {
            MassageWindow.instance.inputTextField.contentType = InputField.ContentType.Name;
            if (num > ActorMenu.instance.ActorResource(DateFile.instance.mianActorId)[5])
            {
                Main.EndToEvent(1998014204);
                return;
            }
            else UIDate.instance.ChangeResource(DateFile.instance.mianActorId, 5, -num);
            Main.haveOtherWay = true;
            int id = MassageWindow.instance.mianEventDate[1];
            int goodness = DateFile.instance.GetActorGoodness(id);
            int level = Math.Abs(int.Parse(DateFile.instance.GetActorDate(dieActorId, 20, false)));
            int level2 = Math.Abs(int.Parse(DateFile.instance.GetActorDate(id, 20, false)));
            int x = 50;
            switch ((10 - level2) / 2)
            {
                case 0:
                case 1:
                    x = 50;
                    break;
                case 2:
                case 3:
                    x = 100;
                    break;
                case 4:
                    x = 200;
                    break;
            }
            int jilv = Mathf.Clamp(num / x, 0, 100) - (10 - level) * 10;
            jilv += int.Parse(DateFile.instance.goodnessDate[goodness][24]);
            Main.Logger.Log(jilv.ToString());
            if (goodness == 2) jilv = 0;
            if (UnityEngine.Random.Range(0, 100) < jilv)
            {
                Main.EndToEvent(199801421);
            }
            else
            {
                Main.EndToEvent(199801422);
            }
        }

        /// <summary>
        /// 忽悠
        /// </summary>
        public static void SweetTalk()
        {
            Main.haveOtherWay = true;
            int actorId = DateFile.instance.mianActorId;
            int fame = DateFile.instance.GetActorFame(actorId);
            int charm = int.Parse(DateFile.instance.GetActorDate(actorId, 15, true));
            int level = 10 - Mathf.Abs(int.Parse(DateFile.instance.GetActorDate(dieActorId, 20, false)));
            int jilv = 0;
            if (int.Parse(DateFile.instance.GetActorDate(actorId, 14, false)) == 2) jilv += 20;
            jilv += charm / 30;
            jilv += fame / 2;
            jilv -= level * 5;
            if (UnityEngine.Random.Range(0, 100) < jilv)
            {
                Main.EndToEvent(199801431);
            }
            else
            {
                Main.EndToEvent(199801432);
            }
        }

        /// <summary>
        /// 束手就擒
        /// </summary>
        public static void NoResistance()
        {
           
            MassageWindow.instance.chooseItemEvents.Remove(RobTomb_LoadData.id["Event_Date"][1998014402]);
            if (MassageWindow.instance.eventValue[1] == 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    getRecourse[i] = 0;
                }
            }
            if (MassageWindow.instance.eventValue[1] == 1)
            {
                if (getItem.Keys.Contains(safeitemId))
                {
                    getItem.Remove(safeitemId);
                    safeitemId = 0;
                }
            }
            if (!haveOtherWay && UnityEngine.Random.Range(0, 100) < 30 + ActorMenu.instance.GetActorResources(DateFile.instance.mianActorId)[5] / 5)
            {
                EndToEvent(1998014403);
            }
            else if (baseGongId > 0) 
            {
                EndToEvent(1998014420);
            }
        }

        /// <summary>
        /// 被抓到之后的转跳
        /// </summary>
        public static void Punish()
        {
            if (baseGongId <= 15)
                EndToEvent(1998014403 + baseGongId);
            else
                EndToEvent(1998014424);
        }

        public static IEnumerator BackToMassageWindow(float waitTime, int giveItemId, int changeEvent, int otherValue1)
        {
            yield return new WaitForSeconds(waitTime);
            MassageWindow.instance.mianEventDate = new int[]
            {
            MassageWindow.instance.mianEventDate[0],
            MassageWindow.instance.mianEventDate[1],
            RobTomb_LoadData.id["Event_Date"][1998003],
            MassageWindow.instance.mianEventDate[3],
            giveItemId,
            changeEvent,
            otherValue1
            };
            MassageWindow.instance.GetEventBooty(DateFile.instance.MianActorID(), MassageWindow.instance.massageItemTyp);
            MassageWindow.instance.ChangeMassageWindow(MassageWindow.instance.massageItemTyp);
            yield break;
        }

        /// <summary>
        /// 按钮转跳
        /// </summary>
        public static void RobTomb2()
        {
            int actorId = DateFile.instance.MianActorID();
            if (!MassageWindow.instance.chooseActorEvents.Contains(RobTomb_LoadData.id["Event_Date"][1998001]))
                MassageWindow.instance.chooseActorEvents.Add(RobTomb_LoadData.id["Event_Date"][1998001]);
            MassageWindow.instance.SetEventWindow(new int[]
            { 0,
             -1,
            RobTomb_LoadData.id["Event_Date"][19981],
             0
             }, false);
        }

        /// <summary>
        /// 古冢遗刻
        /// </summary>
        public static void LearnGongFa()
        {
            if (gongFaId != 0)
            {
                if (DateFile.instance.dayTime<20)
                {
                    debtTime += 20 - DateFile.instance.dayTime;
                    UIDate.instance.ChangeTime(false, 20);
                }
                else
                {
                    UIDate.instance.ChangeTime(false, 20);
                }
                int actorId = DateFile.instance.MianActorID();
                int gongfazizhi = int.Parse(DateFile.instance.gongFaDate[gongFaId][61])+ 500;
                if(DateFile.instance.actorGongFas[actorId].ContainsKey(gongFaId))
                {
                    if (DateFile.instance.GetGongFaLevel(actorId, gongFaId, 0) >= 100 && DateFile.instance.GetGongFaFLevel(actorId, gongFaId, false)>= 10)
                    {
                        DateFile.instance.actorsDate[actorId][gongfazizhi] = (int.Parse(DateFile.instance.actorsDate[actorId][gongfazizhi]) + 20).ToString();
                        TipsWindow.instance.SetTips(0, new string[] { "（太吾对应的资质上升了……）" }, 200);
                        DateFile.instance.actorsDate[dieActorId][79] = "0";
                        Main.EndToEvent(199801613);
                        return;
                    }
                }           
                int rand = 0;
                int level = int.Parse(DateFile.instance.gongFaDate[gongFaId][2]);
                for (int i = 0; i < 10; i++)
                {
                    if (UnityEngine.Random.Range(0, 100) < DateFile.instance.GetActorValue(actorId, gongfazizhi, false) / 5 + ActorMenu.instance.GetActorResources(actorId)[1] / 2 + DateFile.instance.GetActorValue(actorId, 65, false) / 10 - 5 * level)
                    {
                        rand += 1;
                    }
                }
                int badlevel = 0;
                for (int i = 0; i < rand; i++)
                {
                    if (UnityEngine.Random.Range(0, 100) < 100 - DateFile.instance.GetActorValue(actorId, gongfazizhi, false) / 5 - ActorMenu.instance.GetActorResources(actorId)[5] / 2 - DateFile.instance.GetActorValue(actorId, 65, false) / 10 + 5 * level)
                    {
                        badlevel += 1;
                    }
                }
                DateFile.instance.ChangeActorGongFa(actorId, gongFaId, 25, rand, badlevel, false);
                ActorMenu.instance.ChangeMianQi(actorId, 100 * int.Parse(DateFile.instance.gongFaDate[gongFaId][2]) * badlevel, 5);
                if(badlevel!=0) TipsWindow.instance.SetTips(0, new string[] { "你渐渐有了些异样的体悟......" }, 200);
                DateFile.instance.actorsDate[dieActorId][79] = "0";
                EndToEvent(199801611);
            }
        }

        /// <summary>
        /// 古冢遗刻技艺版
        /// </summary>
        public static void LearnJiYi()
        {
            if (gongFaId != 0)
            {
                if (DateFile.instance.dayTime < 20)
                {
                    debtTime += 20 - DateFile.instance.dayTime;
                    UIDate.instance.ChangeTime(false, 20);
                }
                else
                {
                    UIDate.instance.ChangeTime(false, 20);
                }
                int actorId = DateFile.instance.MianActorID();
                int jiyi = int.Parse(DateFile.instance.skillDate[gongFaId][3]);
                if(DateFile.instance.GetSkillLevel(gongFaId)>=100&& DateFile.instance.GetSkillFLevel(gongFaId)>=10)
                {
                    DateFile.instance.actorsDate[actorId][501 + jiyi] = (int.Parse(DateFile.instance.actorsDate[actorId][501+jiyi]) + 20).ToString();
                    TipsWindow.instance.SetTips(0, new string[] { "（太吾对应的资质上升了……）" }, 200);
                    DateFile.instance.actorsDate[dieActorId][79] = "0";
                    EndToEvent(199801713);
                    return;
                }
                int rand = 0;
                int level = int.Parse(DateFile.instance.skillDate[gongFaId][2]);
                for (int i = 0; i < 10; i++)
                {
                    if (UnityEngine.Random.Range(0, 100) < DateFile.instance.GetActorValue(actorId,501+jiyi,false) / 5+ ActorMenu.instance.GetActorResources(actorId)[1] / 2 + DateFile.instance.GetActorValue(actorId, 65, true) / 10 - 5 * level)
                    {
                        rand += 1;
                    }
                }
                DateFile.instance.ChangeMianSkill(gongFaId, 25, rand, false);
                DateFile.instance.actorsDate[dieActorId][79] = "0";
                EndToEvent(199801711);
            }
        }

        /// <summary>
        /// 事件结束转到xx事件
        /// </summary>
        /// <param name="eventId"></param>
        public static void EndToEvent(int eventId)
        {
            MassageWindow.instance.mianEventDate[2] = RobTomb_LoadData.id["Event_Date"][eventId];
            MassageWindow.instance.eventValue = new List<int>();
        }


        /// <summary>
        /// 设置天材地宝
        /// </summary>
        public static void SetTreasure()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 1; j <= 14; j++)
                {
                    if (!treasure.Contains(3000 + i * 100 + j))
                    {
                        treasure.Add(3000 + i * 100 + j);
                    }
                }
            }
            for (int i = 1; i <= 96; i++)
            {
                if (!treasure.Contains(4000 + i))
                {
                    treasure.Add(4000 + i);
                }
            }
            for (int i = 1; i <= 42; i++)
            {
                if (!treasure.Contains(4200 + i))
                {
                    treasure.Add(4200 + i);
                }
            }
            for (int i = 1; i <= 9; i++)
            {
                if (!treasure.Contains(4300 + i))
                {
                    treasure.Add(4300 + i);
                }
            }
        }


        public static void RobTombsuccessfully()
        {
            if(MassageWindow.instance.eventValue[1]==1)
            {

                if (getItemCache.Count > 0)
                {
                    List<int> itemId = new List<int>(getItemCache.Keys);
                    for(int i=0;i<itemId.Count;i++)
                    {                        
                        getItem.Add(itemId[i],getItemCache[itemId[i]]);
                        getItemCache.Remove(itemId[i]);
                    }          
                }
                EndToEvent(1998022);
                return;
            }

            if (getRecourseCache.Max()!=0)
            {
                for(int i=0;i<6;i++)
                {
                    getRecourse[i] += getRecourseCache[i];
                    getRecourseCache[i] = 0;
                }
                EndToEvent(1998019);

                return;
            }

            if (getItemCache.Count>0)
            {
                KeyValuePair<int, int> item = getItemCache.Last();
                getItemCache.Remove(item.Key);
                getItem.Add(item.Key,item.Value);
                MassageWindow.instance.mianEventDate[3] = item.Key;
                EndToEvent(1998020);

                return;
            }
            EndToEvent(1998022);
        }

        /*
        public static void ShowGetItem()
        {
            float num = 0f; 
            foreach (KeyValuePair<int,int> item in getItem)
            {
                int[] array =new int[] {item.Key,item.Value};
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(BattleSystem.instance.battleBootyIcon, Vector3.zero, Quaternion.identity);
                gameObject.name = "Item," + array[0];
                gameObject.transform.SetParent(BattleSystem.instance.battleBootyHolder, false);
                GameObject gameObject2 = gameObject.transform.Find("ItemIcon").gameObject;
                gameObject2.name = "Item," + array[0];
                gameObject2.GetComponent<Image>().sprite = GetSprites.instance.itemSprites[int.Parse(DateFile.instance.GetItemDate(array[0], 98, true))];
                Image component = gameObject.transform.Find("ItemBack").GetComponent<Image>();
                component.sprite = GetSprites.instance.itemBackSprites[int.Parse(DateFile.instance.GetItemDate(array[0], 4, true))];
                component.color = ActorMenu.instance.LevelColor(int.Parse(DateFile.instance.GetItemDate(array[0], 8, true)));
                if (int.Parse(DateFile.instance.GetItemDate(array[0], 6, true)) > 0)
                {
                    gameObject.transform.Find("ItemNumberText").GetComponent<Text>().text = "×" + array[1];
                }
                else
                {
                    int num2 = int.Parse(DateFile.instance.GetItemDate(array[0], 901, true));
                    int num3 = int.Parse(DateFile.instance.GetItemDate(array[0], 902, true));
                    gameObject.transform.Find("ItemNumberText").GetComponent<Text>().text = string.Format("{0}{1}</color>/{2}", ActorMenu.instance.Color3(num2, num3), num2, num3);
                }
                gameObject.transform.localScale = new Vector3(0f, 0f, 1f);
                TweenSettingsExtensions.SetEase<Tweener>(TweenSettingsExtensions.SetDelay<Tweener>(ShortcutExtensions.DOScale(gameObject.GetComponent<RectTransform>(), new Vector3(1.4f, 1.4f, 1f), 0.1f), num), Ease.OutBack);
                TweenSettingsExtensions.SetEase<Tweener>(TweenSettingsExtensions.SetDelay<Tweener>(ShortcutExtensions.DOScale(gameObject.GetComponent<RectTransform>(), new Vector3(1f, 1f, 1f), 0.4f), num + 0.1f), Ease.OutBack);
                num += 0.1f;
            }
        }
        */

        public static void Clear()
        {
            round = 0;
            dieActorId = 0;
            normalActors.Clear();
            gongFaId = 0;
            treasure.Clear();
            isinGang = false;
            hasWaived = false;
            haveOtherWay = false;
            baolu = false;
            enemyValueId = 0;
            safeitemId = 0;
            nextjilv = 0;
            basejilv = 0;
            getItem.Clear();
            getItemCache.Clear();
            hasKill = false;
            for(int i=0;i<6;i++)
            {
                getRecourse[i] = 0;
                getRecourseCache[i] = 0;
            }
            baseGongId = 0;
            SetTreasure();
        }

        /// <summary>
        /// 逃脱僵尸追击？？
        /// </summary>
        public static  void ZombieFlee()
        {
            int actorId = DateFile.instance.mianActorId;
            int jilv = 33;
            int actorSpeed = BattleVaule.instance.GetMoveSpeed(true, actorId, false);
            int level = 10 - Math.Abs(int.Parse(DateFile.instance.GetActorDate(dieActorId, 20, false)));
            if (actorSpeed <= 150)
            {
                jilv = actorSpeed / 5;
            }
            else if (actorSpeed <= 300)
            {
                jilv = 30 + (actorSpeed - 150) / 10;
            }
            else
            {
                jilv = 45 + (actorSpeed - 300) / 20;
            }
            int zoobieSpeed = 100+15*level;
            Logger.Log("我方速度：" + actorSpeed.ToString() + "\n敌方速度：" + zoobieSpeed.ToString());
            jilv -= zoobieSpeed / 10;
            Logger.Log("逃脱几率：" + jilv.ToString() + "%");
            if (UnityEngine.Random.Range(0, 100) < jilv)
            {
                string text = "";
                if (baseGongId != 0)
                {
                    int worldId = int.Parse(DateFile.instance.GetGangDate(baseGongId, 11));
                    int num2 = int.Parse(DateFile.instance.GetGangDate(baseGongId, 3));
                    int num4 = int.Parse(DateFile.instance.partWorldMapDate[num2][98]);
                    int num5 = num4 * num4;
                    int num6 = (14 - DateFile.instance.worldResource * 4) * 3;
                    for (int i = 0; i < num5; i++)
                    {
                        int num7 = i;
                        if (DateFile.instance.placeResource.ContainsKey(num2) && DateFile.instance.placeResource[num2].ContainsKey(num7))
                        {
                            for (int j = 0; j < 8; j++)
                            {
                                UIDate.instance.ChangePlaceResource(false, -20, num2, num7, j, true);
                            }
                        }
                    }

                    text += "当地的资源和文化与安定都下降了......";
                }
                foreach (KeyValuePair<int, int> item in getItem)
                {
                    DateFile.instance.ChangeTwoActorItem(actorId, dieActorId, item.Key, item.Value);
                    if (text != "") text = text + "\n";
                    text = text +"失去了"+ DateFile.instance.SetColoer(20001 + int.Parse(DateFile.instance.GetItemDate(item.Key, 8, false)), DateFile.instance.GetItemDate(item.Key, 0, false)); 
                }
                if (text != "") TipsWindow.instance.SetTips(0, new string[] { text }, 200);
                EndToEvent(199802321);
            }
            else
            {
                EndToEvent(199802322);
            }
        }
            

        /// <summary>
        /// fight！！！
        /// </summary>
        public static void BattleAgainstZoobie()
        {
            StartBattle.instance.ShowStartBattleWindow(RobTomb_LoadData.id["EnemyTeam_Date"][2000], 0,2, new List<int> { RobTomb_LoadData.id["PresetActor_Date"][2000] });
        }

        /// <summary>
        /// 僵尸战胜利
        /// </summary>
        public static void WinZoobie()
        {
            DestroyTomb(dieActorId);
            TipsWindow.instance.SetTips(0, new string[] { "此地墓穴已被摧毁..." },200);             
        }

        public static void DestroyTomb(int actorId)
        {
            List<int> list2 = new List<int>(DateFile.instance.actorItemsDate[actorId].Keys);
            for (int j = 0; j < list2.Count; j++)
            {
                int itemId = list2[j];
                DateFile.instance.LoseItem(actorId, itemId, DateFile.instance.GetItemNumber(actorId, itemId), true, true);
            }
            for (int k = 0; k < 7; k++)
            {
                DateFile.instance.actorsDate[actorId].Remove(401 + k);
            }
            DateFile.instance.MoveOutPlace(actorId);
        }

        /// <summary>
        /// 偷袭未实装
        /// </summary>
        public static void SneakRaid()
        {
            int actorId = DateFile.instance.mianActorId;
            int[] actorResources = ActorMenu.instance.GetActorResources(actorId);
            int jilv = 33;
            int actorPower = int.Parse(DateFile.instance.GetActorDate(actorId, 993, false));
            int enemyPower = int.Parse(DateFile.instance.GetActorDate(MassageWindow.instance.mianEventDate[1], 993, false));
            if (actorPower >= enemyPower) jilv -= 20;
            else jilv += 20;
            jilv += actorResources[5] / 2;
            if (haveOtherWay) jilv -= 20;
            Main.Logger.Log("我方战力：" + actorPower.ToString() + "敌方战力：" + enemyPower.ToString() + "\n偷袭成功率：" + jilv.ToString() + "%");
            if(UnityEngine.Random.Range(0,100)<jilv)
            {
                EndToEvent(1998023123);
            }
            else
            {
                EndToEvent(1998023124);
            }
        }


        public static void Kill()
        {
            int actorId = DateFile.instance.mianActorId;
            int num = MassageWindow.instance.mianEventDate[1];
            Main.hasKill = true;
            if(int.Parse(DateFile.instance.GetActorDate(num,8,false))==1)
            {
                DateFile.instance.actorsDate[num][12] = "0";
                PeopleLifeAI.instance.AISetMassage(95, num, DateFile.instance.mianPartId, DateFile.instance.mianPlaceId, null, -1, true);
                DateFile.instance.RemoveActor(new List<int>
                 {
                    num
                 }, true, false);              
            }
            EndToEvent(19980263);      
        }

        public static void LetHeLeave()
        {
            baolu = true;
            EndToEvent(19980264);
        }

        /// <summary>
        /// 盗墓事件
        /// </summary>
        public static void RobTomb()
        {
            if (MassageWindow.instance.chooseActorEvents.Contains(RobTomb_LoadData.id["Event_Date"][1998001]))
            {
                MassageWindow.instance.chooseActorEvents.Remove(RobTomb_LoadData.id["Event_Date"][1998001]);
            }
            int partId = WorldMapSystem.instance.choosePartId;
            int placeId = WorldMapSystem.instance.choosePlaceId;
            int actorId = DateFile.instance.MianActorID();
            int gangId = int.Parse(DateFile.instance.actorsDate[dieActorId][19]);
            int level = 10 - Math.Abs(int.Parse(DateFile.instance.GetActorDate(dieActorId, 20, false)));
            int[] actorResources = ActorMenu.instance.GetActorResources(actorId);
            int jilv;
            List<int> friends = new List<int>();
            int a = 0;
            for (int i = 0; i < 11; i++)
            {
                friends.AddRange(DateFile.instance.GetActorSocial(dieActorId, 301 + i, false));
            }
            List<int> baseFriends = new List<int>();
            if (friends.Count > 0)
            {
                for (int i = 0; i < friends.Count; i++)
                {
                    if (normalActors.Contains(friends[i])&&!baseFriends.Contains(friends[i]))
                    {
                        baseFriends.Add(friends[i]);
                    }
                }
                a = Mathf.Clamp(baseFriends.Count * 5, 0, 50);             
            }
            switch(MassageWindow.instance.eventValue[1])
            {
                case 1:
                    {
                        basejilv = 50;
                        break;
                    }
                case 2:
                    {
                        basejilv = 70 + actorResources[1]/2;
                        break;
                    }
                case 3:
                    {
                        basejilv = 90 + actorResources[1];
                        break;
                    }
                case 4:
                    {
                        basejilv = 110 + actorResources[1] * 2;
                        break;
                    }
                case 5:
                    {
                        basejilv = 150 + actorResources[1] * 3;
                        break;
                    }
                default:
                    break;
            }
            if (isinGang) a += level * 5;
            if (normalActors.Count <= 0)
            {
                jilv = 100;
                nextjilv = 100;
            }
            else
            {
                jilv = basejilv - Mathf.Clamp(round * (15- actorResources[5] / 2), 0, 200) - a;
                nextjilv = basejilv- Mathf.Clamp((round + 1) * (15 - actorResources[5] / 2), 0, 200) - a;
            }
            if(jilv<=0)
            {
                EndToEvent(1998030);
                return;
            }
            nextjilv = Mathf.Clamp(100 - nextjilv, 0, 100);
            int maxRound = 4 + actorResources[4] / 10;
            bool isTired = round > maxRound;
            if (UnityEngine.Random.Range(0, 100) < jilv && (round < maxRound || isTired)|| hasWaived)
            {
                round += 1;
                if (hasWaived) round -= 1; //使在放弃修习时round不加

                string text = "";
                if(UnityEngine.Random.Range(0, 100)<30-actorResources[4]/2 + (isTired?20:0))
                {
                    int typ = UnityEngine.Random.Range(0, 5);
                    ActorMenu.instance.ChangePoison(actorId,typ , (UnityEngine.Random.Range(0, 100)>10*level?UnityEngine.Random.Range(50,level*50): UnityEngine.Random.Range(100, level * 100))*(100-actorResources[4])/100);
                    if (text != "") text += "\n";
                    text = text + "在墓中吸入不明的气体，NAME中毒了......";
                }

                if(UnityEngine.Random.Range(0, 100) < 30 - actorResources[4] / 2 + (isTired ? 20 : 0))
                {
                    DateFile.instance.MakeRandInjury(actorId, (UnityEngine.Random.Range(0, 100) >= 75) ? 10 : 0, (UnityEngine.Random.Range(0, 100) > 10 * level ? UnityEngine.Random.Range(50, level * 50) : UnityEngine.Random.Range(200, level * 200))* (100 - actorResources[4]) / 100);
                    if (text != "") text += "\n";
                    text = text + "在墓中触发了未知的机关，NAME受伤了......";
                }

                text = text.Replace("NAME", DateFile.instance.GetActorName(actorId));
                if (text!="")
                    TipsWindow.instance.SetTips(0, new string[] { text }, 200);
                //判定是否被挖过秘籍
                string num;
                bool hasgongfa = true;
                if (DateFile.instance.actorsDate[dieActorId].TryGetValue(79, out num))
                {
                    if (int.Parse(num) != 1) hasgongfa = false;
                }
                else
                {
                    DateFile.instance.actorsDate[dieActorId].Add(79, "1");
                }
                //古冢遗刻
                if(UnityEngine.Random.Range(0, 100) < 70 + actorResources[0] && Main.dieActorId % 5==0 && hasgongfa && level>=7&&!hasWaived)
                {
                    if(gangId>=1&&gangId<=15)
                    {
                        if(gangId==4&&level==9)
                        {
                            if(UnityEngine.Random.Range(0,100)<20 + actorResources[6])
                            {
                                gongFaId = 20409;
                                EndToEvent(1998016);
                                return;
                            }
                        }
                        List<int> gongFalist = new List<int>(DateFile.instance.gongFaDate.Keys);
                        List<int> dieActorsGongFa = new List<int>();
                        for (int i = 0; i < gongFalist.Count; i++)
                        {
                            if (int.Parse(DateFile.instance.gongFaDate[gongFalist[i]][3]) == gangId && int.Parse(DateFile.instance.gongFaDate[gongFalist[i]][2]) >= 7 && int.Parse(DateFile.instance.gongFaDate[gongFalist[i]][2]) <= level)
                            {
                                dieActorsGongFa.Add(gongFalist[i]);
                            }
                        }
                        if(dieActorsGongFa.Count>0)
                        {
                            gongFaId = dieActorsGongFa[UnityEngine.Random.Range(0, dieActorsGongFa.Count)];
                            Logger.Log("古冢遗刻功法");
                            EndToEvent(1998016);
                        }
                        return;
                    }
                    else if(gangId!=16)
                    {
                        int zizhi = 0,jiyi=0;
                        for(int i=1;i<17;i++)
                        {
                            int actorjiyi = int.Parse(DateFile.instance.GetActorDate(dieActorId, 500 + i, false));
                            if(actorjiyi>=zizhi)
                            {
                                zizhi = actorjiyi;
                                jiyi = i;
                            }
                        }
                        if(zizhi>=90&&jiyi!=0)
                        {
                            gongFaId = 9 * (jiyi-1)+Mathf.Clamp(zizhi/15,1,9);
                            Logger.Log("古冢遗刻技艺:"+gongFaId); 
                            EndToEvent(1998017);
                            return;
                        }
                        
                    }                    
                }
                hasWaived = false;
                //挖到一个粽子
                if (UnityEngine.Random.Range(0, 100) < 5 || int.Parse(DateFile.instance.actorsDate[dieActorId][79]) == 2)
                {
                    MassageWindow.instance.mianEventDate[1] = RobTomb_LoadData.id["PresetActor_Date"][2000];
                    Main.Logger.Log(MassageWindow.instance.mianEventDate[1].ToString());
                    DateFile.instance.actorsDate[dieActorId][79]="2";
                    EndToEvent(1998023);
                    return;
                }

                //福缘深厚
                if (UnityEngine.Random.Range(0, 100) < 10 + actorResources[2] / 2)
                {
                    int getItemLevel = 1;
                    List<int> getItemIdList = new List<int>();
                    for (int i = 0; i < 9 + actorResources[6] / 10; i++)
                    {
                        if (UnityEngine.Random.Range(0, 100) < 20 + actorResources[6] / 2)
                        {
                            getItemLevel++;
                        }
                    }
                    getItemLevel = Mathf.Clamp(getItemLevel, 1, 8);
                    for (int i = 0; i < treasure.Count; i++)
                    {
                        if (int.Parse(DateFile.instance.GetItemDate(treasure[i], 8, true)) == getItemLevel && !getItemIdList.Contains(treasure[i]))
                        {
                            getItemIdList.Add(treasure[i]);
                        }
                    }
                    int getItemId = getItemIdList[UnityEngine.Random.Range(0, getItemIdList.Count)];
                    DateFile.instance.GetItem(actorId, getItemId, 1, true, -1, 0);
                    if (getItem.ContainsKey(getItemId))
                        getItem[getItemId] += 1;
                    else
                        getItem.Add(getItemId, 1);
                    TipsWindow.instance.SetTips(5007, new string[]
                    {
                            DateFile.instance.GetActorName(actorId, false, false),
                            DateFile.instance.presetitemDate[getItemId][0],
                            ""
                    }, 100, -755f, -380f, 600, 100);
                    Logger.Log("福缘深厚");
                    MassageWindow.instance.mianEventDate[3] = getItemId;
                    EndToEvent(1998018);
                    return;
                }

                

                //盗取道具资源
                for(int i=0;i<6;i++)
                {
                    if (UnityEngine.Random.Range(0, 100) < 50 + actorResources[0])
                    {
                        int amount = PeopleLifeAI.instance.ResourceSize(Main.dieActorId, i, 30, 50);
                        UIDate.instance.ChangeTwoActorResource(Main.dieActorId, actorId, i,amount ,true);
                        getRecourseCache[i] = amount;
                        PeopleLifeAI.instance.AISetMassage(9, actorId, partId, placeId, new int[]
                        {
                        0,
                        i
                         }, Main.dieActorId, true);
                    }
                }

                List<int> itemIds = new List<int>(DateFile.instance.actorItemsDate[Main.dieActorId].Keys);
                foreach(KeyValuePair<int,int> item in DateFile.instance.actorItemsDate[Main.dieActorId])
                {
                    bool flag = false; //已被上过毒
                    for(int i=0;i<6;i++)
                    {
                        if(int.Parse(DateFile.instance.GetItemDate(item.Key,71+i,true))>0)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (UnityEngine.Random.Range(0, 100) < 30 && int.Parse(DateFile.instance.GetItemDate(item.Key, 53)) == 1&&flag)
                    {
                        int rand = UnityEngine.Random.Range(1, 7);
                        for(int i=0;i<rand;i++)
                            DateFile.instance.ChangItemDate(item.Key, 71+UnityEngine.Random.Range(0,6), UnityEngine.Random.Range(1, 11) * 100, true);
                    }
                }
                for (int i = 0; i < itemIds.Count; i++)
                {
                    if (UnityEngine.Random.Range(0, 100) < 50 + actorResources[0])
                    {
                        if (settings.noPoisonItem)
                        {
                            if (int.Parse(DateFile.instance.GetItemDate(itemIds[i], 4, true)) != 4)
                            {
                                for (int j = 0; j < 6; j++)
                                {
                                    if (int.Parse(DateFile.instance.GetItemDate(itemIds[i], 71 + j, true)) > 0)
                                        continue;
                                }
                            }
                        }
                        getItemCache.Add(itemIds[i], DateFile.instance.actorItemsDate[Main.dieActorId][itemIds[i]]);
                        PeopleLifeAI.instance.AISetMassage(13, actorId, partId, placeId, new int[]
                        {
                        0,
                        int.Parse(DateFile.instance.GetItemDate(itemIds[i], 999, true))
                         }, Main.dieActorId, true);
                        DateFile.instance.ChangeTwoActorItem(Main.dieActorId, actorId, itemIds[i], DateFile.instance.actorItemsDate[Main.dieActorId][itemIds[i]], -1);
                    }
                }
                if (getRecourseCache.Max()!=0 || getItemCache.Count!=0)
                {
                    Main.Logger.Log("成功盗取道具资源");
                    MassageWindow.instance.eventValue = new List<int> { 0, 0 };
                    Main.RobTombsuccessfully();
                }
                else
                {
                    Main.Logger.Log("一无所获");
                    EndToEvent(1998021);
                }   
            }
            else if (round == maxRound)
            {
                
                round += 1;
                Main.EndToEvent(1998012);
            }
            else 
            {
                round++;
                enemyValueId = 0;
                if (baseGongId != 0 &&baseGongId!=16)
                {
                    enemyValueId = DateFile.instance.GetGangValueId(baseGongId, Mathf.Clamp(10 - level + UnityEngine.Random.Range(-1, 2), 1, 9));
                }
                else 
                {
                    int id = normalActors[UnityEngine.Random.Range(0, normalActors.Count)];
                    bool battle = false;
                    int goodness = DateFile.instance.GetActorGoodness(id);
                    int brave = ActorMenu.instance.GetActorResources(id)[3];
                    int power = int.Parse(DateFile.instance.GetActorDate(id, 993, false));
                    if (goodness == 2||goodness==4)
                    {
                        if (power + brave * 100 >= int.Parse(DateFile.instance.GetActorDate(DateFile.instance.mianActorId, 993, false))&&int.Parse(DateFile.instance.GetActorDate(id,19,false))!=16)
                        {
                            battle = true;
                        }
                    }
                    if (battle)
                        enemyValueId = id;
                }
                Logger.Log(enemyValueId.ToString());
                if (enemyValueId != 0)
                {
                    if (baseGongId != 0 && baseGongId!=16)
                    {
                        int battleEnemyId = int.Parse(DateFile.instance.presetGangGroupDateValue[enemyValueId][301].Split(new char[] { '|' })[0]);
                        if(baolu)
                        {
                            List<int> boss = new List<int>();
                            boss.AddRange(DateFile.instance.GetGangActor(baseGongId, 1));
                            boss.AddRange(DateFile.instance.GetGangActor(baseGongId, 2));
                            if(boss.Count>0)
                            {
                                MassageWindow.instance.mianEventDate[1] = boss[UnityEngine.Random.Range(0, boss.Count)];
                                Logger.Log("援军到场");
                                EndToEvent(19980265);
                                return;
                            }
                        }
                        Logger.Log(battleEnemyId.ToString());
                        MassageWindow.instance.mianEventDate[1] = battleEnemyId;
                        Logger.Log("被人发现1");
                        EndToEvent(1998013);
                    }
                    else 
                    {
                        MassageWindow.instance.mianEventDate[1] = enemyValueId;
                        Logger.Log("被人发现2");
                        EndToEvent(1998024);
                    }                    
                }
                else
                {
                    Logger.Log("被人发现3");
                    EndToEvent(1998015);
                }
            }
        }
    }

    /// <summary>
    /// 替换按钮功能
    /// </summary>
    [HarmonyPatch(typeof(WorldMapSystem), "OpenToStory")]
    public class RobTomb_OpenToStory_Patch
    {
        public static bool Prefix()
        {
            if (!Main.enabled)
            {
                return true;
            }
            else if (Main.settings.daomu)
            {
                
                int partId = DateFile.instance.mianPartId;
                int placeId = DateFile.instance.mianPlaceId;
                Main.Clear();
                Main.normalActors = DateFile.instance.HaveActor(partId, placeId, true, false, true, true);
                List<int> gangId = new List<int>(DateFile.instance.gangDate.Keys);
                for(int i =0;i<gangId.Count;i++)
                {
                    if(DateFile.instance.GetGangDate(gangId[i], 0) == DateFile.instance.GetNewMapDate(partId, placeId, 98))
                    {
                        Main.baseGongId = gangId[i];                        
                    }
                    if(int.Parse(DateFile.instance.GetGangDate(gangId[i],3))==partId&& int.Parse(DateFile.instance.GetGangDate(gangId[i], 4))==placeId)
                    {
                        Main.isinGang = true;
                        Main.baseGongId = gangId[i];
                        break;
                    }
                }
                Main.Logger.Log(DateFile.instance.GetGangDate(Main.baseGongId,0));
                Main.RobTomb2();
               
                return false;
            }
            else return true;
        }
    }

    /// <summary>
    ///设置按钮可用于否
    /// </summary>
    [HarmonyPatch(typeof(WorldMapSystem), "UpdateToStoryButton")]
    public class RobTomb_UpdateToStoryButton_Patch
    {
        public static bool Prefix()
        {
            if (!Main.enabled)
            {
                DateFile.instance.massageDate[619][1] = "显示此地正在发生的奇遇事件…";
                DateFile.instance.massageDate[619][0] = "奇遇";
                return true;
            }
            else if (Main.settings.daomu)
            {
                DateFile.instance.massageDate[619][0] = "盗墓";
                DateFile.instance.massageDate[619][1] = "消耗时间挖掘此地坟墓获取道具或资源…";
                bool flag = WorldMapSystem.instance.choosePartId == DateFile.instance.mianPartId && WorldMapSystem.instance.choosePlaceId == DateFile.instance.mianPlaceId;
                List<int> list = DateFile.instance.HaveActor(WorldMapSystem.instance.choosePartId, WorldMapSystem.instance.choosePlaceId, false, true, false, false);
                WorldMapSystem.instance.openToStoryButton.interactable = list.Count > 0 && flag;
                return false;
            }
            else
            {
                DateFile.instance.massageDate[619][1] = "显示此地正在发生的奇遇事件…";
                DateFile.instance.massageDate[619][0] = "奇遇";
                return true;
            }
        }
    }

    /// <summary>
    /// 创建坟墓页面
    /// </summary>
    [HarmonyPatch(typeof(MassageWindow), "GetActor")]
    public class RobTomb_GetAcotr_Patch
    {
        public static bool Prefix()
        {
            if (!Main.enabled)
            {
                return true;
            }
            else if (Main.settings.daomu && MassageWindow.instance.massageItemTyp == RobTomb_LoadData.id["Event_Date"][1998001])
            {
                for (int i = 0; i < MassageWindow.instance.actorHolder.childCount; i++)
                {
                    UnityEngine.Object.Destroy(MassageWindow.instance.actorHolder.GetChild(i).gameObject);
                }
                int num = DateFile.instance.MianActorID();
                int partId = DateFile.instance.mianPartId;
                int placeId = DateFile.instance.mianPlaceId;
                List<int> list =DateFile.instance.HaveActor(partId, placeId, false, true, false, true);
                List<int> dieActors = new List<int>();
                switch (Main.settings.search)
                {
                    case 0:
                        {
                            dieActors.AddRange(list);
                            break;
                        }
                    case 1:
                        {
                            foreach (int id in list)
                            {
                                if (DateFile.instance.actorsDate[id].ContainsKey(79))
                                {
                                    dieActors.Add(id);
                                }
                            }
                            break;
                        }
                    case 2:
                        {
                            foreach (int id in list)
                            {
                                if (!DateFile.instance.actorsDate[id].ContainsKey(79))
                                {
                                    dieActors.Add(id);
                                }
                            }
                            break;
                        }
                    case 3:
                        {
                            foreach (int id in list)
                            {
                                string s;
                                if (DateFile.instance.actorsDate[id].TryGetValue(79,out s))
                                {
                                     if(int.Parse(s)==2)
                                         dieActors.Add(id);
                                }
                            }
                            break;
                        }
                }               
                switch (Main.settings.paixu)
                {
                    case 0:
                        {
                            break;
                        }
                    case 1:
                        {
                            dieActors.Sort(Main.SortList1);
                            break;
                        }
                    case 2:
                        {
                            dieActors.Sort(Main.SortList2);
                            break;
                        }                                   
                }
                int number;
                bool flag = int.TryParse(Main.settings.amount,out number);
                if (flag && number == 0) flag = false;
                for (int num10 = 0; num10 < dieActors.Count && (num10<number||!flag); num10++)
                {
                    int num11 = dieActors[num10];
                    int level = Math.Abs(int.Parse(DateFile.instance.GetActorDate(num11, 20, false)));
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(MassageWindow.instance.actorIcon, Vector3.zero, Quaternion.identity);
                    gameObject.name = "Actor," + num11;
                    gameObject.transform.SetParent(MassageWindow.instance.actorHolder, false);
                    gameObject.GetComponent<Toggle>().group = MassageWindow.instance.actorHolder.GetComponent<ToggleGroup>();
                    if (DateFile.instance.acotrTeamDate.Contains(num11))
                    {
                        gameObject.transform.Find("IsInTeamIcon").gameObject.SetActive(true);
                    }
                    gameObject.transform.Find("IsInBuildingIcon").gameObject.SetActive(DateFile.instance.ActorIsWorking(num11) != null);
                    int num12 = DateFile.instance.GetActorFavor(false, num, num11, false, false);
                    gameObject.transform.Find("ListActorFavorText").GetComponent<Text>().text = ((num11 != num && num12 != -1) ? ActorMenu.instance.Color5(num12, true, -1) : DateFile.instance.SetColoer(20002, DateFile.instance.massageDate[303][2], false));
                    gameObject.transform.Find("ListActorNameText").GetComponent<Text>().text = DateFile.instance.SetColoer(20011 - level, DateFile.instance.GetActorName(num11, false, false));
                    Transform transform = gameObject.transform.Find("ListActorFaceHolder").Find("FaceMask").Find("MianActorFace");
                    transform.GetComponent<ActorFace>().SetActorFace(num11, false);
                }
                dieActors.Clear();
                return false;
            }
            else return true;
        }
    }


    /// <summary>
    /// 选择盗墓人物
    /// </summary>
    [HarmonyPatch(typeof(MassageWindow), "SetActor")]
    public class RobTomb_SetActor_Patch
    {
        public static bool Prefix(MassageWindow __instance, ref int ___chooseActorId)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else
            {
                if (__instance.mianEventDate[2] == RobTomb_LoadData.id["Event_Date"][19981])
                {
                    for (int i = 0; i < MassageWindow.instance.actorHolder.childCount; i++)
                    {
                        UnityEngine.Object.Destroy(MassageWindow.instance.actorHolder.GetChild(i).gameObject);
                    }
                    Main.dieActorId = ___chooseActorId;
                    __instance.mianEventDate[1] = ___chooseActorId;
                    MassageWindow.instance.CloseActorsWidow();
                    MassageWindow.instance.StartCoroutine(Main.BackToMassageWindow(0.2f, ___chooseActorId, 0, 0));
                    return false;
                }
                else return true;
            }
        }
    }

    /// <summary>
    ///  创建选择物品菜单
    /// </summary>
    [HarmonyPatch(typeof(MassageWindow), "GetItem")]
    public class RobTomb_GetItem_Patch
    {
        public static bool Prefix()
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else if(MassageWindow.instance.massageItemTyp == RobTomb_LoadData.id["Event_Date"][1998014402])
            {
                for (int i = 0; i < MassageWindow.instance.itemHolder.childCount; i++)
                {
                    UnityEngine.Object.Destroy(MassageWindow.instance.itemHolder.GetChild(i).gameObject);
                }
                List<int> itemId = new List<int>(Main.getItem.Keys);
                int actorID = DateFile.instance.mianActorId;
                for (int i = 0; i < itemId.Count; i++)
                {
                    int num8 = itemId[i];
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(ActorMenu.instance.itemIconNoDrag, Vector3.zero, Quaternion.identity);
                    gameObject.name = "Item," + num8;
                    gameObject.transform.SetParent(MassageWindow.instance.itemHolder, false);
                    gameObject.GetComponent<Toggle>().group = MassageWindow.instance.itemHolder.GetComponent<ToggleGroup>();
                    Image component = gameObject.transform.Find("ItemBack").GetComponent<Image>();
                    SingletonObject.getInstance<DynamicSetSprite>().SetImageSprite(component, "itemBackSprites", new int[]
                    {
                int.Parse(DateFile.instance.GetItemDate(num8, 4, true))
                    });
                    component.color = ActorMenu.instance.LevelColor(int.Parse(DateFile.instance.GetItemDate(num8, 8, true)));
                    bool flag16 = int.Parse(DateFile.instance.GetItemDate(num8, 6, true)) > 0;
                    if (flag16)
                    {
                        gameObject.transform.Find("ItemNumberText").GetComponent<Text>().text = "×" + DateFile.instance.GetItemNumber(actorID, num8);
                    }
                    else
                    {
                        int num11 = int.Parse(DateFile.instance.GetItemDate(num8, 901, true));
                        int num12 = int.Parse(DateFile.instance.GetItemDate(num8, 902, true));
                        gameObject.transform.Find("ItemNumberText").GetComponent<Text>().text = string.Format("{0}{1}</color>/{2}", ActorMenu.instance.Color3(num11, num12), num11, num12);
                    }
                    GameObject gameObject2 = gameObject.transform.Find("ItemIcon").gameObject;
                    gameObject2.name = "ItemIcon," + num8;
                    SingletonObject.getInstance<DynamicSetSprite>().SetImageSprite(gameObject2.GetComponent<Image>(), "itemSprites", new int[]
                    {
                int.Parse(DateFile.instance.GetItemDate(num8, 98, true))
                    });
                }
                return false;
            }
            return true;
        }
    }

    /// <summary>
    ///选择藏匿的物品
    /// </summary>
    [HarmonyPatch(typeof(MassageWindow), "SetItem")]
    public class RobTomb_SetItem_Patch
    {
        public static bool Prefix(MassageWindow __instance)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else
            {
                if (__instance.mianEventDate[2] == RobTomb_LoadData.id["Event_Date"][199801440])
                {
                    Main.safeitemId = ActorMenu.instance.choseItemId;
                    MassageWindow.instance.CloseItemsWidow();
                    MassageWindow.instance.StartCoroutine(Main.BackToMassageWindow(0.2f, ActorMenu.instance.choseItemId, 0, 0));
                    return false;
                }
                else return true;
            }
        }
    }

    /// <summary>
    /// 事件结束
    /// </summary>
    [HarmonyPatch(typeof(MassageWindow), "EndEvent")]
    public class RobTomb_EndEvent_Patch
    {
        public static bool Prefix(string ___inputText)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else
            {
                int actorId = DateFile.instance.mianActorId;
                if (MassageWindow.instance.eventValue.Count > 0 && MassageWindow.instance.eventValue[0] != 0)
                {
                    switch (MassageWindow.instance.eventValue[0])
                    {
                        case 199801: 
                            {
                                Main.RobTomb();
                                return false;
                            }
                        case 199802:
                            {
                                Main.Finish();
                                return false;
                            }
                        case 199803:
                            {
                                Main.DoFlee();
                                return false;
                            }

                        case 199804:
                            {
                                Main.InputFix();
                                return false;
                            }

                        case 199806:
                            {
                                Main.Bribe(int.Parse(___inputText));
                                return false;
                            }

                        case 199807:
                            {
                                Main.SweetTalk();
                                return false;
                            }

                        case 199808:
                            {
                                Main.NoResistance();
                                return false;
                            }
                        case 199805:
                            {
                                if (!MassageWindow.instance.chooseItemEvents.Contains(RobTomb_LoadData.id["Event_Date"][1998014402]))
                                    MassageWindow.instance.chooseItemEvents.Add(RobTomb_LoadData.id["Event_Date"][1998014402]);
                                if (Main.hasKill)
                                    DateFile.instance.SetActorFameList(actorId, 108, 1);
                                return false;
                            }
                        case 199809:
                            {
                                Main.Punish();
                                return false;
                            }
                        case 199810:
                            {
                                Main.LearnGongFa();
                                return false;
                            }
                        case 199811:
                            {
                                Main.LearnJiYi();
                                return false;
                            }
                        case 199812:
                            {
                                Main.hasWaived = true;
                                Main.RobTomb();
                                return false;
                            }
                        case 199813:
                            {
                                Main.RobTombsuccessfully();
                                return false;
                            }
                        case 199814:
                            {
                                Main.ZombieFlee();
                                return false;
                            }
                        case 199815:
                            {
                                Main.BattleAgainstZoobie();
                                return false;
                            }
                        case 199816:
                            {
                                Main.SneakRaid();
                                return false;
                            }
                        case 199817:
                            {
                                Main.WinZoobie();
                                return false;
                            }
                        case 199818:
                            {
                                Main.Kill();
                                return false;
                            }
                        case 199819:
                            {
                                Main.LetHeLeave();
                                return false;
                            }
                    }
                    return true;
                }
                return true;
            }
        }
    }

    /// <summary>
    /// 自定义替换文本
    /// </summary>
    [HarmonyPatch(typeof(MassageWindow), "ChangeText")]
    public class RobTomb_ChangeText_Patch
    {
        public static void Postfix(ref string __result)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return;
            }
            else if(RobTomb_LoadData.id["Event_Date"].Values.Contains(MassageWindow.instance.mianEventDate[2]))
            {
                try
                {
                    int gangId = int.Parse(DateFile.instance.GetActorDate(Main.dieActorId, 19, false));
                    int level = int.Parse(DateFile.instance.GetActorDate(Main.dieActorId, 20, false));
                    __result = __result.Replace("LEVEL", DateFile.instance.presetGangGroupDateValue[DateFile.instance.GetGangValueId(gangId, level)][1001]);
                    __result = __result.Replace("DN", DateFile.instance.GetActorName(Main.dieActorId, false, false));
                    __result = __result.Replace("PLACE", DateFile.instance.GetGangDate(Main.baseGongId, 0));
                    __result = __result.Replace("FAME", DateFile.instance.GetActorFameText(DateFile.instance.mianActorId));
                    __result = __result.Replace("XING", DateFile.instance.actorSurnameDate[int.Parse(DateFile.instance.GetActorDate(DateFile.instance.mianActorId, 29, false))][0]);
                    if (MassageWindow.instance.mianEventDate[2] == RobTomb_LoadData.id["Event_Date"][1998017] || MassageWindow.instance.mianEventDate[2] == RobTomb_LoadData.id["Event_Date"][199801711] || MassageWindow.instance.mianEventDate[2] == RobTomb_LoadData.id["Event_Date"][199801713])
                        __result = __result.Replace("JIYI", DateFile.instance.skillDate[Main.gongFaId][0]);
                    else
                        __result = __result.Replace("GONGFA", DateFile.instance.gongFaDate[Main.gongFaId][0]);
                    __result = __result.Replace("JILV", Main.nextjilv.ToString());
                }
                catch(Exception e)
                {
                    Main.Logger.Log(e.Message);
                    Main.Logger.Log("RobTomb_ChangeText_Patch");
                }
                return;
            }

        }
    }

    [HarmonyPatch(typeof(MassageWindow), "GetEventIF")]
    public class RobTomb_GetEventIF_Patch
    {
        public static bool Prefix(ref bool __result, int eventId)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else if (eventId == RobTomb_LoadData.id["Event_Date"][1998014402])
            {
                if (Main.getItem.Count > 0)
                {
                    __result = true;
                }
                else __result = false;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 生成zoobie
    /// </summary>
    [HarmonyPatch(typeof(DateFile), "MakeNewActor")]
    public class RobTomb_MakeNewActor_Patch
    {
        public static bool Prefix(int baseActorId, bool makeNewFeatures, int temporaryId, int age, int baseCharm, string[] attrValue, string[] skillValue, string[] gongFaValue, string[] resourceValue, int randObbs,ref int __result)
        {
            if(!Main.enabled||!Main.settings.daomu)
            {
                return true;
            }
            else if(baseActorId==RobTomb_LoadData.id["PresetActor_Date"][2000])
            {
                int num = temporaryId;
                int level = 10 - Math.Abs(int.Parse(DateFile.instance.GetActorDate(Main.dieActorId, 20, false)));
                int zoobielevel = Mathf.Clamp(level + UnityEngine.Random.Range(-1, 2), 1, 10);
                MethodInfo DoActorMake= typeof(DateFile).GetMethod("DoActorMake", BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic);
                if(zoobielevel<10)
                {
                    DoActorMake.Invoke(DateFile.instance, new object[] { baseActorId, num, makeNewFeatures, 0, 0, age, attrValue, skillValue, gongFaValue, resourceValue, baseCharm, null, null, randObbs, 0, 0 });
                }                
                else //僵尸王尚未实装
                {
                    DoActorMake.Invoke(DateFile.instance, new object[] { baseActorId, num, makeNewFeatures, 0, 0, age, attrValue, skillValue, gongFaValue, resourceValue, baseCharm, null, null, randObbs, 0, 0 });
                }
                DateFile.instance.MakeActorName(num, int.Parse(DateFile.instance.GetActorDate(num, 29, false)), DateFile.instance.GetActorDate(num, 5, false), true);
                DateFile.instance.actorsDate[num][20] = Mathf.Clamp(10-zoobielevel,1,9).ToString();
                DateFile.instance.actorsDate[num][8] = "3";
                DateFile.instance.actorsDate[num][706] = (int.Parse(DateFile.instance.presetActorDate[baseActorId][706])+5000*zoobielevel).ToString();
                DateFile.instance.actorsDate[num][901] = (zoobielevel * 3).ToString();
                DateFile.instance.actorsDate[num][81] = (int.Parse(DateFile.instance.presetActorDate[baseActorId][81])+ 1000* zoobielevel).ToString();
                DateFile.instance.actorsDate[num][82] = (int.Parse(DateFile.instance.presetActorDate[baseActorId][82]) +1000* zoobielevel).ToString();
                DateFile.instance.actorsDate[num][71] = (int.Parse(DateFile.instance.presetActorDate[baseActorId][71]) + 50 * zoobielevel* Mathf.Clamp(zoobielevel-5,1,7)).ToString();
                DateFile.instance.actorsDate[num][72] = (int.Parse(DateFile.instance.presetActorDate[baseActorId][72]) + 50 * zoobielevel* Mathf.Clamp(zoobielevel - 5, 1, 7)).ToString();
                DateFile.instance.actorsDate[num][73] = (int.Parse(DateFile.instance.presetActorDate[baseActorId][73]) + 50 * zoobielevel* Mathf.Clamp(zoobielevel - 5, 1, 7)).ToString();
                DateFile.instance.actorsDate[num][32] = (int.Parse(DateFile.instance.presetActorDate[baseActorId][32])  + zoobielevel * 500).ToString();
                DateFile.instance.actorsDate[num][33] = (int.Parse(DateFile.instance.presetActorDate[baseActorId][33])  + zoobielevel * 500).ToString();

                int num7 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1101]);
                int num8 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1102]);
                int num9 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1106]);
                int num10 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1107]);
                int num11 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1108]);
                int num12 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1109]);
                int num13 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1111]);
                if (num7 > 100)
                {
                    DateFile.instance.actorsDate[num][1101] = (100 + (num7 - 100) * zoobielevel).ToString();
                }
                if (num8 > 100)
                {
                    DateFile.instance.actorsDate[num][1102] = (100 + (num8 - 100) * zoobielevel).ToString();
                }
                if (num9 > 100)
                {
                    DateFile.instance.actorsDate[num][1106] = (100 + (num9 - 100) * zoobielevel).ToString();
                }
                if (num10 > 100)
                {
                    DateFile.instance.actorsDate[num][1107] = (100 + (num10 - 100) * zoobielevel).ToString();
                }
                if (num11 > 100)
                {
                    DateFile.instance.actorsDate[num][1108] = (100 + (num11 - 100) * zoobielevel).ToString();
                }
                if (num12 > 100)
                {
                    DateFile.instance.actorsDate[num][1109] = (100 + (num12 - 100) * zoobielevel).ToString();
                }
                if (num13 > 100)
                {
                    DateFile.instance.actorsDate[num][1111] = (100 + (num13 - 100) * zoobielevel).ToString();
                }
                int num14 = int.Parse(DateFile.instance.presetActorDate[baseActorId][92]);
                int num15 = int.Parse(DateFile.instance.presetActorDate[baseActorId][93]);
                int num16 = int.Parse(DateFile.instance.presetActorDate[baseActorId][94]);
                int num17 = int.Parse(DateFile.instance.presetActorDate[baseActorId][95]);
                int num18 = int.Parse(DateFile.instance.presetActorDate[baseActorId][96]);
                int num19 = int.Parse(DateFile.instance.presetActorDate[baseActorId][97]);
                int num20 = int.Parse(DateFile.instance.presetActorDate[baseActorId][98]);
                if (num14 > 100)
                {
                    DateFile.instance.actorsDate[num][92] = (100 + (num14 - 100) * zoobielevel).ToString();
                }
                if (num15 > 100)
                {
                    DateFile.instance.actorsDate[num][93] = (100 + (num15 - 100) * zoobielevel).ToString();
                }
                if (num16 > 100)
                {
                    DateFile.instance.actorsDate[num][94] = (100 + (num16 - 100) * zoobielevel).ToString();
                }
                if (num17 > 100)
                {
                    DateFile.instance.actorsDate[num][95] = (100 + (num17 - 100) * zoobielevel).ToString();
                }
                if (num18 > 100)
                {
                    DateFile.instance.actorsDate[num][96] = (100 + (num18 - 100) * zoobielevel).ToString();
                }
                if (num19 > 100)
                {
                    DateFile.instance.actorsDate[num][97] = (100 + (num19 - 100) * zoobielevel).ToString();
                }
                if (num20 > 100)
                {
                    DateFile.instance.actorsDate[num][98] = (100 + (num20 - 100) * zoobielevel).ToString();
                }
                for(int i=0;i<6;i++)
                {
                    DateFile.instance.actorsDate[num][61+i] = (int.Parse(DateFile.instance.presetActorDate[baseActorId][61+i])*zoobielevel).ToString();
                }
                DateFile.instance.MakeNewActorGongFa(num, true);
                int item = RobTomb_LoadData.id["Item_Date"][20000];
                DateFile.instance.actorsDate[num][201] = (item+Mathf.Clamp(zoobielevel-1,0,6)).ToString()+"&" + ((20-zoobielevel)/2).ToString();
                DateFile.instance.MakeNewActorItem(num);
                int weaponId = int.Parse(DateFile.instance.actorsDate[num][301]);
                for(int i=0;i<6;i++)
                {
                    DateFile.instance.ChangItemDate(weaponId, 71 + i, 500 * zoobielevel, true); 
                }
                DateFile.instance.ChangItemDate(weaponId,601,5000+2000 * zoobielevel, true);
                DateFile.instance.ChangItemDate(weaponId, 603, 10000 + 2000 * zoobielevel, true);
                DateFile.instance.ChangItemDate(weaponId,503, 4000 + 100 * zoobielevel, true);
                if(!(DateFile.instance.itemsDate[weaponId].Keys).Contains(8))
                {
                    DateFile.instance.itemsDate[weaponId].Add(8, Mathf.Clamp(zoobielevel, 1, 9).ToString());
                }
                else
                {
                    DateFile.instance.itemsDate[weaponId][8]= Mathf.Clamp(zoobielevel, 1, 9).ToString();
                }
                DateFile.instance.SetActorEquipGongFa(num, true, true);
                __result=num;
                return false;
            }
            return true;
        }
    }


    /// <summary>
    /// 移动限制
    /// </summary>
    [HarmonyPatch(typeof(BattleSystem), "UpdateBattleRange")]
    public class RobTomb_UpdateBattleRange_Patch
    {
        public static bool Prefix(ref int range,bool isActor)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else if (DateFile.instance.GetActorDate(BattleSystem.instance.ActorId(false, false), 997, false) == RobTomb_LoadData.id["PresetActor_Date"][2000].ToString())
            {
                if(range>60)
                {
                    range = 60;
                    BattleSystem.instance.ShowBattleState(RobTomb_LoadData.id["GongFaOtherFPower_Date"][20000], isActor);
                }
            } 
            return true;
        }
    }

    /// <summary>
    /// 辟邪特效增加穿透
    /// </summary>
    [HarmonyPatch(typeof(BattleVaule), "GetAttackDef")]
    public class RobTomb_GetAttackDef_Patch
    {
        public static void Postfix(bool isActor, int defActorId, int weaponId, int gongFaId,ref int __result)
        {
            if(!Main.enabled||!Main.settings.daomu)
            {
                return;
            }
            if(isActor == true &&int.Parse(DateFile.instance.GetActorDate(defActorId,997,false))== RobTomb_LoadData.id["PresetActor_Date"][2000])
            {
                if (!Main.bixieWeapon.Contains(int.Parse(DateFile.instance.GetItemDate(weaponId, 999, true))))
                    return ;
                int level = int.Parse(DateFile.instance.GetItemDate(weaponId, 8, true));
                if(__result < 0)
                {
                    __result = __result - level * 5;
                }
                else if(__result-level*10<0)
                {
                    __result = -(level * 10 - __result) / 2;
                }
                else
                __result = __result - level*10;
                BattleSystem.instance.ShowBattleState(RobTomb_LoadData.id["GongFaOtherFPower_Date"][19999], isActor);
                return;
            }
        }
    }

    /// <summary>
    /// 修复距离限定到6时不能逃跑
    /// </summary>
    [HarmonyPatch(typeof(BattleSystem), "UpdateDoOtherButton")]
    public class RobTomb_UpdateDoOtherButton_Patch
    {
        public static void Postfix(ref bool ___battleGo, ref int ___actorNeedUseGongFa, ref int ___actorDoOtherTyp, ref int ___actorDoingOtherTyp)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return;
            }
            else if (int.Parse(DateFile.instance.GetActorDate(BattleSystem.instance.ActorId(false,false),997,false))== RobTomb_LoadData.id["PresetActor_Date"][2000])
            {
                bool flag = ___battleGo && ___actorNeedUseGongFa == 0 && BattleSystem.instance.actorUseGongFaId == 0 && ___actorDoOtherTyp == 0 && ___actorDoingOtherTyp == 0;
                BattleSystem.instance.battlerRunButton.interactable = (flag && BattleSystem.instance.battleRange >= 60);
            }
        }
    }


    /// <summary>
    /// 修复敌人AI在战斗距离6时产生的BUG       其实还是有bug 但再想修就得去写整个AI了ORZ
    /// </summary>
    [HarmonyPatch(typeof(BattleSystem), "SetNeedRange")]
    public class RobTomb_SetNeedRange_Patch
    {
        public static void Postfix(bool isActor,int value,ref int ___AI_MoveToDefRange,ref int ___AI_MoveToHealRange,ref int ___AI_MoveToUnAttackRange)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return;
            }
            else if (!isActor&& int.Parse(DateFile.instance.GetActorDate(BattleSystem.instance.ActorId(false, false), 997, false)) == RobTomb_LoadData.id["PresetActor_Date"][2000])
            { 
                if (___AI_MoveToDefRange != -1)
                {
                    ___AI_MoveToDefRange = Mathf.Min(___AI_MoveToDefRange, 60);
                    value = ___AI_MoveToDefRange;
                }
                else if (___AI_MoveToHealRange != -1)
                {
                    ___AI_MoveToHealRange = Mathf.Min(___AI_MoveToHealRange, 60);
                    value = ___AI_MoveToHealRange;
                }
                else if (___AI_MoveToUnAttackRange != -1)
                {
                    ___AI_MoveToUnAttackRange = Mathf.Min(___AI_MoveToUnAttackRange, 60);
                    value = ___AI_MoveToUnAttackRange;
                }
                BattleSystem.instance.enemyNeedRange = ((value != -1) ? value : BattleSystem.instance.battleRange);
                BattleSystem.instance.enemyNeedRangeSlider.value = (float)BattleSystem.instance.enemyNeedRange;
                BattleSystem.instance.enemyNeedRangeText.text = ((float)BattleSystem.instance.enemyNeedRange / 10f).ToString("f1");
            }
        }
    }
    
    [HarmonyPatch(typeof(BattleSystem), "SetupBattleEndEvent")]
    public class RobTomb_SetupBattleEndEvent_Patch
    {
        public static bool Prefix(ref int ___battleEndTyp, ref int ___mianEnemyId)
        {
            if(!Main.enabled||!Main.settings.daomu)
            {
                return true;
            }
            else
            {
                string[] array = DateFile.instance.enemyTeamDate[StartBattle.instance.enemyTeamId][101 + ___battleEndTyp].Split(new char[]
                {
                 '&'
                });
                List<int> list = new List<int>(RobTomb_LoadData.id["Event_Date"].Values);
                if (list.Contains(int.Parse(array[0])))
                {

                    if (int.Parse(array[0]) == RobTomb_LoadData.id["Event_Date"][1998025]) //恶战胜利逃走被暴露
                        Main.baolu = true;
                    int num2 =int.Parse(array[0]);
                    DateFile.instance.battleEndEvent = true;
                    DateFile.instance.SetEvent(new int[]
                                    {
                                    0,
                                    MassageWindow.instance.mianEventDate[1],
                                    num2,
                                    0
                                    }, true, true);
                    return false;
                }
            }         
            return true;
        }
    }

    [HarmonyPatch(typeof(UIDate), "UpdateMaxDayTime")]
    public class RobTomb_UpdateMaxDayTime_Patch
    {
        public static void Prefix()
        {
            if (!Main.enabled || !Main.settings.daomu)
                return;
            if(DateFile.instance.dayTime<Main.debtTime)
            {
                Main.debtTime = Main.debtTime - DateFile.instance.dayTime;
                DateFile.instance.dayTime = 0;          
            }
            else
            {
                DateFile.instance.dayTime = DateFile.instance.dayTime - Main.debtTime;
                Main.debtTime = 0;
            }
            
        }
    }

    [HarmonyPatch(typeof(BattleSystem), "AddBattleInjury")]
    public class RobTomb_AddBattleInjury_Patch
    {
        public static bool flag = true;
        public static bool Prefix(bool isActor, int actorId, int injuryId, int injuryPower)
        {
            if (!Main.enabled)
                return true;
            if (flag)
            {
                if (BattleSystem.instance.GetGongFaFEffect(779, isActor, actorId, 0))
                {
                    MethodInfo addbattleinjury = AccessTools.Method(AccessTools.TypeByName("BattleSystem"), "AddBattleInjury");
                    BattleSystem.instance.ShowBattleState(779, isActor);
                    int injurytyp = int.Parse(DateFile.instance.injuryDate[injuryId][1]) > 0 ? 0 : 1;
                    int hp = ActorMenu.instance.MaxHp(actorId);
                    int sp = ActorMenu.instance.MaxSp(actorId);
                    BattleSystem.instance.ShowBattleState(779, isActor);
                    float ssp = (float)sp / (hp + sp);
                    flag = false;
                    int hppower = (int)(injuryPower * (1 - ssp));
                    int sppower = (int)(injuryPower * ssp);
                    addbattleinjury.Invoke(BattleSystem.instance, new object[] { isActor, actorId, injurytyp == 0 ? injuryId : (injuryId - 3), hppower });
                    addbattleinjury.Invoke(BattleSystem.instance, new object[] { isActor, actorId, injurytyp == 0 ? (injuryId + 3) : injuryId, sppower });
                    flag = true;
                    return false;
                }
                return true;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(BattleSystem), "UpdateBattlerMagicAndStrength")]
    public class RobTom_UpdateBattlerMagicAndStrength_Patch
    {
        public static MethodInfo UpdateMagic = AccessTools.Method(AccessTools.TypeByName("BattleSystem"), "UpdateMagic");
        public static MethodInfo UpdateStrength = AccessTools.Method(AccessTools.TypeByName("BattleSystem"), "UpdateStrength");
        public static bool Prefix(bool isActor, int power, float ___actorMagic, float ___actorStrength, float ___enemyMagic, float ___enemyStrength)
        {
            if (!Main.enabled)
                return true;
            int actorId = BattleSystem.instance.ActorId(isActor, false);
            if (BattleSystem.instance.GetGongFaFEffect(5779, isActor, actorId, 0))
            {
                if (isActor)
                {
                    bool flag = ___actorMagic < 20000f;
                    if (flag)
                    {
                        float value = (float)(BattleVaule.instance.GetMagicSpeed(isActor, actorId, true, 1) * power / 100) * Time.timeScale;
                        UpdateMagic.Invoke(BattleSystem.instance, new object[] { isActor, value });
                    }
                    bool flag2 = ___actorStrength < 20000f;
                    if (flag2)
                    {
                        float value = (float)(BattleVaule.instance.GetMagicSpeed(isActor, actorId, true, 1) * power / 100) * Time.timeScale;
                        UpdateStrength.Invoke(BattleSystem.instance, new object[] { isActor, value });
                    }
                }
                else
                {
                    bool flag = ___enemyMagic < 20000f;
                    if (flag)
                    {
                        float value = (float)(BattleVaule.instance.GetMagicSpeed(isActor, actorId, true, 1) * power / 100) * Time.timeScale;
                        UpdateMagic.Invoke(BattleSystem.instance, new object[] { isActor, value });
                    }
                    bool flag2 = ___enemyStrength < 20000f;
                    if (flag2)
                    {
                        float value = (float)(BattleVaule.instance.GetMagicSpeed(isActor, actorId, true, 1) * power / 100) * Time.timeScale;
                        UpdateStrength.Invoke(BattleSystem.instance, new object[] { isActor, value });
                    }
                }
                return false;
            }
            else return true;
        }
    }

    [HarmonyPatch(typeof(BattleSystem), "ShowBaseGongFaState")]
    public class RobTom_ShowBaseGongFaState_Patch
    {
        public static void Postfix(bool isActor,ref float __result, bool showState = true)
        {
            int actorId = BattleSystem.instance.ActorId(isActor, false);
            if (BattleSystem.instance.GetGongFaFEffect(5779, isActor, actorId, 0))
            {
                if (showState)
                {
                    BattleSystem.instance.StartCoroutine(Traverse.Create(BattleSystem.instance).Method("WaitShowBattleState",new object[] { 5779, isActor, __result }).GetValue<IEnumerator>());
                }
                __result += 0.5f;
            }
        }
    }
}
