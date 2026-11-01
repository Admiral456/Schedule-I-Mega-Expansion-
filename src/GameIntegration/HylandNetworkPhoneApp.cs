using ScheduleIMegaExpansion.Core;
using S1API.PhoneApp;
using S1API.UI;
using S1API.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleIMegaExpansion.GameIntegration;

public sealed class HylandNetworkPhoneApp : PhoneApp
{
    private enum Page { Home, Contacts, Orders, Shop, Businesses, Map }

    protected override string AppName => "HylandNetwork";
    protected override string AppTitle => "Hyland Network";
    protected override string IconLabel => "Hyland";
    protected override string IconFileName => "SIME_HylandNetwork.png";

    private static readonly Color Background = new(0.025f, 0.055f, 0.085f, 1f);
    private static readonly Color Panel = new(0.055f, 0.105f, 0.15f, 1f);
    private static readonly Color Muted = new(0.62f, 0.72f, 0.78f, 1f);

    private Transform? _content;
    private Text? _pageTitle;

    protected override void OnCreatedUI(GameObject container)
    {
        GameObject root = UIFactory.Panel("HylandNetworkRoot", container.transform, Background, fullAnchor: true);

        Text header = UIFactory.Text("HylandNetworkHeader", "HYLAND NETWORK", root.transform, 22, TextAnchor.MiddleLeft, FontStyle.Bold);
        SetRect(header.rectTransform, new Vector2(0.05f, 0.89f), new Vector2(0.95f, 0.99f));

        Text subtitle = UIFactory.Text("HylandNetworkSubtitle", "HYLAND POINT  /  EXPANSION SERVICES", root.transform, 10, TextAnchor.MiddleLeft);
        subtitle.color = Muted;
        SetRect(subtitle.rectTransform, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.90f));

        GameObject nav = UIFactory.Panel("HylandNetworkNavPanel", root.transform, Panel);
        SetRect(nav.GetComponent<RectTransform>(), new Vector2(0.035f, 0.04f), new Vector2(0.34f, 0.82f));

        GameObject navContent = new("HylandNetworkNavItems");
        navContent.transform.SetParent(nav.transform, false);
        RectTransform navRect = navContent.AddComponent<RectTransform>();
        SetRect(navRect, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.97f));
        UIFactory.VerticalLayoutOnGO(navContent, spacing: 8);

        GameObject main = UIFactory.Panel("HylandNetworkMainPanel", root.transform, Panel);
        SetRect(main.GetComponent<RectTransform>(), new Vector2(0.365f, 0.04f), new Vector2(0.965f, 0.82f));

        _pageTitle = UIFactory.Text("HylandNetworkPageTitle", "HOME", main.transform, 18, TextAnchor.MiddleLeft, FontStyle.Bold);
        SetRect(_pageTitle.rectTransform, new Vector2(0.06f, 0.87f), new Vector2(0.94f, 0.99f));

        RectTransform list = UIFactory.ScrollableVerticalList("HylandNetworkScroll", main.transform, out ScrollRect scroll);
        SetRect(scroll.GetComponent<RectTransform>(), new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.84f));
        _content = list;

        AddNavigation(navContent.transform, Page.Home, "HOME");
        AddNavigation(navContent.transform, Page.Contacts, "CONTACTS");
        AddNavigation(navContent.transform, Page.Orders, "ORDERS");
        AddNavigation(navContent.transform, Page.Shop, "ONLINE SHOP");
        AddNavigation(navContent.transform, Page.Businesses, "BUSINESSES");
        AddNavigation(navContent.transform, Page.Map, "PROVINCES");

        ShowPage(Page.Home);
    }

    private void AddNavigation(Transform parent, Page page, string label)
    {
        var (_, button, _) = UIFactory.RoundedButtonWithLabel(
            "HylandNav_" + page, label, parent,
            new Color(0.08f, 0.20f, 0.28f, 1f), 150, 42, 13, Color.white);
        ButtonUtils.AddListener(button, () => ShowPage(page));
    }

    private void ShowPage(Page page)
    {
        if (_content == null || _pageTitle == null)
            return;

        _pageTitle.text = page switch
        {
            Page.Home => "HOME",
            Page.Contacts => "CONTACTS",
            Page.Orders => "ORDERS",
            Page.Shop => "ONLINE SHOP",
            Page.Businesses => "BUSINESSES",
            Page.Map => "PROVINCES",
            _ => "HYLAND NETWORK"
        };

        foreach (Transform child in _content)
            UnityEngine.Object.Destroy(child.gameObject);

        switch (page)
        {
            case Page.Home: BuildHome(); break;
            case Page.Contacts: BuildContacts(); break;
            case Page.Orders: BuildOrders(); break;
            case Page.Shop: BuildShop(); break;
            case Page.Businesses: BuildBusinesses(); break;
            case Page.Map: BuildMap(); break;
        }
    }

    private void BuildHome()
    {
        AddCard("NETWORK STATUS", "Native phone app is active. Shared services are enabled only when their game adapters are connected.");
        ExpansionSaveable? save = ExpansionSaveable.Instance;
        if (save == null || !save.IsLoaded)
        {
            AddCard("SAVE SLOT", "Expansion save data has not loaded yet.");
            return;
        }
        if (save.IsReadOnly)
        {
            AddCard("SAVE SAFETY", "Expansion data is read-only because its schema or integrity could not be verified. Original data has been preserved.");
            return;
        }

        ExpansionSaveDocument? snapshot = save.GetDocumentSnapshot();
        if (snapshot == null)
        {
            AddCard("SAVE SLOT", "No expansion snapshot is currently available.");
            return;
        }

        AddCard("WORLD REVISION", snapshot.WorldRevision.ToString());
        AddCard("ACTIVE PROVINCES", snapshot.Data.EnabledProvinceIds.Count.ToString());
        AddCard("OPEN ORDERS", snapshot.Data.Orders.Values.Count(order =>
            !string.Equals(order.Status, "completed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(order.Status, "cancelled", StringComparison.OrdinalIgnoreCase)).ToString());
        AddCard("OWNED BUSINESSES", snapshot.Data.BusinessOwners.Count.ToString());
    }

    private void BuildContacts()
    {
        AddCard("RESIDENT NETWORK", "Nine named expansion residents have individual budgets, price sensitivity, product preferences and availability windows. Profiles are ready for runtime NPC binding; live spawned-NPC linking is still pending.");

        foreach (NpcCustomerDirectoryEntry resident in NpcCustomerDirectory.All)
        {
            string preferences = string.Join(", ", resident.Profile.PreferredProductIds);
            string neutral = string.Join(", ", resident.Profile.NeutralProductIds);
            string avoided = string.Join(", ", resident.Profile.RejectedProductIds);
            string body = resident.Role +
                "\nProvince: " + resident.ProvinceId +
                "\nAvailability: " + resident.AvailabilityWindow +
                "\nBudget: $" + resident.Profile.BudgetLimit +
                "\nPrice sensitivity: " + resident.Profile.PriceSensitivity.ToString("P0") +
                "\nLikes: " + preferences +
                "\nNeutral: " + neutral +
                "\nAvoids: " + avoided;

            AddCard(resident.DisplayName.ToUpperInvariant(), body);
            AddOrderRequestButton(resident);
        }
    }

    private void AddOrderRequestButton(NpcCustomerDirectoryEntry resident)
    {
        if (_content == null)
            return;

        string safeId = resident.Profile.NpcId.Replace(".", "_", StringComparison.Ordinal);
        var (_, button, _) = UIFactory.RoundedButtonWithLabel(
            "RequestNpcOrder_" + safeId,
            "REQUEST CUSTOMER ORDER",
            _content,
            new Color(0.08f, 0.20f, 0.28f, 1f),
            190, 34, 11, Color.white);

        ButtonUtils.AddListener(button, () =>
        {
            ScheduleINetworkBootstrap? bootstrap = ScheduleINetworkBootstrap.Instance;
            if (bootstrap is not null)
                _ = bootstrap.RequestNpcOrderAsync(
                    resident.Profile.NpcId,
                    "sime.business.harborline.portside_supply");
        });
    }

    private void BuildOrders()
    {
        ExpansionSaveDocument? snapshot = ExpansionSaveable.Instance?.GetDocumentSnapshot();
        var renderedOrderIds = new HashSet<string>(StringComparer.Ordinal);
        int rendered = 0;

        if (snapshot?.Data.Orders is not null)
        {
            foreach (var entry in snapshot.Data.Orders.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                ExpansionOrderRecord order = entry.Value;
                string items = string.Join("\n", order.Items.Select(item => item.ProductId + "  x" + item.Quantity));
                AddCard(entry.Key, order.Status.ToUpperInvariant() + "  /  " + order.ProvinceId +
                    "\nCustomer: " + order.CustomerId +
                    "\nEscrowed payment: $" + order.TotalPrice +
                    "\n" + items);
                renderedOrderIds.Add(entry.Key);
                rendered++;
            }
        }

        foreach (ExpansionNpcOrderResultPayload order in ExpansionOrderSyncCache.GetAll())
        {
            if (renderedOrderIds.Contains(order.OrderId))
                continue;

            string items = string.Join("\n", order.Items.Select(item => item.ProductId + "  x" + item.Quantity));
            AddCard(order.OrderId, order.Status.ToUpperInvariant() + "  /  " + order.ProvinceId +
                "\nCustomer: " + order.CustomerId +
                "\nEscrowed payment: $" + order.TotalPrice +
                "\n" + items);
            rendered++;
        }

        if (rendered == 0)
        {
            AddCard("NO ORDERS YET", snapshot is null
                ? "The expansion save has not loaded. Host-confirmed orders will appear here."
                : "No NPC customer orders have been committed yet. Request one from Contacts after shop stock is registered.");
        }
    }

    private void BuildShop()
    {
        AddCard("PORTSIDE SUPPLY", "The online catalogue will use the same inventory ID and price authority as the physical shop.");
        AddCard("CHECKOUT STATUS", "Purchases are disabled until host-authoritative checkout is connected to Schedule I cash, item delivery and persistent stock.");
        AddCard("DELIVERY", "Pickup and delivery are planned fulfilment methods. This prototype screen does not place orders.");
    }

    private void BuildBusinesses()
    {
        ExpansionSaveDocument? snapshot = ExpansionSaveable.Instance?.GetDocumentSnapshot();
        AddCard("REALTOR INTEGRATION", "Every purchasable expansion business must be registered in the vanilla Realtor flow. The runtime adapter is still pending.");
        if (snapshot == null || snapshot.Data.BusinessOwners.Count == 0)
        {
            AddCard("OWNERSHIP", "No expansion business ownership records are saved yet.");
            return;
        }
        foreach (var entry in snapshot.Data.BusinessOwners.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            AddCard(entry.Key, "Owner: " + entry.Value);
    }

    private void BuildMap()
    {
        AddCard("HARBORLINE", "Coastal port, warehouses, industry, Portside Supply and a new dealer contact. World connection is not surveyed.");
        AddCard("RIDGEVIEW", "Mountain quarry, processing facilities, research and freight contracts. World connection is not surveyed.");
        AddCard("LAKESIDE", "Marina, resort, villas, restaurants and recreation. World connection is not surveyed.");
        AddCard("MAP STATUS", "All three provinces remain disabled until terrain, collision, road graph, pedestrian navigation and streaming are proven in the live game.");
    }

    private void AddCard(string title, string body)
    {
        if (_content == null)
            return;

        GameObject card = UIFactory.Panel("HylandCard_" + Guid.NewGuid().ToString("N"), _content, new Color(0.075f, 0.14f, 0.19f, 1f));
        LayoutElement layout = card.AddComponent<LayoutElement>();
        layout.minHeight = 72;
        layout.preferredHeight = Mathf.Clamp(54 + body.Length * 0.34f, 76, 190);
        layout.flexibleWidth = 1;

        Text heading = UIFactory.Text("CardHeading", title, card.transform, 13, TextAnchor.MiddleLeft, FontStyle.Bold);
        heading.color = new Color(0.35f, 0.83f, 0.91f, 1f);
        SetRect(heading.rectTransform, new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.95f));

        Text content = UIFactory.Text("CardBody", body, card.transform, 11, TextAnchor.UpperLeft);
        content.color = new Color(0.88f, 0.92f, 0.94f, 1f);
        SetRect(content.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.62f));
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
