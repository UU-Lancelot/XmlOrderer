using System.CommandLine;

namespace UU.Lancelot.XmlOrderer;

public class Arguments
{
  public static Arguments Configure(OnArgumentsParsed onArgumentsParsed)
  {
    RootCommand rootCommand = new("XmlOrderer - A tool to order XML elements based on a specified attribute.");
    ConfigureArguments(ref rootCommand);

    var arguments = new Arguments(rootCommand);
    arguments.SetAction(onArgumentsParsed);

    return arguments;
  }

  private static void ConfigureArguments(ref RootCommand rootCommand)
  {
    Argument<FileInfo> inputFilePathArgument = new("input")
    {
      Arity = ArgumentArity.ExactlyOne,
      Description = "The path to the input XML file."
    };
    rootCommand.Arguments.Add(inputFilePathArgument);

    Argument<string> orderElementArgument = new("order")
    {
      Arity = ArgumentArity.ExactlyOne,
      Description = "The element to order, specified in the format <orderElement>[<orderByAttribute>]. For example: 'Root/Item[Name]'."
    };
    rootCommand.Arguments.Add(orderElementArgument);
    
    Argument<FileInfo> outputFilePathArgument = new("output")
    {
      Arity = ArgumentArity.ZeroOrOne,
      Description = "The path to the output XML file. If not provided, the input file will be overwritten."
    };
    rootCommand.Arguments.Add(outputFilePathArgument);
  }

  public Arguments(RootCommand rootCommand)
  {
    _rootCommand = rootCommand;
  }

  private RootCommand _rootCommand;

  public void Invoke(string[] args)
  {
    ParseResult parseResult = _rootCommand.Parse(args);
    parseResult.Invoke();
  }

  private void SetAction(OnArgumentsParsed onArgumentsParsed)
  {
    _rootCommand.SetAction(result =>
    {
      string inputFilePath = result.GetRequiredValue<FileInfo>("input").FullName;
      string orderElement = result.GetRequiredValue<string>("order");
      string outputFilePath = result.GetValue<FileInfo>("output")?.FullName ?? inputFilePath;

      ParseOrderElement(orderElement, out string orderParent, out string orderElementName, out string orderByAttribute);
      onArgumentsParsed(inputFilePath, outputFilePath, orderParent, orderElementName, orderByAttribute);
    });
  }

  private void ParseOrderElement(string orderElement, out string orderParent, out string orderElementName, out string orderByAttribute)
  {
    var parts = orderElement.Split('[', ']');
    if (parts.Length != 3)
      throw new ArgumentException("Order element must have the format <orderElement>[<orderByAttribute>].");

    var pathParts = parts[0].Split('/');
    orderParent = string.Join("/", pathParts.Take(pathParts.Length - 1));
    orderElementName = pathParts.Last();
    orderByAttribute = parts[1];
  }

  public delegate void OnArgumentsParsed(string inputFilePath, string outputFilePath, string orderParent, string orderElementName, string orderByAttribute);
}