using System;

namespace FlexBuffers
{
    public enum Type: byte
    {
        Null, Int, Uint, Float,
        Key, String, IndirectInt, IndirectUInt, IndirectFloat,
        Map, Vector, VectorInt, VectorUInt, VectorFloat, VectorKey, VectorString,
        VectorInt2, VectorUInt2, VectorFloat2,
        VectorInt3, VectorUInt3, VectorFloat3,
        VectorInt4, VectorUInt4, VectorFloat4,
        Blob, Bool, VectorBool = 36
    }

    public static class TypesUtil
    {
        public static bool IsInline(Type type)
        {
            return type == Type.Bool || (byte) type <= (byte) Type.Float;
        }

        public static bool IsTypedVectorElement(Type type)
        {
            var typeValue = (byte) type;
            return type == Type.Bool || (typeValue >= (byte) Type.Int && typeValue <= (byte) Type.String);
        }
        
        public static bool IsTypedVector(Type type)
        {
            var typeValue = (byte) type;
            return type == Type.VectorBool || (typeValue >= (byte) Type.VectorInt && typeValue <= (byte) Type.VectorString);
        }
        
        public static bool IsFixedTypedVector(Type type)
        {
            var typeValue = (byte) type;
            return (typeValue >= (byte) Type.VectorInt2 && typeValue <= (byte) Type.VectorFloat4);
        }

        public static bool IsAVector(Type type)
        {
            return IsTypedVector(type) || IsFixedTypedVector(type) || type == Type.Vector;
        }

        public static Type ToTypedVector(Type type, byte length)
        {
            var typeValue = (byte) type;
            if (length == 0)
            {
                return (Type) (typeValue - (byte) Type.Int + (byte) Type.VectorInt);
            }
            if (length == 2)
            {
                return (Type) (typeValue - (byte) Type.Int + (byte) Type.VectorInt2);
            }
            if (length == 3)
            {
                return (Type) (typeValue - (byte) Type.Int + (byte) Type.VectorInt3);
            }
            if (length == 4)
            {
                return (Type) (typeValue - (byte) Type.Int + (byte) Type.VectorInt4);
            }
            throw new Exception($"Unexpected length: {length}");
        }

        public static Type TypedVectorElementType(Type type)
        {
            var typeValue = (byte) type;
            return (Type) (typeValue - (byte) Type.VectorInt + (byte) Type.Int);
        }

        public static Type FixedTypedVectorElementType(Type type)
        {
            var fixedType = (byte) type - (byte) Type.VectorInt2;
            return (Type)(fixedType % 3 + (int) Type.Int);
        }
        
        public static int FixedTypedVectorElementSize(Type type)
        {
            var fixedType = (byte) type - (byte) Type.VectorInt2;
            return fixedType / 3 + 2;
        }

        public static byte PackedType(Type type, BitWidth bitWidth)
        {
            return (byte) ((byte) bitWidth | ((byte)type << 2));
        }

        public static byte NullPackedType()
        {
            return PackedType(Type.Null, BitWidth.Width8);
        }
    }
}