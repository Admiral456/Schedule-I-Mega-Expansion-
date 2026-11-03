using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using S1API.Items;
using NativeStorableItemDefinition = S1API.Items.Storable.StorableItemDefinition;
using S1API.Shops;

namespace ScheduleIMegaExpansion.Runtime
{
    /// <summary>
    /// Extends the native Hardware Store with existing Schedule I item definitions.
    /// The current S1API Shop.AddItem customPrice argument is not implemented by S1API:
    /// ShopIntegration warns and ignores it. Therefore this catalog deliberately updates
    /// the native StorableItemDefinition.BasePurchasePrice before attaching missing items.
    ///
    /// IMPORTANT: BasePurchasePrice belongs to the shared item definition, not a shop
    /// listing. A reprice therefore applies anywhere the same item ID is sold in-game.
    /// This is preferable to claiming a per-shop price override that the API does not honor.
    /// </summary>
    internal static class HarborlineShopCatalog
    {
        private sealed record StockItem(string Id, float Price);

        private static bool _loggedMissingNativeShop;
        private static readonly HashSet<string> LoggedMissingItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> LoggedRejectedItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly StockItem[] ExpansionStock =
        {
            new StockItem("plasticpot", 25f),
            new StockItem("moisturepreservingpot", 65f),
            new StockItem("airpot", 145f),
            new StockItem("suspensionrack", 50f),
            new StockItem("halogengrowlight", 50f),
            new StockItem("ledgrowlight", 95f),
            new StockItem("fullspectrumgrowlight", 235f),
            new StockItem("dryingrack", 300f),
            new StockItem("mixingstation", 575f),
            new StockItem("chemistrystation", 1150f),
            new StockItem("laboven", 1150f),
            new StockItem("mixingstationmk2", 2250f),
            new StockItem("brickpress", 900f),
            new StockItem("cauldron", 3500f)
        };

        public static string[] GetSupplierCatalogIds() =>
            ExpansionStock.Select(stock => stock.Id).ToArray();

        public static void ResetForNewSave()
        {
            _loggedMissingNativeShop = false;
            LoggedMissingItems.Clear();
            LoggedRejectedItems.Clear();
        }

        public static bool TryApply(Action<string> log, Action<string> warn)
        {
            try
            {
                Shop? hardwareStore = ShopManager.GetShopByName("Hardware Store");
                if (hardwareStore == null)
                {
                    string names = string.Join(", ", ShopManager.GetAllShops()
                        .Where(shop => shop != null)
                        .Select(shop => shop.Name));
                    if (!_loggedMissingNativeShop)
                    {
                        _loggedMissingNativeShop = true;
                        warn("Native shop 'Hardware Store' was not found. Loaded native shops: " + names);
                    }
                    return false;
                }

                int pricesChanged = 0;
                int alreadyCorrect = 0;
                int addedToStore = 0;
                int alreadySold = 0;
                int unavailable = 0;

                foreach (StockItem stock in ExpansionStock)
                {
                    ItemDefinition? item = ItemManager.GetDefinition(stock.Id);
                    if (item == null)
                    {
                        unavailable++;
                        if (LoggedMissingItems.Add(stock.Id))
                            MelonLogger.Warning("[Schedule I Mega Expansion] Native item ID is unavailable in this game build: " + stock.Id);
                        continue;
                    }

                    if (item is not NativeStorableItemDefinition storable)
                    {
                        unavailable++;
                        if (LoggedMissingItems.Add(stock.Id))
                            warn("Native item '" + stock.Id + "' is not a storable definition and cannot be priced as shop stock.");
                        continue;
                    }

                    float previousPrice = storable.BasePurchasePrice;
                    if (Math.Abs(previousPrice - stock.Price) > 0.01f)
                    {
                        // The game calculates native shop purchase cost from the item's shared
                        // StorableItemDefinition. S1API's customPrice argument is currently ignored.
                        storable.BasePurchasePrice = stock.Price;
                        pricesChanged++;
                        log("Native item repriced: " + item.Name + " (" + item.ID + ") $" +
                            previousPrice.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) +
                            " -> $" + stock.Price.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) +
                            " (shared item definition; applies to every shop selling this ID).");
                    }
                    else
                    {
                        alreadyCorrect++;
                    }

                    if (hardwareStore.HasItem(item.ID))
                    {
                        alreadySold++;
                        continue;
                    }

                    // Use the native item definition and native shop purchase/listing UI.
                    // Do not pass customPrice: current S1API 3.2.2 does not apply that argument.
                    if (hardwareStore.AddItem(item))
                    {
                        addedToStore++;
                        log("Hardware Store native listing added: " + item.Name + " (" + item.ID + ").");
                    }
                    else
                    {
                        if (LoggedRejectedItems.Add(item.ID))
                            warn("Native shop refused item '" + item.ID + "'. The price definition was updated, but no new listing was added.");
                    }
                }

                log("Native Hardware Store catalog checked: " + pricesChanged + " item prices changed, " +
                    alreadyCorrect + " already had the target price, " + addedToStore +
                    " listings added, " + alreadySold + " already listed, " + unavailable +
                    " not present/usable in this game build.");
                log("Pricing scope: native item-definition level. A price change affects every vanilla shop using that item ID; S1API 3.2.2 has no working per-listing custom price override.");
                return true;
            }
            catch (Exception ex)
            {
                warn("Native shop catalog integration failed: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }
    }

    internal static class HarborlineGameplayBootstrap
    {
        private static bool _npcApplied;
        private static bool _shopApplied;

        public static bool TryActivate(Action<string> log, Action<string> warn)
        {
            if (!_npcApplied)
                _npcApplied = HarborlineNpcRegistrar.EnsureInitialWave(log, warn);
            if (!_shopApplied)
                _shopApplied = HarborlineShopCatalog.TryApply(log, warn);

            if (_npcApplied && _shopApplied)
                log("Harborline native gameplay slice activated: 2 native dealers, 8 native customer NPCs, 1 native Portside Supply dead-drop/meeting supplier and a shared 14-item catalog definition. A separate physical Harborline storefront and Hyland Network phone-order flow are not yet implemented.");
            return _npcApplied && _shopApplied;
        }

        public static void ResetForNewSave()
        {
            _npcApplied = false;
            _shopApplied = false;
            HarborlineNpcRegistrar.ResetForNewSave();
            HarborlineShopCatalog.ResetForNewSave();
        }
    }
}
