﻿using CollapseLauncher.GameSettings.Zenless;
using Hi3Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

#nullable enable
namespace CollapseLauncher.GameSettings.Base
{
    public enum JsonEnumStoreType
    {
        AsNumber,
        AsString,
        AsNumberString
    }

    internal static class MagicNodeBaseValuesExt
    {
        private static JsonSerializerOptions JsonSerializerOpts = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        internal static JsonObject EnsureCreatedObject(this JsonNode? node, string keyName)
        {
            // If the node is empty, then create a new instance of it
            if (node == null)
                node = new JsonObject();

            // Return
            return node.EnsureCreatedInner<JsonObject>(keyName);
        }

        internal static JsonArray EnsureCreatedArray(this JsonNode? node, string keyName)
        {
            // If the node is empty, then create a new instance of it
            if (node == null)
                node = new JsonArray();

            // Return
            return node.EnsureCreatedInner<JsonArray>(keyName);
        }

        private static T EnsureCreatedInner<T>(this JsonNode? node, string keyName)
            where T : JsonNode
        {
            // SANITATION: Avoid creation of JsonNode directly
            if (typeof(T) == typeof(JsonNode))
                throw new InvalidOperationException("You cannot initialize the parent JsonNode type. Only JsonObject or JsonArray is accepted!");

            // Try get if the type is an array or object
            bool isTryCreateArray = typeof(T) == typeof(JsonArray);

            // Set parent node as object
            JsonObject? parentNodeObj = node?.AsObject();

            // If the value node does not exist, then create and add a new one
            if (!(parentNodeObj?.TryGetPropertyValue(keyName, out var valueNode) ?? false))
            {
                // Otherwise, create a new empty one.
                JsonNodeOptions options = new JsonNodeOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                JsonNode jsonValueNode = isTryCreateArray ?
                    new JsonArray(options) :
                    new JsonObject(options);
                valueNode = jsonValueNode;
                parentNodeObj?.Add(new KeyValuePair<string, JsonNode?>(keyName, jsonValueNode));
            }

            // If the value node keeps returning null, SCREW IT!!!
            if (valueNode == null)
                throw new TypeInitializationException(
                    nameof(T),
                    new NullReferenceException(
                        $"Failed to create the type of {nameof(T)} in the parent node as it is a null!"
                        ));

            // Return object node
            return (T)valueNode;
        }

        public static string? GetNodeValue(this JsonNode? node, string keyName, string? defaultValue)
        {
            // Get node as object
            JsonObject? jsonObject = node?.AsObject();

            // If node is null, return the default value
            if (jsonObject == null) return defaultValue;

            // Try get node as struct value
            if (jsonObject.TryGetPropertyValue(keyName, out JsonNode? jsonNodeValue) && jsonNodeValue != null)
            {
                string returnValue = jsonNodeValue.AsValue().GetValue<string>();
                return returnValue;
            }

            return defaultValue;
        }

        public static TValue GetNodeValue<TValue>(this JsonNode? node, string keyName, TValue defaultValue)
            where TValue : struct
        {
            // Get node as object
            JsonObject? jsonObject = node?.AsObject();

            // If node is null, return the default value
            if (jsonObject == null) return defaultValue;

            // Try get node as struct value
            if (jsonObject.TryGetPropertyValue(keyName, out JsonNode? jsonNodeValue) && jsonNodeValue != null)
            {
                if (typeof(TValue) == typeof(bool) && jsonNodeValue.GetValueKind() == JsonValueKind.Number)
                {
                    // Assuming 0 is false, and any non-zero number is true
                    int numValue = jsonNodeValue.AsValue().GetValue<int>();
                    bool boolValue = numValue != 0;
                    return (TValue)(object)boolValue; // Cast bool to TValue
                }
                else
                {
                    return jsonNodeValue.AsValue().GetValue<TValue>();
                }
            }

            return defaultValue;
        }

        public static TEnum GetNodeValueEnum<TEnum>(this JsonNode? node, string keyName, TEnum defaultValue)
            where TEnum : struct
        {
            // Get node as object
            JsonObject? jsonObject = node?.AsObject();

            // If node is null, return the default value
            if (jsonObject == null) return defaultValue;

            // Try get node as struct value
            if (jsonObject.TryGetPropertyValue(keyName, out JsonNode? jsonNodeValue) && jsonNodeValue != null)
            {
                // Get the JsonValue representative from the node and get the kind/type
                JsonValue enumValueRaw = jsonNodeValue.AsValue();
                JsonValueKind enumValueRawKind = enumValueRaw.GetValueKind();

                // Decide the return value
                switch (enumValueRawKind)
                {
                    case JsonValueKind.Number: // If it's a number
                        int enumAsInt = (int)enumValueRaw; // Cast JsonValue as int
                        return EnumFromInt(enumAsInt); // Cast and return it as an enum
                    case JsonValueKind.String: // If it's a string
                        string? enumAsString = (string?)enumValueRaw; // Cast JsonValue as string

                        if (Enum.TryParse(enumAsString, true, out TEnum enumParsedFromString)) // Try parse as a named member
                            return enumParsedFromString; // If successful, return the returned value

                        // If the string is actually a number as a string, then try parse it as int
                        if (int.TryParse(enumAsString, null, out int enumAsIntFromString))
                            return EnumFromInt(enumAsIntFromString); // Cast and return it as an enum

                        // Throw if all the attempts were failed
                        throw new InvalidDataException($"String value: {enumAsString} at key: {keyName} is not a valid member of enum: {nameof(TEnum)}");
                }
            }

            TEnum EnumFromInt(int value) => Unsafe.As<int, TEnum>(ref value); // Unsafe casting from int to TEnum

            // Otherwise, return the default value instead
            return defaultValue;
        }

        public static void SetNodeValue<TValue>(this JsonNode? node, string keyName, TValue value, JsonSerializerContext? context = null)
        {
            // If node is null, return and ignore
            if (node == null) return;

            // Get node as object
            JsonObject jsonObject = node.AsObject();

            // Create an instance of the JSON node value
            JsonValue? jsonValue = CreateJsonValue(value, context);

            // If the node has object, then assign the new value
            if (jsonObject.ContainsKey(keyName))
                node[keyName] = jsonValue;
            // Otherwise, add it
            else
                jsonObject.Add(new KeyValuePair<string, JsonNode?>(keyName, jsonValue));
        }

        public static void SetNodeValueEnum<TEnum>(this JsonNode? node, string keyName, TEnum value, JsonEnumStoreType enumStoreType = JsonEnumStoreType.AsNumber)
            where TEnum : struct, Enum
        {
            // If node is null, return and ignore
            if (node == null) return;

            // Get node as object
            JsonObject jsonObject = node.AsObject();

            // Create an instance of the JSON node value
            JsonValue? jsonValue = enumStoreType switch
            {
                JsonEnumStoreType.AsNumber => AsEnumNumber(value),
                JsonEnumStoreType.AsString => AsEnumString(value),
                JsonEnumStoreType.AsNumberString => AsEnumNumberString(value),
                _ => throw new NotSupportedException($"Enum store type: {enumStoreType} is not supported!")
            };

            // If the node has object, then assign the new value
            if (jsonObject.ContainsKey(keyName))
                node[keyName] = jsonValue;
            // Otherwise, add it
            else
                jsonObject.Add(new KeyValuePair<string, JsonNode?>(keyName, jsonValue));

            JsonValue AsEnumNumber(TEnum v)
            {
                int enumAsNumber = Unsafe.As<TEnum, int>(ref v);
                return JsonValue.Create(enumAsNumber);
            }

            JsonValue? AsEnumString(TEnum v)
            {
                string? enumName = Enum.GetName(v);
                return JsonValue.Create(enumName);
            }

            JsonValue AsEnumNumberString(TEnum v)
            {
                int enumAsNumber = Unsafe.As<TEnum, int>(ref v);
                string enumAsNumberString = $"{enumAsNumber}";
                return JsonValue.Create(enumAsNumberString);
            }
        }

        private static JsonValue? CreateJson<TDynamic>(TDynamic dynamicValue, JsonSerializerContext context)
        {
            JsonTypeInfo jsonTypeInfo = context.GetTypeInfo(typeof(TDynamic))
                ?? throw new NotSupportedException($"Context does not include a JsonTypeInfo<T> of type {nameof(TDynamic)}");
            JsonTypeInfo<TDynamic> jsonTypeInfoT = (JsonTypeInfo<TDynamic>)jsonTypeInfo;
            return JsonValue.Create(dynamicValue, jsonTypeInfoT);
        }

        private static JsonValue? CreateJsonValue<TValue>(TValue value, JsonSerializerContext? context)
            => value switch
        {
            bool v_bool => JsonValue.Create(v_bool),
            byte v_byte => JsonValue.Create(v_byte),
            sbyte v_sbyte => JsonValue.Create(v_sbyte),
            short v_short => JsonValue.Create(v_short),
            char v_char => JsonValue.Create(v_char),
            int v_int => JsonValue.Create(v_int),
            uint v_uint => JsonValue.Create(v_uint),
            long v_long => JsonValue.Create(v_long),
            ulong v_ulong => JsonValue.Create(v_ulong),
            float v_float => JsonValue.Create(v_float),
            double v_double => JsonValue.Create(v_double),
            decimal v_decimal => JsonValue.Create(v_decimal),
            string v_string => JsonValue.Create(v_string),
            DateTime v_datetime => JsonValue.Create(v_datetime),
            DateTimeOffset v_datetimeoffset => JsonValue.Create(v_datetimeoffset),
            Guid v_guid => JsonValue.Create(v_guid),
            JsonElement v_jsonelement => JsonValue.Create(v_jsonelement),
            _ => CreateJson(value, context ?? throw new NotSupportedException("You cannot pass a null context while setting a non-struct value to JsonValue"))
        };
    }

    internal class MagicNodeBaseValues<T>
            where T : MagicNodeBaseValues<T>, new()
    {
        [JsonIgnore]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public byte[] Magic { get; protected set; }

        [JsonIgnore]
        protected SettingsGameVersionManager GameVersionManager { get; set; }

        [JsonIgnore]
        public JsonNode? SettingsJsonNode { get; protected set; }

        [JsonIgnore]
        public JsonSerializerContext Context { get; protected set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.


        [Obsolete("Loading settings with Load() is not supported for IGameSettingsValueMagic<T> member. Use LoadWithMagic() instead!", true)]
        public static T Load() => throw new NotSupportedException("Loading settings with Load() is not supported for IGameSettingsValueMagic<T> member. Use LoadWithMagic() instead!");

        public static T LoadWithMagic(byte[] magic, SettingsGameVersionManager versionManager, JsonSerializerContext context)
        {
            if (magic == null || magic.Length == 0)
                throw new NullReferenceException($"Magic cannot be an empty array!");

            try
            {
                string filePath = versionManager.ConfigFilePath;

                if (!File.Exists(filePath)) throw new FileNotFoundException("MagicNodeBaseValues config file not found!");
                string raw = Sleepy.ReadString(filePath, magic);

#if DEBUG
                Logger.LogWriteLine($"RAW MagicNodeBaseValues Settings: {filePath}\r\n" +
                             $"{raw}", LogType.Debug, true);
#endif
                JsonNode? node = raw.DeserializeAsJsonNode();
                T data = new T();
                data.InjectNodeAndMagic(node, magic, versionManager, context);
                return data;
            }
            catch (Exception ex)
            {
                Logger.LogWriteLine($"Failed to parse MagicNodeBaseValues settings\r\n{ex}", LogType.Error, true);
                return DefaultValue(magic, versionManager, context);
            }
        }

        private static T DefaultValue(byte[] magic, SettingsGameVersionManager versionManager, JsonSerializerContext context)
        {
            // Generate dummy data
            T data = new T();

            // Generate raw JSON string
            string rawJson = data.Serialize(context, false, false);

            // Deserialize it back to JSON Node and inject
            // the node and magic
            JsonNode? defaultJsonNode = rawJson.DeserializeAsJsonNode();
            data.InjectNodeAndMagic(defaultJsonNode, magic, versionManager, context);

            // Return
            return data;
        }

        public void Save()
        {
            // Get the file and dir path
            string filePath = GameVersionManager.ConfigFilePath;
            string? fileDirPath = Path.GetDirectoryName(filePath);

            // Create the dir if not exist
            if (string.IsNullOrEmpty(fileDirPath) && !Directory.Exists(fileDirPath))
                Directory.CreateDirectory(fileDirPath!);

            // Write into the file
            string jsonString = SettingsJsonNode.SerializeJsonNode(Context, false, false);
            Sleepy.WriteString(filePath, jsonString, Magic);
        }

        public bool Equals(GeneralData? other)
        {
            return true;
        }

        protected virtual void InjectNodeAndMagic(JsonNode? jsonNode, byte[] magic, SettingsGameVersionManager versionManager, JsonSerializerContext context)
        {
            SettingsJsonNode = jsonNode;
            GameVersionManager = versionManager;
            Magic = magic;
            Context = context;
        }
    }
}
