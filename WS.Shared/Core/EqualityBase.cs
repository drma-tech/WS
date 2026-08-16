namespace WS.Shared.Core
{
#pragma warning disable S4035
    public abstract class EqualityBase<T> : IEquatable<T> where T : EqualityBase<T>
#pragma warning restore S4035
    {
        protected EqualityBase()
        {
#pragma warning disable S3060
            if (this is not T)
            {
                throw new InvalidOperationException($"{GetType().Name} cannot use {typeof(T).Name} as its equality type.");
            }
#pragma warning restore S3060
        }

        protected abstract object?[] EqualityValues { get; }

        public bool Equals(T? other)
        {
            if (other is null) return false;

            return EqualityValues.SequenceEqual(other.EqualityValues);
        }

        public override bool Equals(object? obj) => Equals(obj as T);

        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var value in EqualityValues)
                hash.Add(value);

            return hash.ToHashCode();
        }
    }
}
