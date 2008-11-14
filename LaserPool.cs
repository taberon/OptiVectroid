using System;
using System.Drawing;

namespace OptiVectroid
{
   class LaserPool
   {
      int maxCount;

      float maxLifeSpan = 3f;
      public float MaxLifeSpan { get { return this.maxLifeSpan; } set { this.maxLifeSpan = value; } }

      float size = 10f;
      public float Length { get { return this.size; } set { this.size = value; } }

      float speed = 500f; // units per second
      public float Speed { get { return this.speed; } set { this.speed = value; } }

      Color color = Color.Red;
      public Color LaserColor { get { return this.color; } set { this.color = value; } }

      bool live;
      public bool HasLiveInstances { get { return this.live; } }

      public struct LaserFire
      {
         public Vector2D Position;
         public Vector2D Direction;
         public float LifeSpan;
      }

      LaserFire[] pool;
      public LaserFire[] Items { get { return this.pool; } }

      public LaserPool( int maxCount )
      {
         this.maxCount = maxCount;
         this.pool = new LaserFire[this.maxCount];
      }

      public bool AddLaserFire( Vector2D position, Vector2D direction )
      {
         bool added = false;
         for( int i = 0; i < this.maxCount; ++i )
         {
            if( this.pool[i].LifeSpan <= 0f )
            {
               added = true;
               // acquire new instance
               this.pool[i].LifeSpan = this.maxLifeSpan;
               this.pool[i].Position = position;
               this.pool[i].Direction = Vector2D.Normalize( direction ); // ensure normalized dir...
               break;
            }
         }
         return added;
      }

      public void Update( float elapsed )
      {
         this.live = false;
         for( int i = 0; i < this.maxCount; ++i )
         {
            if( this.pool[i].LifeSpan > 0f )
            {
               // decay
               this.pool[i].LifeSpan -= elapsed;
               // move
               this.pool[i].Position.X += this.pool[i].Direction.X * this.speed * elapsed;
               this.pool[i].Position.Y += this.pool[i].Direction.Y * this.speed * elapsed;
               // at least once instance is live
               this.live = true;
            }
         }
      }

      public void Draw( Graphics grfx )
      {
         if( !this.live )
            return; // early exit if nothing to draw

         Pen laserPen = new Pen( this.color, 0f );
         for( int i = 0; i < this.maxCount; ++i )
         {
            if( this.pool[i].LifeSpan > 0f )
            {
               grfx.DrawLine( laserPen, this.pool[i].Position.X, this.pool[i].Position.Y, this.pool[i].Position.X + this.pool[i].Direction.X * this.size, this.pool[i].Position.Y + this.pool[i].Direction.Y * this.size );
            }
         }
         laserPen.Dispose();
      }
   }
}
