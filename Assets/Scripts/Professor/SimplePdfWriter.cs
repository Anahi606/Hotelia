using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class SimplePdfWriter
{
    private class ImagePageInfo
    {
        public Texture2D texture;
        public int pageObjectId;
        public int contentObjectId;
        public int imageObjectId;
    }

    private class TextPageInfo
    {
        public List<string> lines;
        public int pageObjectId;
        public int contentObjectId;
    }

    public static void SaveTexturesAsPdf(IList<Texture2D> pageTextures, string path, int jpgQuality = 92)
    {
        SaveTexturesAndFeedbackAsPdf(pageTextures, path, null, null, jpgQuality);
    }

    public static void SaveTexturesAndFeedbackAsPdf(
        IList<Texture2D> pageTextures,
        string path,
        string feedbackTitle,
        string feedbackText,
        int jpgQuality = 92
    )
    {
        List<ImagePageInfo> imagePages = new List<ImagePageInfo>();

        if (pageTextures != null)
        {
            foreach (Texture2D texture in pageTextures)
            {
                if (texture == null)
                    continue;

                imagePages.Add(new ImagePageInfo
                {
                    texture = texture
                });
            }
        }

        List<List<string>> feedbackPages = BuildFeedbackPages(feedbackTitle, feedbackText);

        if (imagePages.Count == 0 && feedbackPages.Count == 0)
            throw new ArgumentException("No PDF content was provided.");

        int nextObjectId = 3;

        foreach (ImagePageInfo page in imagePages)
        {
            page.pageObjectId = nextObjectId++;
            page.contentObjectId = nextObjectId++;
            page.imageObjectId = nextObjectId++;
        }

        int fontObjectId = 0;
        List<TextPageInfo> textPages = new List<TextPageInfo>();

        if (feedbackPages.Count > 0)
        {
            fontObjectId = nextObjectId++;

            foreach (List<string> lines in feedbackPages)
            {
                textPages.Add(new TextPageInfo
                {
                    lines = lines,
                    pageObjectId = nextObjectId++,
                    contentObjectId = nextObjectId++
                });
            }
        }

        int objectCount = nextObjectId - 1;
        long[] offsets = new long[objectCount + 1];

        using (MemoryStream stream = new MemoryStream())
        {
            WriteAscii(stream, "%PDF-1.4\n");

            string kids = "";

            foreach (ImagePageInfo page in imagePages)
                kids += page.pageObjectId + " 0 R ";

            foreach (TextPageInfo page in textPages)
                kids += page.pageObjectId + " 0 R ";

            int totalPageCount = imagePages.Count + textPages.Count;

            WriteObject(stream, offsets, 1, "<< /Type /Catalog /Pages 2 0 R >>");
            WriteObject(stream, offsets, 2, "<< /Type /Pages /Kids [" + kids + "] /Count " + totalPageCount + " >>");

            foreach (ImagePageInfo page in imagePages)
                WriteImagePage(stream, offsets, page, jpgQuality);

            if (textPages.Count > 0)
            {
                WriteObject(
                    stream,
                    offsets,
                    fontObjectId,
                    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
                );

                foreach (TextPageInfo page in textPages)
                    WriteTextPage(stream, offsets, page, fontObjectId);
            }

            long xrefPosition = stream.Position;

            WriteAscii(stream, "xref\n");
            WriteAscii(stream, "0 " + (objectCount + 1) + "\n");
            WriteAscii(stream, "0000000000 65535 f \n");

            for (int i = 1; i <= objectCount; i++)
                WriteAscii(stream, offsets[i].ToString("D10") + " 00000 n \n");

            WriteAscii(stream,
                "trailer\n" +
                "<< /Size " + (objectCount + 1) + " /Root 1 0 R >>\n" +
                "startxref\n" +
                xrefPosition + "\n" +
                "%%EOF"
            );

            File.WriteAllBytes(path, stream.ToArray());
        }

        Debug.Log("PDF saved with report images and feedback page: " + path);
    }

    private static void WriteImagePage(MemoryStream stream, long[] offsets, ImagePageInfo page, int jpgQuality)
    {
        Texture2D texture = page.texture;

        bool landscape = texture.width > texture.height;

        float pageWidth = landscape ? 842f : 595f;
        float pageHeight = landscape ? 595f : 842f;
        float margin = 32f;

        float availableWidth = pageWidth - margin * 2f;
        float availableHeight = pageHeight - margin * 2f;

        float imageAspect = texture.width / (float)texture.height;

        float drawWidth = availableWidth;
        float drawHeight = drawWidth / imageAspect;

        if (drawHeight > availableHeight)
        {
            drawHeight = availableHeight;
            drawWidth = drawHeight * imageAspect;
        }

        float drawX = (pageWidth - drawWidth) / 2f;
        float drawY = (pageHeight - drawHeight) / 2f;

        string imageName = "Im" + page.imageObjectId;

        string pageObject =
            "<< /Type /Page " +
            "/Parent 2 0 R " +
            "/MediaBox [0 0 " + F(pageWidth) + " " + F(pageHeight) + "] " +
            "/Resources << /XObject << /" + imageName + " " + page.imageObjectId + " 0 R >> >> " +
            "/Contents " + page.contentObjectId + " 0 R >>";

        WriteObject(stream, offsets, page.pageObjectId, pageObject);

        string content =
            "q\n" +
            F(drawWidth) + " 0 0 " + F(drawHeight) + " " + F(drawX) + " " + F(drawY) + " cm\n" +
            "/" + imageName + " Do\n" +
            "Q";

        byte[] contentBytes = Encoding.ASCII.GetBytes(content);

        WriteStreamObject(
            stream,
            offsets,
            page.contentObjectId,
            "<< /Length " + contentBytes.Length + " >>",
            contentBytes
        );

        byte[] jpgBytes = texture.EncodeToJPG(jpgQuality);

        string imageDictionary =
            "<< /Type /XObject " +
            "/Subtype /Image " +
            "/Width " + texture.width + " " +
            "/Height " + texture.height + " " +
            "/ColorSpace /DeviceRGB " +
            "/BitsPerComponent 8 " +
            "/Filter /DCTDecode " +
            "/Length " + jpgBytes.Length + " >>";

        WriteStreamObject(stream, offsets, page.imageObjectId, imageDictionary, jpgBytes);
    }

    private static void WriteTextPage(MemoryStream stream, long[] offsets, TextPageInfo page, int fontObjectId)
    {
        float pageWidth = 595f;
        float pageHeight = 842f;
        float margin = 54f;
        float y = pageHeight - margin;

        string pageObject =
            "<< /Type /Page " +
            "/Parent 2 0 R " +
            "/MediaBox [0 0 " + F(pageWidth) + " " + F(pageHeight) + "] " +
            "/Resources << /Font << /F1 " + fontObjectId + " 0 R >> >> " +
            "/Contents " + page.contentObjectId + " 0 R >>";

        WriteObject(stream, offsets, page.pageObjectId, pageObject);

        StringBuilder content = new StringBuilder();

        content.Append("BT\n");

        for (int i = 0; i < page.lines.Count; i++)
        {
            string line = page.lines[i] ?? "";

            bool isTitle = i == 0;
            float fontSize = isTitle ? 18f : 11f;
            float lineHeight = isTitle ? 28f : 16f;

            content.Append("/F1 ");
            content.Append(F(fontSize));
            content.Append(" Tf\n");

            content.Append("1 0 0 1 ");
            content.Append(F(margin));
            content.Append(" ");
            content.Append(F(y));
            content.Append(" Tm\n");

            content.Append("(");
            content.Append(EscapePdfText(line));
            content.Append(") Tj\n");

            y -= lineHeight;
        }

        content.Append("ET");

        byte[] contentBytes = Encoding.ASCII.GetBytes(content.ToString());

        WriteStreamObject(
            stream,
            offsets,
            page.contentObjectId,
            "<< /Length " + contentBytes.Length + " >>",
            contentBytes
        );
    }

    private static List<List<string>> BuildFeedbackPages(string feedbackTitle, string feedbackText)
    {
        List<List<string>> pages = new List<List<string>>();

        if (string.IsNullOrWhiteSpace(feedbackText))
            return pages;

        List<string> allLines = new List<string>();

        if (string.IsNullOrWhiteSpace(feedbackTitle))
            allLines.Add("Learning Feedback");
        else
            allLines.Add(feedbackTitle.Trim());

        allLines.Add("");

        string cleanText = feedbackText.Replace("\r", "");
        string[] rawLines = cleanText.Split('\n');

        foreach (string rawLine in rawLines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                allLines.Add("");
                continue;
            }

            List<string> wrappedLines = WrapText(rawLine.Trim(), 88);

            foreach (string wrappedLine in wrappedLines)
                allLines.Add(wrappedLine);
        }

        int maxLinesPerPage = 42;
        int index = 0;

        while (index < allLines.Count)
        {
            List<string> pageLines = new List<string>();

            for (int i = 0; i < maxLinesPerPage && index < allLines.Count; i++)
            {
                pageLines.Add(allLines[index]);
                index++;
            }

            pages.Add(pageLines);
        }

        return pages;
    }

    private static List<string> WrapText(string text, int maxChars)
    {
        List<string> lines = new List<string>();

        if (string.IsNullOrEmpty(text))
        {
            lines.Add("");
            return lines;
        }

        string[] words = text.Split(' ');
        string currentLine = "";

        foreach (string word in words)
        {
            if (string.IsNullOrEmpty(word))
                continue;

            if (word.Length > maxChars)
            {
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = "";
                }

                int start = 0;

                while (start < word.Length)
                {
                    int length = Mathf.Min(maxChars, word.Length - start);
                    lines.Add(word.Substring(start, length));
                    start += length;
                }

                continue;
            }

            if (string.IsNullOrEmpty(currentLine))
            {
                currentLine = word;
            }
            else if (currentLine.Length + 1 + word.Length <= maxChars)
            {
                currentLine += " " + word;
            }
            else
            {
                lines.Add(currentLine);
                currentLine = word;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines;
    }

    private static string EscapePdfText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        return text
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }

    private static void WriteObject(MemoryStream stream, long[] offsets, int objectId, string content)
    {
        offsets[objectId] = stream.Position;
        WriteAscii(stream, objectId + " 0 obj\n");
        WriteAscii(stream, content + "\n");
        WriteAscii(stream, "endobj\n");
    }

    private static void WriteStreamObject(MemoryStream stream, long[] offsets, int objectId, string dictionary, byte[] data)
    {
        offsets[objectId] = stream.Position;
        WriteAscii(stream, objectId + " 0 obj\n");
        WriteAscii(stream, dictionary + "\n");
        WriteAscii(stream, "stream\n");
        stream.Write(data, 0, data.Length);
        WriteAscii(stream, "\nendstream\n");
        WriteAscii(stream, "endobj\n");
    }

    private static void WriteAscii(Stream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}