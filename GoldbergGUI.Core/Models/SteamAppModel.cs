using SQLite;
using System.Collections.Generic;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming
namespace GoldbergGUI.Core.Models
{
    [Table("steamapp")]
    public class SteamApp
    {
        [JsonPropertyName("appid")]
        [Column("appid")]
        [PrimaryKey]
        public int AppId { get; set; }

        /// <summary>
        /// Name of Steam app
        /// </summary>
        [JsonPropertyName("name")]
        [Column("name")]
        public string Name { get; set; }

        [Column("comparable_name")]
        public string ComparableName { get; set; }

        /// <summary>
        /// App type (Game, DLC, ...)
        /// </summary>
        [Column("type")]
        public string AppType { get; set; }

        public override string ToString()
        {
            return $"{AppId}={Name}";
        }

        [JsonPropertyName("last_modified")]
        [Ignore]
        public long LastModified { get; set; }

        [JsonPropertyName("price_change_number")]
        [Ignore]
        public long PriceChangeNumber { get; set; }
    }

    public class AppList
    {
        [JsonPropertyName("apps")] public List<SteamApp> Apps { get; set; }

        [JsonPropertyName("have_more_results")]
        public bool HaveMoreResults { get; set; }

        [JsonPropertyName("last_appid")] public long LastAppid { get; set; }
    }

    public class SteamApps
    {
        public virtual AppList AppList { get; set; }
    }

    public class SteamAppsV2 : SteamApps
    {
        [JsonPropertyName("applist")] public override AppList AppList { get; set; }
    }

    public class SteamAppsV1 : SteamApps
    {
        [JsonPropertyName("response")] public override AppList AppList { get; set; }
    }
}