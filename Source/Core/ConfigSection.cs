using BepInEx.Configuration;

namespace NineSolsQoL.Core;

// Binds the entries of one config section. The in-game menu (F1) lists them in the order they are
// bound, so a feature binds its Enabled entry first.
internal sealed class ConfigSection {
    private readonly ConfigFile config;
    private readonly string name;
    private int order;

    public ConfigSection(ConfigFile config, string name) {
        this.config = config;
        this.name = name;
    }

    // range: the values the entry accepts, e.g. new AcceptableValueRange<int>(1, 30), which the menu
    // shows as a slider; its type must match T. advanced: hidden until the menu shows advanced
    // settings.
    public ConfigEntry<T> Bind<T>(string key, T defaultValue, string description,
        AcceptableValueBase? range = null, bool advanced = false) {
        var attributes = new ConfigurationManagerAttributes { Order = order--, IsAdvanced = advanced };
        return config.Bind(name, key, defaultValue, new ConfigDescription(description, range, attributes));
    }
}
