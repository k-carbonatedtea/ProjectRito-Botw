using System;
using System.Collections.Generic;
using System.Text;
using MapStudio.UI;
using ImGuiNET;
using System.Linq;

namespace UKingLibrary.UI
{
    public static class PropertyCopier
    {
        public static void CopyProperties(IDictionary<string, dynamic> properties)
        {
            var sb = new StringBuilder();
            
            foreach (var pair in properties)
            {
                if (pair.Key == "!Parameters" || pair.Key == "Scale" || 
                    pair.Key == "Translate" || pair.Key == "Rotate")
                    continue;

                if (pair.Value is IList<dynamic>)
                    continue;

                if (pair.Value is MapData.Property<dynamic>)
                {
                    var val = pair.Value.Value?.ToString() ?? "null";
                    var type = pair.Value.Value?.GetType()?.Name ?? "null";
                    sb.AppendLine($"{pair.Key}={type}:{val}");
                }
                else
                {
                    var val = pair.Value?.ToString() ?? "null";
                    var type = pair.Value?.GetType()?.Name ?? "null";
                    sb.AppendLine($"{pair.Key}={type}:{val}");
                }
            }

            var text = sb.ToString();
            try
            {
                ImGui.SetClipboardText(text);
                DialogHandler.Show("Success", () => { ImGui.Text("Properties copied to clipboard"); }, null);
            }
            catch (Exception ex)
            {
                DialogHandler.Show("Error", () => { 
                    ImGui.Text("Failed to copy to clipboard:"); 
                    ImGui.Text(ex.Message); 
                }, null);
            }
        }

        public static void ImportProperties(IDictionary<string, dynamic> properties, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                DialogHandler.Show("Error", () => { ImGui.Text("No text to import"); }, null);
                return;
            }

            try
            {
                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bool anyUpdated = false;
                StringBuilder debugInfo = new StringBuilder();

                // First, collect all properties to be imported
                var importProperties = new Dictionary<string, (string type, string value)>();
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var typeValue = parts[1].Split(':');
                        if (typeValue.Length == 2)
                        {
                            importProperties[key] = (typeValue[0].Trim(), typeValue[1].Trim());
                        }
                        else
                        {
                            // Fallback for old format without type information
                            importProperties[key] = ("String", parts[1].Trim());
                        }
                    }
                }

                // Clear all existing properties except special ones
                var keysToKeep = new HashSet<string> { "!Parameters", "Scale", "Translate", "Rotate" };
                
                // First collect all keys to remove
                var keysToRemove = properties.Keys.Where(key => !keysToKeep.Contains(key)).ToList();
                
                // Then remove them all at once
                debugInfo.AppendLine("Removing existing properties:");
                foreach (var key in keysToRemove)
                {
                    properties.Remove(key);
                    debugInfo.AppendLine($"Removed property: {key}");
                }

                // Now import all properties in the order they appear in the text
                debugInfo.AppendLine("\nImporting new properties:");
                foreach (var pair in importProperties)
                {
                    var key = pair.Key;
                    var (type, value) = pair.Value;

                    debugInfo.AppendLine($"\nProcessing: {key}={type}:{value}");

                    // Skip special properties
                    if (keysToKeep.Contains(key))
                    {
                        debugInfo.AppendLine($"Skipping special property: {key}");
                        continue;
                    }

                    // Create new property with appropriate type
                    MapData.Property<dynamic> newProp;
                    if (value == "null")
                    {
                        newProp = new MapData.Property<dynamic>(null);
                    }
                    else
                    {
                        // Create property based on the stored type information
                        object typedValue = null;
                        switch (type)
                        {
                            case "Int32":
                                typedValue = int.Parse(value);
                                break;
                            case "UInt32":
                                typedValue = uint.Parse(value);
                                break;
                            case "Single":
                                typedValue = float.Parse(value);
                                break;
                            case "Double":
                                typedValue = double.Parse(value);
                                break;
                            case "Boolean":
                                typedValue = bool.Parse(value);
                                break;
                            default:
                                typedValue = value;
                                break;
                        }
                        newProp = new MapData.Property<dynamic>(typedValue);
                    }

                    properties.Add(key, newProp);
                    anyUpdated = true;
                    debugInfo.AppendLine($"Added property {key} with value {value} ({newProp.Value?.GetType()})");
                }

                var debugText = debugInfo.ToString();
                DialogHandler.Show("Debug - Import Process", () => {
                    ImGui.Text("Import process details:");
                    if (ImGui.BeginChild("##debug_scroll", new System.Numerics.Vector2(-1, -1), true))
                    {
                        ImGui.InputTextMultiline("##debug_process", ref debugText, 2000, ImGui.GetContentRegionAvail(), ImGuiInputTextFlags.ReadOnly);
                        ImGui.EndChild();
                    }
                }, (ok) => {
                    if (anyUpdated)
                        DialogHandler.Show("Success", () => { ImGui.Text("Properties updated successfully"); }, null);
                    else
                        DialogHandler.Show("Warning", () => { ImGui.Text("No properties were updated"); }, null);
                });
            }
            catch (Exception ex)
            {
                DialogHandler.Show("Error", () => { 
                    ImGui.Text("Failed to import properties:"); 
                    ImGui.Text(ex.Message); 
                }, null);
            }
        }

        private static object ConvertValue(string value, Type targetType)
        {
            try
            {
                if (targetType == typeof(float))
                    return float.Parse(value);
                if (targetType == typeof(int))
                    return int.Parse(value);
                if (targetType == typeof(uint))
                    return uint.Parse(value);
                if (targetType == typeof(bool))
                    return bool.Parse(value);
                if (targetType == typeof(string))
                    return value;
                if (targetType == typeof(double))
                    return double.Parse(value);
                if (targetType == typeof(long))
                    return long.Parse(value);
                if (targetType == typeof(ulong))
                    return ulong.Parse(value);
            }
            catch (Exception ex)
            {
                DialogHandler.Show("Conversion Error", () => { 
                    ImGui.Text($"Failed to convert '{value}' to {targetType}"); 
                    ImGui.Text($"Error: {ex.Message}"); 
                }, null);
                return null;
            }
            return null;
        }
    }
} 