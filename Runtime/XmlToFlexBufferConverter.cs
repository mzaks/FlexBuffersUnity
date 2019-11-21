using System.IO;
using System.Xml;

namespace FlexBuffers
{
    public static class XmlToFlexBufferConverter
    {
        public static byte[] Convert(string xmlData)
        {
            XmlDocument doc = new XmlDocument();
            
            doc.Load(new StringReader(xmlData));

            var flx = new FlexBuffer();
            
            Process(flx, doc.DocumentElement);
            
            return flx.Finish();
        }

        private static void Process(FlexBuffer flx, XmlNode element)
        {
            var node = flx.StartVector();
            flx.AddKey("tagName");
            flx.Add(element.Name);
            var attributes = element.Attributes;
            if (attributes != null)
            {
                for (var i = 0; i < attributes.Count; i++)
                {
                    var att = attributes.Item(i);
                    flx.AddKey(att.Name);
                    flx.Add(att.Value);
                }
            }

            var children = element.ChildNodes;
            if (children.Count > 0)
            {
                flx.AddKey("children");
                var childVector = flx.StartVector();
                for (var i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (child.NodeType == XmlNodeType.Text || child.NodeType == XmlNodeType.CDATA)
                    {
                        flx.Add(child.Value);
                    } else if (child.NodeType == XmlNodeType.Comment)
                    {
                        
                    } else
                    {
                        Process(flx, child);    
                    }
                }

                flx.EndVector(childVector, false, false);
            }
            flx.SortAndEndMap(node);
        }
    }
}