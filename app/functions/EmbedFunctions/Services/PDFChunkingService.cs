// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace EmbedFunctions.Services;
public class PDFChunkingService
{
    private BlobServiceClient _blobServiceClient;
    public PDFChunkingService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async ValueTask UploadBlobsAsync(Stream blobStream, string blobName)
    {
        var container = _blobServiceClient.GetBlobContainerClient("content");
        using var documents = PdfReader.Open(blobStream, PdfDocumentOpenMode.Import);
        for (int i = 0; i < documents.PageCount; i++)
        {
            var documentName = BlobNameFromFilePage(blobName, i);
            var blobClient = container.GetBlobClient(documentName);
            if (await blobClient.ExistsAsync())
            {
                continue;
            }

            var tempFileName = Path.GetTempFileName();

            try
            {
                using var document = new PdfDocument();
                document.AddPage(documents.Pages[i]);
                document.Save(tempFileName);

                await using var stream = File.OpenRead(tempFileName);
                await blobClient.UploadAsync(stream, new BlobHttpHeaders
                {
                    ContentType = "application/pdf"
                });
            }
            finally
            {
                File.Delete(tempFileName);
            }
        }
    }

    private async Task UploadBlobAsync(string fileName, string blobName, BlobContainerClient container)
    {
        var blobClient = container.GetBlobClient(blobName);
        if (await blobClient.ExistsAsync())
        {
            return;
        }

        var blobHttpHeaders = new BlobHttpHeaders
        {
            ContentType = GetContentType(fileName)
        };

        await using var fileStream = File.OpenRead(fileName);
        await blobClient.UploadAsync(fileStream, blobHttpHeaders);
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",

            _ => "application/octet-stream"
        };
    }

    private string BlobNameFromFilePage(string filename, int page = 0)
    {
        if (Path.GetExtension(filename).ToLower() is ".pdf")
        {
            return $"{Path.GetFileNameWithoutExtension(filename)}-{page}.pdf";
        }
        else
        {
            return Path.GetFileName(filename);
        }
    }
}
