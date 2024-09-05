namespace RadialPrinter.Models
{
    public class RadialInstruction(int mode, int r, int a)
    {
        public int Mode { get; set; } = mode;
        public int R { get; set; } = r;
        public int A { get; set; } = a;
    }
}
