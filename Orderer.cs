using System.Xml;
using System.Xml.Linq;

namespace UU.Lancelot.XmlOrderer;

public class Orderer
{
  public static void Order(string loadFilePath, string saveFilePath, string orderParent, string orderElementName, string orderByAttribute)
  {
    var orderer = new Orderer(orderParent, orderElementName, orderByAttribute);

    orderer.Order(loadFilePath, saveFilePath);
  }

  public Orderer(string orderParent, string orderElementName, string orderByAttribute)
  {
    _orderParent = orderParent;
    _orderElementName = orderElementName;
    _orderByAttribute = orderByAttribute;
  }

  private readonly string _orderParent;
  private readonly string _orderElementName;
  private readonly string _orderByAttribute;

  public void Order(string loadFilePath, string saveFilePath)
  {
    var document = XDocument.Load(loadFilePath, LoadOptions.PreserveWhitespace);
    var parentElements = FindParentElements(document, _orderParent);

    foreach (var parent in parentElements)
    {
      var orderedChildren = parent
        .Elements(_orderElementName)
        .OrderBy(e => (string?)e.Attribute(_orderByAttribute), StringComparer.Ordinal)
        .ToList();

      if (orderedChildren.Count < 2)
        continue;

      int orderedChildIndex = 0;
      var updatedNodes = parent.Nodes().Select(node =>
      {
        if (node is XElement element && element.Name.LocalName == _orderElementName)
          return orderedChildren[orderedChildIndex++];

        return node;
      });

      parent.ReplaceNodes(updatedNodes);
    }

    var settings = new XmlWriterSettings
    {
      Indent = false,
      OmitXmlDeclaration = false,
      NewLineHandling = NewLineHandling.None
    };

    using var writer = XmlWriter.Create(saveFilePath, settings);
    document.Save(writer);
  }

  private static IEnumerable<XElement> FindParentElements(XDocument document, string orderParent)
  {
    if (document.Root == null)
      return Enumerable.Empty<XElement>();

    if (string.IsNullOrWhiteSpace(orderParent))
      return [document.Root];

    var pathParts = orderParent
      .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    IEnumerable<XElement> current = [document.Root];

    foreach (var part in pathParts)
    {
      current = current.SelectMany(element =>
        element.Elements().Where(child => child.Name.LocalName == part));
    }

    return current;
  }
}