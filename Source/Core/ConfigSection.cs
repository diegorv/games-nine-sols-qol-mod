using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace NineSolsQoL.Core;

// The groups of the in-game menu (F1). ConfigurationManager draws each one as a centred title and sorts
// them by name, so the dashes make them separator lines.
internal static class MenuCategory {
    public const string Abilities = "--------- Abilities ---------";
    public const string Combat = "--------- Combat ---------";
    public const string Death = "--------- Death ---------";
    public const string Hud = "--------- HUD ---------";
    public const string Loot = "--------- Loot and Jin ---------";
    public const string Travel = "--------- Travel ---------";
    public const string Debug = "--------- Debug ---------";
}

// Binds the entries of one config section (one feature, one [Section] in the .cfg file) and places them
// in the in-game menu (F1): under the feature's group, the feature's Enabled switch shown with the
// feature's name, its other entries indented below it.
internal sealed class ConfigSection {
    private const string Indent = "    ";

    // The menu sorts a group's entries by Order (higher first): one counter for the whole mod keeps every
    // feature's entries in the order they are bound, also when several features share a group
    private static int nextOrder;

    private readonly ConfigFile config;
    private readonly string section;
    private readonly string category;
    private readonly string? title;

    // title: the feature's name in the menu, shown on its Enabled entry; null for a section without one
    public ConfigSection(ConfigFile config, string section, string category, string? title) {
        this.config = config;
        this.section = section;
        this.category = category;
        this.title = title;
    }

    // range: the values the entry accepts, e.g. new AcceptableValueRange<int>(1, 30), which the menu
    // shows as a slider; its type must match T. advanced: hidden until the menu shows advanced
    // settings. name: the menu's label when the key's words are not enough.
    public ConfigEntry<T> Bind<T>(string key, T defaultValue, string description,
        AcceptableValueBase? range = null, bool advanced = false, string? name = null) {
        var label = key == "Enabled" && title != null ? title : name ?? Words(key);
        if (title != null && key != "Enabled") label = Indent + label;
        var attributes = new ConfigurationManagerAttributes {
            Order = nextOrder--,
            IsAdvanced = advanced,
            Category = category,
            DispName = label,
        };
        return config.Bind(section, key, defaultValue, new ConfigDescription(description, range, attributes));
    }

    // "BossHPFontSize" -> "Boss HP font size": a word starts at a capital that follows a small letter, or
    // that is followed by one; words in capitals (HP) stay as they are
    private static string Words(string key) {
        var words = new List<string>();
        var start = 0;
        for (var i = 1; i < key.Length; i++) {
            var next = i + 1 < key.Length ? key[i + 1] : ' ';
            if (char.IsUpper(key[i]) && (char.IsLower(key[i - 1]) || char.IsLower(next))) {
                words.Add(key.Substring(start, i - start));
                start = i;
            }
        }
        words.Add(key.Substring(start));
        return string.Join(" ", words.Select((word, i) => i == 0 || word.All(char.IsUpper) ? word : word.ToLowerInvariant()));
    }
}
