using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class Program
{
    static void Main()
    {
        //MakeRowImage("row_increasing.fits");
        //MakeColumnImage("col_increasing.fits");
        //Console.WriteLine("FITS files created!");
        Mat img = Cv2.ImRead("testImage1.png", ImreadModes.Grayscale);

        Stopwatch sw = new Stopwatch();
        sw.Start();
        SaveMatAsFits(
            img,
            "testOutput.fits",
            telescopeName: "MyScope-200mm",
            obsTime: DateTime.UtcNow,
            exposure: 120.0,
            gain: 1.5
        );
        sw.Stop();
        Console.WriteLine($"Time taken: {sw.Elapsed.TotalMilliseconds} ms");
    }

    public static void SaveMatAsFits(
        Mat img,
        string filename,
        string telescopeName,
        DateTime obsTime,
        double exposure,
        double gain)
    {
        if (img.Empty())
            throw new ArgumentException("Input image is empty!");

        int width = img.Cols;
        int height = img.Rows;

        IntPtr fptr = IntPtr.Zero;
        int status = 0;

        // 建立 FITS 檔案（! 表示若存在則覆寫）
        Cfitsio.fits_create_file(ref fptr, "!" + filename, ref status);
        CheckStatus(status);

        // 建立影像，這裡選擇 16-bit short
        int[] naxes = { width, height };
        Cfitsio.fits_create_img(fptr, Cfitsio.SHORT_IMG, 2, naxes, ref status);
        CheckStatus(status);

        // 影像資料轉換
        short[] pixels = new short[width * height];
        unsafe
        {
            byte* ptr = (byte*)img.DataPointer;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte value = ptr[y * width + x];   // 0–255 灰階
                    pixels[y * width + x] = (short)value;
                }
            }
        }

        // 寫入影像資料
        GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            Cfitsio.fits_write_img(fptr, Cfitsio.TSHORT, 1, pixels.Length, ptr, ref status);
            CheckStatus(status);
        }
        finally
        {
            handle.Free();
        }

        // 寫入一些 Header 關鍵字
        Cfitsio.fits_update_key_str(fptr, "TELESCOP", telescopeName, "Telescope name", ref status);
        CheckStatus(status);

        // 觀測時間 (ISO 8601 格式)
        string dateObs = obsTime.ToString("yyyy-MM-ddTHH:mm:ss");
        Cfitsio.fits_update_key_str(fptr, "DATE-OBS", dateObs, "Observation time UTC", ref status);
        CheckStatus(status);

        Cfitsio.fits_update_key_dbl(fptr, "EXPOSURE", exposure, 3, "Exposure time (s)", ref status);
        CheckStatus(status);

        Cfitsio.fits_update_key_dbl(fptr, "GAIN", gain, 3, "Gain (e-/ADU)", ref status);
        CheckStatus(status);

        // 關閉 FITS
        Cfitsio.fits_close_file(fptr, ref status);
        CheckStatus(status);

        Console.WriteLine($"FITS file saved: {filename}");
    }

    static void MakeRowImage(string filename)
    {
        IntPtr fptr = IntPtr.Zero;
        int status = 0;

        // 建立 FITS 檔案
        Cfitsio.fits_create_file(ref fptr, "!" + filename, ref status);
        CheckStatus(status);

        // 建立影像 (255x255, 16-bit int)
        int[] naxes = { 255, 255 };
        Cfitsio.fits_create_img(fptr, Cfitsio.SHORT_IMG, 2, naxes, ref status);
        CheckStatus(status);

        // 填資料：row 增加
        short[] image = new short[255 * 255];
        for (int row = 0; row < 255; row++)
        {
            for (int col = 0; col < 255; col++)
            {
                image[row * 255 + col] = (short)(row + 1);
            }
        }

        // pin 陣列並寫入
        GCHandle handle = GCHandle.Alloc(image, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            Cfitsio.fits_write_img(fptr, Cfitsio.TSHORT, 1, image.Length, ptr, ref status);
            CheckStatus(status);
        }
        finally
        {
            handle.Free();
        }

        // 關閉
        Cfitsio.fits_close_file(fptr, ref status);
        CheckStatus(status);
    }

    static void MakeColumnImage(string filename)
    {
        IntPtr fptr = IntPtr.Zero;
        int status = 0;

        // 建立 FITS 檔案
        Cfitsio.fits_create_file(ref fptr, "!" + filename, ref status);
        CheckStatus(status);

        // 建立影像 (255x255, 16-bit int)
        int[] naxes = { 255, 255 };
        Cfitsio.fits_create_img(fptr, Cfitsio.SHORT_IMG, 2, naxes, ref status);
        CheckStatus(status);

        // 填資料：column 增加
        short[] image = new short[255 * 255];
        for (int row = 0; row < 255; row++)
        {
            for (int col = 0; col < 255; col++)
            {
                image[row * 255 + col] = (short)(col + 1);
            }
        }

        // pin 陣列並寫入
        GCHandle handle = GCHandle.Alloc(image, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            Cfitsio.fits_write_img(fptr, Cfitsio.TSHORT, 1, image.Length, ptr, ref status);
            CheckStatus(status);
        }
        finally
        {
            handle.Free();
        }

        // 關閉
        Cfitsio.fits_close_file(fptr, ref status);
        CheckStatus(status);
    }

    static void CheckStatus(int status)
    {
        if (status != 0)
        {
            Console.WriteLine("CFITSIO Error, status=" + status);
            IntPtr nullStream = IntPtr.Zero;
            Cfitsio.fits_report_error(nullStream, status);
            Environment.Exit(1);
        }
    }
}
