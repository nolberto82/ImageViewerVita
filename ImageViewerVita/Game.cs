using System;
using Sce.PlayStation.Core;
using Sce.PlayStation.HighLevel.GameEngine2D;
using Sce.PlayStation.HighLevel.GameEngine2D.Base;
using Sce.PlayStation.Core.Graphics;
using Sce.PlayStation.Core.Environment;
using System.IO;
using Sce.PlayStation.Core.Input;
using Sample;
using SharpCompress.Archive.Zip;
using Sce.PlayStation.Core.Imaging;
using SharpCompress.Archive;
using SharpCompress.Common;
using System.Collections.Generic;

namespace ImageViewerVita
{
	public class Game
	{
		GraphicsContext graphics;
		Font font;
		GamePadData pad;
		SampleSprite sprite;
		List<IArchiveEntry> Pages;
		string[] directories;
		string[] files;
		int dirselection;
		int fileselection;
		int page;
		
		enum State
		{
			DIR,
			FILE,
			VIEW
		}
		State state;

		public Game()
		{
		}
		
		public void MainLoop()
		{
			Initialize();

			while (true)
			{
				SystemEvents.CheckEvents();
				Update();
				Render();
			}
		}
		
		public void Initialize()
		{
			// Set up the graphics system
			graphics = new GraphicsContext();
			SampleDraw.Init(graphics);
			font = new Font(FontAlias.System, 16, FontStyle.Regular);
			GetFiles();
		}
		
		void GetFiles()
		{
			directories = Directory.GetDirectories("Application/Images");
			files = Directory.GetFiles(directories[dirselection]);
		}

		public void Update()
		{
			pad = GamePad.GetData(0);
			
			if (state != State.VIEW)
				BrowseInput();
			else if (state == State.VIEW)
				ReadModeInput();
		}

		public void Render()
		{
			// Clear the screen
			graphics.SetClearColor(0.0f, 0.0f, 0.0f, 0.0f);
			graphics.Clear();

			if (state != State.VIEW)
			{
				if (directories != null)
					DrawDirectories();
				if (files != null)
					DrawFiles();
			} else if (state == State.VIEW)
			{
				DrawImages();
				DrawPageNumber();
			}
			
			//SampleDraw.DrawText(pad.ButtonsPrev.ToString(),0xffffffff,10,100);
			
			// Present the screen
			graphics.SwapBuffers();
		}
		
		void DrawPageNumber()
		{
			SampleDraw.DrawText((page + 1).ToString() + "/" + Pages.Count.ToString(), 0xff000000, 0, 0);
		}
		
		void DrawImages()
		{
			SampleDraw.DrawSprite(sprite);
		}
		
		void LoadImage()
		{
			MemoryStream ms = new MemoryStream();
			Pages[page].OpenEntryStream().CopyTo(ms);
			var image = new Image(ms.ToArray());
			
			if (image != null)
			{
				image = image.Resize(new ImageSize(1024, 2048));
				var texture = new Texture2D(image.Size.Width, image.Size.Height, false, PixelFormat.Rgba);
				texture.SetPixels(0, image.ToBuffer());
				sprite = new SampleSprite(texture, 0, 0);
			}
			ms.Close();
		}
		
		void DrawFiles()
		{
			int x = 0;
			int y = 0;			
			
			DrawRectangle(475, 5, 470, 534, 0xffffffff);
					
			int selection = dirselection;

			if (state == State.FILE)
			{
				x = 470;
				selection = fileselection;
			}
			
			DrawRectangle(x + 5, 5 + (selection * 40), 470, 40, 0xffffff00);
			
			foreach (string s in files)
			{
				int textheight = SampleDraw.CurrentFont.Metrics.Height;
				int valign = VAlign("center", textheight, 40);
				SampleDraw.DrawText(Path.GetFileName(s), 0xffffffff, 480, y + valign);
				y += 40;
			}
		}
		
		void DrawDirectories()
		{
			int x = 0;
			int y = 0;			
			
			DrawRectangle(5, 5, 470, 534, 0xffffffff);
					
			int selection = dirselection;

			if (state == State.FILE)
			{
				x = 470;
				selection = fileselection;
			}
			
			DrawRectangle(x + 5, 5 + (selection * 40), 470, 40, 0xffffff00);
			
			foreach (string s in directories)
			{
				int textheight = SampleDraw.CurrentFont.Metrics.Height;
				int valign = VAlign("center", textheight, 40);
				SampleDraw.DrawText(Path.GetFileName(s), 0xffffffff, 10, y + valign);
				y += 40;
			}
		}
		
		void DrawRectangle(int x, int y, int w, int h, uint argb)
		{
			SampleDraw.FillRect(argb, x, y, w, 1);
			SampleDraw.FillRect(argb, x, y, 1, h);
			SampleDraw.FillRect(argb, x + w, y, 1, h);
			SampleDraw.FillRect(argb, x, y + h, w, 1);
		}
		
		void BrowseInput()
		{
			if (pad.Buttons == GamePadButtons.Right)
				state = State.FILE;
			else if (pad.Buttons == GamePadButtons.Left)
				state = State.DIR;
			else if (pad.ButtonsDown == GamePadButtons.Down)
			{
				if (state == State.DIR && dirselection < directories.Length - 1)
				{
					dirselection++;	
					GetFiles();
				} else if (state == State.FILE && fileselection < files.Length - 1)
					fileselection++;
			} else if (pad.ButtonsDown == GamePadButtons.Up)
			{
				if (state == State.DIR && dirselection > 0)
				{
					dirselection--;
					GetFiles();
				} else if (state == State.FILE && fileselection > 0)
					fileselection--;
			}
				
			if ((pad.ButtonsDown & GamePadButtons.Cross) > 0)
			{
				if (state == State.FILE)
				{
					state = State.VIEW;
					
					Pages = new List<IArchiveEntry>();
					Stream Zip = File.Open(files[fileselection], FileMode.Open, FileAccess.Read);
					IArchive archive = ArchiveFactory.Open(Zip, Options.KeepStreamsOpen);
					foreach (IArchiveEntry archiveEntry in archive.Entries)
					{
						if (!archiveEntry.IsDirectory)
						{
							string a = Path.GetExtension(archiveEntry.FilePath).ToUpper();
							if (a == ".JPG" || a == ".JPEG" || a == ".PNG" || a == ".GIF" || a == ".BMP")
							{
								//var unzippedEntryStream = archiveEntry.OpenEntryStream();//
								//unzippedEntryStream.CopyTo(ms);
								Pages.Add(archiveEntry);
							}
						}
					}
	
					LoadImage();
				}
			}
		}
		
		void ReadModeInput()
		{
			if (pad.Buttons == GamePadButtons.Down)
			{
				if (sprite.PositionY > -(sprite.Texture.Height - 544))
					sprite.PositionY -= 15;
			} else if (pad.Buttons == GamePadButtons.Up)
			{
				if (sprite.PositionY < -1)
					sprite.PositionY += 15;		
			} else if ((pad.ButtonsDown & GamePadButtons.Circle) > 0)
			{
				state = State.FILE;
				page = 0;
			} else if ((pad.ButtonsDown & GamePadButtons.L) > 0)
			{
				if (page > 0)
				{
					page--;
					LoadImage();
				}

			} else if ((pad.ButtonsDown & GamePadButtons.R) > 0)
			{
				if (page < Pages.Count - 1)
				{
					page++;
					LoadImage();
				}
			}
		}
		
		int VAlign(string align, int textheight, int recth)
		{
			if (align == "center")
				return (textheight + 4) / 2;
			else if (align == "bottom")
				return textheight - 2;
			return 5;
		}
	}
}

