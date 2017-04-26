namespace Sitecore.Support.Data.Fields
{
    using Sitecore.Collections;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Pipelines.GetLayoutSourceFields;
    using Sitecore.Xml;
    using Sitecore.Xml.Patch;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Bug #157536
    /// The class is created instead of LayoutField in order to rewrite the SetFieldValue method. 
    /// </summary>
    static class CustomLayoutField
    {
        [Obsolete("Use GetLayoutSourceFieldsPipeline.Run(GetLayoutSourceFieldsArgs args) method instead.")]
        private static List<string> DoGetFieldValue(Sitecore.Data.Fields.Field field)
        {
            Sitecore.Data.Items.Item item = field.Item;
            FieldCollection fields = item.Fields;
            IEnumerable<Lazy<string>> source = new Lazy<string>[] { new Lazy<string>(() => fields[FieldIDs.FinalLayoutField].GetValue(false, false) ?? fields[FieldIDs.FinalLayoutField].GetInheritedValue(false)), new Lazy<string>(() => fields[FieldIDs.LayoutField].GetValue(false, false) ?? fields[FieldIDs.LayoutField].GetInheritedValue(false)), new Lazy<string>(() => fields[FieldIDs.FinalLayoutField].GetStandardValue()), new Lazy<string>(() => fields[FieldIDs.LayoutField].GetStandardValue()) };
            bool flag = item.Name == "__Standard Values";
            bool flag2 = field.ID == FieldIDs.LayoutField;
            if (flag && flag2)
            {
                source = source.Skip<Lazy<string>>(3);
            }
            else if (flag)
            {
                source = source.Skip<Lazy<string>>(2);
            }
            else if (flag2)
            {
                source = source.Skip<Lazy<string>>(1);
            }
            return (from x in source select x.Value).ToList<string>();
        }

        public static string GetFieldValue(Sitecore.Data.Fields.Field field)
        {
            Assert.ArgumentNotNull(field, "field");
            Assert.IsTrue((field.ID == FieldIDs.LayoutField) || (field.ID == FieldIDs.FinalLayoutField), "The field is not a layout/renderings field");
            GetLayoutSourceFieldsArgs args = new GetLayoutSourceFieldsArgs(field);
            bool flag = GetLayoutSourceFieldsPipeline.Run(args);
            List<string> list = new List<string>();
            if (flag)
            {
                list.AddRange(from fieldValue in args.FieldValuesSource select fieldValue.GetValue(false, false) ?? (fieldValue.GetInheritedValue(false) ?? fieldValue.GetValue(false, false, true, false, false)));
                list.AddRange(from fieldValue in args.StandardValuesSource select fieldValue.GetStandardValue());
            }
            else
            {
                list = DoGetFieldValue(field);
            }
            System.Collections.Generic.Stack<string> source = new System.Collections.Generic.Stack<string>();
            string str = null;
            foreach (string str2 in list)
            {
                if (!string.IsNullOrWhiteSpace(str2))
                {
                    if (XmlPatchUtils.IsXmlPatch(str2))
                    {
                        source.Push(str2);
                    }
                    else
                    {
                        str = str2;
                        break;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(str))
            {
                return string.Empty;
            }

            /// Bug #157536
            /// CustomXmlDeltas is used instead XmlDeltas
            /// return source.Aggregate<string, string>(str, new Func<string, string, string>(XmlDeltas.ApplyDelta));
            return source.Aggregate<string, string>(str, new Func<string, string, string>(CustomXmlDeltas.ApplyDelta));
        }

        public static void SetFieldValue(Sitecore.Data.Fields.Field field, string value)
        {
            Sitecore.Data.Fields.Field field2;
            Assert.ArgumentNotNull(field, "field");
            Assert.ArgumentNotNull(value, "value");
            Assert.IsTrue((field.ID == FieldIDs.LayoutField) || (field.ID == FieldIDs.FinalLayoutField), "The field is not a layout/renderings field");
            string fieldValue = null;
            bool flag = field.Item.Name == "__Standard Values";
            bool flag2 = field.ID == FieldIDs.LayoutField;
            if (flag && flag2)
            {
                field2 = null;
            }
            else if (flag)
            {
                field2 = field.Item.Fields[FieldIDs.LayoutField];
            }
            else if (flag2)
            {
                TemplateItem template = field.Item.Template;
                field2 = ((template != null) && (template.StandardValues != null)) ? template.StandardValues.Fields[FieldIDs.FinalLayoutField] : null;
            }
            else
            {
                field2 = field.Item.Fields[FieldIDs.LayoutField];
            }
            if (field2 != null)
            {
                fieldValue = GetFieldValue(field2);
            }
            if (XmlUtil.XmlStringsAreEqual(value, fieldValue))
            {
                field.Reset();
            }
            else if (!string.IsNullOrWhiteSpace(fieldValue))
            {
                // The class XmlDeltas is rewritten.
                field.Value = CustomXmlDeltas.GetDelta(value, fieldValue);
            }
            else
            {
                field.Value = value;
            }
        }

        public static void SetFieldValue(Sitecore.Data.Fields.Field field, string value, string baseValue)
        {
            Assert.ArgumentNotNull(field, "field");
            Assert.ArgumentNotNull(value, "value");
            Assert.ArgumentNotNull(baseValue, "baseValue");
            Assert.IsTrue((field.ID == FieldIDs.LayoutField) || (field.ID == FieldIDs.FinalLayoutField), "The field is not a layout/renderings field");
            if (XmlUtil.XmlStringsAreEqual(value, baseValue))
            {
                field.Reset();
            }
            else
            {
                string delta;
                if (!string.IsNullOrWhiteSpace(baseValue))
                {
                    /// Bug #157536
                    /// CustomXmlDeltas is used instead XmlDeltas
                    /// delta = XmlDeltas.GetDelta(value, baseValue);
                    delta = CustomXmlDeltas.GetDelta(value, baseValue);
                }
                else
                {
                    delta = value;
                }

                /// Bug #157536
                /// CustomXmlDeltas is used instead XmlDeltas
                /// if (!XmlUtil.XmlStringsAreEqual(CustomXmlDeltas.ApplyDelta(baseValue, field.Value), XmlDeltas.ApplyDelta(baseValue, delta)))
                /// {
                ///     field.Value = delta;
                /// }
                if (!XmlUtil.XmlStringsAreEqual(CustomXmlDeltas.ApplyDelta(baseValue, field.Value), CustomXmlDeltas.ApplyDelta(baseValue, delta)))
                {
                    field.Value = delta;
                }
            }
        }
    }
}