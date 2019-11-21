namespace FlexBuffers
{
    public enum BitWidth: byte
    {
        Width8, Width16, Width32, Width64
    }
    
    public static class BitWidthUtil
    {
        public static BitWidth Width(sbyte value)
        {
            return BitWidth.Width8;
        }
        
        public static BitWidth Width(short value)
        {
            if (value >= 0)
            {
                return value <= sbyte.MaxValue ? BitWidth.Width8 : BitWidth.Width16;
            }
            return value >= sbyte.MinValue ? BitWidth.Width8 : BitWidth.Width16;
        }

        public static BitWidth Width(int value)
        {
            if (value >= 0)
            {
                if (value <= sbyte.MaxValue)
                {
                    return BitWidth.Width8;
                }
                return value <= short.MaxValue ? BitWidth.Width16 : BitWidth.Width32;
            }
            if (value >= sbyte.MinValue)
            {
                return BitWidth.Width8;
            }
            return value >= short.MinValue ? BitWidth.Width16 : BitWidth.Width32;
        }
        
        public static BitWidth Width(long value)
        {
            if (value >= 0)
            {
                return value <= int.MaxValue ? Width((int) value) : BitWidth.Width64;
            }
            else
            {
                return value >= int.MinValue ? Width((int) value) : BitWidth.Width64;
            }
        }
        
        public static BitWidth Width(byte value)
        {
            return BitWidth.Width8;
        }
        
        public static BitWidth Width(ushort value)
        {
            return value <= byte.MaxValue ? BitWidth.Width8 : BitWidth.Width16;
        }

        public static BitWidth Width(uint value)
        {
            if (value <= byte.MaxValue)
            {
                return BitWidth.Width8;
            }

            return value <= ushort.MaxValue ? BitWidth.Width16 : BitWidth.Width32;
        }
        
        public static BitWidth Width(ulong value)
        {
            return value <= uint.MaxValue ? Width((uint) value) : BitWidth.Width64;
        }

        public static BitWidth Width(float value)
        {
            return BitWidth.Width32;
        }
        
        public static BitWidth Width(double value)
        {
            return ((double)((float)value)) == value ? BitWidth.Width32 : BitWidth.Width64;
        }

        public static int PaddingSize(int bufSize, int scalarSize)
        {
            return (~bufSize + 1) & (scalarSize - 1);
        }
    }
}