using System.Text;
using JetBrains.Annotations;
using FlexBuffers;

namespace FlexBuffers
{
    
    public static class FlxQueryParser
    {
        public static FlxQuery Convert(string query)
        {
            var cursor = 0;
            FlxQuery result = null;
            FlxQuery current = null;
            while (cursor < query.Length)
            {
                EatWhiteSpace(query, ref cursor);
                var propertyQuery = ParsePropertyQuery(query, ref cursor);
                if (propertyQuery != null)
                {
                    if (current == null)
                    {
                        result = propertyQuery;
                        current = propertyQuery;
                    }
                    else
                    {
                        current.Next = propertyQuery;
                        current = propertyQuery;
                    }
                    continue;
                }
                
                var indexQuery = ParseIndexQuery(query, ref cursor);
                if (indexQuery != null)
                {
                    if (current == null)
                    {
                        result = indexQuery;
                        current = indexQuery;
                    }
                    else
                    {
                        current.Next = indexQuery;
                        current = indexQuery;
                    }
                    continue;
                }

                var numberQuery = ParseNumberQuery(query, ref cursor);
                if (numberQuery != null)
                {
                    if (current == null)
                    {
                        result = numberQuery;
                        current = numberQuery;
                    }
                    else
                    {
                        current.Next = numberQuery;
                        current = numberQuery;
                    }
                    continue;
                }

                break;
            }

            return result;
        }

        private static FlxQuery ParsePropertyQuery(string query, ref int cursor)
        {
            var initCursor = cursor;
            var optional = true;
            if (TryEat("..", query, ref cursor))
            {
            } else if(TryEat(".", query, ref cursor))
            {
                optional = false;
            } else
            {
                return null;
            }
            var propertyName = EatId(query, ref cursor);
            if (propertyName.Length == 0)
            {
                cursor = initCursor;
                return null;
            }
            return new FlxQuery(optional, new IsProperty(propertyName), false);
        }
        
        private static FlxQuery ParseIndexQuery(string query, ref int cursor)
        {
            var initCursor = cursor;
            var optional = true;
            if (TryEat("..", query, ref cursor))
            {
            } else if(TryEat(".", query, ref cursor))
            {
                optional = false;
            } else
            {
                return null;
            }

            if (TryEat("[", query, ref cursor) == false)
            {
                cursor = initCursor;
                return null;
            }

            var start = EatInt(query, ref cursor);
            var end = start;

            if (TryEat(":", query, ref cursor))
            {
                end = EatInt(query, ref cursor);
            }
            
            if (TryEat("]", query, ref cursor) == false)
            {
                cursor = initCursor;
                return null;
            }
            
            return new FlxQuery(optional, new IsInIndexRangeConstraint(start, end), false);
        }
        
        private static FlxQuery ParseNumberQuery(string query, ref int cursor)
        {
            var initCursor = cursor;
            var optional = false;
            if (TryEat("..", query, ref cursor))
            {
            } else if(TryEat(".", query, ref cursor))
            {
                optional = false;
            } else
            {
                return null;
            }

            if (TryEat("{", query, ref cursor) == false)
            {
                cursor = initCursor;
                return null;
            }

            var start = EatDouble(query, ref cursor);
            var end = start;

            if (TryEat(":", query, ref cursor))
            {
                end = EatDouble(query, ref cursor);
            }
            
            if (TryEat("}", query, ref cursor) == false)
            {
                cursor = initCursor;
                return null;
            }
            
            return new FlxQuery(optional, new IsNumberConstraint(start, end), true);
        }

        private static void EatWhiteSpace(string query, ref int cursor)
        {
            for (; cursor < query.Length; cursor++)
            {
                if (char.IsWhiteSpace(query, cursor) == false)
                {
                    break;
                }
            }
        }
        
        private static string EatId(string query, ref int cursor)
        {
            var builder = new StringBuilder();
            for (; cursor < query.Length; cursor++)
            {
                if (char.IsLetterOrDigit(query[cursor]))
                {
                    builder.Append(query[cursor]);
                }
                else
                {
                    break;
                }
            }

            return builder.ToString();
        }
        
        private static int? EatInt(string query, ref int cursor)
        {
            EatWhiteSpace(query, ref cursor);
            var negative = TryEat("-", query, ref cursor);
            EatWhiteSpace(query, ref cursor);
            var builder = new StringBuilder();
            for (; cursor < query.Length; cursor++)
            {
                if (char.IsDigit(query[cursor]))
                {
                    builder.Append(query[cursor]);
                }
                else
                {
                    break;
                }
            }

            if (builder.Length == 0)
            {
                return null;
            }

            var number = int.Parse(builder.ToString());
            return negative ? - number : number ;
        }
        
        private static double? EatDouble(string query, ref int cursor)
        {
            EatWhiteSpace(query, ref cursor);
            var negative = TryEat("-", query, ref cursor);
            EatWhiteSpace(query, ref cursor);
            var builder = new StringBuilder();
            for (; cursor < query.Length; cursor++)
            {
                if (char.IsDigit(query[cursor]) || query[cursor] == '.')
                {
                    builder.Append(query[cursor]);
                }
                else
                {
                    break;
                }
            }

            if (builder.Length == 0)
            {
                return null;
            }

            var number = double.Parse(builder.ToString());
            return negative ? - number : number ;
        }

        private static bool TryEat(string value, string query, ref int cursor)
        {
            var initCursor = cursor;
            var length = value.Length;
            if (query.Length < cursor + length)
            {
                return false;
            }

            foreach (var c1 in value)
            {
                if (query[cursor] == c1)
                {
                    cursor++;
                }
                else
                {
                    cursor = initCursor;
                    return false;
                }
            }

            return true;
        }
        
        [CanBeNull]
        private static string EatUntil(string value, string query, ref int cursor)
        {
            var initCursor = cursor;
            var outerBuilder = new StringBuilder();
            for (; cursor < query.Length;)
            {
                if (query[cursor] == value[0])
                {
                    var builder = new StringBuilder();
                    for (var i = 0; i < value.Length; i++)
                    {
                        if (query[cursor] == value[i])
                        {
                            builder.Append(query[cursor]);
                            cursor++;
                        }
                        else
                        {
                            outerBuilder.Append(builder.ToString());
                            break;
                        }

                        if (cursor >= query.Length)
                        {
                            cursor = initCursor;
                            return null;
                        }
                    }

                    return outerBuilder.ToString();
                }
                else
                {
                    outerBuilder.Append(query[cursor]);
                }

                cursor++;
            }

            cursor = initCursor;
            return null;
        }
    }
}