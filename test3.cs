using System;

class Program {
    static void Main() {
        string fileName = "..";
        if (fileName == "." || fileName == ".." || string.IsNullOrEmpty(fileName)) {
            Console.WriteLine("Invalid filename");
        }
    }
}
