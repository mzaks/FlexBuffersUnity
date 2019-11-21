using FlexBuffers;

namespace FlexBuffers
{
    public class FlxQuery
    {
        public readonly bool Optional;
        public readonly bool Propagating;
        public readonly IConstraint Constraint;
        public FlxQuery Next = null;

        public FlxQuery(bool optional, IConstraint constraint, bool propagating)
        {
            Optional = optional;
            Constraint = constraint;
            Propagating = propagating;
        }
    }

    public interface IConstraint
    {
        bool Confirms(FlxMap value, string key);
        bool Confirms(FlxVector value, int index);
    }

    public class IsProperty : IConstraint
    {
        private readonly string _propertyName;

        public IsProperty(string propertyName)
        {
            _propertyName = propertyName;
        }

        public bool Confirms(FlxMap map, string key)
        {
            return key == _propertyName;
        }

        public bool Confirms(FlxVector value, int index)
        {
            return false;
        }
    }
    
    public class IsInIndexRangeConstraint : IConstraint
    {
        private readonly int? _start;
        private readonly int? _end;

        public IsInIndexRangeConstraint(int? start, int? end)
        {
            _start = start;
            _end = end;
        }

        public bool Confirms(FlxMap map, string key)
        {
            return false;
        }

        public bool Confirms(FlxVector value, int index)
        {
            var start = _start >= 0 || _start == null ? _start : value.Length + _start;
            var end = _end >= 0 || _end == null ? _end : value.Length + _end; 
            if (index < value.Length)
            {
                if (start != null && start.Value > index)
                {
                    return false;
                }

                return end == null || end.Value >= index;
            }

            return false;
        }
    }

    public class IsNumberConstraint : IConstraint
    {
        private readonly double? _min;
        private readonly double? _max;

        public IsNumberConstraint(double? min, double? max)
        {
            _min = min;
            _max = max;
        }

        public bool Confirms(FlxMap value, string key)
        {
            var flxValue = value[key];
            if (IsNumber(flxValue) == false)
            {
                return true;
            }

            var number = flxValue.AsDouble;
            return IsInRange(number);
        }

        public bool Confirms(FlxVector value, int index)
        {
            var flxValue = value[index];
            if (IsNumber(flxValue) == false)
            {
                return true;
            }

            var number = flxValue.AsDouble;
            return IsInRange(number);
        }

        bool IsNumber(FlxValue value)
        {
            return value.ValueType == Type.Int
                   || value.ValueType == Type.Uint
                   || value.ValueType == Type.Float
                   || value.ValueType == Type.IndirectInt
                   || value.ValueType == Type.IndirectUInt
                   || value.ValueType == Type.IndirectFloat
                ;
        }

        bool IsInRange(double value)
        {
            if (_min != null && _min.Value > value)
            {
                return false;
            }

            return _max == null || _max.Value >= value;
        }
    }
}