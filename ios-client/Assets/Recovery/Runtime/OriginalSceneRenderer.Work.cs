using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kairisei.Recovery
{
    public sealed partial class OriginalSceneRenderer
    {
        public const string WorkBundleName = "recovery/work.dat";
        private JArray workDefinitions;
        private string workPage;
        private int workFirst, workCount, workCapacity;
        private int? workFinger;
        private Vector2 workTouchStart;
        private Action<int> workSelect;
        private Action workBack;
        private Func<int, Dictionary<string, string>> workText;
        private string partyDeckCaption, ownedPartnerCaption;

        // Carries the real server values into the original picker templates. Node IDs come from the
        // verified source prefab. Unsupported decorations with authored sample data stay hidden.
        private sealed class WorkPicker
        {
            public readonly string Scene, Panel, Template, Back;
            public readonly float Pitch;
            public readonly int Visible;
            public readonly string Background;
            public readonly string[] PictureNodes;

            public WorkPicker(string scene, string panel, string template, string back, float pitch, int visible, string background, params string[] pictureNodes)
            {
                Scene = scene; Panel = panel; Template = template; Back = back; Pitch = pitch;
                Visible = visible; Background = background; PictureNodes = pictureNodes;
            }
        }

        // DynamicPicker item heights plus the vertical margins in the 2.0.4 scene export.
        private static readonly Dictionary<string, WorkPicker> WorkPickers = new Dictionary<string, WorkPicker>
        {
            { "QuestGroups", new WorkPicker("TeamSlSt", "1026196548002408", "1279359913592680", "1722951815217645", 165, 3, "1884547391421755", "1574190619683098") },
            { "QuestDifficulties", new WorkPicker("TeamSlSt", "1617365563186364", "1408345039935966", "1722951815217645", 158, 3, "1884547391421755", "1633296557590524", "1831874417830518", "1177930474251431") },
            { "QuestPartners", new WorkPicker("TeamSlPl", "1408345039935966", "1295428814336086", "1190181876552715", 155, 3, null, "1528911807818478") },
        };

        // The work bundle is optional for earlier server profiles. A failed load leaves Home usable.
        public bool CanOpenWork()
        {
            if (workDefinitions != null) return true;
            try
            {
                JObject work = JObject.Parse(content.Load<TextAsset>(WorkBundleName, "originalwork").text);
                if ((int?)work["schemaVersion"] != 1 || !(work["definitions"] is JArray definitions))
                    throw new FormatException("原版副本场景清单无效。");
                foreach (WorkPicker picker in WorkPickers.Values)
                    FindWorkDefinition(definitions, picker.Scene);
                FindWorkDefinition(definitions, "DeckSl");
                FindWorkDefinition(definitions, "CardInve");
                LoadWorkCardCatalog();
                workDefinitions = definitions;
                return true;
            }
            catch (Exception error) when (error is InvalidOperationException || error is FormatException || error is Newtonsoft.Json.JsonException)
            {
                ShowWorkError("原版卡组或副本场景资源未就绪，请连接提供该资源的服务器后重试。", null);
                return false;
            }
        }

        // Open a bounded set of real entries. Swiping reuses the authored templates on the next page.
        public void OpenWorkPicker(string page, int count, Func<int, Dictionary<string, string>> text, Action<int> select, Action back)
        {
            workPage = page;
            workCount = count;
            workFirst = 0;
            workCapacity = WorkPickers[page].Visible;
            workText = text;
            workSelect = select;
            workBack = back;
            RenderWorkPicker();
        }

        // Keep the authored player tile visible even if there are no rentable partners.
        public void OpenQuestPartnerPicker(int count, Func<int, Dictionary<string, string>> text, Action<int> select, Action back,
            string deckCaption, Action chooseDeck, string ownCaption, Action chooseOwnedPartner)
        {
            partyDeckCaption = deckCaption;
            ownedPartnerCaption = ownCaption;
            deckActions.Clear();
            deckActions.Add("choose-party-deck", chooseDeck);
            if (chooseOwnedPartner != null) deckActions.Add("choose-owned-partner", chooseOwnedPartner);
            OpenWorkPicker("QuestPartners", count, text, select, back);
        }

        // Use the real main scene renderer with only the authored layers needed for this path.
        private void RenderWorkPicker()
        {
            WorkPicker picker = WorkPickers[workPage];
            JObject source = FindWorkDefinition(workDefinitions, picker.Scene);
            var sourceNodes = new Dictionary<string, JObject>();
            foreach (JObject node in (JArray)source["nodes"]) sourceNodes.Add((string)node["id"], node);
            var projected = new Dictionary<string, JObject>();
            CopyWorkNode(sourceNodes, projected, picker.Back);
            Vector3 backPosition = Vector(projected[picker.Back]["position"]); backPosition.x = 0;
            projected[picker.Back]["position"] = new JArray(backPosition.x, backPosition.y, backPosition.z);
            foreach (var entry in sourceNodes)
                if (WithinWorkNode(sourceNodes, entry.Key, picker.Back))
                {
                    CopyWorkNode(sourceNodes, projected, entry.Key);
                    if (((JArray)projected[entry.Key]["widgets"]).Count > 0) projected[entry.Key]["workBack"] = true;
                }

            // The original TeamSlStBackBtn switches between the Home and Back atlas frames.
            if (workPage == "QuestDifficulties")
            {
                JObject partner = FindWorkDefinition(workDefinitions, "TeamSlPl");
                foreach (JObject node in (JArray)partner["nodes"])
                    if ((string)node["id"] == "1617365563186364")
                    {
                        var back = (JObject)node["widgets"][0];
                        var home = (JObject)projected["1215879266689626"]["widgets"][0];
                        home["frame"] = back["frame"].DeepClone();
                        home["resolved"] = back["resolved"].DeepClone();
                        break;
                    }
            }

            // Keep the background's authored sprite if present. Its map icons are separate controls.
            if (picker.Background != null) CopyWorkNode(sourceNodes, projected, picker.Background);
            CopyWorkNode(sourceNodes, projected, (string)sourceNodes[picker.Template]["parent"]);
            Vector3 panelPosition = Vector(projected[picker.Panel]["position"]);
            if (picker.Scene == "TeamSlSt") panelPosition.y = 0;
            else panelPosition.x = 0;
            projected[picker.Panel]["position"] = new JArray(panelPosition.x, panelPosition.y, panelPosition.z);
            if (workPage == "QuestPartners")
            {
                AddPartyDeckTile(sourceNodes, projected);
                if (deckActions.ContainsKey("choose-owned-partner")) AddOwnedPartnerTile(sourceNodes, projected);
            }

            var nodes = new JArray(projected.Values);
            for (int slot = 0; slot < workCapacity && workFirst + slot < workCount; slot++)
            {
                int index = workFirst + slot;
                Dictionary<string, string> rowText = workText(index);
                var included = new Dictionary<string, JObject>();
                CopyWorkRow(sourceNodes, included, picker.Template, picker.Template);
                foreach (string picture in picker.PictureNodes) CopyWorkRow(sourceNodes, included, picture, picker.Template);
                foreach (string label in rowText.Keys) CopyWorkRow(sourceNodes, included, label, picker.Template);
                foreach (var entry in included)
                {
                    JObject node = entry.Value;
                    string id = entry.Key, parent = (string)node["parent"];
                    node["id"] = id + "_" + slot;
                    node["parent"] = id == picker.Template ? parent : parent + "_" + slot;
                    node["enterAnimation"] = null;
                    node["active"] = true;
                    if (id == picker.Template)
                    {
                        Vector3 position = Vector(node["position"]);
                        // PickerItem is stored at the editor preview position. Its first runtime row
                        // aligns with the top of the scroll area before the authored pitch is applied.
                        node["position"] = new JArray(position.x, -103 - picker.Pitch * slot, position.z);
                    }
                    if (id == picker.PictureNodes[0]) node["workIndex"] = index;
                    if (rowText.TryGetValue(id, out string value))
                        foreach (JObject widget in (JArray)node["widgets"]) if ((string)widget["kind"] == "UILabel") widget["text"] = value;
                    nodes.Add(node);
                }
            }
            ShowScene(workPage, new JObject { { "name", workPage }, { "nodes", nodes } });
        }

        // Use the original player plate and label. Offset the exported offscreen templates into the left anchor.
        private void AddPartyDeckTile(Dictionary<string, JObject> source, Dictionary<string, JObject> projected)
        {
            const string playerPanel = "1061268324235159", plate = "1372920208702427", label = "1044625135789424";
            CopyWorkNode(source, projected, plate);
            CopyWorkNode(source, projected, label);
            SetWorkAxis(projected[playerPanel], 0, 0);
            SetWorkAxis(projected[plate], 0, 152);
            SetWorkAxis(projected[label], 0, 152);
            SetWorkAxis(projected[label], 1, (float)projected[plate]["position"][1]);
            projected[plate]["deckAction"] = "choose-party-deck";
            var caption = (JObject)projected[label]["widgets"][0];
            caption["pivot"] = 4;
            caption["maxLineWidth"] = 220;
            caption["text"] = partyDeckCaption;
        }

        // Reuse the original first member's plate for the self-deck selection entry.
        private void AddOwnedPartnerTile(Dictionary<string, JObject> source, Dictionary<string, JObject> projected)
        {
            const string panel = "1985305603660343", member = "1705390221481576";
            const string plate = "1336590748365959", label = "1875609422105747";
            CopyWorkNode(source, projected, plate);
            CopyWorkNode(source, projected, label);
            SetWorkAxis(projected[panel], 0, 0);
            SetWorkAxis(projected[member], 0, 152);
            SetWorkAxis(projected[member], 1, -110);
            SetWorkAxis(projected[label], 0, 0);
            SetWorkAxis(projected[label], 1, 0);
            projected[plate]["deckAction"] = "choose-owned-partner";
            var caption = (JObject)projected[label]["widgets"][0];
            caption["pivot"] = 4;
            caption["maxLineWidth"] = 210;
            caption["text"] = ownedPartnerCaption;
        }

        private static JObject FindWorkDefinition(JArray definitions, string scene)
        {
            foreach (JObject item in definitions) if ((string)item["name"] == scene) return item;
            throw new FormatException("缺少原版副本场景：" + scene);
        }

        private static void CopyWorkNode(Dictionary<string, JObject> source, Dictionary<string, JObject> destination, string id)
        {
            if (id == null || destination.ContainsKey(id)) return;
            JObject node = (JObject)source[id].DeepClone();
            CopyWorkNode(source, destination, (string)node["parent"]);
            node["enterAnimation"] = null;
            destination.Add(id, node);
        }

        private static bool WithinWorkNode(Dictionary<string, JObject> source, string id, string root)
        {
            while (id != null)
            {
                if (id == root) return true;
                id = (string)source[id]["parent"];
            }
            return false;
        }

        private static void CopyWorkRow(Dictionary<string, JObject> source, Dictionary<string, JObject> destination, string id, string root)
        {
            if (destination.ContainsKey(id)) return;
            JObject node = (JObject)source[id].DeepClone();
            if (id != root) CopyWorkRow(source, destination, (string)node["parent"], root);
            destination.Add(id, node);
        }

        // On a row, defer activation until touch release so a swipe never selects a quest.
        private bool HandleWorkTouch(int finger, TouchPhase phase, Vector2 screen)
        {
            if (currentScene != workPage || dialogRoot != null) { workFinger = null; return false; }
            if (phase == TouchPhase.Began)
            {
                if (!WorkRowAt(sceneCamera.ScreenPointToRay(screen))) return false;
                workFinger = finger;
                workTouchStart = screen;
                return true;
            }
            if (workFinger != finger) return false;
            if (phase == TouchPhase.Canceled) workFinger = null;
            if (phase == TouchPhase.Ended)
            {
                workFinger = null;
                float offset = screen.y - workTouchStart.y;
                if (Mathf.Abs(offset) >= 28) ScrollWork(offset > 0 ? 1 : -1);
                else if (Vector2.Distance(screen, workTouchStart) < 28) HandlePress(sceneCamera.ScreenPointToRay(screen), screen);
            }
            return true;
        }

        private bool WorkRowAt(Ray ray)
        {
            foreach (Widget widget in widgets)
                if (widget.Node["workIndex"] != null && WidgetContains(widget, ray)) return true;
            return false;
        }

        private void ScrollWork(int direction)
        {
            int stride = workPage == DeckInventory ? 5 : 1;
            int last = Mathf.Max(0, Mathf.CeilToInt((workCount - workCapacity) / (float)stride)) * stride;
            int first = Mathf.Clamp(workFirst + direction * stride, 0, last);
            if (first == workFirst) return;
            workFirst = first;
            if (workPage == DeckInventory) RenderDeckInventory();
            else RenderWorkPicker();
        }

        // Show selection state on the server-derived candidate, without opening a paid battle.
        public void RefreshWorkPicker()
        {
            if (workPage == currentScene) RenderWorkPicker();
        }
    }
}
