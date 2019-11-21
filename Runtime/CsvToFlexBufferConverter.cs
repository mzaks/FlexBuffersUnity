using System.Text;

namespace FlexBuffers
{
    public static class CsvToFlexBufferConverter
    {
        public static byte[] Convert(string csv, char separator = ',')
        {
            var flx = new FlexBuffer();
            var sb = new StringBuilder();
            var outerVec = flx.StartVector();
            var innerVec = -1;
            var isInDoubleQuotes = false;
            for (var offset = 0; offset < csv.Length;)
            {
                if (innerVec == -1)
                {
                    innerVec = flx.StartVector();
                }

                if (csv[offset] == '"')
                {
                    if (isInDoubleQuotes == false)
                    {
                        isInDoubleQuotes = true;
                        offset++;    
                    }
                    else
                    {
                        if (csv[offset + 1] == '"')
                        {
                            sb.Append('"');
                            offset += 2;
                        }
                        else
                        {
                            isInDoubleQuotes = false;
                            offset++;
                        }
                    }
                    
                } else if (csv[offset] == separator && isInDoubleQuotes == false)
                {
                    flx.Add(sb.ToString());
                    sb.Clear();
                    offset++;
                } else if (csv[offset] == '\n' && isInDoubleQuotes == false)
                {
                    flx.Add(sb.ToString());
                    flx.EndVector(innerVec, false, false);
                    innerVec = -1;
                    sb.Clear();
                    offset++;
                } else if (csv[offset] == '\r' && csv[offset+1] == '\n' && isInDoubleQuotes == false)
                {
                    flx.Add(sb.ToString());
                    flx.EndVector(innerVec, false, false);
                    innerVec = -1;
                    sb.Clear();
                    offset += 2;
                }
                else
                {
                    sb.Append(csv[offset]);
                    offset++;
                }
            }

            if (innerVec != -1)
            {
                flx.Add(sb.ToString());
                flx.EndVector(innerVec, false, false);
            }
            flx.EndVector(outerVec, false, false);
            return flx.Finish();
        }
    }
}