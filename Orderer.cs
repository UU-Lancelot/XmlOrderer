using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace UU.Lancelot.XmlOrderer;

public class Orderer
{
  public static void Order(string loadFilePath, string saveFilePath, IEnumerable<OrderInfo> orderInfo, bool normalizeHql)
  {
    var orderer = new Orderer(orderInfo);

    XDocument document = orderer.LoadDocument(loadFilePath);
    orderer.Order(ref document);
    if (normalizeHql)
      orderer.NormalizeHql(document);
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

  public void NormalizeHql(XDocument document)
  {
    var textNodes = document
      .DescendantNodes()
      .OfType<XText>()
      .Where(text => SqlStartRegex.IsMatch(text.Value))
      .ToList();

    foreach (var text in textNodes)
    {
      string normalized = WhitespaceRegex.Replace(text.Value, " ").Trim();
      if (normalized == text.Value)
        continue;

      Console.WriteLine($"Normalizing HQL in element: <{text.Parent?.Name.LocalName}>");
      text.Value = normalized;
    }
  }

  private static readonly Regex SqlStartRegex = new(
    @"^\s*(hql\s*;\s*)?select\s+\S.*?\s+from\s+[\w\[\]""`.]+",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

  private static readonly Regex WhitespaceRegex = new(@"\s+");

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