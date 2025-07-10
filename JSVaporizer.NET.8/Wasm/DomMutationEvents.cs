using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JSVaporizer;

public static partial class JSVapor
{

    [JsonSerializable(typeof(List<DomMutationEvent>))]
    [JsonSerializable(typeof(DomMutationEvent))]
    public partial class DomMutationEventContext : JsonSerializerContext { }
    public sealed class DomMutationEvent
    {
        public string? Type { get; set; }
        public string? AttributeName { get; set; }
        public string? AttributeOldVal { get; set; }
        public string? AttributeNewVal { get; set; }
        public string? TargetId { get; set; }
        public string[]? Added { get; set; }
        public string[]? Removed { get; set; }
    }

}