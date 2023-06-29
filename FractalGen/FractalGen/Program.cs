using FractalGen;

public class Program
{
    public static async Task Main(String[] args)
    {
        Console.WriteLine("Hello world!");
        var startTime = DateTime.Now;
        var bdg = new BifurcationDiagramGenerator(
            "C:\\Temp\\BifurcationDiagram.png",
            imageSize: 1000
            );
        await bdg.RenderBifurcationDiagram();
        var endTime = DateTime.Now;
        Console.WriteLine($"Ran in {(endTime - startTime).TotalSeconds} seconds");
        Console.ReadLine();
    }
}