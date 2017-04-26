namespace Sitecore.Support.Shell.Framework.Commands
{
    using Data.Items;
    using Sitecore.Configuration;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Shell.Applications.Dialogs.LayoutDetails;
    using Sitecore.Text;
    using Sitecore.Web.UI.Sheer;
    using System;

    /// <summary>
    /// Bug #157536
    /// The class is created in order to rewrite command which is performed when setting layout deltas. 
    /// </summary>
    [Serializable]
    public class SetLayoutDetails : Sitecore.Shell.Framework.Commands.SetLayoutDetails
    {
        protected override void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            CheckModifiedParameters parameters = new CheckModifiedParameters
            {
                ResumePreviousPipeline = true
            };

            if (SheerResponse.CheckModified(parameters))
            {
                if (args.IsPostBack)
                {
                    if (args.HasResult)
                    {
                        Sitecore.Data.Database database = Factory.GetDatabase(args.Parameters["database"]);
                        Assert.IsNotNull(database, "Database \"" + args.Parameters["database"] + "\" not found.");
                        Sitecore.Data.Items.Item item = database.GetItem(Sitecore.Data.ID.Parse(args.Parameters["id"]), Language.Parse(args.Parameters["language"]), Sitecore.Data.Version.Parse(args.Parameters["version"]));
                        Assert.IsNotNull(item, "item");
                        LayoutDetailsDialogResult result = LayoutDetailsDialogResult.Parse(args.Result);

                        /// Bug #157536
                        /// CustomItemUtil is used instead of ItemUtil.
                        /// ItemUtil.SetLayoutDetails(item, result.Layout, result.FinalLayout);
                        CustomItemUtil.SetLayoutDetails(item, result.Layout, result.FinalLayout);

                        if (result.VersionCreated)
                        {
                            Context.ClientPage.SendMessage(this, string.Concat(new object[] { "item:versionadded(id=", item.ID, ",version=", item.Version, ",language=", item.Language, ")" }));
                        }
                    }
                }
                else
                {
                    UrlString str = new UrlString(UIUtil.GetUri("control:LayoutDetails"));
                    str.Append("id", args.Parameters["id"]);
                    str.Append("la", args.Parameters["language"]);
                    str.Append("vs", args.Parameters["version"]);
                    SheerResponse.ShowModalDialog(str.ToString(), "650px", string.Empty, string.Empty, true);
                    args.WaitForPostBack();
                }
            }
        }
    }
}