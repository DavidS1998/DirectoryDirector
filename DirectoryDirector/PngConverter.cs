using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DirectoryDirector;

public static class PngConverter
{
    // Based on the following WPF code: https://gist.github.com/darkfall/1656050?permalink_comment_id=4920979
    // Converted to support the WinUI 3.0 syntax and APIs
    
    public static async Task<bool> Convert(string inputImagePath, string outputIconPath, int size,
        bool keepAspectRatio = false)
    {
        try
        {
            // Load the input image
            StorageFile inputFile = await StorageFile.GetFileFromPathAsync(inputImagePath);
            using IRandomAccessStream inputStream = await inputFile.OpenReadAsync();
            
            // Decode the input image
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(inputStream);
            BitmapTransform transform = new BitmapTransform();
            BitmapPixelFormat pixelFormat = decoder.BitmapPixelFormat;
            BitmapAlphaMode alphaMode = decoder.BitmapAlphaMode;
            uint aspectRatioWidth = keepAspectRatio ? (uint)size : 0;
            uint aspectRatioHeight = keepAspectRatio ? (uint)(size * decoder.PixelHeight / decoder.PixelWidth) : 0;
            transform.ScaledWidth = aspectRatioWidth > 0 ? aspectRatioWidth : (uint)size;
            transform.ScaledHeight = aspectRatioHeight > 0 ? aspectRatioHeight : (uint)size;

            // Create a new software bitmap based on the input image and transformation
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(pixelFormat, alphaMode, transform,
                ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);

            // Create a new storage file for the output icon
            StorageFolder outputFolder = Path.GetDirectoryName(outputIconPath) != null
                ? await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(outputIconPath))
                : throw new DirectoryNotFoundException("Output folder does not exist");
            StorageFile outputFile = await outputFolder.CreateFileAsync(Path.GetFileName(outputIconPath), CreationCollisionOption.GenerateUniqueName);
            using IRandomAccessStream outputStream = await outputFile.OpenAsync(FileAccessMode.ReadWrite);
            
            // Create an ICO file with a single icon image
            using (BinaryWriter writer = new BinaryWriter(outputStream.AsStreamForWrite()))
            {
                // Write the ICO file header
                writer.Write((ushort)0); // Reserved, must be 0
                writer.Write((ushort)1); // Type: 1 for icon, 2 for cursor
                writer.Write((ushort)1); // Number of images in the ICO file

                // Get the PNG data from the software bitmap
                byte[] pngData;
                using (InMemoryRandomAccessStream pngStream = new InMemoryRandomAccessStream())
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, pngStream);
                    encoder.SetSoftwareBitmap(softwareBitmap);
                    await encoder.FlushAsync();
                    pngStream.Seek(0);
                    pngData = new byte[pngStream.Size];
                    await pngStream.ReadAsync(pngData.AsBuffer(), (uint)pngStream.Size, InputStreamOptions.None);
                }

                // Write the icon image entry
                writer.Write((byte)transform.ScaledWidth); // Image width
                writer.Write((byte)transform.ScaledHeight); // Image height
                writer.Write((byte)0); // Color count (0 for true color)
                writer.Write((byte)0); // Reserved (must be 0)
                writer.Write((short)1); // Color planes (must be 1)
                writer.Write((short)32); // Bits per pixel
                writer.Write(pngData.Length); // Image data size
                writer.Write(22); // Image data offset (ICO header size + icon entry size)
                writer.Write(pngData); // Image data
            }
            
            return true;
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            Debug.WriteLine("Error converting image to icon: " + ex.Message);
            return false;
        }
    }
}