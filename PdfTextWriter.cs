using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Text;

namespace cvBotConsole
{
    public class PdfTextWriter
    {
        private readonly PdfDocument document;
        private XGraphics gfx;
        private PdfPage currentPage;

        private readonly int startX;
        private int currentY;
        private readonly int lineSpacing;
        private readonly int marginBottom;

        private readonly int maxWidth;

        public PdfTextWriter(PdfDocument doc, int startX, int startY, int lineSpacing, int maxWidth, int marginBottom = 50)
        {
            this.document = doc;
            this.startX = startX;
            this.currentY = startY;
            this.lineSpacing = lineSpacing;
            this.maxWidth = maxWidth;
            this.marginBottom = marginBottom;

            CreateNewPage(); // первая страница
        }

        private void CreateNewPage()
        {
            currentPage = document.AddPage();
            gfx = XGraphics.FromPdfPage(currentPage);
            currentY = 50; // верхний отступ
        }

        private void CheckForNewPage()
        {
            if (currentY + lineSpacing > currentPage.Height - marginBottom)
            {
                CreateNewPage();
            }
        }

        public void WriteLine(string text, XFont font, XBrush brush)
        {
            CheckForNewPage();
            gfx.DrawString(text, font, brush, new XPoint(startX, currentY));
            currentY += lineSpacing;
        }

        public void WriteMultiline(string text, XFont font)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var lines = WrapText(text, gfx, font, maxWidth);

            foreach (var line in lines)
            {
                CheckForNewPage();
                gfx.DrawString(line, font, XBrushes.Black, new XPoint(startX, currentY));
                currentY += lineSpacing;
            }
        }

        public void AddEmptyLine(int count = 1)
        {
            currentY += lineSpacing * count;
        }

        private List<string> WrapText(string text, XGraphics gfx, XFont font, int maxWidth)
        {
            var lines = new List<string>();
            var words = text.Split(' ');
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                var testLine = currentLine + word + " ";
                if (gfx.MeasureString(testLine, font).Width > maxWidth)
                {
                    lines.Add(currentLine.ToString().Trim());
                    currentLine.Clear();
                }

                currentLine.Append(word + " ");
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString().Trim());
            }

            return lines;
        }
    }

}
