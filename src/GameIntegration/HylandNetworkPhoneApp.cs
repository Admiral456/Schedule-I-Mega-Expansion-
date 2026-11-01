using S1API.PhoneApp;
using S1API.UI;
using S1API.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleIMegaExpansion.GameIntegration;

/// <summary>
/// First native S1API phone-app shell. Navigation is interactive; data-backed screens
/// are connected incrementally as NPC, order, shop and Realtor adapters are implemented.
/// </summary>
public sealed class HylandNetworkPhoneApp : PhoneApp
{
    protected override string AppName => "HylandNetwork";
    protected override string AppTitle => "Hyland Network";
    protected override string IconLabel => "Hyland";
    protected override string IconFileName => "SIME_HylandNetwork.png";

    private Text? _pageTitle;
    private Text? _pageBody;

    protected override void OnCreatedUI(GameObject container)
    {
        GameObject panel = UIFactory.Panel(
            "HylandNetworkPanel",
            container.transform,
            new Color(0.035f, 0.075f, 0.12f, 1f),
            fullAnchor: true);

        Text header = UIFactory.Text(
            "HylandNetworkHeader",
            "HYLAND NETWORK",
            panel.transform,
            24,
            TextAnchor.MiddleLeft,
            FontStyle.Bold);
        SetAnchors(header.rectTransform, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.98f));

        GameObject nav = new GameObject("HylandNetworkNavigation");
        nav.transform.SetParent(panel.transform, false);
        RectTransform navRect = nav.AddComponent<RectTransform>();
        SetAnchors(navRect, new Vector2(0.04f, 0.06f), new Vector2(0.34f, 0.81f));
        UIFactory.VerticalLayoutOnGO(nav, spacing: 7);

        GameObject content = new GameObject("HylandNetworkContent");
        content.transform.SetParent(panel.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        SetAnchors(contentRect, new Vector2(0.39f, 0.06f), new Vector2(0.96f, 0.81f));

        _pageTitle = UIFactory.Text(
            "HylandNetworkPageTitle",
            "Home",
            content.transform,
            20,
            TextAnchor.MiddleLeft,
            FontStyle.Bold);
        SetAnchors(_pageTitle.rectTransform, new Vector2(0f, 0.78f), new Vector2(1f, 1f));

        _pageBody = UIFactory.Text(
            "HylandNetworkPageBody",
            string.Empty,
            content.transform,
            15,
            TextAnchor.UpperLeft);
        SetAnchors(_pageBody.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.74f));

        AddNavigationButton(nav.transform, "Home", new Color(0.08f, 0.28f, 0.38f), "Home",
            "Your expansion dashboard. New messages, available orders and local activity will appear here.");
        AddNavigationButton(nav.transform, "Contacts", new Color(0.10f, 0.25f, 0.34f), "Contacts",
            "Resident profiles will include role, district, schedule, product preferences, budget and reputation.");
        AddNavigationButton(nav.transform, "Orders", new Color(0.10f, 0.25f, 0.34f), "Orders",
            "Customer-generated orders will show requested products, quantities, deadlines, rewards and fulfilment status.");
        AddNavigationButton(nav.transform, "Online shop", new Color(0.10f, 0.25f, 0.34f), "Online shop",
            "Portside Supply will share one inventory and price source with its physical checkout.");
        AddNavigationButton(nav.transform, "Businesses", new Color(0.10f, 0.25f, 0.34f), "Businesses",
            "New purchasable businesses will be listed through the existing Realtor interaction.");
        AddNavigationButton(nav.transform, "Map", new Color(0.10f, 0.25f, 0.34f), "Map",
            "Harborline, Ridgeview and Lakeside remain disabled until world survey, collision and navigation tests pass.");

        ShowPage("Home",
            "Welcome to Hyland Network.\n\nThis is the first native phone-app shell. Each section has working navigation; data-backed features are being connected behind these screens.");
    }

    private void AddNavigationButton(
        Transform parent,
        string label,
        Color background,
        string pageTitle,
        string pageBody)
    {
        var (_, button, _) = UIFactory.RoundedButtonWithLabel(
            "HylandNav_" + label.Replace(" ", string.Empty),
            label,
            parent,
            background,
            width: 150,
            height: 42,
            fontSize: 15,
            textColor: Color.white);

        ButtonUtils.AddListener(button, () => ShowPage(pageTitle, pageBody));
    }

    private void ShowPage(string title, string body)
    {
        if (_pageTitle != null)
            _pageTitle.text = title;

        if (_pageBody != null)
            _pageBody.text = body;
    }

    private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
