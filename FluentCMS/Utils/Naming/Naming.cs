namespace FluentCMS.Utils.Naming;
using System.Globalization;

public class Naming
{
    public static string SnakeToTitle(string snakeStr)
    {
        // Split the snake_case string by underscores
        string[] components = snakeStr.Split('_');
        // Capitalize the first letter of each component and join them with spaces
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].Length > 0)
            {
                components[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(components[i]);
            }
        }
        return string.Join(" ", components);
    }
}