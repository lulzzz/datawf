﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.ComponentModel;
using System.Collections;
using System.Globalization;
using System.Xml.Serialization;
using System.Linq;

namespace DataWF.Common
{
    /// <summary>
    /// Type service.
    /// </summary>
    public static class TypeHelper
    {
        private static Type[] typeOneArray = { typeof(string) };
        private static Dictionary<string, MemberInfo> casheNames = new Dictionary<string, MemberInfo>(200, StringComparer.Ordinal);
        private static Dictionary<MemberInfo, bool> cacheIsXmlText = new Dictionary<MemberInfo, bool>(500);
        private static Dictionary<MemberInfo, TypeConverter> cacheTypeConverter = new Dictionary<MemberInfo, TypeConverter>(500);
        private static Dictionary<MemberInfo, bool> cacheIsXmlAttribute = new Dictionary<MemberInfo, bool>(500);
        private static Dictionary<Type, bool> cacheTypeIsXmlAttribute = new Dictionary<Type, bool>(500);
        private static Dictionary<MemberInfo, bool> cacheIsXmlSerialize = new Dictionary<MemberInfo, bool>(500);
        private static Dictionary<MemberInfo, object> cacheDefault = new Dictionary<MemberInfo, object>(500);

        public static PropertyInfo GetIndexProperty(Type itemType)
        {
            typeOneArray[0] = typeof(string);
            return GetIndexProperty(itemType, typeOneArray);
        }

        public static PropertyInfo GetIndexProperty(Type itemType, Type indexType)
        {
            typeOneArray[0] = indexType;
            return GetIndexProperty(itemType, typeOneArray);
        }

        public static PropertyInfo GetIndexProperty(Type itemType, Type[] parameters)
        {
            if (itemType == null)
                return null;
            return itemType.GetProperty("Item", parameters);
        }

        public static bool IsInterface(Type type, Type interfaceType)
        {
            return interfaceType.IsAssignableFrom(type);
        }

        public static List<MemberInfo> GetMemberInfoList(Type type, string property)
        {
            var list = new List<MemberInfo>();
            MemberInfo last = null;
            int s = 0, i = 0;
            do
            {
                i = property.IndexOf('.', s);
                var memberName = property.Substring(s, (i > 0 ? i : property.Length) - s);
                last = GetMemberInfo(last == null ? type : GetMemberType(last), memberName, false);
                if (last == null)
                    throw new ArgumentException();
                list.Add(last);
                s = i + 1;
            }
            while (i > 0);
            return list;
        }

        //public static string GetPropertyString(string property)
        //{
        //    if (property == null)
        //        return null;
        //    string[] split = property.Sp_lit(new char[] { '.' });
        //    return split[split.Length - 1];
        //}

        /// <summary>
        /// Determines whether the specified type is dictionary.
        /// </summary>
        /// <returns>
        /// <c>true</c> if specified type implement IDictionary; otherwise, <c>false</c>.
        /// </returns>
        /// <param name='type'>
        /// type.
        /// </param>
        public static bool IsDictionary(Type type)
        {
            return IsInterface(type, typeof(IDictionary)) && type != typeof(byte[]);
        }

        /// <summary>
        /// Determines whether the specified type is list.
        /// </summary>
        /// <returns>
        /// <c>true</c> if specified type implement IList; otherwise, <c>false</c>.
        /// </returns>
        /// <param name='type'>
        /// type.
        /// </param>
        public static bool IsList(Type type)
        {
            return IsInterface(type, typeof(IList)) && type != typeof(byte[]);
        }

        /// <summary>
        /// Determines whether this specified type is collection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this specified type is collection; otherwise, <c>false</c>.
        /// </returns>
        /// <param name='type'>
        /// If set to <c>true</c> type.
        /// </param>
        public static bool IsCollection(Type type)
        {
            return IsInterface(type, typeof(ICollection)) && type != typeof(byte[]);
        }

        /// <summary>
        /// Determines whether the specified type is file serialized.
        /// </summary>
        /// <returns>
        /// <c>true</c> if specified type implement IFSerialize; otherwise, <c>false</c>.
        /// </returns>
        /// <param name='type'>
        /// type
        /// </param>
        public static bool IsFSerialize(Type type)
        {
            return IsInterface(type, typeof(IFileSerialize));
        }

        public static bool IsBaseTypeName(Type type, string filterType)
        {
            while (type != null)
            {
                if (type.FullName == filterType)
                    return true;
                type = type.BaseType;
            }
            return false;
        }

        public static bool IsBaseType(Type type, Type filterType)
        {
            while (type != null)
            {
                if (type == filterType)
                    return true;
                type = type.BaseType;
            }
            return false;
        }

        public static Type ParseType(string value)
        {
            Type type = Type.GetType(value);
            if (type == null)
            {
                int index = value.IndexOf(',');
                string code = index >= 0 ? value.Substring(0, index) : value;
                type = Type.GetType(code);
                var asseblyes = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in asseblyes)
                {
                    type = assembly.GetType(code);
                    if (type != null)
                        break;
                }
            }
            return type;
        }

        public static TypeConverter GetTypeConverter(Type type)
        {
            if (!cacheTypeConverter.TryGetValue(type, out var converter))
            {
                var attribute = type.GetCustomAttribute(typeof(TypeConverterAttribute)) as TypeConverterAttribute;
                TypeConverter typeConverter = null;
                if (attribute != null && !string.IsNullOrEmpty(attribute.ConverterTypeName))
                {
                    var converterType = ParseType(attribute.ConverterTypeName);
                    if (converterType != null)
                        typeConverter = CreateObject(converterType) as TypeConverter;
                }
                return cacheTypeConverter[type] = typeConverter;
            }
            return converter;
        }

        public static bool IsXmlText(MemberInfo info)
        {
            if (!cacheIsXmlText.TryGetValue(info, out bool flag))
            {
                var attribute = info.GetCustomAttribute(typeof(XmlTextAttribute), false);
                return cacheIsXmlText[info] = attribute != null;
            }
            return flag;
        }

        public static bool IsXmlAttribute(MemberInfo info)
        {
            if (!cacheIsXmlAttribute.TryGetValue(info, out bool flag))
            {
                var attribute = info.GetCustomAttribute(typeof(XmlAttributeAttribute), false);
                return cacheIsXmlAttribute[info] = attribute != null || IsXmlAttribute(GetMemberType(info));
            }
            return flag;
        }

        public static bool IsXmlAttribute(Type type)
        {
            if (!cacheTypeIsXmlAttribute.TryGetValue(type, out bool flag))
            {
                if (type.IsPrimitive || type.IsEnum
                   || type == typeof(string) || type == typeof(decimal) || type == typeof(byte[])
                   || type == typeof(CultureInfo) || type == typeof(Type))
                {
                    flag = true;
                }
                else
                {
                    var typeConverter = TypeHelper.GetTypeConverter(type);
                    if (typeConverter != null && typeConverter.CanConvertTo(typeof(string)))
                        flag = true;
                }
                cacheTypeIsXmlAttribute[type] = flag;
            }
            return flag;
        }

        /// <summary>
        /// Determines whether specified field is non serialized.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the specified field have NonSerializedAttribute ; otherwise, <c>false</c>.
        /// </returns>
        /// <param name='info'>
        /// filed info.
        /// </param>
        public static bool IsNonSerialize(MemberInfo info)
        {
            if (!cacheIsXmlSerialize.TryGetValue(info, out bool flag))
            {
                Type itemType = GetMemberType(info);
                if (itemType.IsSubclassOf(typeof(Delegate))
                || (itemType == info.DeclaringType && itemType.IsValueType)
                || info is PropertyInfo && !((PropertyInfo)info).CanWrite && !IsDictionary(itemType) && !IsCollection(itemType))
                    flag = true;

                try { XmlConvert.VerifyName(info.Name); }
                catch { flag = true; }
                if (!flag)
                {
                    var attribute = info.GetCustomAttribute(typeof(XmlIgnoreAttribute), false);
                    flag = attribute != null;
                    if (!flag && info is FieldInfo)
                    {
                        var dscArray = info.GetCustomAttribute(typeof(NonSerializedAttribute), false);
                        flag = dscArray != null;
                    }
                }
                cacheIsXmlSerialize[info] = flag;
            }
            return flag;
        }

        /// <summary>
        /// Compare the default value the specified field DefaultValueAttribute and specified value.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the specified field have DefaultValueAttribute and it's value equal to specified value ; otherwise, <c>false</c>.
        /// </returns>
        /// <param name='field'>
        /// Field Info.
        /// </param>
        /// <param name='value'>
        /// Value.
        /// </param>
        public static bool CheckDefault(MemberInfo field, object value)
        {
            if (!cacheDefault.TryGetValue(field, out object defaultValue))
            {
                var defaultAttribute = (DefaultValueAttribute)field.GetCustomAttribute(typeof(DefaultValueAttribute), false);
                defaultValue = cacheDefault[field] = defaultAttribute == null ? null : defaultAttribute.Value;
            }

            if (defaultValue == null && value == null)
                return true;
            if (defaultValue == null)
                return false;
            return defaultValue.Equals(value);
        }

        public static Type GetMemberType(MemberInfo info)
        {
            if (info is FieldInfo)
                return ((FieldInfo)info).FieldType;
            if (info is PropertyInfo)
                return ((PropertyInfo)info).PropertyType;
            if (info is MethodInfo)
                return ((MethodInfo)info).ReturnType;
            return info.ReflectedType;
        }

        public static MemberInfo GetMemberInfo(Type type, string name, bool generic = false, params Type[] types)
        {
            if (type == null || name == null)
                return null;
            string cachename = string.Format("{0}.{1}{2}", type.FullName, name, generic ? "G" : "");
            foreach (var t in types)
                cachename += t.Name;
            MemberInfo mi = null;
            if (casheNames.TryGetValue(cachename, out mi))
                return mi;
            int i = name.IndexOf('.');
            while (i > 0)
            {
                mi = GetMemberInfo(type, name.Substring(0, i), generic, types);
                if (mi == null)
                    break;

                type = GetMemberType(mi);
                mi = null;
                name = name.Substring(i + 1);
                i = name.IndexOf('.');
            }

            if (type.IsInterface && name == "ToString")
                mi = typeof(object).GetMethod(name, types);
            if (mi == null)
            {
                mi = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (mi == null)
            {
                var props = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var p in props)
                {
                    if (p.Name.Equals(name, StringComparison.Ordinal) && p.IsGenericMethod == generic)
                    {
                        mi = p;
                        var ps = p.GetParameters();

                        if (ps.Length >= types.Length)
                        {
                            var flag = true;
                            for (int j = 0; j < types.Length; j++)
                            {
                                if (ps[j].ParameterType != types[j])
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (flag) break;
                        }
                    }
                }
            }
            if (mi == null)
            {
                var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var p in props)
                {
                    if (p.Name.Equals(name, StringComparison.Ordinal))
                    {
                        mi = p;
                        var ps = p.GetIndexParameters();
                        if (ps.Length >= types.Length)
                        {
                            var flag = ps.Length == types.Length;
                            for (int j = 0; j < types.Length; j++)
                            {
                                if (ps[j].ParameterType != types[j])
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (flag) break;
                        }
                    }
                }
            }
            casheNames[cachename] = mi;
            return mi;
        }

        public static void SetValue(MemberInfo info, object item, object val)
        {
            EmitInvoker.SetValue(info, item, val);
        }

        public static object GetValue(MemberInfo info, object item)
        {
            return EmitInvoker.GetValue(info, item);
        }

        /// <summary>
        /// Gets the fields.
        /// </summary>
        /// <returns>
        /// The fields array.
        /// </returns>
        /// <param name='type'>
        /// Type.
        /// </param>
        /// <param name='nonPublic'>
        /// Non public flag.
        /// </param>
        public static FieldInfo[] GetFields(Type type, bool nonPublic)
        {
            BindingFlags flag = BindingFlags.Instance;
            if (nonPublic)
                flag |= BindingFlags.NonPublic;
            FieldInfo[] buf = type.GetFields(flag);
            return buf;
        }

        public static PropertyInfo[] GetPropertyes(Type type, bool nonPublic)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.Public;
            if (nonPublic)
                flag |= BindingFlags.NonPublic;
            PropertyInfo[] buf = type.GetProperties(flag);
            if (buf.Length > 1 && buf[0].DeclaringType != buf[buf.Length - 1].DeclaringType)
            {
                type = buf[0].DeclaringType;
                var bufs = new List<PropertyInfo>(buf);
                for (int i = bufs.Count - 1, j = 0; i > 0; i--, j++)
                {
                    PropertyInfo b = bufs[i];
                    if (b.DeclaringType != type)
                    {
                        bufs.RemoveAt(i);
                        bufs.Insert(0, b);
                        i++;
                    }
                    else
                        break;
                    if (j >= bufs.Count)
                        break;
                }
                for (int i = 0; i < bufs.Count; i++)
                    buf[i] = bufs[i];
            }
            //Array.Sort(buf, delegate(PropertyInfo x, PropertyInfo y) {
            //	int v = x.DeclaringType.BaseType == y.DeclaringType.BaseType?0:1;
            //	return v;
            //});
            return buf;
        }

        /// <summary>
        /// Creates the object using EmitInvoker.
        /// </summary>
        /// <returns>
        /// The object.
        /// </returns>
        /// <param name='type'>
        /// Type.
        /// </param>
        public static object CreateObject(Type type)
        {
            if (type == typeof(string))
                return "";
            var ra = EmitInvoker.Initialize(type, Type.EmptyTypes, true);
            return ra == null ? null : ra.Create();
        }

        public static string GetDisplayName(PropertyInfo property)
        {
            var name = (DisplayNameAttribute)property.GetCustomAttribute(typeof(DisplayNameAttribute), false);
            return name == null ? property.Name : name.DisplayName;
        }

        public static string GetCategory(MemberInfo info)
        {
            var category = (CategoryAttribute)info.GetCustomAttribute(typeof(CategoryAttribute), false);
            return category == null ? "Misclenation" : category.Category;
        }

        public static string GetDescription(MemberInfo property)
        {
            var description = (DescriptionAttribute)property.GetCustomAttribute(typeof(DescriptionAttribute), false);
            return description?.Description;
        }

        public static bool GetPassword(MemberInfo property)
        {
            var password = (PasswordPropertyTextAttribute)property.GetCustomAttribute(typeof(PasswordPropertyTextAttribute), false);
            return password != null && password.Password;
        }

        public static string GetDefaultFormat(MemberInfo info)
        {
            var defauiltAttr = (DefaultFormatAttribute)info.GetCustomAttribute(typeof(DefaultFormatAttribute), false);
            return defauiltAttr == null ? null : defauiltAttr.Format;
        }

        public static bool GetBrowsable(MemberInfo info)
        {
            var browsable = (BrowsableAttribute)info.GetCustomAttribute(typeof(BrowsableAttribute), false);
            return browsable == null || browsable.Browsable;
        }

        public static bool GetReadOnly(MemberInfo info)
        {
            if (info is PropertyInfo)
            {
                var property = (PropertyInfo)info;
                if (!property.CanWrite)
                    return true;
                var readOnly = (ReadOnlyAttribute)property.GetCustomAttribute(typeof(ReadOnlyAttribute), false);
                return readOnly != null && readOnly.IsReadOnly;
            }
            return !(info is FieldInfo);
        }

        public static bool GetModule(Type type)
        {
            var attrs = (ModuleAttribute)type.GetCustomAttribute(typeof(ModuleAttribute), false);
            return attrs != null && attrs.IsModule;
        }

        public static List<MemberInfo> GetTypeItems(Type type, bool byProperty)
        {
            var flist = byProperty ? GetPropertyes(type, false).Cast<MemberInfo>().ToList() : GetFields(type, true).Cast<MemberInfo>().ToList();
            flist.Sort(delegate (MemberInfo x, MemberInfo y)
            {
                if (IsXmlAttribute(x) && !IsXmlText(x))
                {
                    if (IsXmlAttribute(y) && !IsXmlText(y))
                        return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
                    return -1;
                }
                if (IsXmlAttribute(y) && !IsXmlText(y))
                    return 1;
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            });
            return flist;
        }

        public static Type GetListItemType(ICollection collection, bool ignoreInteface = true)
        {
            Type t = typeof(object);

            if (collection is ISortable)
            {
                if (!((ISortable)collection).ItemType.IsInterface || ignoreInteface)
                    return ((ISortable)collection).ItemType;
            }
            if (collection is IList)
            {
                if (collection.GetType().IsGenericType)
                {
                    t = collection.GetType().GetGenericArguments().FirstOrDefault();
                }
                else if (collection.GetType().IsArray)
                {
                    t = collection.GetType().GetElementType();
                }
                else if (collection.Count != 0)
                {
                    t = ((IList)collection)[0].GetType();
                }
                else
                {
                    t = collection.GetType().GetProperty("Item", new Type[] { typeof(int) })?.PropertyType;
                }
            }
            else
            {
                foreach (object o in collection)
                {
                    if (o != null)
                    {
                        t = o.GetType();
                        break;
                    }
                }
            }
            return t;
        }

        public static bool IsStatic(MemberInfo mInfo)
        {
            return ((mInfo.MemberType == MemberTypes.Method && ((MethodInfo)mInfo).IsStatic) ||
                (mInfo.MemberType == MemberTypes.Property && ((PropertyInfo)mInfo).GetGetMethod().IsStatic) ||
                (mInfo.MemberType == MemberTypes.Field && ((FieldInfo)mInfo).IsStatic));
        }

        public static string FormatType(Type type)
        {
            var index = type.FullName.IndexOf('[');
            return index < 0 ? type.FullName : type.FullName.Substring(0, index);
        }

        //private static Dictionary<string, MethodInfo> _cGenericMethods = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);
        //public static MethodInfo MakeGenericMethod(MethodInfo info, Type type)
        //{
        //    string name = info.DeclaringType.FullName + info.Name + type.Name;
        //    MethodInfo result = null;
        //    if (!_cGenericMethods.TryGetValue(name, out result))
        //        _cGenericMethods[name] = result = info.MakeGenericMethod(type);
        //    return result;
        //}

    }
}