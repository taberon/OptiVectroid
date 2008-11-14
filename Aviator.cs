using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;

namespace OptiVectroid
{
   class Aviator
   {
      Vector2D position;
      public Vector2D Position { get { return this.position; } set { this.position = value; } }

      Vector2D direction;
      public Vector2D Direction { get { return this.direction; } set { this.direction = value; } }

      float speed;
      public float Speed { get { return this.speed; } set { this.speed = value; } }

      float acceleration;
      public float Acceleration { get { return this.acceleration; } set { this.acceleration = value; } }

      Color color;
      public Color Color { get { return this.color; } set { this.color = value; } }

      float size;
      public float Size { get { return this.size; } set { this.size = value; } }

      bool enabled;
      public bool Enabled { get { return this.enabled; } set { this.enabled = value; } }

      bool autoPilot = false;
      public bool AutoPilot { get { return this.autoPilot; } set { this.autoPilot = value; } }

      // aviator flight parameters
      float autoPilot_CruisingSpeed = 100f; // units per second
      float autoPilot_MaxSpeed = 200f; // units per second
      float autoPilot_Acceleration = 100f; // units per second per second
      float autoPilot_MaxDistance = 200f; // units
      float autoPilot_RotateSpeed = 180f; // degrees per second

      public static RectangleF WorldLimitRectangle = new RectangleF( -250f, -250, 500f, 500f );

      static Random rand = new Random( 0 ); // if decisions are necessary...

      enum Maneuver { MaintainCourse, AdjustHeading, AdjustSpeed, EvadeThreat } // ...
      Maneuver autoPilot_Maneuver = Maneuver.MaintainCourse;
      Vector2D autoPilot_NewHeading;
      float autoPilot_NewHeadingAdj;

      public static bool DebugDisplay = false;

      bool formation = false;
      public bool Formation { get { return this.formation; } set { this.formation = value; } }

      Squadron group;
      public Squadron FlightGroup { get { return this.group; } set { this.group = value; } }

      public Aviator()
      {
         this.direction = new Vector2D( 0f, 1f );
         this.size = 10f;
         this.color = Color.CadetBlue;
         this.enabled = true;
      }

      public void Rotate( float degrees )
      {
         this.direction.Rotate( degrees );
         this.direction.Normalize();
      }

      public void Thrust( float speedChange )
      {
         this.speed += speedChange;
      }

      public void Flip()
      {
         this.direction = -this.direction;
         this.speed = -this.speed;
      }

      #region Auto Pilot Control

      void ApplyAutomaticFlightControls( float elapsed )
      {
         // modify flight parameters based on current state for automatic control
         float speedChange = 0f;
         Vector2D directionChange = Vector2D.Zero;

         // check if formation flying is enabled
         if( this.formation && this.group != null )
         {
            // adjust direction to fly towards the squadron center
            Vector2D centroidDir = Vector2D.Normalize( this.group.Centroid - this.position );
            float centroidAngle = (float)( Math.Acos( this.direction.Dot( centroidDir ) ) / Math.PI * 180f );
            // apply direction change amount appropriate for current auto pilot rotation speed
            if( centroidAngle != 0f && !float.IsNaN( centroidAngle ) )
            {
               //float adjustmentScale = centroidAngle / this.autoPilot_RotateSpeed * elapsed;
               //float adjustmentScale = elapsed;
               directionChange.X += centroidDir.X / 10f;// *adjustmentScale;
               directionChange.Y += centroidDir.Y / 10f;// * adjustmentScale;
            }


            // adjust speed and direction to maintain separation from nearby aviators
            float targetSeparation = this.size * 10f;
            float nearbyAvgSpeed = 0f;
            int nearbyAvgSpeedCount = 0;
            for( int i = 0; i < this.group.Count; ++i )
            {
               // skip current instance
               if( this.group[i] == this )
                  continue;

               Vector2D nearMoveDir = this.position - this.group[i].position;
               float currDist = nearMoveDir.Length();
               if( currDist < targetSeparation )
               {
                  nearMoveDir.Normalize();
                  float nearMoveDirDot = this.direction.Dot( nearMoveDir );
                  if( Math.Abs( nearMoveDirDot ) > 0.7 /* ~45 degress */ )
                  {
                     float adjustmentScale = currDist / targetSeparation * elapsed;
                     // adjust speed
                     if( nearMoveDirDot > 0 )
                        speedChange += this.autoPilot_Acceleration * adjustmentScale;
                     else
                        speedChange -= this.autoPilot_Acceleration * adjustmentScale;
                  }

                  // adjust direction
                  float nearMoveDirAngle = (float)( Math.Acos( nearMoveDirDot ) / Math.PI * 180f );
                  // apply direction change amount appropriate for current auto pilot rotation speed
                  if( nearMoveDirAngle != 0f && !float.IsNaN( nearMoveDirAngle ) )
                  {
                     float adjustmentScale = nearMoveDirAngle / this.autoPilot_RotateSpeed * elapsed;
                     directionChange.X += nearMoveDir.X * adjustmentScale;
                     directionChange.Y += nearMoveDir.Y * adjustmentScale;
                  }

                  // calculate average speed of nearby aviators
                  ++nearbyAvgSpeedCount;
                  nearbyAvgSpeed += this.group[i].speed;
               }
            }

            // match speed of nearby aviators by adjusting for overall average
            if( nearbyAvgSpeedCount > 0 )
            {
               nearbyAvgSpeed /= (float)nearbyAvgSpeedCount;
               // calculate difference from average speed
               float avgSpeedDiff = nearbyAvgSpeed - this.speed;
               // update speed adjustment
               speedChange += avgSpeedDiff * elapsed;
            }
            else // nothing near, speed up to join the mass
               speedChange += this.autoPilot_Acceleration * elapsed;


            if( float.IsNaN( speedChange ) || float.IsInfinity( speedChange ) )
            {
            }

            if( float.IsNaN( directionChange.X ) || float.IsInfinity( directionChange.X ) || float.IsNaN( directionChange.Y ) || float.IsInfinity( directionChange.Y ) )
            {
            }


            // adjust current flight parameters
            if( this.speed < this.autoPilot_MaxSpeed && speedChange > 0 )
               this.speed += speedChange;
            if( this.speed > this.autoPilot_CruisingSpeed && speedChange < 0 )
               this.speed += speedChange;
            this.direction = Vector2D.Normalize( this.direction + directionChange );

            // set heading vector as way of visually displaying automatic control params
            this.autoPilot_NewHeading = Vector2D.Scale( directionChange, speedChange + 10f );
            this.autoPilot_Maneuver = Maneuver.MaintainCourse; // ensure heading vector is not used as actual direction


            // old formation control stuff and junk...

            //// find nearest neighbor to fly with
            //Aviator nearest = null;
            //float nearestDist = float.MaxValue;
            //for( int i = 0; i < this.group.Count; ++i )
            //{
            //   // skip current instance
            //   if( this.group[i] == this )
            //      continue;

            //   // get current distance
            //   float currDist = Vector2D.Distance( this.position, this.group[i].position );
            //   if( currDist < nearestDist )
            //   {
            //      nearestDist = currDist;
            //      nearest = this.group[i];
            //   }
            //}

            //if( nearest != null )
            //{
            //   // calculate direction to centroid of squadron
            //   Vector2D centerDir = this.group.Centroid - this.position;
            //   centerDir.Normalize();


            //   // match heading of neighbor
            //   Vector2D neighborDir = nearest.direction;
            //   //this.autoPilot_Maneuver = Maneuver.AdjustHeading;
            //   //this.autoPilot_NewHeading = neighborDir;
            //   //this.direction = neighborDir;

            //   // set new direction based on current, neighbor, and centroid vectors
            //   Vector2D newDir = this.direction + neighborDir + centerDir;
            //   newDir.Normalize();
            //   this.direction = newDir;
            //   //this.autoPilot_NewHeading = newDir;
            //   //this.autoPilot_Maneuver = Maneuver.AdjustHeading;


            //   /*/ maintain certain distance from neighbor
            //   float neighborDist = Vector2D.Distance( this.position, nearest.position );
            //   float desiredDist = this.size * 4;
            //   // determine if nearest aviator is too close, and if a collision should be avoided
            //   if( neighborDist < desiredDist )
            //   {
            //      // avoid collision with nearest object by adjusting speed, if necessary
            //      Vector2D avgDir = Vector2D.Normalize( this.direction + nearest.direction );
            //      float pathAngle = (float)( Math.Acos( this.direction.Dot( nearest.direction ) ) / Math.PI * 180f );
            //      if( pathAngle < 22.5 )
            //      {
            //         // aviators are heading in similar direction, determine speed increase/decrease
            //         float dirScale = Vector2D.Multiply( avgDir, this.position ).Length();
            //         float dirScaleNeighbor = Vector2D.Multiply( avgDir, nearest.position ).Length();
            //         if( dirScale > dirScaleNeighbor )
            //         {
            //            // speed up
            //            this.acceleration = this.autoPilot_Acceleration;
            //            //this.speed += this.autoPilot_Acceleration * elapsed;
            //         }
            //         else
            //         {
            //            // slow down
            //            this.acceleration = -this.autoPilot_Acceleration;
            //            //this.speed -= this.autoPilot_Acceleration * elapsed;
            //         }
            //      }
            //   }
            //   else
            //   {
            //      // speed up to join group
            //      this.acceleration = this.autoPilot_Acceleration;
            //      //this.acceleration = 0f; // maintain current speed
            //      //this.speed = this.autoPilot_CruisingSpeed;
            //   }//*/


            //   // berserker speed mode...
            //   float neighborDist = Vector2D.Distance( this.position, nearest.position );
            //   float separation = this.size * 4;
            //   if( neighborDist > separation ) // speed up
            //   {
            //      //this.acceleration = this.autoPilot_Acceleration;
            //      this.speed += this.autoPilot_Acceleration * elapsed;
            //   }
            //   else // slow down
            //   {
            //      //this.acceleration = -this.autoPilot_Acceleration;
            //      this.speed -= this.autoPilot_Acceleration * elapsed;
            //   }
            //   //*/


            //}
         }


         if( !this.formation )
         {
            // adjust to match desired cruising speed
            if( this.speed < this.autoPilot_CruisingSpeed )
            {
               this.acceleration = this.autoPilot_Acceleration;
            }
            // slow down to acquire cruising speed
            else if( this.speed > this.autoPilot_CruisingSpeed )
            {
               this.acceleration = -this.autoPilot_Acceleration;
            }//*/
            if( Math.Abs( this.speed - this.autoPilot_CruisingSpeed ) < 0.1f )
            {
               this.acceleration = 0f; // maintain current speed
            }

            // handle current maneuver operation
            switch( this.autoPilot_Maneuver )
            {
               case Maneuver.MaintainCourse:
               {
                  // check if maneuver should be performed
                  if( this.position.Length() > this.autoPilot_MaxDistance )
                  {
                     // check current heading
                     Vector2D targetDir = -Vector2D.Normalize( this.position );
                     float innerProduct = this.direction.Dot( targetDir );
                     float angle = (float)( Math.Acos( innerProduct ) / Math.PI * 180f );
                     //if( innerProduct == -1f )
                     //angle = 180f; // parallel, but in opposite directions
                     //else if( innerProduct < 0 ) // current heading is greater than 90 degrees away from target heading
                     //angle += 90; // add 90 degree offset to calculated inner angle

                     float targetAngle = (float)( Math.Atan2( targetDir.Y, targetDir.X ) / Math.PI * 180f );
                     float directionAngle = (float)( Math.Atan2( this.direction.Y, this.direction.X ) / Math.PI * 180f );
                     float adjustAngle = directionAngle - targetAngle;
                     //angle = adjustAngle;


                     if( angle > 45 ) // only set new heading if angle greater than 45 degrees from target direction
                     {
                        // steer to maintain max distance from center
                        this.autoPilot_Maneuver = Maneuver.AdjustHeading;

                        // add random variance to target heading to be within 45 degrees of direct target heading
                        //float randAngle = 0f; // this.rand.Next( 22 ) - 11;

                        /*/ set new target heading by adjusting current direction by calculated angle
                        this.autoPilot_NewHeading = new Vector2D( this.direction );
                        this.autoPilot_NewHeading.Rotate( angle + randAngle );
                        this.autoPilot_NewHeading.Normalize();
                        //*/

                        // set new target heading to just be the target direction
                        this.autoPilot_NewHeading = targetDir;

                        // pick random turn direction in order to reach new heading
                        this.autoPilot_NewHeadingAdj = rand.Next( 2 ) == 0 ? -1f : 1f;


                     }
                  }
                  break;
               }
               case Maneuver.AdjustHeading:
               {
                  float headingAdj = this.autoPilot_RotateSpeed * elapsed * this.autoPilot_NewHeadingAdj;
                  this.Rotate( headingAdj );
                  // check if new heading acquired
                  //if( this.direction.Equals( this.autoPilot_NewHeading, 0.2f ) )
                  float headingDot = this.direction.Dot( this.autoPilot_NewHeading );
                  //if( headingDot > .75f )
                  float headingAngle = (float)( Math.Acos( headingDot ) / Math.PI * 180f );
                  //if( headingAngle < 22.5 )
                  if( headingAngle < 5f )
                  {
                     // heading adjustment complete
                     this.autoPilot_Maneuver = Maneuver.MaintainCourse;

                  }
                  break;
               }
            }
         }
      }

     #endregion Auto Pilot Control

      public void Update( float elapsed )
      {
         if( !this.enabled )
            return; // skip update if aviator is currently disabled

         // check if auto pilot enabled
         if( this.autoPilot )
         {
            // apply auto pilot control
            ApplyAutomaticFlightControls( elapsed );
         }

         // ensure within maximum allowed world limit
         bool directionUpdated = false;
         if( this.position.X < Aviator.WorldLimitRectangle.Left ) // && this.direction.X < 0f )
         {
            this.direction.X += ( this.speed > 0f ? 10f : -10f ) * elapsed;
            directionUpdated = true;
         }
         if( this.position.X > Aviator.WorldLimitRectangle.Right ) // && this.direction.X > 0f )
         {
            this.direction.X -= ( this.speed > 0f ? 10f : -10f ) * elapsed;
            directionUpdated = true;
         }
         if( this.position.Y < Aviator.WorldLimitRectangle.Top ) // && this.direction.X < 0f )
         {
            this.direction.Y += ( this.speed > 0f ? 10f : -10f ) * elapsed;
            directionUpdated = true;
         }
         if( this.position.Y > Aviator.WorldLimitRectangle.Bottom ) // && this.direction.X < 0f )
         {
            this.direction.Y -= ( this.speed > 0f ? 10f : -10f ) * elapsed;
            directionUpdated = true;
         }
         if( directionUpdated )
         {
            if( Math.Abs( this.direction.X ) < 0.001f )
               this.direction.X += this.direction.X < 0f ? -0.1f : 0.1f;
            if( Math.Abs( this.direction.Y ) < 0.001f )
               this.direction.Y += this.direction.Y < 0f ? -0.1f : 0.1f;
            this.direction.Normalize();
         }

         // update current speed
         if( this.acceleration != 0f )
         {
            this.speed += this.acceleration * elapsed;
         }
         // update current position
         if( this.speed != 0f )
         {
            float dist = this.speed * elapsed;
            this.position.X += this.direction.X * dist;
            this.position.Y += this.direction.Y * dist;
         }
      }

      public void Draw( Graphics grfx )
      {
         if( !this.enabled )
            return; // skip drawing if aviator is currently disabled

         PointF center = new PointF( this.position.X, this.position.Y );
         PointF forward = new PointF( this.position.X + this.size * this.direction.X, this.position.Y + this.size * this.direction.Y );
         PointF back = new PointF( this.position.X - this.size * this.direction.X, this.position.Y - this.size * this.direction.Y );
         Vector2D perpDir = new Vector2D( -this.direction.Y, this.direction.X );
         PointF backLeft = new PointF( back.X - this.size * perpDir.X, back.Y - this.size * perpDir.Y );
         PointF backRight = new PointF( back.X + this.size * perpDir.X, back.Y + this.size * perpDir.Y );

         PointF[] poly = new PointF[] { forward, backLeft, center, backRight };
         SolidBrush br = new SolidBrush( !this.formation ? this.color : Color.FromArgb( (byte)~this.color.R, (byte)~this.color.G, (byte)~this.color.B ) );
         grfx.FillPolygon( br, poly );
         br.Dispose();
         //Pen pen = new Pen( Color.Black, 0f );
         //grfx.DrawPolygon( pen, poly );
         //pen.Dispose();

         if( DebugDisplay )
         {
            /*/ add antenna thingy to indicate auto pilot enabled
            if( this.autoPilot )
            {
               float antennaSize = 5f;
               Pen antPen = new Pen( Color.Black, 0f );
               grfx.DrawLine( antPen, forward, new PointF( forward.X + this.direction.X * antennaSize, forward.Y + this.direction.Y * antennaSize ) );
               antPen.Dispose();
            }//*/

            // debug directions
            if( this.autoPilot_Maneuver == Maneuver.AdjustHeading || ( this.formation && this.autoPilot ) )
            {
               Pen headingPn = new Pen( Color.Red, 0f );
               float headingScale = 25f;
               grfx.DrawLine( headingPn, this.position.X, this.position.Y, this.position.X + this.autoPilot_NewHeading.X * headingScale, this.position.Y + this.autoPilot_NewHeading.Y * headingScale );
               headingPn.Dispose();
            }//*/

            /*/ draw auto pilot center radius
            // TODO: draws the same circle for every aviator instance... but still useful for debugging perhaps
            grfx.DrawEllipse( Pens.Red, -this.autoPilot_MaxDistance, -this.autoPilot_MaxDistance, this.autoPilot_MaxDistance * 2, this.autoPilot_MaxDistance * 2 );
            //*/
         }
      }

      public bool CollideSphere( Vector2D position, float radius )
      {
         if( Vector2D.Distance( this.position, position ) < this.size + radius )
         {
            return true;
         }
         return false;
      }

   }

   class Squadron : IEnumerable
   {
      List<Aviator> list = new List<Aviator>();

      public Squadron()
      {
      }

      public int Count { get { return this.list.Count; } }

      public Aviator this[int index]
      {
         get { return this.list[index]; }
         set { this.list[index] = value; }
      }

      public void Add( Aviator item )
      {
         this.list.Add( item );
         // associate aviator with sqaudron
         item.FlightGroup = this;
      }

      public void Remove( Aviator item )
      {
         this.list.Remove( item );
         // disassociate aviator with sqaudron
         item.FlightGroup = null;
      }

      public void RemoveAt( int index )
      {
         // disassociate aviator with sqaudron
         this.list[index].FlightGroup = null;
         this.list.RemoveAt( index );
      }

      public void Clear()
      {
         // disassociate aviators with sqaudron
         for( int i = 0; i < this.list.Count; ++i )
            this.list[i].FlightGroup = null;
         // clear internal list
         this.list.Clear();
      }

      public void Update( float elapsed )
      {
         // reset frame based calculated squadron parameters
         this.centroidNeedCalc = true;
         // TODO: update position map/quadtree structure..?

         for( int i = 0; i < this.list.Count; ++i )
            this.list[i].Update( elapsed );
      }

      public void Draw( Graphics grfx )
      {
         for( int i = 0; i < this.list.Count; ++i )
            this.list[i].Draw( grfx );

         // draw optional squadron debug info
         if( Aviator.DebugDisplay )
         {
            // draw squadron centroid
            Vector2D centroid = this.Centroid;
            float centoidRadius = 5f;
            grfx.DrawEllipse( Pens.Purple, centroid.X - centoidRadius, centroid.Y - centoidRadius, centoidRadius * 2, centoidRadius * 2 );
            centoidRadius *= 2;
            grfx.DrawEllipse( Pens.Purple, centroid.X - centoidRadius, centroid.Y - centoidRadius, centoidRadius * 2, centoidRadius * 2 );
            //*/
         }
      }

      Vector2D CalculateCentroid()
      {
         // calculate current squadron centriod
         Vector2D centroid = Vector2D.Zero;
         for( int i = 0; i < this.list.Count; ++i )
         {
            centroid.X += this.list[i].Position.X;
            centroid.Y += this.list[i].Position.Y;
         }
         centroid.X /= (float)this.list.Count;
         centroid.Y /= (float)this.list.Count;
         return centroid;
      }

      bool centroidNeedCalc = true;
      Vector2D lastCentroid;
      public Vector2D Centroid
      {
         get
         {
            if( this.centroidNeedCalc )
            {
               this.lastCentroid = CalculateCentroid();
               this.centroidNeedCalc = false;
            }
            return this.lastCentroid;
         }  
      }

      #region IEnumerable

      class SquadronEnumerator : IEnumerator
      {
         int position = -1;
         List<Aviator> list;
         public SquadronEnumerator( List<Aviator> list )
         {
            this.list = list;
         }

         #region IEnumerator

         public object Current
         {
            get { return this.list[this.position]; }
         }

         public bool MoveNext()
         {
            ++this.position;
            return this.position < this.list.Count;
         }

         public void Reset()
         {
            this.position = -1;
         }

         #endregion IEnumerator
      }

      public IEnumerator GetEnumerator()
      {
         return new SquadronEnumerator( this.list );
      }

      #endregion IEnumerable
   }
}
