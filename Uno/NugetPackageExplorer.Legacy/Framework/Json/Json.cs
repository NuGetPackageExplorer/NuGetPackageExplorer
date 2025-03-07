using Newtonsoft.Json.Linq;

namespace NupkgExplorer.Framework.Json
{
    public partial class Json<T> : JObject
    {
        public T Content { get; } = default!;

        public Json() { }
        public Json(string json) : base(JObject.Parse(json))
        {
            Content = ToObject<T>() ?? throw new InvalidOperationException("Failed to parse JSON content to the specified type.");
        }
        public static new Json<T> Parse(string json) => new(json);

        private object? ToDump() => Content;

        public override string ToString()
        {
            // hide default ToString
            return typeof(T).GetMethod(nameof(ToString))!.DeclaringType != typeof(object)
                ? Content?.ToString() ?? string.Empty
                : string.Empty;
        }
    }
}
