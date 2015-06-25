// LICENSE:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// AUTHORS:
//
//  Moritz Eberl <moritz@semiodesk.com>
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Globalization;
#if NET_3_5
using Semiodesk.Trinity.Utility;
#endif
using VDS.RDF;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Provides functionality for the serialization and deserialization of .NET 
    /// objects to XML Schema encoded strings.
    /// </summary>
    public class XsdTypeMapper
    {
        #region Fields

        /// <summary>
        /// XSD URI vocabulary.
        /// </summary>
        private struct xsd
        {
            //static string Prefix = "xsd";
            public static string _namespace = "http://www.w3.org/2001/XMLSchema#";
            public static Uri ns = new Uri("http://www.w3.org/2001/XMLSchema");
            public static Uri datetime = new Uri(ns, "#dateTime");
            public static Uri date = new Uri(ns, "#date");
            public static Uri base64Binary = new Uri(ns, "#base64Binary");
            public static Uri boolean_ = new Uri(ns, "#boolean_");
            public static Uri boolean = new Uri(ns, "#boolean");
            public static Uri _double = new Uri(ns, "#double");
            public static Uri _float = new Uri(ns, "#float");
            public static Uri _short = new Uri(ns, "#short");
            public static Uri _int = new Uri(ns, "#int");
            public static Uri integer = new Uri(ns, "#integer");
            public static Uri _long = new Uri(ns, "#long");
            public static Uri _ushort = new Uri(ns, "#unsignedShort");
            public static Uri _uint = new Uri(ns, "#unsignedInt");
            public static Uri _ulong = new Uri(ns, "#unsignedLong");
            public static Uri _decimal = new Uri(ns, "#decimal");
            public static Uri nonNegativeInteger = new Uri(ns, "#nonNegativeInteger");
        }

        /// <summary>
        /// Maps .NET types to XSD type URIs.
        /// </summary>
        protected static Dictionary<Type, Uri> NativeToXsd = new Dictionary<Type, Uri>()
        {
            {typeof(Int16), xsd._short},
            {typeof(Int32), xsd._int},
            {typeof(Int64), xsd._long},
            {typeof(UInt16), xsd._ushort},
            {typeof(UInt32), xsd._uint},
            {typeof(UInt64), xsd._ulong},
            {typeof(DateTime), xsd.datetime},
            {typeof(byte[]), xsd.base64Binary},
            {typeof(bool), xsd.boolean_},
            {typeof(decimal), xsd._decimal},
            {typeof(double), xsd._double},
            {typeof(float), xsd._float}
        };

        /// <summary>
        /// Maps XSD type URIs to .NET types.
        /// </summary>
        protected static Dictionary<string, Type> XsdToNative = new Dictionary<string, Type>()
        {
            
            {xsd.nonNegativeInteger.AbsoluteUri, typeof(UInt64)},
            {xsd._short.AbsoluteUri, typeof(Int16)},
            {xsd._int.AbsoluteUri, typeof(Int32)},
            {xsd._long.AbsoluteUri, typeof(Int64)},
            {xsd._ushort.AbsoluteUri, typeof(UInt16)},
            {xsd._uint.AbsoluteUri, typeof(UInt32)},
            {xsd._ulong.AbsoluteUri, typeof(UInt64)},
            {xsd.datetime.AbsoluteUri,typeof(DateTime) },
            {xsd.boolean.AbsoluteUri, typeof(bool)},
            {xsd.boolean_.AbsoluteUri, typeof(bool)},
            {xsd._decimal.AbsoluteUri, typeof(decimal)},
            {xsd._double.AbsoluteUri, typeof(double)},
            {xsd._float.AbsoluteUri, typeof(float)},
            {xsd.base64Binary.AbsoluteUri, typeof(byte[])},
        };

        /// <summary>
        /// Maps .NET types to object serialization delegates.
        /// </summary>
        protected static Dictionary<Type, ObjectSerializationDelegate> Serializers = new Dictionary<Type, ObjectSerializationDelegate>()
        {
            {typeof(Int16), SerializeInt16},
            {typeof(Int32), SerializeInt32},
            {typeof(Int64), SerializeInt64},
            {typeof(UInt16), SerializeUInt16},
            {typeof(UInt32), SerializeUInt32},
            {typeof(UInt64), SerializeUInt64},
            {typeof(DateTime), SerializeDateTime},
            {typeof(bool), SerializeBool},
            {typeof(decimal), SerializeDecimal},
            {typeof(double), SerializeDouble},
            {typeof(float), SerializeSingle},
            {typeof(IResource), SerializeIResource},
            {typeof(IModel), SerializeIResource},
            {typeof(string), SerializeString},
            {typeof(string[]), SerializeStringArray},
            {typeof(Tuple<string, CultureInfo>), SerializeStringCultureInfoTuple},
            {typeof(Uri), SerializeUri},
            {typeof(byte[]), SerializeByteArray},
        };

        /// <summary>
        /// Maps XSD type URIs to object deserialization delegates.
        /// </summary>
        static Dictionary<string, ObjectDeserializationDelegate> Deserializers = new Dictionary<string, ObjectDeserializationDelegate>()
        {
            {xsd._short.AbsoluteUri, DeserializeInt16},
            {xsd._int.AbsoluteUri, DeserializeInt32},
            {xsd._long.AbsoluteUri, DeserializeInt64},
            {xsd._ushort.AbsoluteUri, DeserializeUInt16},
            {xsd._uint.AbsoluteUri, DeserializeUInt32},
            {xsd._ulong.AbsoluteUri, DeserializeUInt64},
            {xsd.nonNegativeInteger.AbsoluteUri, DeserializeUInt64},
            {xsd.integer.AbsoluteUri, DeserializeInt32},
            {xsd.datetime.AbsoluteUri, DeserializeDateTime},
            {xsd.date.AbsoluteUri, DeserializeDateTime},
            {xsd.boolean.AbsoluteUri, DeserializeBool},
            {xsd.boolean_.AbsoluteUri, DeserializeBool},
            {xsd._decimal.AbsoluteUri, DeserializeDecimal},
            {xsd._double.AbsoluteUri, DeserializeDouble},
            {xsd._float.AbsoluteUri, DeserializeSingle},
            {"http://www.w3.org/1999/02/22-rdf-syntax-ns#resource", DeserializeResource},
            {xsd.base64Binary.AbsoluteUri, DeserializeByteArray}
        };

        #endregion

        #region Methods

        /// <summary>
        /// Provides the XML Schema type URI for a given .NET type.
        /// </summary>
        /// <param name="type">A .NET type object.</param>
        /// <returns>A XML Schema type URI.</returns>
        public static Uri GetXsdTypeUri(Type type)
        {
            return NativeToXsd[type];
        }

        #endregion

        #region Serialization

        public delegate string ObjectSerializationDelegate(object obj);

        public static string SerializeObject(object obj)
        {
            Type type = obj.GetType();

            if (Serializers.ContainsKey(type))
            {
                return Serializers[type](obj);
            }
            else if (type.GetInterface("IResource") != null)
            {
                return SerializeObject(obj, typeof(IResource));
            }
            else
            {
                string msg = string.Format("No serialiser availabe for object of type {0}.", obj.GetType());
                throw new ArgumentException(msg);
            }
        }

        public static string SerializeObject(object obj, Type type)
        {
            return Serializers[type](obj);
        }

        public static string SerializeIResource(object obj)
        {
            IResource resource = obj as IResource;

            if (resource != null)
            {
                // The .NET Uri class makes the host lower case, this is a problem for OpenLink Virtuoso
                return resource.Uri.OriginalString.ToString();
            }
            else
            {
                throw new ArgumentException("Argument 1 must be of type Semiodesk.Trinity.IResource");
            }
        }

        public static string SerializeUri(object obj)
        {
            Uri uri = obj as Uri;

            if (uri != null)
            {
                return uri.OriginalString;
            }
            else
            {
                throw new ArgumentException("Argument 1 must be of type System.Uri");
            }
        }

        public static string SerializeString(object obj)
        {
            return "\"" + obj.ToString() + "\"";
        }

        public static string SerializeStringArray(object obj)
        {
            string[] array = obj as string[];

            if (array != null)
            {
                return array.First();
            }
            else
            {
                throw new ArgumentException("Argument 1 must be of type string[]");
            }
        }

        public static string SerializeStringCultureInfoTuple(object obj)
        {
            Tuple<string, CultureInfo> tuple = obj as Tuple<string, CultureInfo>;

            if (tuple != null)
            {
                return tuple.Item1;
            }
            else
            {
                throw new ArgumentException("Argument 1 must be of type System.Tuple<string, System.Globalization.CultureInfo>");
            }
        }

        public static string SerializeDateTime(object obj)
        {
            return XmlConvert.ToString((DateTime)obj, XmlDateTimeSerializationMode.Utc).ToString();
        }

        public static string SerializeByteArray(object obj)
        {
            byte[] array = obj as byte[];

            if (array != null)
            {
                return Convert.ToBase64String(array);
            }
            else
            {
                throw new ArgumentException("Argument 1 must be of type byte[]");
            }
        }

        public static string SerializeBool(object obj)
        {
            return XmlConvert.ToString((bool)obj).ToString();
        }

        public static string SerializeInt16(object obj)
        {
            return XmlConvert.ToString((Int16)obj).ToString();
        }

        public static string SerializeInt32(object obj)
        {
            return XmlConvert.ToString((Int32)obj).ToString();
        }

        public static string SerializeInt64(object obj)
        {
            return XmlConvert.ToString((Int64)obj).ToString();
        }

        public static string SerializeUInt16(object obj)
        {
            return XmlConvert.ToString((UInt16)obj).ToString();
        }

        public static string SerializeUInt32(object obj)
        {
            return XmlConvert.ToString((UInt32)obj).ToString();
        }

        public static string SerializeUInt64(object obj)
        {
            return XmlConvert.ToString((UInt64)obj).ToString();
        }

        public static string SerializeDecimal(object obj)
        {
            return XmlConvert.ToString((Decimal)obj).ToString();
        }

        public static string SerializeDouble(object obj)
        {
            return XmlConvert.ToString((double)obj).ToString();
        }

        public static string SerializeSingle(object obj)
        {
            return XmlConvert.ToString((float)obj).ToString();
        }

        #endregion

        #region Deserialization

        public delegate object ObjectDeserializationDelegate(string str);

        public static object DeserializeString(string str)
        {
            return str;
        }

        public static object DeserializeString(string str, Uri typeUri)
        {
            if( Deserializers.ContainsKey(typeUri.AbsoluteUri) )
                return Deserializers[typeUri.AbsoluteUri](str);
            return str;
        }

        public static object DeserializeInt16(string str)
        {
            return (object)XmlConvert.ToInt16(str);
        }

        public static object DeserializeInt32(string str)
        {
            return (object)XmlConvert.ToInt32(str);
        }

        public static object DeserializeInt64(string str)
        {
            return (object)XmlConvert.ToInt64(str);
        }

        public static object DeserializeUInt16(string str)
        {
            return (object)XmlConvert.ToUInt16(str);
        }

        public static object DeserializeUInt32(string str)
        {
            return (object)XmlConvert.ToUInt32(str);
        }

        public static object DeserializeUInt64(string str)
        {
            return (object)XmlConvert.ToUInt64(str);
        }

        public static object DeserializeBool(string str)
        {
            return XmlConvert.ToBoolean(str);
        }

        public static object DeserializeDecimal(string str)
        {
            return XmlConvert.ToDecimal(str);
        }

        public static object DeserializeDouble(string str)
        {
            return XmlConvert.ToDouble(str);
        }

        public static object DeserializeSingle(string str)
        {
            return XmlConvert.ToSingle(str);
        }

        public static object DeserializeDateTime(string str)
        {
            return XmlConvert.ToDateTime(str, XmlDateTimeSerializationMode.Utc);
        }

        public static object DeserializeResource(string str)
        {
            return new Resource(new Uri(str));
        }

        public static object DeserializeByteArray(string str)
        {
            return Convert.FromBase64String(str);
        }

        public static object DeserializeXmlNode(XmlNode node)
        {
            object result = node.InnerText;

            XmlAttribute resource = node.Attributes["rdf:resource"];
            XmlAttribute dataType = node.Attributes["rdf:datatype"];
            XmlAttribute lang = node.Attributes["xml:lang"];

            if (dataType != null)
            {
                try
                {

                    string key = dataType.Value;
                    result = Deserializers[key](node.InnerText);
                }
                catch
                {
                    string msg = string.Format("No converter found for following type: {0}", dataType.Value);
                    throw new ArgumentException(msg);
                }
            }
            else if (resource != null)
            {
                try
                {
                    result = new Resource(new Uri(resource.Value));
                }
                catch
                {
                    result = resource.Value;
                }
            }
            else if (lang != null)
            {
                return new string[] { result.ToString(), lang.Value };
            }

            return result;
        }

        public static object DeserializeLiteralNode(BaseLiteralNode node)
        {
            if (node.DataType != null)
            {
                try
                {
                    return Deserializers[node.DataType.OriginalString](node.Value);
                }catch
                {
                    return node.Value;
                }
            }
            return node.Value;
        }

        #endregion
    }
}
