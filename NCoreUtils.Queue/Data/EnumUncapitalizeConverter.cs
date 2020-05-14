using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Google.Cloud.Firestore;

namespace NCoreUtils.Queue.Data
{
    class EnumUncapitalizeConverter<T> : IFirestoreConverter<T>
        where T : Enum
    {
        readonly static Type _underlyingType = Enum.GetUnderlyingType(typeof(T));

        readonly static Dictionary<T, string> _names;

        readonly static Dictionary<string, T> _values;

        static EnumUncapitalizeConverter()
        {
            _names = new Dictionary<T, string>();
            _values = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            var pairs = Enum.GetValues(typeof(T))
                .Cast<T>()
                .Select(value => (Uncapitalize(Enum.GetName(typeof(T), value)), value));
            foreach (var (name, value) in pairs)
            {
                _names.Add(value, name);
                _values.Add(name, value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string Uncapitalize(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            if (char.IsLetterOrDigit(input[0]))
            {
                return input;
            }
            if (input.Length < 8192)
            {
                Span<char> buffer = stackalloc char[input.Length];
                input.AsSpan().CopyTo(buffer);
                buffer[0] = char.ToLowerInvariant(buffer[0]);
                return buffer.ToString();
            }
            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        public T FromFirestore(object value)
            => value switch
            {
                null => (T)Activator.CreateInstance(typeof(T))!,
                string svalue => _values.TryGetValue(svalue, out var v) ? v : throw new InvalidOperationException($"\"{svalue}\" is not a valid value for type {typeof(T)}."),
                short svalue => (T)Enum.ToObject(typeof(T), Convert.ChangeType(svalue, _underlyingType)),
                int ivalue => (T)Enum.ToObject(typeof(T), Convert.ChangeType(ivalue, _underlyingType)),
                long lvalue => (T)Enum.ToObject(typeof(T), Convert.ChangeType(lvalue, _underlyingType)),
                _ => throw new InvalidOperationException($"{value} (of type {value.GetType()}) cannot be used as {typeof(T)}")
            };

        public object ToFirestore(T value)
            => _names.TryGetValue(value, out var name) ? name : (value.ToString() ?? string.Empty);
    }
}