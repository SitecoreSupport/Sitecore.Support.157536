namespace Sitecore.Support.Data.Fields
{
    using Sitecore.Data.Fields;
    using Sitecore.Diagnostics;
    using Sitecore.Xml;
    using System;
    using Xml.Patch;

    /// <summary>
    /// Bug #157536
    /// The class is created instead of XmlDeltas. 
    /// </summary>
    public static class CustomXmlDeltas
    {
        public static string ApplyDelta(string baseValue, string delta)
        {
            Assert.ArgumentNotNull(baseValue, "baseValue");
            if (!Sitecore.Xml.Patch.XmlPatchUtils.IsXmlPatch(delta))
            {
                return baseValue;
            }
            System.Xml.XmlDocument document = XmlUtil.LoadXml(delta);
            Assert.IsNotNull(document, "Layout Delta is not a valid XML");
            System.Xml.XmlNode documentElement = document.DocumentElement;
            Assert.IsNotNull(documentElement, "Xml document root element is missing (delta)");
            System.Xml.XmlDocument document2 = XmlUtil.LoadXml(baseValue);
            Assert.IsNotNull(document2, "Layout Value is not a valid XML");
            System.Xml.XmlNode node2 = document2.DocumentElement;
            Assert.IsNotNull(node2, "Xml document root element is missing (base)");
            new Sitecore.Xml.Patch.XmlPatcher("s", "p").Merge(node2, documentElement);
            return node2.OuterXml;
        }

        public static string GetDelta(string layoutValue, string baseValue)
        {
            System.Xml.XmlDocument original = XmlUtil.LoadXml(baseValue);
            if (original != null)
            {
                System.Xml.XmlDocument modified = XmlUtil.LoadXml(layoutValue);
                if (modified == null)
                {
                    return layoutValue;
                }

                /// Bug #157536
                /// 
                /// System.Xml.XmlDocument delta = XmlDiffUtils.Compare(original, modified, XmlDiffUtils.GetDefaultElementIdentification(), XmlDiffUtils.GetDefaultPatchNamespaces());
                /// if (XmlDiffUtils.IsEmptyDelta(delta))
                /// {
                ///    return string.Empty;
                /// }

                XmlDiffHelper diffHelper = new XmlDiffHelper();
                System.Xml.XmlDocument delta = diffHelper.Compare(original, modified, diffHelper.GetDefaultElementIdentification(), diffHelper.GetDefaultPatchNamespaces());

                if (diffHelper.IsEmptyDelta(delta))
                {
                    return string.Empty;
                }

                layoutValue = delta.DocumentElement.HasChildNodes ? delta.OuterXml : string.Empty;
            }
            return layoutValue;
        }

        public static string GetFieldValue(Field field, Func<Field, string> getBaseValue)
        {
            Assert.ArgumentNotNull(field, "field");
            Assert.ArgumentNotNull(getBaseValue, "getBaseValue");
            string str = field.GetValue(false, false);
            if (string.IsNullOrEmpty(str))
            {
                return field.Value;
            }
            return ApplyDelta(getBaseValue(field), str);
        }

        public static string GetStandardValue(Field field) =>
            field.GetStandardValue();

        public static void SetFieldValue(Field field, string value)
        {
            Assert.ArgumentNotNull(field, "field");
            Assert.ArgumentNotNull(value, "value");
            if (field.Item.Name == "__Standard Values")
            {
                field.Value = value;
            }
            else
            {
                field.Value = GetDelta(value, field.GetStandardValue());
            }
        }

        public static Func<Field, string> WithEmptyValue(string emptyValue) =>
            delegate (Field field) {
                string standardValue = field.GetStandardValue();
                if ((standardValue != null) && (standardValue.Trim().Length != 0))
                {
                    return standardValue;
                }
                return emptyValue;
            };
    }
}