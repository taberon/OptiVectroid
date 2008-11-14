using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OptiVectroid
{
   class ParticleSystem
   {
      float gravity = -300; // units per second per second
      public float Gravity { get { return this.gravity; } set { this.gravity = value; } }

      bool regenerate = true;
      public bool ReGenerate { get { return this.regenerate; } set { this.regenerate = value; } }

      public int MaxCount { get { return this.particles.Length; } }

      int usage;
      public int Usage { get { return this.usage; } }

      Random rand;

      struct ParticleInfo
      {
         public PointF Position;
         public PointF Speed;
         public Color Color;
         public float Size;
         public float LifeTime;
      }

      ParticleInfo[] particles;

      public ParticleSystem( int maxParticles )
      {
         this.particles = new ParticleInfo[maxParticles];

         // init random number generator
         this.rand = new Random();
      }

      public void Update( float elapsed )
      {
         // precalculate fixed gravity decelleration amount
         float gravAmount = this.gravity * elapsed;

         // reset current particle usage
         this.usage = 0;

         // update particles
         for( int i = 0; i < this.particles.Length; ++i )
         {
            if( this.particles[i].LifeTime > 0f )
            {
               // update usage count
               ++this.usage;

               // subtract remaining lifetime
               this.particles[i].LifeTime -= elapsed;

               // apply gravity to current vertical speed component
               this.particles[i].Speed.Y += gravAmount;

               // update position based on current particle speed
               this.particles[i].Position.X += this.particles[i].Speed.X * elapsed;
               this.particles[i].Position.Y += this.particles[i].Speed.Y * elapsed;
            }

            // TODO: dynamic color/size updates...
         }
      }

      public void Draw( Graphics grfx )
      {
         if( this.usage == 0 )
            return; // skip drawing if no particles currently in use

         RectangleF partRect = RectangleF.Empty;
         SolidBrush br;
         float radius;
         // draw currently active particles
         for( int i = 0; i < this.particles.Length; ++i )
         {
            if( this.particles[i].LifeTime > 0f )
            {
               // create fill rectangle
               radius = this.particles[i].Size / 2f;
               partRect.X = this.particles[i].Position.X - radius;
               partRect.Y = this.particles[i].Position.Y - radius;
               partRect.Width = this.particles[i].Size;
               partRect.Height = this.particles[i].Size;

               // draw particle
               br = new SolidBrush( this.particles[i].Color );
               grfx.FillRectangle( br, partRect );
               br.Dispose(); // TODO: sort particles by color to reuse resources...
            }
         }
      }

      void GetRandUnitVect2( ref PointF vector )
      {
         // get random direction
         double dir = this.rand.NextDouble() * Math.PI * 2.0;
         // set vector component values
         vector.X = (float)Math.Cos( dir );
         vector.Y = (float)Math.Sin( dir );
      }

      void GetUnitVect2( ref PointF vector )
      {
         float len = (float)Math.Sqrt( vector.X * vector.X + vector.Y * vector.Y );
         vector.X /= len;
         vector.Y /= len;
      }

      void GetRandColor( byte min, byte max, ref Color c )
      {
         c = Color.FromArgb( this.rand.Next( min, max ), this.rand.Next( min, max ), this.rand.Next( min, max ) );
      }

      void GetRandShade( byte min, byte max, ref Color c )
      {
         int val = this.rand.Next( min, max );
         c = Color.FromArgb( val, val, val );
      }

      public int AddExplosion( float centerX, float centerY, int particleCount, bool grayscale )
      {
         int created = 0;

         // fixed explosion effect params
         float lifeSpan = 2f;
         float variantLifeSpan = 1f;

         float initSpeed = 50f; // feet per second
         float variantSpeed = 100f; // +/- random amount

         float baseSize = 3f;
         float variantSize = 5f;

         byte colorMin = 64;
         byte colorMax = 192;

         // find unused particles to allocate for new exposion effect
         for( int i = 0; i < this.particles.Length; ++i )
         {
            if( this.particles[i].LifeTime <= 0f )
            {
               ++created;
               // allocate unused particle
               this.particles[i].LifeTime = lifeSpan * (float)(this.rand.NextDouble() * variantLifeSpan );
               this.particles[i].Position.X = centerX;
               this.particles[i].Position.Y = centerY;
               this.particles[i].Size = baseSize + variantSize * (float)this.rand.NextDouble();

               // set random color
               if( grayscale )
                  GetRandShade( colorMin, colorMax, ref this.particles[i].Color );
               else
                  GetRandColor( colorMin, colorMax, ref this.particles[i].Color );

               // set init speed direction
               GetRandUnitVect2( ref this.particles[i].Speed );
               // apply speed scale and variation
               float partSpeed = initSpeed + (float)( variantSpeed * this.rand.NextDouble() );
               this.particles[i].Speed.X = this.particles[i].Speed.X * partSpeed;
               this.particles[i].Speed.Y = this.particles[i].Speed.Y * partSpeed;
            }

            // stop adding particles if desired max count is met
            if( created >= particleCount )
               break;
         }

         return created;
      }

      public int ExplodeImage( Bitmap image, PointF location, SizeF displaySize, int particleCount )
      {
         // calculate requested image sampling rate in order to approximate the given particle count
         float aspect = (float)image.Width / (float)image.Height;
         float particleSq = (float)Math.Sqrt( particleCount );
         int spanX = (int)( particleSq * aspect );
         int spanY = (int)( particleSq / aspect );
         int neededParticles = spanX * spanY;

         int sampleX = image.Width / spanX;
         int sampleY = image.Height / spanY;
         if( sampleX > image.Width )
            sampleX = image.Width;
         if( sampleY > image.Height )
            sampleY = image.Height;

         float particleSizeX = displaySize.Width / spanX;
         float particleSizeY = displaySize.Height / spanY;
         float particleSize = Math.Min( particleSizeX, particleSizeY );

         int midParticleX = spanX / 2;
         int midParticleY = spanY / 2;

         float lifeSpan = 2f;
         float fixedSpeed = 100f;

         int created = 0;
         Color pixel;
         PointF dir;
         int currX, currY, imageX, imageY;

         // find unused particles to allocate for new exposion effect
         for( int i = 0; i < this.particles.Length; ++i )
         {
            if( this.particles[i].LifeTime <= 0f )
            {
               // find current pixel within image to be created
               currX = created % spanX;
               currY = created / spanX;
               imageX = currX * sampleX;
               imageY = currY * sampleY;

               // sample the current pixel color from the image
               pixel = image.GetPixel( imageX, image.Height - 1 - imageY );

               ++created;
               // allocate unused particle
               this.particles[i].LifeTime = lifeSpan; // TODO: helper methods for random variant values...
               this.particles[i].Color = pixel;
               this.particles[i].Position = new PointF( location.X + currX * particleSizeX, location.Y + currY * particleSizeY );
               this.particles[i].Size = particleSize;
               // calculate movement direction for current particle, outward from image center point
               dir = new PointF( currX - midParticleX, currY - midParticleY );
               if( dir.IsEmpty )
                  GetRandUnitVect2( ref dir );
               GetUnitVect2( ref dir );
               this.particles[i].Speed = new PointF( dir.X * fixedSpeed, dir.Y * fixedSpeed );
            }

            if( created >= neededParticles )
               break;
         }

         return created;
      }

      public bool AddParticle( Vector2D pos, Vector2D velocity, float size, float lifeTime, Color c )
      {
         bool added = false;

         // find unused particles to allocate for new exposion effect
         for( int i = 0; i < this.particles.Length; ++i )
         {
            if( this.particles[i].LifeTime <= 0f )
            {
               // allocate unused particle
               this.particles[i].Position = new PointF( pos.X, pos.Y );
               this.particles[i].Speed = new PointF( velocity.X, velocity.Y );
               this.particles[i].Size = size;
               this.particles[i].Color = c;
               this.particles[i].LifeTime = lifeTime;
               break; // single particle added
            }
         }

         return added;
      }

   }
}
