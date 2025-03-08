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
                    sb.AppendLine($"{pair.Key}={val}");
                }
                else
                {
                    var val = pair.Value?.ToString() ?? "null";
                    sb.AppendLine($"{pair.Key}={val}");
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
                var importProperties = new Dictionary<string, string>();
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        importProperties[key] = value;
                    }
                }

                // Clear all existing properties except special ones
                var keysToKeep = new HashSet<string> { "!Parameters", "Scale", "Translate", "Rotate" };
                var keysToRemove = properties.Keys.Where(key => !keysToKeep.Contains(key)).ToList();
                
                debugInfo.AppendLine("Removing existing properties:");
                foreach (var key in keysToRemove)
                {
                    if (!importProperties.ContainsKey(key))
                    {
                        properties.Remove(key);
                        debugInfo.AppendLine($"Removed property: {key}");
                    }
                }

                // Now import the new properties
                debugInfo.AppendLine("\nImporting new properties:");
                foreach (var pair in importProperties)
                {
                    var key = pair.Key;
                    var value = pair.Value;

                    debugInfo.AppendLine($"\nProcessing: {key}={value}");

                    // Skip special properties
                    if (keysToKeep.Contains(key))
                    {
                        debugInfo.AppendLine($"Skipping special property: {key}");
                        continue;
                    }

                    // If property exists, update its value
                    if (properties.ContainsKey(key))
                    {
                        var prop = properties[key];
                        if (prop is MapData.Property<dynamic>)
                        {
                            var currentValue = prop.Value;
                            debugInfo.AppendLine($"Current value of {key}: {currentValue} ({currentValue?.GetType()})");
                            
                            object convertedValue = null;
                            if (value == "null")
                            {
                                convertedValue = null;
                            }
                            else if (currentValue != null)
                            {
                                convertedValue = ConvertValue(value, currentValue.GetType());
                            }
                            else if (value.ToLower() == "true" || value.ToLower() == "false")
                            {
                                convertedValue = bool.Parse(value);
                            }
                            else if (int.TryParse(value, out int intValue))
                            {
                                convertedValue = intValue;
                            }
                            else if (float.TryParse(value, out float floatValue))
                            {
                                convertedValue = floatValue;
                            }
                            else
                            {
                                convertedValue = value;
                            }

                            if (convertedValue != null || value == "null")
                            {
                                prop.Value = convertedValue;
                                anyUpdated = true;
                                debugInfo.AppendLine($"Updated {key} to {convertedValue}");
                            }
                            else
                            {
                                debugInfo.AppendLine($"Failed to convert value for {key}");
                            }
                        }
                        else
                        {
                            debugInfo.AppendLine($"Property {key} is not a MapData.Property<dynamic>");
                        }
                    }
                    else
                    {
                        // Create new property with appropriate type
                        MapData.Property<dynamic> newProp;
                        if (value == "null")
                        {
                            newProp = new MapData.Property<dynamic>(null);
                        }
                        else if (value.ToLower() == "true" || value.ToLower() == "false")
                        {
                            newProp = new MapData.Property<dynamic>(bool.Parse(value));
                        }
                        else if (int.TryParse(value, out int intValue))
                        {
                            newProp = new MapData.Property<dynamic>(intValue);
                        }
                        else if (float.TryParse(value, out float floatValue))
                        {
                            newProp = new MapData.Property<dynamic>(floatValue);
                        }
                        else
                        {
                            newProp = new MapData.Property<dynamic>(value);
                        }

                        properties.Add(key, newProp);
                        anyUpdated = true;
                        debugInfo.AppendLine($"Added new property {key} with value {value}");
                    }
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