using System;
using System.Collections.Generic;
using System.Text;

namespace OptiVectroid
{
   public struct Vector2D
   {
      public float X, Y;

      public Vector2D( float x, float y )
      {
         this.X = x;
         this.Y = y;
      }

      public Vector2D( Vector2D v )
      {
         this.X = v.X;
         this.Y = v.Y;
      }

      public static readonly Vector2D Zero = new Vector2D();

      public float Length()
      {
         return (float)Math.Sqrt( this.X * this.X + this.Y * this.Y );
      }

      public void Normalize()
      {
         float len = this.Length();
         this.X /= len;
         this.Y /= len;
      }

      public float Dot( Vector2D v )
      {
         return Dot( this, v );
      }

      /// <summary> Rotate the given vector about the origin by given angle in degrees. </summary>
      public void Rotate( float angle )
      {
         double radians = angle * Math.PI / 180f;
         float c = (float)Math.Cos( radians );
         float s = (float)Math.Sin( radians );
         float x = this.X;
         float y = this.Y;
         this.X = x * c - y * s;
         this.Y = x * s + y * c;
      }

      public void Translate( float x, float y )
      {
         this.X += x;
         this.Y += y;
      }

      public static Vector2D Add( Vector2D a, Vector2D b )
      {
         return new Vector2D( a.X + b.X, a.Y + b.Y );
      }

      public static Vector2D Subtract( Vector2D a, Vector2D b )
      {
         return new Vector2D( a.X - b.X, a.Y - b.Y );
      }

      public static Vector2D Multiply( Vector2D a, Vector2D b )
      {
         return new Vector2D( a.X * b.X, a.Y * b.Y );
      }

      public static Vector2D Scale( Vector2D v, float scale )
      {
         return new Vector2D( v.X * scale, v.Y * scale );
      }

      public static float Dot( Vector2D a, Vector2D b )
      {
         return a.X * b.X + a.Y * b.Y;
      }

      public static Vector2D Normalize( Vector2D v )
      {
         Vector2D normalized = new Vector2D( v );
         normalized.Normalize();
         return normalized;
      }

      public static float DistanceSq( Vector2D a, Vector2D b )
      {
         float x = a.X - b.X;
         float y = a.Y - b.Y;
         return x * x + y * y;
      }

      public static float Distance( Vector2D a, Vector2D b )
      {
         float x = a.X - b.X;
         float y = a.Y - b.Y;
         return (float)Math.Sqrt( x * x + y * y );
      }

      public static Vector2D operator -( Vector2D v )
      {
         return new Vector2D( -v.X, -v.Y );
      }

      public static Vector2D operator +( Vector2D a, Vector2D b )
      {
         return Add( a, b );
      }

      public static Vector2D operator -( Vector2D a, Vector2D b )
      {
         return Subtract( a, b );
      }

      public static Vector2D operator *( Vector2D a, Vector2D b )
      {
         return Multiply( a, b );
      }

      public static bool operator ==( Vector2D a, Vector2D b )
      {
         return a.X == b.Y && a.Y == b.Y;
      }

      public static bool operator !=( Vector2D a, Vector2D b )
      {
         return a.X != b.Y || a.Y != b.Y;
      }

      public bool Equals( Vector2D v, float tolerance )
      {
         return Math.Abs( this.X - v.X ) <= tolerance && Math.Abs( this.Y - v.Y ) <= tolerance;
      }

      public override bool Equals( object obj )
      {
         if( obj is Vector2D )
            return this.X == ((Vector2D)obj).X && this.Y == ((Vector2D)obj).Y;
         else
            return base.Equals( obj );
      }

      public override int GetHashCode()
      {
         return this.X.GetHashCode() ^ this.Y.GetHashCode();
      }
   }
}
