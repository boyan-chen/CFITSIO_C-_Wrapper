using System;
using System.Runtime.InteropServices;
using System.Text;

public static class Cfitsio
{
    private const string DLL_NAME = "cfitsio.dll";

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "ffinit", CharSet = CharSet.Ansi)]
    public static extern int fits_create_file(ref IntPtr fptr, string filename, ref int status);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "ffcrim", CharSet = CharSet.Ansi)]
    public static extern int fits_create_img(IntPtr fptr, int bitpix, int naxis, [In] int[] naxes, ref int status);

    /* note */
    /* In CFITSIO fits_create_img (or say ffcrim) is form with args like 
    int fits_create_img(fitsfile *fptr, int bitpix, int naxis, long *naxes, int *status);
    yet, in C#, long = 64 bits, while in C, long = 32 bits (on Windows [MSCV]).
    So here we use int[] for naxes instead of long[].
    */

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "ffppr", CharSet = CharSet.Ansi)]
    public static extern int fits_write_img(IntPtr fptr, int datatype, long firstelem, long nelements, IntPtr array, ref int status);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "ffukls", CharSet = CharSet.Ansi)]
    public static extern int fits_update_key_str(IntPtr fptr, string keyname, string value, string comment, ref int status);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "ffukyd", CharSet = CharSet.Ansi)]
    public static extern int fits_update_key_dbl(IntPtr fptr, string keyname, double value, int decimals, string comment, ref int status);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "ffclos", CharSet = CharSet.Ansi)]
    public static extern int fits_close_file(IntPtr fptr, ref int status);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "ffrprt", CharSet = CharSet.Ansi)]
    public static extern void fits_report_error(IntPtr stream, int status);

    // CFITSIO datatype mapping
    public const int TBYTE = 11;
    public const int TSHORT = 21;
    public const int TLONG = 41;
    public const int TFLOAT = 42;
    public const int TDOUBLE = 82;

    // BITPIX values
    public const int BYTE_IMG = 8;
    public const int SHORT_IMG = 16;
    public const int LONG_IMG = 32;
    public const int FLOAT_IMG = -32;
    public const int DOUBLE_IMG = -64;
}
