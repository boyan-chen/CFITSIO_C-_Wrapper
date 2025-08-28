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
        Mat img = Cv2.ImRead("test_image.png", ImreadModes.Grayscale);

        Mat mat16 = new Mat(img.Size(), MatType.CV_16UC1, Scalar.All(0)); // Create a 16-bit image with the same size
        img.ConvertTo(mat16, MatType.CV_16UC1, 32767.0 / 256.0); // Convert the 8-bit image to 16-bit

        Stopwatch sw = new Stopwatch();
        sw.Start();
        /*
        SaveMatAsFits(
            img,
            "testOutput.fits",
            telescopeName: "MyScope-200mm",
            obsTime: DateTime.UtcNow,
            exposure: 120.0,
            gain: 1.5
        );
        */
        SaveFits16bit("testOutput16.fits",
            mat16,
            telescope: "C11",
            exposureTime: 0.5,
            gain: 240,
            binX: 2,
            binY: 2,
            img_ts: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            satname: "ISS",
            tle1: "1 20580U 90037B   24075.12345678  .00000023  00000+0  12345-4 0  9991",
            tle2: "2 20580  28.4696 123.4567 0001234 234.5678 345.6789 15.12345678901234"
        );
        sw.Stop();
        Console.WriteLine($"Time taken: {sw.Elapsed.TotalMilliseconds} ms");
    }

    public static void SaveFits16bit(string filePath, Mat mat, string telescope, double exposureTime,
                                                        double gain, int binX, int binY, long img_ts, string satname, string tle1, string tle2)
    {
        if (mat.Empty())
            throw new ArgumentException("Mat is empty.");
        if (mat.Channels() != 1)
            throw new ArgumentException("Only single-channel grayscale Mats are supported.");

        int width = mat.Cols;
        int height = mat.Rows;

        IntPtr fptr = IntPtr.Zero;
        int status = 0;

        Cfitsio.fits_create_file(ref fptr, "!" + filePath, ref status);
        Console.WriteLine($"Creating FITS file: {filePath}");
        CheckStatus(status);

        int[] naxes = new int[] { width, height };
        Cfitsio.fits_create_img(fptr, Cfitsio.SHORT_IMG, 2, naxes, ref status);
        Console.WriteLine($"Created FITS image with dimensions: {width}x{height}");
        CheckStatus(status);
        /*
        ushort[] data_16bit = new ushort[width * height];
        unsafe
        {
            ushort* p = (ushort*)mat.DataPointer;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    data_16bit[y * width + x] = *(p + y * (mat.Step() / 2) + x);
                }
            }
        }
        */

        // 將 ushort[] 改為 short[]
        short[] data_16bit = new short[width * height];
        unsafe
        {
            ushort* p = (ushort*)mat.DataPointer;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 將 ushort 轉成 short，並限制在 short 範圍
                    ushort val = *(p + y * (mat.Step() / 2) + x);
                    data_16bit[y * width + x] = (short)Math.Min(val, short.MaxValue);
                }
            }
        }

        GCHandle handle = GCHandle.Alloc(data_16bit, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            Cfitsio.fits_write_img(fptr, Cfitsio.TSHORT, 1, width * height, ptr, ref status);
            Console.WriteLine("Wrote image data to FITS file.");
            CheckStatus(status);
        }
        finally
        {
            handle.Free();
        }

        Console.WriteLine("Adding FITS header keywords...");

        // Add FITS header keywords
        Cfitsio.fits_update_key_str(fptr, "TELESCOP", telescope, "Telescope used", ref status);
        CheckStatus(status);
        Cfitsio.fits_update_key_dbl(fptr, "EXPTIME", exposureTime, 2, "Exposure time (s)", ref status);
        CheckStatus(status);
        Cfitsio.fits_update_key_dbl(fptr, "GAIN", gain, 2, "Camera gain", ref status);
        CheckStatus(status);
        //Cfitsio.fits_update_key_dbl(fptr, "BITPIX", Cfitsio.SHORT_IMG, 0, "Number of bits per pixel (16)", ref status);
        //CheckStatus(status);
        //Cfitsio.fits_update_key_dbl(fptr, "NAXIS1", width, 0, "Image width", ref status);
        //CheckStatus(status);
        //Cfitsio.fits_update_key_dbl(fptr, "NAXIS2", height, 0, "Image height", ref status);
        //CheckStatus(status);
        Cfitsio.fits_update_key_dbl(fptr, "XBINNING", binX, 0, "Binning factor in X", ref status);
        CheckStatus(status);
        Cfitsio.fits_update_key_dbl(fptr, "YBINNING", binY, 0, "Binning factor in Y", ref status);
        CheckStatus(status);
        Cfitsio.fits_update_key_str(fptr, "BINNING", $"{binX}x{binY}", "Binning", ref status);
        CheckStatus(status);
        Cfitsio.fits_update_key_str(fptr, "DATE-OBS", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"), "UTC date/time of observation", ref status);
        CheckStatus(status);
        Cfitsio.fits_update_key_str(fptr, "SATNAME", satname, "Satellite name", ref status);
        CheckStatus(status);
        Cfitsio.fits_update_key_str(fptr, "TLE1", tle1, "Satellite TLE line 1", ref status);
        CheckStatus(status);
        Cfitsio.fits_update_key_str(fptr, "TLE2", tle2, "Satellite TLE line 2", ref status);
        CheckStatus(status);

        Cfitsio.fits_close_file(fptr, ref status);
        Console.WriteLine("Closed FITS file.");
        CheckStatus(status);
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
