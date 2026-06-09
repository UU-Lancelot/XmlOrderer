using System.Xml;
using System.Xml.Linq;

namespace UU.Lancelot.XmlOrderer;

public class Orderer
{
  public static void Order(string loadFilePath, string saveFilePath, IEnumerable<OrderInfo> orderInfo)
  {
    var orderer = new Orderer(orderInfo);

    XDocument document = orderer.LoadDocument(loadFilePath);
    orderer.Order(ref document);
    orderer.SaveDocument(document, saveFilePath);
  }

  public Orderer(IEnumerable<OrderInfo> orderInfo)
  {
    _orderInfo = orderInfo;
  }

  private readonly IEnumerable<OrderInfo> _orderInfo;

  public XDocument LoadDocument(string filePath)
  {
    return XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
  }

  public void SaveDocument(XDocument document, string filePath)
  {
    var settings = new XmlWriterSettings
    {
      Indent = false,
      OmitXmlDeclaration = false,
      NewLineHandling = NewLineHandling.None
    };

    using XmlWriter writer = XmlWriter.Create(filePath, settings);
    document.Save(writer);
  }

  public void Order(ref XDocument document)
  {
    foreach (var info in _orderInfo)
    {
      var parentElements = FindParentElements(document, info.OrderParent);

      foreach (var parent in parentElements)
      {
        var orderedChildren = parent
          .Elements()
          .Where(e => e.Name.LocalName == info.OrderElementName)
          .OrderBy(e => (string?)e.Attribute(info.OrderByAttribute), StringComparer.InvariantCulture)
          .ToList();

        if (orderedChildren.Count < 2)
          continue;

        int orderedChildIndex = 0;
        var updatedNodes = parent.Nodes().Select(node =>
        {
          if (node is XElement element && element.Name.LocalName == info.OrderElementName)
          {
            if (element.Attribute(info.OrderByAttribute)?.Value != orderedChildren[orderedChildIndex].Attribute(info.OrderByAttribute)?.Value)
              Console.WriteLine($"Ordering element: <{info.OrderElementName} {info.OrderByAttribute}=\"{element.Attribute(info.OrderByAttribute)?.Value}\"> => <{info.OrderElementName} {info.OrderByAttribute}=\"{orderedChildren[orderedChildIndex].Attribute(info.OrderByAttribute)?.Value}\">");

            return orderedChildren[orderedChildIndex++];
          }

          return node;
        });

        parent.ReplaceNodes(updatedNodes);
      }
    }
  }

  private static IEnumerable<XElement> FindParentElements(XDocument document, string orderParent)
  {
    if (document.Root == null)
      return Enumerable.Empty<XElement>();

    var pathParts = orderParent
      .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (document.Root.Name.LocalName == pathParts.FirstOrDefault())
      pathParts = pathParts.Skip(1).ToArray();

    IEnumerable<XElement> current = [document.Root];
    foreach (var part in pathParts)
    {
      current = current.SelectMany(e => e.Elements().Where(child => child.Name.LocalName == part));
    }

    return current;
  }
}