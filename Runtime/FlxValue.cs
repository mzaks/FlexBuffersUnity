using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FlexBuffers
{
    public struct FlxValue
    {
        private readonly byte[] _buffer;
        private readonly int _offset;
        private readonly byte _parentWidth;
        private readonly byte _byteWidth;
        private readonly Type _type;

        internal FlxValue(byte[] buffer, int offset, byte parentWidth, byte packedType)
        {
            _buffer = buffer;
            _offset = offset;
            _parentWidth = parentWidth;
            _byteWidth = (byte) (1 << (packedType & 3));
            _type = (Type) (packedType >> 2);
        }
        
        internal FlxValue(byte[] buffer, int offset, byte parentWidth, byte byteWidth, Type type)
        {
            _buffer = buffer;
            _offset = offset;
            _parentWidth = parentWidth;
            _byteWidth = byteWidth;
            _type = type;
        }

        public static FlxValue FromBytes(byte[] bytes)
        {
            if (bytes.Length < 3)
            {
                throw new Exception($"Invalid buffer {bytes}");
            }

            var byteWidth = bytes[bytes.Length - 1];
            var packedType = bytes[bytes.Length - 2];
            var offset = bytes.Length - byteWidth - 2;
            return new FlxValue(bytes, offset, byteWidth, packedType);
        }

        public Type ValueType => _type;
        public int BufferOffset => _offset;

        public bool IsNull => _type == Type.Null;

        public long AsLong
        {
            get
            {
                if (_type == Type.Int)
                {
                    return ReadLong(_buffer, _offset, _parentWidth);    
                }

                if (_type == Type.IndirectInt)
                {
                    var indirectOffset = ComputeIndirectOffset(_buffer, _offset, _parentWidth);
                    return ReadLong(_buffer, indirectOffset, _byteWidth);
                }

                if (_type == Type.Uint)
                {
                    var value = ReadULong(_buffer, _offset, _parentWidth);
                    if (value <= long.MaxValue)
                    {
                        return (long) value;
                    }
                }
                if (_type == Type.IndirectUInt)
                {
                    var indirectOffset = ComputeIndirectOffset(_buffer, _offset, _parentWidth);
                    var value = ReadULong(_buffer, indirectOffset, _byteWidth);
                    if (value <= long.MaxValue)
                    {
                        return (long) value;
                    }
                }
                throw new Exception($"Type {_type} is not convertible to long");
            }
        }
        
        public ulong AsULong
        {
            get
            {
                if (_type == Type.Uint)
                {
                    return ReadULong(_buffer, _offset, _parentWidth);    
                }
                
                if (_type == Type.IndirectUInt)
                {
                    var indirectOffset = ComputeIndirectOffset(_buffer, _offset, _parentWidth);
                    return ReadULong(_buffer, indirectOffset, _byteWidth);
                }

                if (_type == Type.Int)
                {
                    var value = ReadLong(_buffer, _offset, _parentWidth);
                    if (value >= 0)
                    {
                        return (ulong) value;
                    }
                }
                
                if (_type == Type.IndirectInt)
                {
                    var indirectOffset = ComputeIndirectOffset(_buffer, _offset, _parentWidth);
                    var value = ReadLong(_buffer, indirectOffset, _byteWidth);
                    if (value >= 0)
                    {
                        return (ulong) value;
                    }
                }
                throw new Exception($"Type {_type} is not convertible to ulong");
            }
        }
        
        public double AsDouble
        {
            get
            {
                if (_type == Type.Float)
                {
                    return ReadDouble(_buffer, _offset, _parentWidth);    
                }
                if (_type == Type.Int)
                {
                    return ReadLong(_buffer, _offset, _parentWidth);    
                }
                if (_type == Type.Uint)
                {
                    return ReadULong(_buffer, _offset, _parentWidth);    
                }
                if (_type == Type.IndirectFloat)
                {
                    var indirectOffset = ComputeIndirectOffset(_buffer, _offset, _parentWidth);
                    return ReadDouble(_buffer, indirectOffset, _byteWidth);
                }
                if (_type == Type.IndirectUInt)
                {
                    var indirectOffset = ComputeIndirectOffset(_buffer, _offset, _parentWidth);
                    return ReadULong(_buffer, indirectOffset, _byteWidth);
                }
                if (_type == Type.IndirectInt)
                {
                    var indirectOffset = ComputeIndirectOffset(_buffer, _offset, _parentWidth);
                    return ReadLong(_buffer, indirectOffset, _byteWidth);
                }
                throw new Exception($"Type {_type} is not convertible to double");
            }
        }
        
        public bool AsBool
        {
            get
            {
                if (_type == Type.Bool)
                {
                    return _buffer[_offset] != 0;
                }
                if (_type == Type.Int)
                {
                    return ReadLong(_buffer, _offset, _parentWidth) != 0;    
                }
                if (_type == Type.Uint)
                {
                    return ReadULong(_buffer, _offset, _parentWidth) != 0;    
                }
                throw new Exception($"Type {_type} is not convertible to bool");
            }
        }
        
        public string AsString
        {
            get
            {
                if (_type == Type.String)
                {
                    var indirectOffset = ComputeIndirectOffset(_buffer, _offset, _parentWidth);
                    var size = (int)ReadULong(_buffer, indirectOffset - _byteWidth, _byteWidth);
                    var sizeWidth = (int)_byteWidth;
                    while (_buffer[indirectOffset + size] != 0)
                    {
                        sizeWidth <<= 1;
                        size = (int)ReadULong(_buffer, indirectOffset - sizeWidth, (byte)sizeWidth);
                    }
                    
                    return Encoding.UTF8.GetString(_buffer, indirectOffset, size);
                }

                if (_type == Type.Key)
                {
                    var indirectOffset = ComputeIndirectOffset(_buffer, _offset, _parentWidth);
                    var size = 0;
                    while (indirectOffset + size < _buffer.Length && _buffer[indirectOffset + size] != 0)
                    {
                        size++;
                    }
                    return Encoding.UTF8.GetString(_buffer, indirectOffset, size);
                }
                
                throw new Exception($"Type {_type} is not convertible to string");
            }
        }
        
        public FlxValue this[int index] => AsVector[index];
        
        public FlxValue this[string key] => AsMap[key];

        public FlxVector AsVector
        {
            get
            {
                if (TypesUtil.IsAVector(_type) == false)
                {
                    throw new Exception($"Type {_type} is not a vector.");
                }

                var indirectOffset = ComputeIndirectOffset(_buffer, _offset, _parentWidth);
                var size = TypesUtil.IsFixedTypedVector(_type) 
                    ? TypesUtil.FixedTypedVectorElementSize(_type) 
                    : (int)ReadULong(_buffer, indirectOffset - _byteWidth, _byteWidth);
                return new FlxVector(_buffer, indirectOffset, _byteWidth, _type, size);
            }
        }

        public FlxMap AsMap
        {
            get
            {
                if (_type != Type.Map)
                {
                    throw new Exception($"Type {_type} is not a map.");
                }

                var indirectOffset = ComputeIndirectOffset(_buffer, _offset, _parentWidth);
                var size = ReadULong(_buffer, indirectOffset - _byteWidth, _byteWidth);
                return new FlxMap(_buffer, indirectOffset, _byteWidth, (int)size);
            }
        }

        public byte[] AsBlob
        {
            get
            {
                if (_type != Type.Blob)
                {
                    throw new Exception($"Type {_type} is not a blob.");
                }
                var indirectOffset = ComputeIndirectOffset(_buffer, _offset, _parentWidth);
                var size = ReadULong(_buffer, indirectOffset - _byteWidth, _byteWidth);
                var blob = new byte[size];
                System.Buffer.BlockCopy(_buffer, indirectOffset, blob, 0, (int)size);
                return blob;
            }
        }

        public string ToJson
        {
            get
            {
                if (IsNull)
                {
                    return "null";
                }

                if (_type == Type.Bool)
                {
                    return AsBool ? "true" : "false";
                }

                if (_type == Type.Int || _type == Type.IndirectInt)
                {
                    return AsLong.ToString();
                }

                if (_type == Type.Uint || _type == Type.IndirectUInt)
                {
                    return AsULong.ToString();
                }

                if (_type == Type.Float || _type == Type.IndirectFloat)
                {
                    return AsDouble.ToString(CultureInfo.CurrentCulture);
                }

                if (TypesUtil.IsAVector(_type))
                {
                    return AsVector.ToJson;
                }

                if (_type == Type.String || _type == Type.Key)
                {
                    var jsonConformString = AsString.Replace("\"", "\\\"")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t")
                        .Replace("/", "\\/");
                    return $"\"{jsonConformString}\"";
                }

                if (_type == Type.Map)
                {
                    return AsMap.ToJson;
                }

                if (_type == Type.Blob)
                {
                    return $"\"{Convert.ToBase64String(AsBlob)}\"";
                }
                
                throw new Exception($"Unexpected type {_type}");
            }
        }

        public string ToPrettyJson(string left = "", bool childrenOnly = false)
        {
            if (_type == Type.Map)
            {
                return AsMap.ToPrettyJson(left, childrenOnly);
            }
            if (TypesUtil.IsAVector(_type))
            {
                return AsVector.ToPrettyJson(left, childrenOnly);
            }

            if (childrenOnly)
            {
                return ToJson;
            }
            
            return $"{left}{ToJson}";
        }

        internal static long ReadLong(byte[] bytes, int offset, byte width)
        {
            if (offset < 0 || bytes.Length <= (offset + width) || (offset & (width - 1)) != 0)
            {
                throw new Exception("Bad offset");
            }

            if (width == 1)
            {
                return (sbyte)bytes[offset];
            }

            if (width == 2)
            {
                return BitConverter.ToInt16(bytes, offset);
            }

            if (width == 4)
            {
                return BitConverter.ToInt32(bytes, offset);
            }

            return BitConverter.ToInt64(bytes, offset);
        }
        
        internal static ulong ReadULong(byte[] bytes, int offset, byte width)
        {
            if (offset < 0 || bytes.Length <= (offset + width) || (offset & (width - 1)) != 0)
            {
                throw new Exception("Bad offset");
            }

            if (width == 1)
            {
                return bytes[offset];
            }

            if (width == 2)
            {
                return BitConverter.ToUInt16(bytes, offset);
            }

            if (width == 4)
            {
                return BitConverter.ToUInt32(bytes, offset);
            }

            return BitConverter.ToUInt64(bytes, offset);
        }
        
        internal static double ReadDouble(byte[] bytes, int offset, byte width)
        {
            if (offset < 0 || bytes.Length <= (offset + width) || (offset & (width - 1)) != 0)
            {
                throw new Exception("Bad offset");
            }

            if (width != 4 && width != 8)
            {
                throw new Exception($"Bad width {width}");
            }

            if (width == 4)
            {
                return BitConverter.ToSingle(bytes, offset);
            }

            return BitConverter.ToDouble(bytes, offset);
        }

        internal static int ComputeIndirectOffset(byte[] bytes, int offset, byte width)
        {
            var step = (int)ReadULong(bytes, offset, width);
            return offset - step;
        }
        
        internal byte[] Buffer => _buffer;
        internal int Offset => _offset;

        internal int IndirectOffset => ComputeIndirectOffset(_buffer, _offset, _parentWidth);

    }

    public struct FlxVector: IEnumerable<FlxValue>
    {
        private readonly byte[] _buffer;
        private readonly int _offset;
        private readonly int _length;
        private readonly byte _byteWidth;
        private readonly Type _type;

        internal FlxVector(byte[] buffer, int offset, byte byteWidth, Type type, int length)
        {
            _buffer = buffer;
            _offset = offset;
            _byteWidth = byteWidth;
            _type = type;
            _length = length;
        }

        public int Length => _length;

        public FlxValue this[int index]
        {
            get
            {
                if (index < 0 || index >= _length)
                {
                    throw new Exception($"Bad index {index}, should be 0...{_length}");
                }

                if (TypesUtil.IsTypedVector(_type))
                {
                    var elemOffset = _offset + (index * _byteWidth);
                    return new FlxValue(_buffer, elemOffset, _byteWidth, 1, TypesUtil.TypedVectorElementType(_type));
                }

                if (TypesUtil.IsFixedTypedVector(_type))
                {
                    var elemOffset = _offset + (index * _byteWidth);
                    return new FlxValue(_buffer, elemOffset, _byteWidth, 1, TypesUtil.FixedTypedVectorElementType(_type));
                }

                if (_type == Type.Vector)
                {
                    var packedType = _buffer[_offset + _length * _byteWidth + index];
                    var elemOffset = _offset + (index * _byteWidth);
                    return new FlxValue(_buffer, elemOffset, _byteWidth, packedType);
                }
                throw new Exception($"Bad index {index}, should be 0...{_length}");
            }
        }

        public string ToJson
        {
            get
            {
                var builder = new StringBuilder();
                builder.Append("[");
                for (var i = 0; i < _length; i++)
                {
                    builder.Append(this[i].ToJson);
                    if (i < _length - 1)
                    {
                        builder.Append(",");
                    }
                }

                builder.Append("]");

                return builder.ToString();
            }
        }

        public string ToPrettyJson(string left = "", bool childrenOnly = false)
        {
            var builder = new StringBuilder();
            if (childrenOnly == false)
            {
                builder.Append(left);    
            }
            
            builder.Append("[\n");
            for (var i = 0; i < _length; i++)
            {
                builder.Append(this[i].ToPrettyJson($"{left}  "));
                if (i < _length - 1)
                {
                    builder.Append(",");
                }

                builder.Append("\n");
            }
            builder.Append(left);
            builder.Append("]");

            return builder.ToString();
        }
        
        public IEnumerator<FlxValue> GetEnumerator()
        {
            for (var i = 0; i < _length; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct FlxMap: IEnumerable<KeyValuePair<string, FlxValue>>
    {
        private readonly byte[] _buffer;
        private readonly int _offset;
        private readonly int _length;
        private readonly byte _byteWidth;

        internal FlxMap(byte[] buffer, int offset, byte byteWidth, int length)
        {
            _buffer = buffer;
            _offset = offset;
            _byteWidth = byteWidth;
            _length = length;
        }

        public int Length => _length;

        private FlxVector Keys
        {
            get
            {
                var keysOffset = _offset - _byteWidth * 3;
                var indirectOffset = FlxValue.ComputeIndirectOffset(_buffer, keysOffset, _byteWidth);
                var bWidth = FlxValue.ReadULong(_buffer, keysOffset + _byteWidth, _byteWidth);
                return new FlxVector(_buffer, indirectOffset, (byte) bWidth, Type.VectorKey, _length);
            }
        }
        
        private FlxVector Values => new FlxVector(_buffer, _offset, _byteWidth, Type.Vector, _length);

        public FlxValue this[string key]
        {
            get
            {
                var index = KeyIndex(key);
                if (index < 0)
                {
                    throw new Exception($"No key '{key}' could be found");
                }
                return Values[index];
            }
        }

        public FlxValue ValueByIndex(int keyIndex)
        {
            if (keyIndex < 0 || keyIndex >= Length)
            {
                throw new Exception($"Bad Key index {keyIndex}");
            }

            return Values[keyIndex];
        }

        public string ToJson
        {
            get
            {
                var builder = new StringBuilder();
                builder.Append("{");
                var keys = Keys;
                var values = Values;
                for (var i = 0; i < _length; i++)
                {
                    builder.Append($"{keys[i].ToJson}:{values[i].ToJson}");
                    if (i < _length - 1)
                    {
                        builder.Append(",");
                    }
                }
                builder.Append("}");
                return builder.ToString();
            }
        }

        public string ToPrettyJson(string left = "", bool childrenOnly = false)
        {
            var builder = new StringBuilder();
            if (childrenOnly == false)
            {
                builder.Append(left);    
            }
            builder.Append("{\n");
            var keys = Keys;
            var values = Values;
            for (var i = 0; i < _length; i++)
            {
                builder.Append($"{left}  {keys[i].ToPrettyJson()} : {values[i].ToPrettyJson($"{left}  ", true)}");
                if (i < _length - 1)
                {
                    builder.Append(",");
                }

                builder.Append("\n");
            }
            builder.Append(left);
            builder.Append("}");
            return builder.ToString();
        }

        public int KeyIndex(string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var low = 0;
            var high = _length - 1;
            while (low <= high)
            {
                var mid = (high + low) >> 1;
                var dif = Comp(mid, keyBytes);
                if (dif == 0)
                {
                    return mid;
                }
                if (dif < 0)
                {
                    high = mid - 1;
                } else
                {
                    low = mid + 1;
                }
            }

            return -1;
        }
        
        private int Comp(int i, string key)
        {
            // TODO: keep it so we can profile it against byte comparison
            var key2 = Keys[i].AsString;
            return string.Compare(key, key2, StringComparison.Ordinal);
        }
        
        private int Comp(int i, byte[] key)
        {
            var key2 = Keys[i];
            var indirectOffset = key2.IndirectOffset;
            for (int j = 0; j < key.Length; j++)
            {
                var dif = key[j] - key2.Buffer[indirectOffset + j];
                if (dif != 0)
                {
                    return dif;
                }
            }
            // keys are zero terminated
            return key2.Buffer[indirectOffset + key.Length] == 0 ? 0 : -1;
        }

        public IEnumerator<KeyValuePair<string, FlxValue>> GetEnumerator()
        {
            var keys = Keys;
            var values = Values;
            for (var i = 0; i < _length; i++)
            {
                yield return new KeyValuePair<string, FlxValue>(keys[i].AsString, values[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}