using System;
using System.IO;

class Program
{
    static void Main()
    {
        string src = @"c:\Users\user\AppData\Roaming\Code\User\globalStorage\github.copilot-chat\copilot-cli-images\1776257074868-p0kq1fk6.png";
        string dst = @"c:\Users\user\Desktop\Monogame\MyGame\Content\level1.png";
        
        try
        {
            File.Copy(src, dst, true);
            Console.WriteLine($"✓ Файл успешно скопирован в {dst}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Ошибка: {ex.Message}");
        }
    }
}
