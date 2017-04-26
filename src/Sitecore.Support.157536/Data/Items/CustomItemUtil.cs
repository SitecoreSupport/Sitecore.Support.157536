namespace Sitecore.Support.Data.Items
{
    using Fields;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Layouts;
    using Sitecore.Xml;

    /// <summary>
    /// Bug #157536
    /// The class is created instead of ItemUtil in order to rewrite the SetLayoutDetails method. 
    /// </summary>
    public static class CustomItemUtil
    {
        private static void CleanupInheritedItems(Item item)
        {
            Database database = item.Database;
            string query = $"fast:/sitecore/content//*[@@templateid='{item.TemplateID}']";
            Item[] itemArray = database.SelectItems(query);
            if (itemArray != null)
            {
                using (new StatisticDisabler(StatisticDisablerState.ForItemsWithoutVersionOnly))
                {
                    foreach (Item item2 in itemArray)
                    {
                        Sitecore.Data.Fields.Field field = item2.Fields[FieldIDs.LayoutField];
                        Sitecore.Data.Fields.Field field2 = item2.Fields[FieldIDs.FinalLayoutField];
                        using (new EditContext(item2))
                        {
                            if (field.HasValue)
                            {
                                /// Bug #157536
                                /// CustomLayoutField is used instead of LayoutField.
                                /// string str2 = CleanupLayoutValue(LayoutField.GetFieldValue(field));
                                /// LayoutField.SetFieldValue(field, str2);
                                string str2 = CleanupLayoutValue(CustomLayoutField.GetFieldValue(field));
                                CustomLayoutField.SetFieldValue(field, str2);
                            }
                            if (field2.HasValue)
                            {
                                /// Bug #157536
                                /// CustomLayoutField is used instead of LayoutField.
                                /// string str3 = CleanupLayoutValue(LayoutField.GetFieldValue(field2));
                                /// LayoutField.SetFieldValue(field2, str3);
                                string str3 = CleanupLayoutValue(CustomLayoutField.GetFieldValue(field2));
                                CustomLayoutField.SetFieldValue(field2, str3);
                            }
                        }
                    }
                }
            }
        }

        private static string CleanupLayoutValue(string layout)
        {
            if (!string.IsNullOrEmpty(layout))
            {
                layout = LayoutDefinition.Parse(layout).ToXml();
            }
            return layout;
        }

        public static void SetLayoutDetails(Sitecore.Data.Items.Item item, string sharedLayout, string finalLayout)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(sharedLayout, "sharedLayout");
            Assert.ArgumentNotNull(finalLayout, "finalLayout");
            string str = sharedLayout + finalLayout;
            sharedLayout = CleanupLayoutValue(sharedLayout);
            finalLayout = CleanupLayoutValue(finalLayout);
            using (new StatisticDisabler(StatisticDisablerState.ForItemsWithoutVersionOnly))
            {
                item.Editing.BeginEdit();
                Sitecore.Data.Fields.Field field = item.Fields[FieldIDs.LayoutField];

                if (!XmlUtil.XmlStringsAreEqual(CleanupLayoutValue(CustomLayoutField.GetFieldValue(field)), sharedLayout))
                {
                    /// Bug #157536
                    /// CustomLayoutField is used instead of LayoutField.
                    /// LayoutField.SetFieldValue(field, sharedLayout);
                    CustomLayoutField.SetFieldValue(field, sharedLayout);
                }
                if (!item.RuntimeSettings.TemporaryVersion)
                {
                    Sitecore.Data.Fields.Field field2 = item.Fields[FieldIDs.FinalLayoutField];

                    /// Bug #157536
                    /// CustomLayoutField is used instead LayoutField
                    /// LayoutField.SetFieldValue(field2, finalLayout, sharedLayout);
                    CustomLayoutField.SetFieldValue(field2, finalLayout, sharedLayout);
                }
                item.Editing.EndEdit();
            }
            if (item.Name == "__Standard Values")
            {
                CleanupInheritedItems(item);
            }
            /// Bug #157536
            /// Log.Audit(typeof(ItemUtil), "Set layout details: {0}, layout: {1}", new string[] { AuditFormatter.FormatItem(item), str });
            Log.Audit(typeof(CustomItemUtil), "Set layout details: {0}, layout: {1}", new string[] { AuditFormatter.FormatItem(item), str });
        }
    }
}