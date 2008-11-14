using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OptiVectroid
{
   class ImageUtility
   {
      public static byte[] GetBitmapData( Bitmap image )
      {
         System.Drawing.Imaging.BitmapData data = image.LockBits( new Rectangle( 0, 0, image.Width, image.Height ), System.Drawing.Imaging.ImageLockMode.ReadOnly, image.PixelFormat );
         byte[] buffer = new byte[data.Stride * data.Height];
         System.Runtime.InteropServices.Marshal.Copy( data.Scan0, buffer, 0, buffer.Length );
         return buffer;
      }

   }
}
