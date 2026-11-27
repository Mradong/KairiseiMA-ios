using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kairisei.Recovery
{
    public sealed partial class OriginalSceneRenderer
    {
        private const string DeckEditor = "DeckEditor";
        private const string DeckInventory = "DeckInventory";
        private const string QuestDeck = "QuestDeck";
        private const string SlotArtwork = "1011164340880974";
        private const string InventoryTemplate = "1372920208702427";
        private readonly Dictionary<string, Action> deckActions = new Dictionary<string, Action>();
        private readonly Dictionary<long, string> workCardPictures = new Dictionary<long, string>();
        private JObject workCards;

        // An earlier server profile may supply the board without its card-presentation catalog.
        private void LoadWorkCardCatalog()
        {
            TextAsset asset = content.LoadOptional<TextAsset>(WorkBundleName, "originalcards");
            if (asset == null) return;
            var catalog = JObject.Parse(asset.text);
            if ((int?)catalog["schemaVersion"] != 1 || !(catalog["cards"] is JObject cards) || !(catalog["textures"] is JArray portraits))
                throw new FormatException("原卡牌资源清单格式不正确。");
            workCardPictures.Clear();
            foreach (JObject portrait in portraits)
            {
                long picture = (long?)portrait["picture"] ?? 0;
                string key = (string)portrait["key"];
                if (picture <= 0 || key != "card-icon-" + picture.ToString(CultureInfo.InvariantCulture) || workCardPictures.ContainsKey(picture))
                    throw new FormatException("原卡牌插画标识无效。");
                workCardPictures.Add(picture, key);
            }
            workCards = cards;
        }

        // Pair the inventory instance with its definition. The original icon uses the max portrait only at both caps.
        public (string Caption, string Portrait) DescribeDeckCard(CnCardCollection collection, long ownedId)
        {
            if (ownedId == 0) return ("空槽", null);
            if (!collection.Cards.TryGetValue(ownedId, out CnCard card)) return ("库存未返回\n" + ownedId, null);
            JObject definition = workCards?[card.CardId.ToString(CultureInfo.InvariantCulture)] as JObject;
            string name = (string)definition?["name"];
            if (String.IsNullOrWhiteSpace(name)) name = "卡牌 " + card.CardId;
            long picture = (long?)definition?["normalPicture"] ?? 0;
            JToken levelMax = card.Wire?["3"], love = card.Wire?["5"];
            if (definition != null && levelMax?.Type == JTokenType.Integer && love?.Type == JTokenType.Integer &&
                (long)levelMax > 0 && card.Level >= (long)levelMax && (long)love >= (long)definition["loveMax"])
                picture = (long)definition["maximumPicture"];
            workCardPictures.TryGetValue(picture, out string portrait);
            return (name + "\nLv." + card.Level, portrait);
        }

        // DeckListCardGroup.m_partdatas supplies the 2.0.4 main deck coordinates.
        private static readonly Vector2[] DeckSlotPositions = {
            new Vector2(-276, 0),
            new Vector2(-210, -137), new Vector2(-80, -137), new Vector2(50, -137),
            new Vector2(-210, -258), new Vector2(-80, -258), new Vector2(50, -258),
            new Vector2(-210, -379), new Vector2(-80, -379), new Vector2(50, -379),
        };

        // Stay in business scenes until the user returns, instead of auto-reentering Home.
        private static bool IsWorkScene(string name)
        {
            return name == DeckEditor || name == DeckInventory || name == QuestDeck || WorkPickers.ContainsKey(name ?? "");
        }

        // Render a single live deck using the original DeckSl board and CardSlot geometry.
        public void ShowDeckEditor(string deckName, IReadOnlyList<(string Caption, string Portrait)> cards, Action<int> selectSlot,
            Action previous, Action next, Action save, Action back)
        {
            ShowDeckBoard(DeckEditor, deckName, cards, selectSlot, previous, next, save, back);
        }

        // The party deck browser displays the same board with read-only slots and a confirm action.
        public void ShowQuestDeck(string deckName, IReadOnlyList<(string Caption, string Portrait)> cards, Action previous, Action next,
            Action confirm, Action back)
        {
            ShowDeckBoard(QuestDeck, deckName, cards, null, previous, next, confirm, back);
        }

        // Project the original deck board and bind the controls provided by the current flow.
        private void ShowDeckBoard(string scene, string deckName, IReadOnlyList<(string Caption, string Portrait)> cards, Action<int> selectSlot,
            Action previous, Action next, Action confirm, Action back)
        {
            var source = WorkNodeIndex("DeckSl");
            var nodes = new Dictionary<string, JObject>();
            deckActions.Clear();
            workPage = null;
            CopyWorkNode(source, nodes, "1320592835458227"); // Deck_Base01
            CopyWorkNode(source, nodes, "1499517081926786"); // Label_DeckName
            CopyWorkNode(source, nodes, "1295926465608920"); // CardSlots
            ((JObject)nodes["1499517081926786"]["widgets"][0])["text"] = deckName;
            SetWorkAxis(nodes["1801075752484240"], 0, 0);
            AddDeckButton(source, nodes, "1239265225482239", "1463395028392447", "back", back);
            AddDeckButton(source, nodes, "1033903555039309", "1045646810609841", scene == QuestDeck ? "confirm" : "save", confirm);
            AddDeckButton(source, nodes, "1726790013014859", "1632746491512912", "previous", previous);
            AddDeckButton(source, nodes, "1693329660128220", "1632746491512912", "next", next);
            var output = new JArray(nodes.Values);
            var slotSource = source[SlotArtwork];
            for (int index = 0; index < cards.Count; index++)
            {
                Vector2 point = DeckSlotPositions[index];
                // The leader template is left-anchored. Its portrait occupies the authored
                // LeaderCardSlot/Anm_Color section to the right of the slot origin.
                if (index == 0) point += new Vector2(150, -20);
                string key = "slot-" + index;
                int slot = index;
                if (selectSlot != null) deckActions.Add(key, () => selectSlot(slot));
                AddCardTile(output, slotSource, key, "1295926465608920", point, index == 0 ? 280 : 130,
                    cards[index], selectSlot == null ? null : "deckAction", key);
            }
            ShowScene(scene, new JObject { { "name", scene }, { "nodes", output } });
        }

        // The original CardInve picker lays out five cards per row.
        public void OpenDeckInventory(int count, Func<int, (string Caption, string Portrait)> card, Action<int> select, Action back)
        {
            workPage = DeckInventory;
            workCount = count;
            workFirst = 0;
            workCapacity = 15;
            inventoryCard = card;
            workSelect = select;
            workBack = back;
            RenderDeckInventory();
        }

        private Func<int, (string Caption, string Portrait)> inventoryCard;

        // Build only visible inventory tiles. A swipe scrolls by one authored five-card row.
        private void RenderDeckInventory()
        {
            var expanded = WorkNodeIndex("CardInve");
            var deck = WorkNodeIndex("DeckSl");
            var nodes = new Dictionary<string, JObject>();
            deckActions.Clear();
            string parent = (string)expanded[InventoryTemplate]["parent"];
            CopyWorkNode(expanded, nodes, parent);
            SetWorkAxis(nodes["1061268324235159"], 0, 0);
            AddDeckButton(expanded, nodes, "1966426351977976", "1537582596312083", "back", workBack);
            var output = new JArray(nodes.Values);
            Vector3 start = Vector(expanded[InventoryTemplate]["position"]);
            for (int slot = 0; slot < workCapacity && slot + workFirst < workCount; slot++)
            {
                int index = slot + workFirst;
                var position = new Vector2(start.x + slot % 5 * 140, start.y - slot / 5 * 130);
                AddCardTile(output, deck[SlotArtwork], "owned-" + slot, parent, position, 130, inventoryCard(index), "workIndex", index);
            }
            ShowScene(DeckInventory, new JObject { { "name", DeckInventory }, { "nodes", output } });
        }

        private Dictionary<string, JObject> WorkNodeIndex(string scene)
        {
            var result = new Dictionary<string, JObject>();
            foreach (JObject node in (JArray)FindWorkDefinition(workDefinitions, scene)["nodes"]) result.Add((string)node["id"], node);
            return result;
        }

        private void AddDeckButton(Dictionary<string, JObject> source, Dictionary<string, JObject> nodes,
            string button, string panel, string action, Action handler)
        {
            if (handler == null) return;
            CopyWorkNode(source, nodes, button);
            SetWorkAxis(nodes[panel], 0, 0);
            nodes[button]["deckAction"] = action;
            deckActions.Add(action, handler);
        }

        private static void SetWorkAxis(JObject node, int axis, float value) { node["position"][axis] = value; }

        // Project the authored slot frame and, when verified artwork is available, its original chr20 portrait.
        private static void AddCardTile(JArray output, JObject source, string id, string parent,
            Vector2 position, int width, (string Caption, string Portrait) card, string actionKey, JToken actionValue)
        {
            var image = (JObject)source.DeepClone();
            image["id"] = id;
            image["parent"] = parent;
            image["position"] = new JArray(position.x, position.y, 0);
            image["scale"] = new JArray(width, 120, 1);
            image["active"] = true;
            image["enterAnimation"] = null;
            if (actionKey != null) image[actionKey] = actionValue;
            output.Add(image);
            int depth = (int)image["widgets"][0]["depth"];
            bool hasPortrait = card.Portrait != null;
            if (hasPortrait)
                output.Add(new JObject {
                    { "id", id + "-portrait" }, { "parent", parent }, { "name", "CardPortrait" }, { "active", true },
                    { "position", new JArray(position.x, position.y + 22, -1) }, { "scale", new JArray(68, 68, 1) },
                    { "rotation", new JArray(0, 0, 0, 1) }, { "scripts", new JArray() },
                    { "widgets", new JArray(new JObject {
                        { "id", id + "-portrait" }, { "kind", "UITexture" }, { "enabled", true },
                        { "depth", depth + 1 }, { "pivot", 4 }, { "color", new JArray(1, 1, 1, 1) },
                        { "resolved", new JObject { { "texture", card.Portrait }, { "uv", new JArray(0, 0, 1, 1) } } },
                    }) },
                });
            output.Add(new JObject {
                { "id", id + "-caption" }, { "parent", parent }, { "name", "CardInventoryLabel" }, { "active", true },
                { "position", new JArray(position.x, position.y - (hasPortrait ? 30 : 0), -2) },
                { "scale", hasPortrait ? new JArray(13, 13, 1) : new JArray(17, 17, 1) },
                { "rotation", new JArray(0, 0, 0, 1) }, { "scripts", new JArray() },
                { "widgets", new JArray(new JObject {
                    { "id", id + "-caption" }, { "kind", "UILabel" }, { "enabled", true },
                    { "depth", depth + 2 }, { "pivot", 4 },
                    { "color", new JArray(1, 1, 1, 1) }, { "maxLineWidth", width - 8 }, { "text", card.Caption },
                }) },
            });
        }
    }
}
