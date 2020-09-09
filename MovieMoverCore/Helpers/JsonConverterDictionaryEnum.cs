using MovieMoverCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieMoverCore.Helpers
{
    public class JsonConverterDictionaryEnum<T> : JsonConverter<Dictionary<string, T>> where T : Enum
    {
        public override Dictionary<string, T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dict = new Dictionary<string, T>();

            var enumType = typeof(T);
            
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.Read();
            }

            while (reader.TokenType != JsonTokenType.EndObject)
            {
                var key = reader.GetString();
                reader.Read();
                var elem = reader.GetString();
                reader.Read();

                T val = default;
                var valSet = false;
                foreach (T e in Enum.GetValues(enumType))
                {
                    var mInfos = enumType.GetMember(e.ToString());
                    var vmInfo = mInfos.FirstOrDefault(m => m.DeclaringType == enumType);
                    var attr = vmInfo?.GetCustomAttributes(typeof(EnumMemberAttribute), false).FirstOrDefault();
                    if (attr != null)
                    {
                        var jsonValue = ((EnumMemberAttribute)attr).Value;
                        if (elem == jsonValue)
                        {
                            valSet = true;
                            val = e;
                            break;
                        }
                    } else
                    {
                        if (e.ToString() == elem)
                        {
                            valSet = true;
                            val = e;
                            break;
                        }
                    }
                }
                if (!valSet)
                {
                    throw new JsonException("Could not parse enum value for dictionary");
                }
                dict.Add(key, val);
            }

            return dict;
        }

        //public override JD_Priority? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        //{
        //    var str = reader.GetString();
        //    var enumType = typeof(JD_Priority);
        //    foreach (JD_Priority p in Enum.GetValues(enumType))
        //    {
        //        var mInfos = enumType.GetMember(p.ToString());
        //        var vmInfo = mInfos.FirstOrDefault(m => m.DeclaringType == enumType);
        //        var attr = vmInfo?.GetCustomAttributes(typeof(EnumMemberAttribute), false).FirstOrDefault();
        //        if (attr != null)
        //        {
        //            var jsonValue = ((EnumMemberAttribute)attr).Value;
        //            if (str == jsonValue)
        //            {
        //                return p;
        //            }
        //        }
        //    }
        //    return null;
        //}

        //public override void Write(Utf8JsonWriter writer, JD_Priority? value, JsonSerializerOptions options)
        //{
        //    throw new NotImplementedException();
        //}

        public override void Write(Utf8JsonWriter writer, Dictionary<string, T> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
