namespace IceReversi.Core
{
    public interface ISidePreferenceStore
    {
        string Read();
        void Write(string value);
    }

    public sealed class HumanSidePreferences
    {
        private readonly ISidePreferenceStore store;

        public HumanSidePreferences(ISidePreferenceStore store)
        {
            this.store = store;
        }

        public PieceColor Load(PieceColor fallback = PieceColor.Black)
        {
            if (store == null)
            {
                return fallback.IsPlayer() ? fallback : PieceColor.Black;
            }

            var stored = store.Read();
            if (string.Equals(stored, "black", System.StringComparison.OrdinalIgnoreCase))
            {
                return PieceColor.Black;
            }

            if (string.Equals(stored, "white", System.StringComparison.OrdinalIgnoreCase))
            {
                return PieceColor.White;
            }

            return fallback.IsPlayer() ? fallback : PieceColor.Black;
        }

        public void Save(PieceColor color)
        {
            if (store == null || !color.IsPlayer())
            {
                return;
            }

            store.Write(color == PieceColor.Black ? "black" : "white");
        }
    }
}
