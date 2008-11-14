using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace OptiVectroid
{
   class MainForm : Form
   {
      Aviator userAviator;
      float userKillTime = 3f; // seconds to wait before resetting user ship
      float userKillElapsed; // counter for elapsed time after user killed
      int userKillTransistion; // state to wait for user explostion, and then move to respawn position
      int userExtraShips = 5;
      float userAcceleration = 200f;
      // TODO: GameState state...

      // view parameters
      float worldUnit = 1f;
      Vector2D lookAt;
      SizeF maxWorldSize = new SizeF( 1000f, 1000f ); // max level (world) size
      SizeF visibleWorldSize = new SizeF( 500f, 500f ); // max visible area on screen

      const int randDroneCreateCount = 25;
      const int randAggressorCreateCount = 10;
      Squadron squadron;

      Destructor enemyThing;

      ParticleSystem particles;
      ParticleSystem backgroundStars;

      bool showMouse = false;

      Random rand = new Random( 0 );

      bool displayAxis = false;
      bool displayFPS = false;
      float lastFPS;
      float lastFPSUpdate;

      // game params
      int score;
      int level; // wave...

      LaserPool lasers;
      float laserAutoFireRate = 0.25f; // shots per second
      float laserAutoFireElapsed;

      const string AppName = "OptiVectroid v0.1";

      public MainForm()
      {
         this.Text = AppName;
         this.Size = new Size( 800, 600 );
         this.SetStyle( ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true );
         this.BackColor = Color.Black;

         // create squadron to manage collection of aviators
         this.squadron = new Squadron();

         this.userAviator = new Aviator();
         //this.userAviator.Direction = Vector2D.Normalize( new Vector2D( 1f, -1f ) );
         //this.userAviator.AutoPilot = true;
         // set default user aviator position
         PlayerAviatorReset();
         this.squadron.Add( this.userAviator );

         // set aviator world limit
         Aviator.WorldLimitRectangle = new RectangleF( -this.maxWorldSize.Width / 2f, -this.maxWorldSize.Height / 2f, this.maxWorldSize.Width, this.maxWorldSize.Height );

         // create default number of aviators
         CreateDefaultAviators();

         // create enemy thing
         this.enemyThing = new Destructor();
         this.enemyThing.Enabled = true;

         // create particle system for explosions
         this.particles = new ParticleSystem( 250 );
         // create particle system for background star display
         this.backgroundStars = new ParticleSystem( 100 );
         this.backgroundStars.Gravity = 0f; // stars remain stationary
         SizeF starFieldSize = new SizeF( this.maxWorldSize.Width + this.visibleWorldSize.Width, this.maxWorldSize.Height + this.visibleWorldSize.Height ); 
         for( int i = 0; i < this.backgroundStars.MaxCount; ++i )
         {
            Vector2D pos = new Vector2D( this.rand.Next( (int)starFieldSize.Width ) - starFieldSize.Width / 2f, this.rand.Next( (int)starFieldSize.Height ) - starFieldSize.Height / 2f );
            float randSize = 1f + (float)( this.rand.NextDouble() * 2f );
            int intensity = 127 + this.rand.Next( 128 );
            Color c = Color.FromArgb( intensity, intensity, intensity );
            this.backgroundStars.AddParticle( pos, Vector2D.Zero, randSize, 2f, c );
         }
         this.backgroundStars.Update( 1f ); // update only once to initialize usage count

         // initialize laser pool instance
         this.lasers = new LaserPool( 50 );
         this.lasers.LaserColor = Color.Cyan;

         if( !this.showMouse )
            Cursor.Hide();
      }


      Vector2D GetRandomDir()
      {
         // get random direction angle
         float angle = (float)( this.rand.NextDouble() * ( Math.PI * 2 ) );
         return new Vector2D( (float)Math.Cos( angle ), (float)Math.Sin( angle ) );
      }

      void AddRandomAviator( bool drone )
      {
         Aviator randAviator = new Aviator();
         randAviator.Position = new Vector2D( this.rand.Next( this.ClientSize.Width ) - this.ClientSize.Width / 2f, this.rand.Next( this.ClientSize.Height ) - this.ClientSize.Height / 2f );
         randAviator.Direction = GetRandomDir();
         randAviator.Color = Color.YellowGreen;
         randAviator.AutoPilot = true;
         randAviator.Formation = !drone;
         this.squadron.Add( randAviator );
      }

      void UpdateWindowTitleCount()
      {
         // update total count in window title
         this.Text = AppName + " (" + this.squadron.Count.ToString() + ")";
      }

      protected override void OnShown( EventArgs e )
      {
         base.OnShown( e );
         StartLoop();
      }

      protected override void OnResize( EventArgs e )
      {
         base.OnResize( e );

         // TODO: update view parameters to maintain zoom level...
         // calc scale for visible screen area
         //float scaleX = this.visibleWorldSize.Width / this.ClientSize.Width;
         //float scaleY = this.visibleWorldSize.Height / this.ClientSize.Height;
         float scaleX = this.ClientSize.Width / this.visibleWorldSize.Width;
         float scaleY = this.ClientSize.Height / this.visibleWorldSize.Height;
         float scale = Math.Min( scaleX, scaleY );
         this.worldUnit = scale;

         if( !this.LoopRunning )
            this.Invalidate();
      }

      #region Simulation Loop Timer

      Timer loopTimer;
      int lastTick;

      bool LoopRunning
      {
         get { return this.loopTimer != null && this.loopTimer.Enabled; }
      }

      void StartLoop()
      {
         if( this.loopTimer == null )
         {
            this.loopTimer = new Timer();
            this.loopTimer.Interval = 30; // max 30 fps
            this.loopTimer.Tick += new EventHandler( LoopTimer_Tick );
         }

         if( !this.loopTimer.Enabled )
         {
            this.lastTick = Environment.TickCount;
            this.loopTimer.Start();
         }
      }

      void StopLoop()
      {
         if( this.loopTimer.Enabled )
            this.loopTimer.Stop();
      }

      void LoopTimer_Tick( object sender, EventArgs e )
      {
         int currTick = Environment.TickCount;
         float elapsed = (float)( currTick - this.lastTick ) / 1000f;
         this.lastTick = currTick;

         UpdateSimulation( elapsed );
         this.Invalidate(); // request repaint
      }

      #endregion Simulation Loop Timer

      #region Painting Overrides

      void SetupScreenTransform( Graphics grfx )
      {
         grfx.TranslateTransform( this.ClientSize.Width / 2f, this.ClientSize.Height / 2f );
         grfx.ScaleTransform( worldUnit, -worldUnit );
         // set view look at position
         grfx.TranslateTransform( -this.lookAt.X, -this.lookAt.Y );
      }

      protected override void OnPaint( PaintEventArgs e )
      {
         SetupScreenTransform( e.Graphics );
         DrawSimulation( e.Graphics );

         // allow screen-space drawing
         e.Graphics.ResetTransform();
         DrawScreen( e.Graphics );
      }

      #endregion Painting Overrides

      void CreateDefaultAviators()
      {
         // create random aviators
         for( int i = 0; i < randDroneCreateCount; ++i )
         {
            AddRandomAviator( true );
         }
         for( int i = 0; i< randAggressorCreateCount; ++i )
         {
            AddRandomAviator( false );
         }
         // display initial count in the window title
         UpdateWindowTitleCount();
      }

      Vector2D GetPlayerRespawnPosition()
      {
         return new Vector2D( 0f, -this.maxWorldSize.Height / 2f + this.userAviator.Size * 4f );
      }

      private void PlayerAviatorReset()
      {
         // reset user aviator
         this.userAviator.Position = GetPlayerRespawnPosition();
         this.userAviator.Direction = new Vector2D( 0f, 1f );
         this.userAviator.Speed = 0f;
         this.userAviator.Acceleration = 0f;
         this.userAviator.Enabled = true;
      }

      private void BeginPlayerAviatorReset()
      {
         this.userAviator.Enabled = false;
         this.userKillElapsed = 0f;
         this.userKillTransistion = 0;
      }

      void UpdateSimulation( float elapsed )
      {
         // calculate last FPS, if enabled for display
         if( this.displayFPS )
         {
            this.lastFPSUpdate += elapsed;
            if( this.lastFPSUpdate > 1f )
            {
               this.lastFPSUpdate = 0f;
               this.lastFPS = 1f / elapsed;
            }
         }

         // update keyboard component (allow polling update)
         Keyboard.Update( elapsed );

         if( this.userAviator.Enabled )
         {
            // use immediate key state input for ship control
            float rotateSpeed = 180f; // 180 degrees per second
            // check key down to control user aviator
            if( Keyboard.IsKeyDown( Keys.Left ) )
               this.userAviator.Rotate( rotateSpeed * elapsed );
            if( Keyboard.IsKeyDown( Keys.Right ) )
               this.userAviator.Rotate( -rotateSpeed * elapsed );
            this.userAviator.Acceleration = 0f;
            if( Keyboard.IsKeyDown( Keys.Up ) )
               this.userAviator.Acceleration = userAcceleration;
            if( Keyboard.IsKeyDown( Keys.Down ) )
               this.userAviator.Acceleration = -userAcceleration;

            bool fireLaser = false; // determine if laser should be fired
            if( Keyboard.IsKeyPressed( Keys.ControlKey ) )
               fireLaser = true;
            else if( Keyboard.IsKeyDown( Keys.ControlKey ) )
            {
               // enable limited rapid fire if key is held down
               this.laserAutoFireElapsed += elapsed;
               if( this.laserAutoFireElapsed > this.laserAutoFireRate )
                  fireLaser = true;
            }
            if( fireLaser )
            {
               // initiate laser fire
               this.laserAutoFireElapsed = 0f; // reset auto-fire repeat counter
               this.lasers.AddLaserFire( this.userAviator.Position, this.userAviator.Direction );
            }

            // check aviator flip action
            if( Keyboard.IsKeyPressed( Keys.End ) )
               this.userAviator.Flip();
         }
         // handle player kill action when ship is set disabled
         else if( this.userKillTransistion < 2 )
         {
            // handle player kill transition
            switch( this.userKillTransistion )
            {
               case 0: // wait to view player explosion
                  this.userKillElapsed += elapsed;
                  // check if kill wait time has completed
                  if( this.userKillElapsed > this.userKillTime )
                  {
                     ++this.userKillTransistion;
                     Vector2D resetPos = GetPlayerRespawnPosition();
                     this.userAviator.Direction = Vector2D.Normalize( resetPos - this.userAviator.Position );
                  }
                  break;
               case 1: // move camera back to respawn position
                  // check if respawn location has been moved to
                  if( Vector2D.Distance( this.userAviator.Position, GetPlayerRespawnPosition() ) < 5f )
                  {
                     ++this.userKillTransistion;
                     PlayerAviatorReset();
                     --this.userExtraShips; // subtract extra ships
                  }
                  else // move to respawn location
                  {
                     Vector2D updatedPosition = this.userAviator.Position;
                     updatedPosition.Translate( this.userAviator.Direction.X * 200f * elapsed, this.userAviator.Direction.Y * 200f * elapsed );
                     this.userAviator.Position = updatedPosition;
                  }
                  break;
            }
         }

         // move camera view to follow user ship location
         this.lookAt = this.userAviator.Position;

         // update enemy thing
         this.enemyThing.Update( elapsed );

         // update aviator sqaudron
         this.squadron.Update( elapsed );

         // TODO: enable option for aviator for collision destructable or something..?
         // check for collision with user controllable aviator
         if( !this.userAviator.AutoPilot && !this.userAviator.Formation && this.userAviator.Enabled )
         {
            bool userCollided = false;
            // check for collision with destructor (big enemy)
            if( this.enemyThing.Enabled )
            {
               if( this.enemyThing.CollideSphereCore( this.userAviator.Position, this.userAviator.Size ) )
               {
                  Vector2D pos = this.userAviator.Position; // save position for explode effect
                  // reset user aviator
                  BeginPlayerAviatorReset();
                  // add explosion particle system effect
                  this.particles.AddExplosion( pos.X, pos.Y, 50, false );
                  userCollided = true;
               }
               else if( this.enemyThing.CollideSphereTurrets( this.userAviator.Position, this.userAviator.Size ) )
               {
                  Vector2D pos = this.userAviator.Position; // save position for explode effect
                  // reset user aviator
                  BeginPlayerAviatorReset();
                  // add explosion particle system effect
                  this.particles.AddExplosion( pos.X, pos.Y, 50, false );
                  userCollided = true;
               }
            }
            if( !userCollided )
            {
               for( int i = 1; i < this.squadron.Count; ++i )
               {
                  if( this.userAviator.CollideSphere( this.squadron[i].Position, this.userAviator.Size ) )
                  {
                     Vector2D pos = this.userAviator.Position; // save position for explode effect
                     // reset user aviator
                     BeginPlayerAviatorReset();
                     // kill other aviator
                     this.squadron.RemoveAt( i );
                     UpdateWindowTitleCount();
                     // add explosion particle system effect
                     this.particles.AddExplosion( pos.X, pos.Y, 50, false );
                     break;
                  }
               }
            }
         }

         // update particle systems for explosions
         this.particles.Update( elapsed );
         // update background star particle system (TODO: add twinkling fading effect...)
         //this.backgroundStars.Update( elapsed ); // .. currently leave as static effect

         // process lasers
         this.lasers.Update( elapsed );

         // check if active lasers collide with objects
         if( this.lasers.HasLiveInstances )
         {
            for( int i = 0; i < this.lasers.Items.Length; ++i )
            {
               if( this.lasers.Items[i].LifeSpan > 0f )
               {
                  // skip user controllable 0th aviator...
                  for( int j = 1; j < this.squadron.Count; ++j )
                  {
                     if( this.squadron[j].CollideSphere( this.lasers.Items[i].Position, 0f ) )
                     {
                        // hit, kill aviator and laser
                        this.lasers.Items[i].LifeSpan = 0f;
                        Vector2D pos = this.squadron[j].Position; // save position for explode effect
                        this.score += this.squadron[j].Formation ? 150 : 50;
                        this.squadron.RemoveAt( j );
                        UpdateWindowTitleCount();
                        // add explosion particle system effect
                        this.particles.AddExplosion( pos.X, pos.Y, 25, true );
                        break;
                     }
                  }

                  // check laser collision with turrets
                  if( this.enemyThing.Enabled && this.lasers.Items[i].LifeSpan > 0f )
                  {
                     if( this.enemyThing.CollideSphereTurrets( this.lasers.Items[i].Position, 0f ) )
                     {
                        // hit, kill turret and laser
                        this.lasers.Items[i].LifeSpan = 0f;
                        Vector2D pos = this.lasers.Items[i].Position; // save position for explode effect
                        // add explosion particle system effect
                        this.particles.AddExplosion( pos.X, pos.Y, 25, false );
                        this.score += 100;
                     }
                     // check for laser collision with destructor (big enemy)
                     else if( this.enemyThing.CollideSphereCore( this.lasers.Items[i].Position, 0f ) )
                     {
                        // hit, kill laser, and possibly big enemy, if all turrets are gone, and damage is max...
                        this.lasers.Items[i].LifeSpan = 0f;
                        Vector2D pos = this.lasers.Items[i].Position; // save position for explode effect
                        // add explosion particle system effect
                        if( this.enemyThing.TurretsEnabled )
                           this.particles.AddExplosion( pos.X, pos.Y, 3, true );
                        else
                           this.particles.AddExplosion( pos.X, pos.Y, 15, false );
                        break; // laser hit big enemy
                     }
                  }
               }
            }
         }

         if( this.enemyThing.Enabled && this.enemyThing.PowerLevel == 0 )
         {
            // destroy main enemy
            this.enemyThing.Enabled = false;
            this.particles.AddExplosion( this.enemyThing.Position.X, this.enemyThing.Position.Y, 200, false );
            this.score += 1000;
         }
      }

      void DrawSimulation( Graphics grfx )
      {
         // draw test visible screen area
         //grfx.DrawRectangle( Pens.Gray, -this.visibleWorldSize.Width / 2f, -this.visibleWorldSize.Height / 2f, this.visibleWorldSize.Width, this.visibleWorldSize.Height );
         // draw test max world size
         //Pen p = new Pen( Color.Maroon, 0f );
         grfx.DrawRectangle( Pens.Maroon, -this.maxWorldSize.Width / 2f, -this.maxWorldSize.Height / 2f, this.maxWorldSize.Width, this.maxWorldSize.Height );
         //p.Dispose();

         // draw background star particle system
         this.backgroundStars.Draw( grfx );

         // draw enemy thing
         this.enemyThing.Draw( grfx );

         // draw origin
         if( this.displayAxis )
         {
            int originScale = 10;
            grfx.DrawLine( Pens.Black, -originScale, 0, originScale, 0 );
            grfx.DrawLine( Pens.Black, 0, -originScale, 0, originScale );
         }

         // draw aviators
         this.squadron.Draw( grfx );

         // draw explosion particle system
         this.particles.Draw( grfx );

         // draw lasers
         this.lasers.Draw( grfx );
      }

      void DrawScreen( Graphics grfx )
      {
         // handle screen-space drawing
         if( this.displayFPS )
         {
            grfx.DrawString( string.Format( "FPS: {0:N1}", this.lastFPS ), this.Font, Brushes.White, this.ClientSize.Width - 60f, 5f );
         }

         //
         Font displayFont = new Font( "Arial", 12f, FontStyle.Regular );
         grfx.DrawString( string.Format( "Score: {0}", this.score ), displayFont, Brushes.DodgerBlue, 5f, 5f );
         //grfx.DrawString( string.Format( "Level: {0}", this.level ), displayFont, Brushes.DodgerBlue, this.ClientSize.Width / 2f - 70f, 5f );
         displayFont.Dispose();
         //*/

         // draw remaining lives
         //if( this.userExtraShips > 0 )
         {
            float barSpacing = 4f;
            SizeF barSize = new SizeF( 5f, 10f );
            RectangleF bar = new RectangleF( 5f, this.ClientSize.Height - barSize.Height - barSpacing, barSize.Width, barSize.Height );
            Brush barBrush = this.userExtraShips >= 0 ? Brushes.Gainsboro : Brushes.Firebrick;
            for( int i = 0; i < Math.Abs( this.userExtraShips ); ++i )
            {
               bar.X = 5f + i * ( barSize.Width + barSpacing );
               grfx.FillRectangle( barBrush, bar );
            }
         }//*/
      }

      protected override void OnKeyDown( KeyEventArgs e )
      {
         switch( e.KeyCode )
         {
            case Keys.Oemplus:
               // add another aviator
               AddRandomAviator( !e.Shift );
               // update the aviator count in the window title
               UpdateWindowTitleCount();
               break;
            case Keys.OemMinus:
               // remove an aviator
               if( this.squadron.Count > 1 ) // don't remove last one, is user controllable instance
               {
                  this.squadron.RemoveAt( this.squadron.Count - 1 );
                  // update the aviator count in the window title
                  UpdateWindowTitleCount();
               }
               break;
            case Keys.D0: // zero key, clear all
               this.squadron.Clear();
               this.squadron.Add( this.userAviator );
               break;

            case Keys.R: // reset game
               CreateDefaultAviators();
               PlayerAviatorReset();
               this.score = 0;
               this.userExtraShips = 5;
               this.enemyThing.Enabled = true;
               this.enemyThing.ReActivate();
               break;

            case Keys.A: // toggle user aviator auto-pilot
               this.userAviator.AutoPilot = !this.userAviator.AutoPilot;
               break;
            case Keys.F: // aviator formation flying
               if( !e.Shift ) // user aviator
                  this.userAviator.Formation = !this.userAviator.Formation;
               else // all aviators (except user's)
               {
                  foreach( Aviator randAviator in this.squadron )
                  {
                     if( randAviator != this.userAviator )
                        randAviator.Formation = !randAviator.Formation;
                  }
               }
               break;

            case Keys.P: // play/pause
               if( this.LoopRunning )
                  this.StopLoop();
               else
                  this.StartLoop();
               break;

            case Keys.X: // enable the big enemy thingy
               this.enemyThing.Enabled = !this.enemyThing.Enabled;
               // reset turrets when set visible
               if( this.enemyThing.Enabled )
                  this.enemyThing.ReActivate();
               break;

            case Keys.M:
               this.showMouse = !this.showMouse;
               if( this.showMouse )
                  Cursor.Show();
               else
                  Cursor.Hide();
               break;

            case Keys.D: // debug display
               this.displayAxis = !this.displayAxis;
               //this.displayFPS = !this.displayFPS;
               Aviator.DebugDisplay = !Aviator.DebugDisplay;
               break;
            case Keys.I: // info frame rate toggle
               this.displayFPS = !this.displayFPS;
               break;

            case Keys.Z: // cycle preset zoom levels
               this.worldUnit *= 1.5f;
               if( this.worldUnit > 3f )
                  this.worldUnit = 1f;
               break;

            case Keys.B: // cycle preset background colors
               if( this.BackColor == SystemColors.Control )
                  this.BackColor = Color.DarkGray;
               else if( this.BackColor == Color.DarkGray )
                  this.BackColor = Color.Gray;
               else if( this.BackColor == Color.Gray )
                  this.BackColor = Color.Black;
               else if( this.BackColor == Color.Black )
                  this.BackColor = Color.White;
               else if( this.BackColor == Color.White )
                  this.BackColor = SystemColors.Control;
               break;

            case Keys.Q: // quit
            case Keys.Escape:
               this.Close();
               break;
         }

         if( !this.LoopRunning )
            this.Invalidate(); // request redraw when key pressed and loop is stopped
      }

      [STAThread]
      public static void Main( string[] args )
      {
         Application.Run( new MainForm() );
      }
   }
}
