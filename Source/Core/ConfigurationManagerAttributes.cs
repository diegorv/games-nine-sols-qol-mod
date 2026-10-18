namespace NineSolsQoL.Core;

// How the in-game config menu (BepInEx ConfigurationManager, F1) shows an entry, passed as a tag of
// its ConfigDescription. ConfigurationManager finds this class by its name and reads the fields by
// name, so neither may be renamed. A field left null keeps the menu's default. The full template:
// https://github.com/BepInEx/BepInEx.ConfigurationManager#overriding-default-configuration-manager-behavior
internal sealed class ConfigurationManagerAttributes {
    // Position within the group: higher comes first
    public int? Order;

    // Hidden until "Advanced settings" is ticked in the menu
    public bool? IsAdvanced;

    // The group the entry is shown under (by default the entry's [Section])
    public string? Category;

    // The entry's label (by default its key)
    public string? DispName;
}
