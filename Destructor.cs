using System;
using System.Drawing;

namespace OptiVectroid
{
   class Destructor
   {
      Vector2D position;
      public Vector2D Position { get { return this.position; } set { this.position = value; } }

      Vector2D layerDir1;
      public Vector2D LayerDirection1 { get { return this.layerDir1; } set { this.layerDir1 = value; } }

      Vector2D layerDir2;
      public Vector2D LayerDirection2 { get { return this.layerDir2; } set { this.layerDir2 = value; } }

      float speed;
      public float Speed { get { return this.speed; } set { this.speed = value; } }

      float acceleration;
      public float Acceleration { get { return this.acceleration; } set { this.acceleration = value; } }

      Color color;
      public Color Color { get { return this.color; } set { this.color = value; } }

      float size;
      public float Size { get { return this.size; } set { this.size = value; } }

      float TurretSize { get { return this.size / 4f; } }
      float CoreSize { get { return this.size * 0.7f; } }

      bool enabled;
      public bool Enabled { get { return this.enabled; } set { this.enabled = value; } }

      int powerLevel;
      public int PowerLevel { get { return this.powerLevel; } }
      int initialPowerLevel = 5;

      struct TurretInfo
      {
         public Vector2D Position;
         public Vector2D Direction;
         public bool Enabled;
      }

      TurretInfo[] turrets;
      public bool TurretsEnabled
      {
         get
         {
            for( int i = 0; i < this.turrets.Length; ++i )
            {
               if( this.turrets[i].Enabled )
                  return true;
            }
            return false;
         }
      }

      public Destructor()
      {
         this.size = 25f;
         this.color = Color.Gray;

         // layer 1 default to up +Y axis
         this.layerDir1 = new Vector2D( 0f, 1f );
         // layer 2 default to 45 degrees offset from layer 1
         this.layerDir2 = this.layerDir1;
         this.layerDir2.Rotate( 45f );
         this.layerDir2.Normalize();

         // set initial power level
         this.powerLevel = this.initialPowerLevel;

         // initialize turrets
         this.turrets = new TurretInfo[8];
         for( int i = 0; i < this.turrets.Length; ++i )
         {
            Vector2D dir = i % 2 == 0 ? this.layerDir1 : this.layerDir2;
            float angleOffset = 45f + i / 2 * 90f;
            dir.Rotate( angleOffset );
            dir.Normalize();
            this.turrets[i].Direction = dir;
            this.turrets[i].Enabled = true;
         }
      }

      public bool CollideSphereCore( Vector2D position, float radius )
      {
         // check for collision with the center core
         //if( Vector2D.Distance( this.position, position ) < this.CoreSize + radius )
         if( Vector2D.Distance( this.position, position ) < this.size + radius ) // use larger size for collision
         {
            // hit to destructor core, decrease power level only when all turrets are gone
            if( !this.TurretsEnabled )
               --this.powerLevel;
            return true;
         }
         return false;
      }

      public bool CollideSphereTurrets( Vector2D position, float radius )
      {
         // check turrets for collision
         for( int i = 0; i < this.turrets.Length; ++i )
         {
            if( !this.turrets[i].Enabled )
               continue; // skip disabled turret

            if( Vector2D.Distance( this.turrets[i].Position, position ) < this.TurretSize + radius )
            {
               // damage turret
               this.turrets[i].Enabled = false;
               return true;
            }
         }

         return false;
      }

      public void ActivateTurrets()
      {
         for( int i = 0; i < this.turrets.Length; ++i )
            this.turrets[i].Enabled = true;
      }

      public void ReActivate()
      {
         //this.enabled = true; // set enabled automatically..?
         this.powerLevel = this.initialPowerLevel;
         // re-enable turrets
         ActivateTurrets();
      }

      public void Update( float elapsed )
      {
         if( !this.enabled )
            return;

         // update turrets if not yet all destroyed, will stop rotating when damage will be applied to core
         if( this.TurretsEnabled )
         {
            float rotateSpeed = 45; // degrees per second
            float angleAdj = rotateSpeed * elapsed;

            this.layerDir1.Rotate( angleAdj );
            this.layerDir1.Normalize();

            this.layerDir2.Rotate( -angleAdj );
            this.layerDir2.Normalize();

            // update turrets
            for( int i = 0; i < this.turrets.Length; ++i )
            {
               float layerAdj = i % 2 == 0 ? angleAdj : -angleAdj;
               // update current direction
               this.turrets[i].Direction.Rotate( layerAdj );
               this.turrets[i].Direction.Normalize();
               // calc current turret position
               this.turrets[i].Position = Vector2D.Scale( this.turrets[i].Direction, this.size * 1.41f ); // aproximate sqaure hypotenuse
            }
         }

      }


      PointF[] GetTurretPoints( Vector2D pos, Vector2D dir, float size )
      {
         float frontSize = size * 2f; // +2f / 3f * size;
         PointF forward = new PointF( pos.X + frontSize * dir.X, pos.Y + frontSize * dir.Y );
         PointF backward = new PointF( pos.X - size * dir.X, pos.Y - size * dir.Y );
         Vector2D perpDir = new Vector2D( -dir.Y, dir.X );
         PointF left = new PointF( pos.X - size * perpDir.X, pos.Y - size * perpDir.Y );
         PointF right = new PointF( pos.X + size * perpDir.X, pos.Y + size * perpDir.Y );
         return new PointF[] { forward, right, backward, left };
      }

      void DrawLayerTurrets( Graphics grfx, bool primary )
      {
         // draw specified layer turrets (layer 1 turrets are even indexed)
         for( int i = primary ? 0 : 1; i < this.turrets.Length; i += 2 )
         {
            if( !this.turrets[i].Enabled )
               continue; // skip de-activated (destroyed) turret

            // get polygon points for current turret
            PointF[] poly = GetTurretPoints( this.turrets[i].Position, this.turrets[i].Direction, this.TurretSize );
            grfx.FillPolygon( Brushes.Firebrick, poly ); // Brown - Firebrick - IndianRed - Crimson
            grfx.DrawPolygon( Pens.Black, poly );
         }
      }

      PointF[] GetRectanglePoints( Vector2D pos, Vector2D dir, float size )
      {
         PointF forward = new PointF( pos.X + size * dir.X, pos.Y + size * dir.Y );
         PointF backward = new PointF( pos.X - size * dir.X, pos.Y - size * dir.Y );
         Vector2D perpDir = new Vector2D( -dir.Y, dir.X );

         PointF forwardLeft = new PointF( forward.X - perpDir.X * size, forward.Y - perpDir.Y * size );
         PointF forwardRight = new PointF( forward.X + perpDir.X * size, forward.Y + perpDir.Y * size );
         PointF backwardLeft = new PointF( backward.X - perpDir.X * size, backward.Y - perpDir.Y * size );
         PointF backwardRight = new PointF( backward.X + perpDir.X * size, backward.Y + perpDir.Y * size );

         return new PointF[] { forwardLeft, forwardRight, backwardRight, backwardLeft };
      }

      public void Draw( Graphics grfx )
      {
         if( !this.enabled )
            return;

         // draw first layer
         PointF[] poly = GetRectanglePoints( this.position, this.layerDir1, this.size );
         grfx.FillPolygon( Brushes.Gray, poly );
         grfx.DrawPolygon( Pens.Black, poly );
         // draw first layer turrets
         DrawLayerTurrets( grfx, true );

         // draw second layer
         poly = GetRectanglePoints( this.position, this.layerDir2, this.size );
         grfx.FillPolygon( Brushes.DarkGray, poly );
         grfx.DrawPolygon( Pens.Black, poly );
         // draw second layer turrets
         DrawLayerTurrets( grfx, false );

         // RosyBrown - DarkOliveGreen - Olive

         Brush coreBrush = ( this.powerLevel == this.initialPowerLevel ) ? Brushes.Gray : Brushes.DarkOliveGreen;
         if( this.powerLevel == 1 ) // one more hit to destroy
            coreBrush = Brushes.Olive;
         // draw stationary rect (captured image...)
         float innerRectRad = this.CoreSize;
         grfx.FillRectangle( coreBrush, this.position.X - innerRectRad, this.position.Y - innerRectRad, innerRectRad * 2f, innerRectRad * 2f );
         grfx.DrawRectangle( Pens.Black, this.position.X - innerRectRad, this.position.Y - innerRectRad, innerRectRad * 2f, innerRectRad * 2f );

      }

   }
}
