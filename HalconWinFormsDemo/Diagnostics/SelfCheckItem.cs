namespace HalconWinFormsDemo.Diagnostics
{
    public sealed class SelfCheckItem
    {
        public string Name { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
    }
}
