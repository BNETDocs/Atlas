namespace Atlasd.Battlenet
{
    class AccountKeyValue
    {
        public enum ReadLevel
        {
            Any,
            Owner,
            Internal,
        };
        public enum WriteLevel
        {
            Any,
            Owner,
            Internal,
            ReadOnly,
        };

        public string Key { get; private set; }
        public ReadLevel Readable { get; private set; }
        public dynamic Value;
        public WriteLevel Writable { get; private set; }

        public AccountKeyValue(string key, dynamic value, ReadLevel readable, WriteLevel writable)
        {
            Key = key;
            Readable = readable;
            Value = value;
            Writable = writable;
        }
    }
}
