namespace NineSolsQoL.Core;

// How the in-game config menu (BepInEx ConfigurationManager, F1) shows an entry, passed as a tag of
// its ConfigDescription. ConfigurationManager finds this class by its name and reads the fields by
// name, so neither may be renamed. A field left null keeps the menu's default. The full template:
// https://github.com/BepInEx/BepInEx.ConfigurationManager#overriding-default-configuration-manager-behavior
internal sealed class ConfigurationManagerAttributes {
    // Position within the section: higher comes first
    public int? Order;

    // Hidden until "Advanced settings" is ticked in the menu
    public bool? IsAdvanced;
}
