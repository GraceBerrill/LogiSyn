
using System;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;

class Program {
    static void Main() {
        string path = @"C:\Users\matth\Downloads\Test_Order_Spar_2026.pdf";
        using var doc = PdfDocument.Open(path);
        foreach (var page in doc.GetPages()) {
            Console.WriteLine("PAGE TEXT:");
            Console.WriteLine(page.Text);
        }
    }
}
