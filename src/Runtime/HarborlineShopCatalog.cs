using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using S1API.Items;
using S1API.Shops;

namespace ScheduleIMegaExpansion.Runtime
{
    /// <summary>
    /// Adds real vanilla Schedule I item definitions to the existing native Hardware Store.
    /// Shop.AddItem uses the game's actual shop listings, buy button, money transfer and item delivery.
    /// It does not create a fake shop UI or placeholder item.
    /// </summary>
    internal static class HarborlineShopCatalog
    {
        private sealed record StockItem(string Id, float Price);

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
                    warn("Native shop 'Hardware Store' was not found. Loaded native shops: " + names);
                    return false;
                }

                int added = 0;
                int alreadyThere = 0;
                int unavailable = 0;

                foreach (StockItem stock in ExpansionStock)
                {
                    ItemDefinition? item = ItemManager.GetDefinition(stock.Id);
                    if (item == null)
                    {
                        unavailable++;
                        MelonLogger.Warning("[Schedule I Mega Expansion] Native item ID is unavailable in this game build: " + stock.Id);
                        continue;
                    }

                    if (hardwareStore.HasItem(item.ID))
                    {
                        alreadyThere++;
                        continue;
                    }

                    if (hardwareStore.AddItem(item, stock.Price))
                    {
                        added++;
                        log("Hardware Store stock added: " + item.Name + " (" + item.ID + ") $" +
                            stock.Price.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        warn("Native shop refused item '" + item.ID + "'. The entry was not replaced or duplicated.");
                    }
                }

                log("Native Hardware Store catalog checked: " + added + " added, " +
                    alreadyThere + " already sold, " + unavailable + " not present in this game build.");
                return true;
            }
            catch (Exception ex)
            {
                warn("Native shop catalog integration failed without changing other stores: " +
                    ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }
    }

    internal static class HarborlineGameplayBootstrap
    {
        private static bool _shopApplied;

        public static bool TryActivate(Action<string> log, Action<string> warn)
        {
            bool npcsReady = HarborlineNpcRegistrar.EnsureInitialWave(log, warn);
            if (!_shopApplied)
                _shopApplied = HarborlineShopCatalog.TryApply(log, warn);

            if (npcsReady && _shopApplied)
                log("Harborline native gameplay slice activated: 2 native dealers, 3 native customer NPCs and extended Hardware Store stock.");
            return npcsReady && _shopApplied;
        }

        public static void ResetForNewSave()
        {
            _shopApplied = false;
        }
    }
}
