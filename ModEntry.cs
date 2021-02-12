using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using CustomPhoneCalls;

namespace CustomPhoneCalls
{
    public class ModEntry : Mod
    {
        public static IModHelper helper;
        public static IMonitor monitor;

        public const int customcallId = 12001;
        public static Dictionary<string, CustomCall> customCalls;

        public static List<string> receivedCalls;
        public const string receivedCallsSavename = "DadNavi.CustomhoneCalls.ReceivedCalls";

        public static string currentCall;
        public override void Entry(IModHelper helper)
        {
            ModEntry.helper = helper;
            ModEntry.monitor = Monitor;

            helper.Events.GameLoop.GameLaunched += (s, e) => {
                customCalls = helper.Content.Load<Dictionary<string, CustomCall>>("calls.json", ContentSource.ModFolder);
                monitor.Log("Loading custom calls from " + helper.Content.GetActualAssetKey("calls.json", ContentSource.ModFolder), LogLevel.Trace);
            };

            helper.Events.GameLoop.DayStarted += (s, e) => {
                var data = helper.Data.ReadSaveData<List<string>>(receivedCallsSavename);
                if (data is null) receivedCalls = new List<string>();
                else receivedCalls = data;
            };

            helper.Events.GameLoop.Saving += (s, e) => {
                helper.Data.WriteSaveData(receivedCallsSavename, receivedCalls);
            };

            helper.Events.GameLoop.TimeChanged += (s, e) => {
                if (!Game1.IsMasterGame) return;
                if (Phone.lastMinutesElapsedTick != Game1.ticks)
                {
                    Phone.lastMinutesElapsedTick = Game1.ticks;
                    if (Phone.intervalsToRing == 0)
                    {
                        Random r = new Random((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed * 2);
                        if (r.NextDouble() < 0.01)
                        {
                            var call = r.Next(customCalls.Keys.Count());
                            if (!receivedCalls.Contains(customCalls.Keys.ElementAt(call)) && Game1.timeOfDay < 1800)
                            {
                                currentCall = customCalls.Keys.ElementAt(call);
                                Phone.intervalsToRing = 3;
                                Game1.player.team.ringPhoneEvent.Fire(customcallId);
                            }
                        }
                    }
                }
            };

            helper.ConsoleCommands.Add("customcall", "Usage: customcall <call name from json>", (s, a) =>
            {
                Random r = new Random((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed * 2);
                var call = r.Next(customCalls.Keys.Count());
                if (a.Any())
                {
                    if (!customCalls.ContainsKey(a.First())) return;
                    currentCall = a.First();
                }
                else currentCall = customCalls.Keys.ElementAt(call);
                Phone.intervalsToRing = 3;
                Game1.player.team.ringPhoneEvent.Fire(customcallId);
            });

            var harmony = HarmonyInstance.Create(ModManifest.UniqueID);
            harmony.Patch(
               original: AccessTools.Method(typeof(Phone), nameof(Phone.checkForAction)),
               prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.checkForAction_Prefix))
            );
        }

        public static bool _phoneSoundPlayed {
            get => helper.Reflection.GetField<bool>(typeof(Phone), "_phoneSoundPlayed").GetValue();
            set => helper.Reflection.GetField<bool>(typeof(Phone), "_phoneSoundPlayed").SetValue(value);
        }
        public static bool checkForAction_Prefix(Phone __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
        {
            try
            {
                if (Phone.whichPhoneCall == customcallId)
                {
                    if (_phoneSoundPlayed)
                    {
                        Game1.soundBank.GetCue("phone").Stop(AudioStopOptions.Immediate);
                        _phoneSoundPlayed = false;
                    }
                    Game1.playSound("openBox");
                    Game1.player.freezePause = 500;
                    DelayedAction.functionAfterDelay((() =>
                    {
                        customCalls[currentCall].Receive();
                        Phone.whichPhoneCall = -1;
                        Phone.ringingTimer = 0;
                    }), 500);
                    __result = true;
                    return false;
                }
            }
            catch (Exception ex) { ModEntry.monitor.Log(ex.ToString(), LogLevel.Error); }
            return true;
        }
    }
}
