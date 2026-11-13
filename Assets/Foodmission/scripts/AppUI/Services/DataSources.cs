using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public enum DataSource
    {
        OpenFoodFacts,
        Nevo,
        TheMealDb,
        FoodKeeper,
    }

    public readonly struct DataSourceLink
    {
        public readonly string Label;
        public readonly string Url;

        public DataSourceLink(string label, string url)
        {
            Label = label;
            Url = url;
        }
    }

    public sealed class DataSourceInfo
    {
        public DataSource Source { get; }
        public string Name { get; }
        public string NoticeKey { get; }
        public IReadOnlyList<DataSourceLink> Links { get; }

        /// <summary>Extra credit shown only in the About summary (e.g. the score logos), or null.</summary>
        public string AboutNoteKey { get; }

        public DataSourceInfo(DataSource source, string name, string noticeKey, params DataSourceLink[] links)
            : this(source, name, noticeKey, null, links)
        {
        }

        public DataSourceInfo(DataSource source, string name, string noticeKey, string aboutNoteKey, params DataSourceLink[] links)
        {
            Source = source;
            Name = name;
            NoticeKey = noticeKey;
            AboutNoteKey = aboutNoteKey;
            Links = links ?? Array.Empty<DataSourceLink>();
        }
    }

    /// <summary>
    /// External data the app shows and the attribution each source asks for. The notice next to the data
    /// (food detail, pantry expiry) and the About summary read the same keys, so a text lives in one place.
    /// </summary>
    public static class DataSources
    {
        public const string AboutTitleKey = "ABOUT";
        public const string DataSourcesHeadingKey = "DATA_SOURCES";
        public const string AppVersionKey = "APP_VERSION";
        public const string ExpiryEstimatedKey = "EXPIRY_DATE_ESTIMATED";
        public const string FoodKeeperExpirySource = "auto_foodkeeper";
        public const string ScoreLogosCreditKey = "SCORE_LOGOS_CREDIT";

        public static readonly IReadOnlyList<DataSourceInfo> All = new[]
        {
            new DataSourceInfo(DataSource.OpenFoodFacts, "Open Food Facts", "DATA_SOURCE_OFF", ScoreLogosCreditKey,
                new DataSourceLink("openfoodfacts.org", "https://world.openfoodfacts.org"),
                new DataSourceLink("Open Database License", "https://opendatacommons.org/licenses/odbl/1-0/")),
            new DataSourceInfo(DataSource.Nevo, "NEVO (RIVM)", "DATA_SOURCE_NEVO",
                new DataSourceLink("nevo-online.rivm.nl", "https://nevo-online.rivm.nl")),
            new DataSourceInfo(DataSource.TheMealDb, "TheMealDB", "DATA_SOURCE_THEMEALDB",
                new DataSourceLink("themealdb.com", "https://www.themealdb.com")),
            new DataSourceInfo(DataSource.FoodKeeper, "USDA FoodKeeper", "DATA_SOURCE_FOODKEEPER",
                new DataSourceLink("foodsafety.gov/keep-food-safe/foodkeeper-app", "https://www.foodsafety.gov/keep-food-safe/foodkeeper-app")),
        };

        public static DataSourceInfo Get(DataSource source)
        {
            foreach (var info in All)
            {
                if (info.Source == source)
                {
                    return info;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(source), source, null);
        }

        /// <summary>Source of the data shown in the food detail: Open Food Facts for products, NEVO for generic foods.</summary>
        public static DataSource ForFoodType(FoodInfoType foodType)
        {
            return foodType == FoodInfoType.Generic ? DataSource.Nevo : DataSource.OpenFoodFacts;
        }

        /// <summary>True when the backend estimated the expiry date from FoodKeeper shelf-life data.</summary>
        public static bool IsFoodKeeperExpiry(string expiryDateSource)
        {
            return string.Equals(expiryDateSource, FoodKeeperExpirySource, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Inline note next to the data: the source notice plus its links (rich text, see <see cref="BindLinks"/>).</summary>
        public static string NoticeWithLinks(DataSource source)
        {
            return NoticeWithLinks(source, Localize);
        }

        public static string NoticeWithLinks(DataSource source, Func<string, string> localize)
        {
            var info = Get(source);
            return localize(info.NoticeKey) + "\n" + LinksRichText(info);
        }

        /// <summary>Note under an estimated pantry expiry date: "Estimated date. Shelf-life guidance based on USDA FoodKeeper" + link.</summary>
        public static string EstimatedExpiryNotice()
        {
            return EstimatedExpiryNotice(Localize);
        }

        public static string EstimatedExpiryNotice(Func<string, string> localize)
        {
            return $"{localize(ExpiryEstimatedKey)}. {NoticeWithLinks(DataSource.FoodKeeper, localize)}";
        }

        /// <summary>The source links as underlined rich-text &lt;link&gt; tags; the link id is the URL.</summary>
        public static string LinksRichText(DataSourceInfo info)
        {
            var text = new StringBuilder();
            for (int i = 0; i < info.Links.Count; i++)
            {
                if (i > 0)
                {
                    text.Append("  ·  ");
                }
                var link = info.Links[i];
                text.Append("<link=\"").Append(link.Url).Append("\"><u>").Append(link.Label).Append("</u></link>");
            }
            return text.ToString();
        }

        /// <summary>Opens the URL of a tapped &lt;link&gt; in a note built with <see cref="NoticeWithLinks(DataSource)"/>. Call once per element.</summary>
        public static void BindLinks(TextElement note)
        {
            note?.RegisterCallback<UnityEngine.UIElements.Experimental.PointerUpLinkTagEvent>(evt =>
            {
                if (IsWebUrl(evt.linkID))
                {
                    Application.OpenURL(evt.linkID);
                }
            });
        }

        public static bool IsWebUrl(string url)
        {
            return !string.IsNullOrEmpty(url) && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Markdown body of the About dialog: every data source with its notice, links and About note, then the app version.</summary>
        public static string AboutMarkdown(string appVersion)
        {
            return AboutMarkdown(Localize, appVersion);
        }

        public static string AboutMarkdown(Func<string, string> localize, string appVersion)
        {
            var md = new StringBuilder();
            md.Append("## ").Append(localize(DataSourcesHeadingKey)).Append("\n\n");
            foreach (var info in All)
            {
                md.Append("**").Append(info.Name).Append("**\n\n");
                md.Append(localize(info.NoticeKey)).Append("\n\n");
                foreach (var link in info.Links)
                {
                    md.Append('[').Append(link.Label).Append("](").Append(link.Url).Append(")\n\n");
                }
                if (!string.IsNullOrEmpty(info.AboutNoteKey))
                {
                    md.Append(localize(info.AboutNoteKey)).Append("\n\n");
                }
            }
            if (!string.IsNullOrEmpty(appVersion))
            {
                md.Append("---\n\n");
                md.Append(localize(AppVersionKey)).Append(": ").Append(appVersion).Append('\n');
            }
            return md.ToString();
        }

        private static string Localize(string key)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
        }
    }
}
