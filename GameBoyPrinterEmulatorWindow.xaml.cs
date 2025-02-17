﻿using RetroSpy.Readers;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RetroSpy
{
    /// <summary>
    /// Interaction logic for GameBoyPrinterEmulatorWindow.xaml
    /// </summary>
    public partial class GameBoyPrinterEmulatorWindow : Window, INotifyPropertyChanged
    {
        private readonly int TILE_PIXEL_WIDTH = 8;
        private readonly int TILE_PIXEL_HEIGHT = 8;
        private readonly int TILES_PER_LINE = 20; // Gameboy Printer Tile Constant

        private readonly byte[] colors_red = { 0xff, 0xaa, 0x55, 0x00 };
        private readonly byte[] colors_green = { 0xff, 0xaa, 0x55, 0x00 };
        private readonly byte[] colors_blue = { 0xff, 0xaa, 0x55, 0x00 };

        private readonly byte[] DMG_colors_red = { 0x9b, 0x8b, 0x30, 0x0f };
        private readonly byte[] DMG_colors_green = { 0xbc, 0xac, 0x62, 0x38 };
        private readonly byte[] DMG_colors_blue = { 0x0f, 0x0f, 0x30, 0x0f };

        private readonly Image _image;
        private BitmapPixelMaker _imageBuffer;
        private readonly IControllerReader _reader;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _DMGPaletteEnabled;

        public bool DMGPaletteEnabled
        {
            get => _DMGPaletteEnabled;
            set
            {
                _DMGPaletteEnabled = value;
                NotifyPropertyChanged(nameof(DMGPaletteEnabled));
            }
        }

        private void DMGPaletteEnabled_Click(object sender, RoutedEventArgs e)
        {

            for (int i = 0; i < 4; ++i)
            {
                if (DMGPaletteEnabled)
                {
                    _imageBuffer.ReplaceColor(DMG_colors_red[i], DMG_colors_green[i], DMG_colors_blue[i],
                                                colors_red[i], colors_green[i], colors_blue[i]);
                }
                else
                {
                    _imageBuffer.ReplaceColor(colors_red[i], colors_green[i], colors_blue[i],
                                                DMG_colors_red[i], DMG_colors_green[i], DMG_colors_blue[i]);
                }
            }
            WriteableBitmap wbitmap = _imageBuffer.MakeBitmap(96, 96);
            _image.Source = wbitmap;

            DMGPaletteEnabled = !DMGPaletteEnabled;
            Properties.Settings.Default.DMGPaletteEnabled = DMGPaletteEnabled;
        }

        public GameBoyPrinterEmulatorWindow(IControllerReader reader)
        {
            InitializeComponent();
            DataContext = this;

            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            DMGPaletteEnabled = Properties.Settings.Default.DMGPaletteEnabled;

            _imageBuffer = new BitmapPixelMaker(480, 432);

            if (DMGPaletteEnabled)
            {
                _imageBuffer.SetColor(DMG_colors_red[3], DMG_colors_green[3], DMG_colors_blue[3], 255);
            }
            else
            {
                _imageBuffer.SetColor(colors_red[3], colors_green[3], colors_blue[3], 255);
            }

            WriteableBitmap wbitmap = _imageBuffer.MakeBitmap(96, 96);

            // Create an Image to display the bitmap.
            _image = new Image
            {
                Stretch = Stretch.None,
                Margin = new Thickness(0)
            };

            _ = GameBoyPrinterEmulatorWindowGrid.Children.Add(_image);
            _image.Source = wbitmap;

            _reader.ControllerStateChanged += Reader_ControllerStateChanged;
            _reader.ControllerDisconnected += Reader_ControllerDisconnected;


        }

        private void Reader_ControllerDisconnected(object sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess())
            {
                Close();
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    Close();
                });
            }
        }

        private void Reader_ControllerStateChanged(object reader, ControllerStateEventArgs e)
        {
            _imageBuffer.SetColor(0, 0, 0, 255);

            int square_width = 480 / (TILE_PIXEL_WIDTH * TILES_PER_LINE);
            int square_height = square_width;

            string[] tiles_rawBytes_array = e.RawPrinterData.Split('\n');

            int total_tile_count = 0;

            for (int tile_i = 0; tile_i < tiles_rawBytes_array.Length; tile_i++)
            {
                string tile_element = tiles_rawBytes_array[tile_i];

                // Check for invalid raw lines
                if (tile_element.Length == 0)
                {   // Skip lines with no bytes (can happen with .split() )
                    continue;
                }
                else if (tile_element.StartsWith("!", StringComparison.Ordinal))
                {   // Skip lines used for comments
                    continue;
                }
                else if (tile_element.StartsWith("#", StringComparison.Ordinal))
                {   // Skip lines used for comments
                    continue;
                }
                else if (tile_element.StartsWith("//", StringComparison.Ordinal))
                {   // Skip lines used for comments
                    continue;
                }
                else if (tile_element.StartsWith("{", StringComparison.Ordinal))
                {   // Skip lines used for comments
                    continue;
                }
                total_tile_count++;
            }

            int tile_height_count = total_tile_count / TILES_PER_LINE;

            _imageBuffer = new BitmapPixelMaker(square_width * TILE_PIXEL_WIDTH * TILES_PER_LINE, square_height * TILE_PIXEL_HEIGHT * tile_height_count);

            _image.Height = square_height * TILE_PIXEL_HEIGHT * tile_height_count;
            _image.Width = square_width * TILE_PIXEL_WIDTH * TILES_PER_LINE;
            GameBoyPrinterEmulatorWindowGrid.Height = square_height * TILE_PIXEL_HEIGHT * tile_height_count; ;
            GameBoyPrinterEmulatorWindowGrid.Width = square_width * TILE_PIXEL_WIDTH * TILES_PER_LINE;
            Height = square_height * TILE_PIXEL_HEIGHT * tile_height_count;
            Width = square_width * TILE_PIXEL_WIDTH * TILES_PER_LINE;

            int tile_count = 0;

            for (int tile_i = 0; tile_i < tiles_rawBytes_array.Length; tile_i++)
            {
                string tile_element = tiles_rawBytes_array[tile_i];

                // Check for invalid raw lines
                if (tile_element.Length == 0)
                {   // Skip lines with no bytes (can happen with .split() )
                    continue;
                }
                else if (tile_element.StartsWith("!", StringComparison.Ordinal))
                {   // Skip lines used for comments
                    continue;
                }
                else if (tile_element.StartsWith("#", StringComparison.Ordinal))
                {   // Skip lines used for comments
                    continue;
                }
                else if (tile_element.StartsWith("//", StringComparison.Ordinal))
                {   // Skip lines used for comments
                    continue;
                }
                else if (tile_element.StartsWith("{", StringComparison.Ordinal))
                {   // Skip lines used for comments
                    continue;
                }

                // Gameboy Tile Offset
                int tile_x_offset = tile_count % TILES_PER_LINE;
                int tile_y_offset = tile_count / TILES_PER_LINE;

                byte[] pixels = Decode(tile_element);

                if (pixels != null)
                {
                    Paint(_imageBuffer, pixels, square_width, square_height, tile_x_offset, tile_y_offset);
                }
                else
                {
                    //status = false;
                }


                // Increment Tile Count Tracker
                tile_count++;

            }

            //imageBuffer.SetColor(0, 0, 0);
            // Convert the pixel data into a WriteableBitmap.
            WriteableBitmap wbitmap = _imageBuffer.MakeBitmap(96, 96);

            // Set the Image source.
            _image.Source = wbitmap;
        }

        private void Paint(BitmapPixelMaker canvas, byte[] pixels, int pixel_width, int pixel_height, int tile_x_offset, int tile_y_offset)
        {   // This paints the tile with a specified offset and pixel width

            int pixel_x_offset = TILE_PIXEL_WIDTH * tile_x_offset * pixel_width;
            int pixel_y_offset = TILE_PIXEL_HEIGHT * tile_y_offset * pixel_height;


            for (int i = 0; i < TILE_PIXEL_WIDTH; i++)
            {   // pixels along the tile's x axis
                for (int j = 0; j < TILE_PIXEL_HEIGHT; j++)
                {   // pixels along the tile's y axis

                    canvas.SetRect(pixel_x_offset + (i * pixel_width),
                            pixel_y_offset + (j * pixel_height),
                            pixel_width,
                            pixel_height,
                            DMGPaletteEnabled ? DMG_colors_red[pixels[(j * TILE_PIXEL_WIDTH) + i]] : colors_red[pixels[(j * TILE_PIXEL_WIDTH) + i]],
                            DMGPaletteEnabled ? DMG_colors_green[pixels[(j * TILE_PIXEL_WIDTH) + i]] : colors_green[pixels[(j * TILE_PIXEL_WIDTH) + i]],
                            DMGPaletteEnabled ? DMG_colors_blue[pixels[(j * TILE_PIXEL_WIDTH) + i]] : colors_blue[pixels[(j * TILE_PIXEL_WIDTH) + i]]);
                }
            }
        }

        private byte[] Decode(string rawBytes)
        {
            string bytes = rawBytes.Replace(" ", "").Replace("\r", "");
            if (bytes.Length != 32)
            {
                return null;
            }

            byte[] byteArray = new byte[16];
            for (int i = 0; i < byteArray.Length; i++)
            {
                byteArray[i] = byte.Parse(bytes.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
            }

            byte[] pixels = new byte[TILE_PIXEL_WIDTH * TILE_PIXEL_HEIGHT];
            for (int j = 0; j < TILE_PIXEL_HEIGHT; j++)
            {
                for (int i = 0; i < TILE_PIXEL_WIDTH; i++)
                {
                    byte hiBit = (byte)((byteArray[(j * 2) + 1] >> (7 - i)) & 1);
                    byte loBit = (byte)((byteArray[j * 2] >> (7 - i)) & 1);
                    pixels[(j * TILE_PIXEL_WIDTH) + i] = (byte)((hiBit << 1) | loBit);
                }
            }
            return pixels;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            _reader.Finish();
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                AddExtension = true,
                Filter = Properties.Resources.ResourceManager.GetString("PNGFilter", CultureInfo.CurrentUICulture)
            };
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                SaveUsingEncoder(_image, saveFileDialog.FileName, encoder);
            }
            saveFileDialog.Dispose();
        }

        private static void SaveUsingEncoder(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (FileStream stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }

    }
}
