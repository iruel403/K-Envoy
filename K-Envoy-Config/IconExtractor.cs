using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace K_Envoy
{
    /// <summary>
    /// Utility class for extracting high-quality icons from executable files and image files.
    /// </summary>
    public class IconExtractor
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Extracts the largest available icon from an executable file.
        /// </summary>
        public static Image ExtractIconFromExe(string exePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[IconExtractor] Starting extraction from: {exePath}");

                IntPtr[] largeIcons = new IntPtr[10];
                IntPtr[] smallIcons = new IntPtr[10];

                uint iconCount = ExtractIconEx(exePath, 0, largeIcons, smallIcons, (uint)largeIcons.Length);
                System.Diagnostics.Debug.WriteLine($"[IconExtractor] Found {iconCount} icons");

                if (iconCount > 0)
                {
                    Bitmap largestBitmap = null;
                    int largestArea = 0;

                    for (int i = 0; i < iconCount && i < largeIcons.Length; i++)
                    {
                        if (largeIcons[i] != IntPtr.Zero)
                        {
                            try
                            {
                                Icon icon = Icon.FromHandle(largeIcons[i]);
                                Bitmap bitmap = icon.ToBitmap();
                                int area = icon.Width * icon.Height;

                                if (area > largestArea)
                                {
                                    largestArea = area;
                                    largestBitmap?.Dispose();
                                    largestBitmap = bitmap;
                                }
                                else
                                {
                                    bitmap.Dispose();
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[IconExtractor] Error processing icon {i}: {ex.Message}");
                            }
                        }
                    }

                    for (int i = 0; i < iconCount && i < largeIcons.Length; i++)
                    {
                        if (largeIcons[i] != IntPtr.Zero)
                            DestroyIcon(largeIcons[i]);
                        if (i < smallIcons.Length && smallIcons[i] != IntPtr.Zero)
                            DestroyIcon(smallIcons[i]);
                    }

                    if (largestBitmap != null)
                        return largestBitmap;
                }

                Icon fallbackIcon = Icon.ExtractAssociatedIcon(exePath);
                if (fallbackIcon != null)
                {
                    Bitmap bitmap = fallbackIcon.ToBitmap();
                    fallbackIcon.Dispose();
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IconExtractor] Error: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Loads an image file and returns it as a bitmap.
        /// </summary>
        public static Image LoadImageFile(string imagePath)
        {
            try
            {
                Image img = Image.FromFile(imagePath);
                Bitmap bitmap = new Bitmap(img);
                img.Dispose();
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IconExtractor] Error loading image: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Loads an icon or image from a file path, handling both executables and image files.
        /// </summary>
        public static Image LoadIcon(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || 
                path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractIconFromExe(path);
            }
            else
            {
                return LoadImageFile(path);
            }
        }
    }
}
