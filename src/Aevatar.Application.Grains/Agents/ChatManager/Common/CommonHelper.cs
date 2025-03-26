using System.Security.Cryptography;
using System.Text;

namespace Aevatar.Application.Grains.Agents.ChatManager.Common;

public class CommonHelper
{
    public static Guid StringToGuid(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(hash);
        }
    }

    public static Guid GetSessionManagerConfigurationId()
    {
        return StringToGuid("GetStreamSessionManagerConfigurationId15");
    }
    
    /// <summary>
    /// A method to load the content of a file.
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>Returns the complete content of the file as a string</returns>
    public static string LoadFileContent(string filePath)  
    {  
        // Check if the file exists  
        if (!File.Exists(filePath))  
        {  
            throw new FileNotFoundException($"File not found: {filePath}");  
        }  
  
        // Use a StringBuilder to efficiently concatenate strings  
        StringBuilder fileContent = new StringBuilder();  
        using (StreamReader reader = new StreamReader(filePath))  
        {  
            string? line;  
            while ((line = reader.ReadLine()) != null)  
            {  
                fileContent.Append(line);  
            }  
        }  
  
        // Return the concatenated content, trimming the last '\n' if needed  
        return fileContent.ToString().TrimEnd('\n');  
    }
    
    /// <summary>
    /// A method to load file content from the current directory.
    /// </summary>
    /// <param name="fileName">The name of the file located in the current directory</param>
    /// <returns>Returns the complete content of the file as a string</returns>
    public static string LoadFileFromCurrentDirectory(string fileName)
    {
        string currentDirectory = Directory.GetCurrentDirectory();

        string fullPath = Path.Combine(currentDirectory, fileName);

        return LoadFileContent(fullPath);
    }

  
}