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
    public class JsonConverterNullableJDPriority : JsonConverter<JD_Priority?>
    {
        public override JD_Priority? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            var enumType = typeof(JD_Priority);
            foreach (JD_Priority p in Enum.GetValues(enumType))
            {
                var mInfos = enumType.GetMember(p.ToString());
                var vmInfo = mInfos.FirstOrDefault(m => m.DeclaringType == enumType);
                var attr = vmInfo?.GetCustomAttributes(typeof(EnumMemberAttribute), false).FirstOrDefault();
                if (attr != null)
                {
                    var jsonValue = ((EnumMemberAttribute)attr).Value;
                    if (str == jsonValue)
                    {
                        return p;
                    }
                }
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, JD_Priority? value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
