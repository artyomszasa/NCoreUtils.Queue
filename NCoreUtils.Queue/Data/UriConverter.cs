using System;
using Google.Cloud.Firestore;

namespace NCoreUtils.Queue.Data
{
    sealed class UriConverter : IFirestoreConverter<Uri?>
    {
        public Uri? FromFirestore(object value) => value switch
        {
            null => null,
            string svalue => new Uri(svalue, UriKind.Absolute),
            _ => throw new InvalidOperationException($"{value} (of type {value.GetType()}) cannot be used as {typeof(Uri)}")
        };

        public object ToFirestore(Uri? value)
            => value?.AbsoluteUri!;
    }
}