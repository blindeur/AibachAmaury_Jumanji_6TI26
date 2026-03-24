namespace test_martye
{
    public sealed class PlayerData
    {
        public int Id { get; set; }
        public int PlayerIndex { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Row { get; set; }
        public int Col { get; set; }
        public string Color { get; set; } = "#FFFFFF";
        public bool IsBlocked { get; set; }

        public string Initial =>
            string.IsNullOrWhiteSpace(Name) ? "?" : Name[..1].ToUpperInvariant();
    }
}
