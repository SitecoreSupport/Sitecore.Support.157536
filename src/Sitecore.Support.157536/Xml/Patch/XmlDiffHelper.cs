namespace Sitecore.Support.Xml.Patch
{
    using Sitecore.Diagnostics;
    using Sitecore.Text.Diff;
    using System;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Bug #157536
    /// The class is overrided in order to change implementation of the CompareAttributes method. 
    /// </summary>
    public class XmlDiffHelper : Sitecore.Xml.Patch.XmlDiffHelper
    {
        public override System.Xml.XmlDocument Compare(System.Xml.XmlDocument original, System.Xml.XmlDocument modified, Sitecore.Xml.Patch.IElementIdentification id, Sitecore.Xml.Patch.XmlPatchNamespaces ns)
        {
            Assert.IsNotNull(original, "Failed to load original XML");
            Assert.IsNotNull(modified, "Failed to load modified XML");
            System.Xml.XmlDocument document = new System.Xml.XmlDocument();
            System.Xml.XmlNode documentElement = original.DocumentElement;
            Assert.IsNotNull(documentElement, "originalRoot != null");
            System.Xml.XmlNode node2 = modified.DocumentElement;
            Assert.IsNotNull(node2, "modifiedRoot != null");
            document.AppendChild(document.CreateElement(documentElement.Prefix, documentElement.LocalName, documentElement.NamespaceURI));
            this.DoCompare(new Sitecore.Xml.Patch.XmlDomSource(documentElement), new Sitecore.Xml.Patch.XmlDomSource(node2), id, new Sitecore.Xml.Patch.XmlElementContext(document.DocumentElement, ns));
            return document;
        }

        /// The implementation from Sitecore 8.2 update 1
        //protected virtual void CompareAttributes(IXmlElement original, IXmlElement modified, IComparisonContext context)
        //{
        //    IXmlNode[] attributes = (from node in original.GetAttributes()
        //                             orderby node.NamespaceURI + ":" + node.LocalName
        //                             select node).ToArray<IXmlNode>();
        //    IXmlNode[] nodeArray2 = (from node in modified.GetAttributes()
        //                             orderby node.NamespaceURI + ":" + node.LocalName
        //                             select node).ToArray<IXmlNode>();
        //    IComparisonContextEx ex = context as IComparisonContextEx;
        //    DiffEngine engine = new DiffEngine();
        //    engine.ProcessDiff(new AttributeDiffList(attributes), new AttributeDiffList(nodeArray2));
        //    foreach (DiffResultSpan span in engine.DiffReport())
        //    {
        //        if ((span.Status == DiffResultSpanStatus.DeleteSource) && (ex != null))
        //        {
        //            for (int i = 0; i < span.Length; i++)
        //            {
        //                ex.DeleteAttribute(attributes[span.SourceIndex + i]);
        //            }
        //        }
        //        else
        //        {
        //            if (span.Status == DiffResultSpanStatus.AddDestination)
        //            {
        //                for (int j = 0; j < span.Length; j++)
        //                {
        //                    context.SetAttribute(nodeArray2[span.DestIndex + j]);
        //                }
        //            }
        //            if (span.Status == DiffResultSpanStatus.Replace)
        //            {
        //                for (int k = 0; k < span.Length; k++)
        //                {
        //                    context.SetAttribute(nodeArray2[span.DestIndex + k]);
        //                    if (ex != null)
        //                    {
        //                        ex.DeleteAttribute(attributes[span.SourceIndex + k]);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        /// <summary> 
        /// The implementation of the method is taken from Sitecore 8.1 update 1
        /// </summary>
        /// <param name="original"></param>
        /// <param name="modified"></param>
        /// <param name="context"></param>
        protected override void CompareAttributes(Sitecore.Xml.Patch.IXmlElement original, Sitecore.Xml.Patch.IXmlElement modified, Sitecore.Xml.Patch.IComparisonContext context)
        {
            Sitecore.Xml.Patch.IXmlNode[] attributes = (from node in original.GetAttributes()
                                                        orderby node.NamespaceURI + ":" + node.LocalName
                                                        select node).ToArray<Sitecore.Xml.Patch.IXmlNode>();
            Sitecore.Xml.Patch.IXmlNode[] nodeArray2 = (from node in modified.GetAttributes()
                                                        orderby node.NamespaceURI + ":" + node.LocalName
                                                        select node).ToArray<Sitecore.Xml.Patch.IXmlNode>();
            Sitecore.Xml.Patch.IComparisonContextEx ex = context as Sitecore.Xml.Patch.IComparisonContextEx;
            DiffEngine engine = new DiffEngine();
            engine.ProcessDiff(new Sitecore.Xml.Patch.AttributeDiffList(attributes), new Sitecore.Xml.Patch.AttributeDiffList(nodeArray2));

            foreach (DiffResultSpan span in engine.DiffReport())
            {
                if ((span.Status == DiffResultSpanStatus.AddDestination) || (span.Status == DiffResultSpanStatus.Replace))
                {
                    for (int i = 0; i < span.Length; i++)
                    {
                        context.SetAttribute(nodeArray2[span.DestIndex + i]);
                    }
                }
            }
        }

        protected override void CompareChildren(Sitecore.Xml.Patch.IXmlElement original, Sitecore.Xml.Patch.IXmlElement modified, Sitecore.Xml.Patch.IElementIdentification id, Sitecore.Xml.Patch.IComparisonContext context)
        {
            Sitecore.Xml.Patch.IXmlElement[] elements = original.GetChildren().ToArray<Sitecore.Xml.Patch.IXmlElement>();
            Sitecore.Xml.Patch.IXmlElement[] elementArray2 = modified.GetChildren().ToArray<Sitecore.Xml.Patch.IXmlElement>();
            DiffEngine engine = new DiffEngine();
            engine.ProcessDiff(new Sitecore.Xml.Patch.ElementDiffList(elements, id), new Sitecore.Xml.Patch.ElementDiffList(elementArray2, id));
            foreach (DiffResultSpan span in this.Postprocess(engine.DiffReport(), elements, elementArray2, id))
            {
                bool flag = (span.Status == DiffResultSpanStatus.DeleteSource) || (span.Status == DiffResultSpanStatus.Replace);
                bool flag2 = (span.Status == DiffResultSpanStatus.AddDestination) || (span.Status == DiffResultSpanStatus.Replace);
                bool flag3 = span.Status == DiffResultSpanStatus.NoChange;
                if (flag && (span.Link == null))
                {
                    for (int i = 0; i < span.Length; i++)
                    {
                        Sitecore.Xml.Patch.IComparisonContext childContext;
                        Sitecore.Xml.Patch.IXmlElement xmlElement = elements[span.SourceIndex + i];
                        if (context is Sitecore.Xml.Patch.IComparisonContextEx)
                        {
                            childContext = (context as Sitecore.Xml.Patch.IComparisonContextEx).GetChildContext(xmlElement.LocalName, xmlElement.NamespaceURI);
                        }
                        else
                        {
                            childContext = context.GetChildContext(xmlElement.LocalName);
                        }
                        this.SetIdentification(childContext, xmlElement, id);
                        childContext.Delete();
                    }
                }
                if (flag2)
                {
                    Sitecore.Xml.Patch.IXmlElement reference = null;
                    Sitecore.Xml.Patch.PatchPosition undefined = Sitecore.Xml.Patch.PatchPosition.Undefined;
                    if (span.DestIndex > 0)
                    {
                        if (span.DestIndex == (elementArray2.Length - 1))
                        {
                            undefined = Sitecore.Xml.Patch.PatchPosition.End;
                        }
                        else
                        {
                            reference = elementArray2[span.DestIndex - 1];
                            if ((reference != null) && reference.LocalName.StartsWith("#"))
                            {
                                for (int j = span.DestIndex - 1; ((reference != null) && reference.LocalName.StartsWith("#")) && (j > 0); j--)
                                {
                                    reference = elementArray2[j - 1];
                                }
                                if ((reference == null) || ((reference != null) && reference.LocalName.StartsWith("#")))
                                {
                                    reference = null;
                                    undefined = Sitecore.Xml.Patch.PatchPosition.Beginning;
                                }
                                else
                                {
                                    undefined = Sitecore.Xml.Patch.PatchPosition.After;
                                }
                            }
                            else
                            {
                                undefined = Sitecore.Xml.Patch.PatchPosition.After;
                            }
                        }
                    }
                    else if (elementArray2.Length > 1)
                    {
                        undefined = Sitecore.Xml.Patch.PatchPosition.Beginning;
                    }
                    if (span.Link == null)
                    {
                        for (int k = 0; k < span.Length; k++)
                        {
                            this.InsertNode(context, elementArray2[span.DestIndex + k], reference, id, undefined);
                        }
                    }
                    else
                    {
                        Assert.IsTrue(span.Length == 1, "When moving, expect span.Length = 1");
                        Sitecore.Xml.Patch.IXmlElement element3 = elements[span.Link.SourceIndex];
                        Sitecore.Xml.Patch.IXmlElement element = elementArray2[span.DestIndex];
                        Sitecore.Xml.Patch.IComparisonContext context3 = this.GetChildContext(context, element, reference, id, undefined);
                        this.CompareAttributes(element3, element, context3);
                        this.CompareChildren(element3, element, id, context3);
                    }
                }
                if (flag3)
                {
                    for (int m = 0; m < span.Length; m++)
                    {
                        Sitecore.Xml.Patch.IComparisonContext context4;
                        Sitecore.Xml.Patch.IXmlElement element5 = elements[span.SourceIndex + m];
                        Sitecore.Xml.Patch.IXmlElement element6 = elementArray2[span.DestIndex + m];
                        if (context is Sitecore.Xml.Patch.IComparisonContextEx)
                        {
                            context4 = (context as Sitecore.Xml.Patch.IComparisonContextEx).GetChildContext(element6.LocalName, element6.NamespaceURI);
                        }
                        else
                        {
                            context4 = context.GetChildContext(element6.LocalName);
                        }
                        this.SetIdentification(context4, element5, id);
                        this.CompareAttributes(element5, element6, context4);
                        this.CompareChildren(element5, element6, id, context4);
                    }
                }
            }
        }

        protected override void DoCompare(Sitecore.Xml.Patch.IXmlElement original, Sitecore.Xml.Patch.IXmlElement modified, Sitecore.Xml.Patch.IElementIdentification identification, Sitecore.Xml.Patch.IComparisonContext context)
        {
            if (identification.GetID(original) != identification.GetID(modified))
            {
                throw new Exception("Can't start with unequal nodes");
            }
            context.SetIdentification(identification.GetSignificantAttributes(modified));
            this.CompareAttributes(original, modified, context);
            this.CompareChildren(original, modified, identification, context);
        }
    }
}