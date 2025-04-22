using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace cvBotConsole
{
    public static class FileWorker
    {
        public static string GeneratePdf(ResumeData data)
        {
            string filePath = $"Resume_{data.Name}.pdf";
            var document = new PdfDocument();

            var headersFont = new XFont("Arial", 16, XFontStyleEx.Bold);
            var smallHeaderFont = new XFont("Arial", 14);
            var textFont = new XFont("Arial", 12);

            var writer = new PdfTextWriter(document, startX: 50, startY: 50, lineSpacing: 20, maxWidth: 500);

            writer.WriteLine(data.Name, headersFont, XBrushes.Black);
            writer.WriteLine($"Телефон: {data.PhoneNumber}", textFont, XBrushes.Black);
            writer.AddEmptyLine();

            writer.WriteLine("Опыт работы", headersFont, XBrushes.Black);
            writer.AddEmptyLine();

            foreach (var work in data.WorkPlaces)
            {
                writer.WriteLine($"{work.CompanyName}, {work.PositionName}", headersFont, XBrushes.DarkGray);
                writer.AddEmptyLine();

                writer.WriteLine($"{work.StartDate} - {work.EndDate}", smallHeaderFont, XBrushes.Black);
                writer.WriteMultiline(work.Experience, textFont);
                writer.AddEmptyLine();
            }

            writer.AddEmptyLine(2);

            writer.WriteLine("О себе", headersFont, XBrushes.Black);
            writer.WriteMultiline(data.About, textFont);

            document.Save(filePath);
            return filePath;
        }
    }
}
