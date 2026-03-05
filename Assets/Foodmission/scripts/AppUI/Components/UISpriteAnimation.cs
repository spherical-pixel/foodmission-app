using System;
using UnityEngine;
using UnityEngine.UIElements;


namespace eu.foodmission.platform.Components
{
    public enum CycleType
	{
		Loop,
		Reverse,
		PingPong,
		OneShot,
	}

    [UxmlElement]
    public partial class UISpriteAnimation : Image
    {
        /* ========= UXML ATTRIBUTES ========= */

        
        
        [UxmlAttribute("start-delay")]
		private float startDelay = 0f;

		[UxmlAttribute("fps")]
		private int fps = 30;

		[UxmlAttribute("cycle")]
		private CycleType cycle
        {
            get => _cycle;
            set
            {
                _cycle = value;
                if (panel != null)
                    Play(0,true);
            }
        }

        [UxmlAttribute("sprites")]
        public Sprite[] Sprites
        {
            get => _sprites;
            set
            {
                _sprites = value ?? Array.Empty<Sprite>();
                // Put the first frame in the sprite array as the default sprite.
                if (_sprites.Length > 0)
                {
                    sprite = _sprites[0];
                }
                if (panel != null)
                    Play(0,true);
            }
        }

        

        /* ========= INTERNAL ELEMENTS ========= */

        private Sprite[] _sprites = Array.Empty<Sprite>();

        private int  _spriteIdx = 0;
		private int  _increment = 1;
		private long _frameTime;
		private long _startDelay;
        private IVisualElementScheduledItem _startTask;
        private CycleType _cycle = CycleType.Loop;
        

        public UISpriteAnimation()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        public UISpriteAnimation(Sprite[] sprites) : this()
		{
			this._sprites = sprites;
		}

        public UISpriteAnimation(Sprite[] sprites, float startDelay) : this(sprites)
		{
			this.startDelay = startDelay;
		}

        public UISpriteAnimation(Sprite[] sprites, float startDelay, int fps) : this(sprites, startDelay)
		{
			this.fps = fps;
		}

		public UISpriteAnimation(Sprite[] sprites, float startDelay, int fps, CycleType cycle) : this(sprites, startDelay, fps)
		{
			this.cycle = cycle;
		}

        private void OnAttach(AttachToPanelEvent evt)
        {
            Play(_startDelay, true);
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            Pause();
            
        }

        private void HandleFPSAndDelay()
		{
			_frameTime  = (long) ((1f / fps) * 1000);
			_startDelay = (long) (startDelay * 1000);
		}



        /// <summary>
		/// Update the sprites.
		/// </summary>
		/// <param name="newSprites"></param>
		/// <param name="autoPlay"></param>
		public void UpdateSprites(Sprite[] newSprites, bool autoPlay = false)
		{
			Pause();
			this._sprites = newSprites;

			if(autoPlay)
				Play();
		}

		/// <summary>
		/// Play the animation.
		/// </summary>
		/// <param name="delay"></param>
		/// <param name="restart"></param>
		public void Play(long delay = 0, bool restart = false)
		{
			Pause();
			HandleFPSAndDelay();
			if(restart)
			{
				_spriteIdx = 0;
				_increment = 1;
			}

			if(_sprites.Length == 0)
				return;

			_startTask = schedule.Execute(() =>
										  {
                                              if (_sprites.Length > 0)
                                              {
                                                if( _spriteIdx < 0 || _spriteIdx >= _sprites.Length)
                                                {
                                                    _spriteIdx = 0;
                                                }
                                                this.sprite =  _sprites[_spriteIdx];
                                                _spriteIdx  += _increment;

                                                switch(cycle)
                                                {
                                                    case CycleType.Loop when _spriteIdx >= _sprites.Length:
                                                        _spriteIdx = 0;
                                                        _increment = 1;
                                                        break;
                                                    case CycleType.Reverse when _spriteIdx == _sprites.Length - 1:
                                                        _increment = -1;
                                                        break;
                                                    case CycleType.PingPong when _spriteIdx == _sprites.Length - 1:
                                                        _increment = -1;
                                                        break;
                                                    case CycleType.PingPong when _spriteIdx == 0:
                                                        _increment = 1;
                                                        break;
                                                } 
                                              }
										  })
								 .StartingIn(delay)
								 .Every(_frameTime)
								 .Until(() =>
										{
											var result = false;
											switch(cycle)
											{
												case CycleType.OneShot when _spriteIdx == _sprites.Length - 1:
												case CycleType.Reverse when _spriteIdx == 0 && _increment == -1:
													result = true;
													break;
											}

											return result;
										});
		}

		/// <summary>
		/// Pause the animation.
		/// </summary>
		public void Pause() => _startTask?.Pause();

		/// <summary>
		/// Stop the animation and set the sprite to the first frame.
		/// </summary>
		public void Stop()
		{
			Pause();
			_spriteIdx  = 0;
			this.sprite = _sprites[_spriteIdx];
		}

        
    }
}
