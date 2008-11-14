using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace OptiVectroid
{
   public static class Keyboard
   {
      [System.Runtime.InteropServices.DllImport( "user32.dll" )]
      static extern byte GetKeyState( int virtualKey );

      [System.Runtime.InteropServices.DllImport( "user32.dll" )]
      static extern bool GetKeyboardState( byte[] keyStates );

      static readonly byte[] prevKeyStates = new byte[256];
      static readonly byte[] currKeyStates = new byte[256];

      public static void Update( float elapsed )
      {
         currKeyStates.CopyTo( prevKeyStates, 0 );
         GetKeyboardState( currKeyStates );
      }

      public static bool IsKeyDown( Keys key )
      {
         return ( currKeyStates[(int)key] & 0xf0 ) > 0;
      }

      public static bool IsKeyPressed( Keys key )
      {
         // check that previous state was up, and current state is down
         return ( prevKeyStates[(int)key] & 0xf0 ) == 0 && ( currKeyStates[(int)key] & 0xf0 ) > 0;
      }
   }
}