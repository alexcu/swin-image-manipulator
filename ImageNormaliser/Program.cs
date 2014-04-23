using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using ConcurrencyUtils;
using AlexIO;
namespace ImageNormaliser
{
    class MainClass
    {
        /// <summary>
        /// The barrier for processing
        /// </summary>
        private static ConcurrencyUtils.Barrier _reached;

        /// <summary>
        /// The ranges for each chunk
        /// </summary>
        private static BrightnessRange[] _rangesForChunk;
       

        /// <summary>
        /// The final normalised range
        /// </summary>
        private static BrightnessRange _normalisedRange;

        /// The struct for containing max and min ranges for brightness
        private struct BrightnessRange
        {
            public float min;
            public float max;
        }

        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main (string[] args)
        {
            string fname = args [0];

            // Load in bitmap given it exists!
            if (!File.Exists (fname))
                throw new FileLoadException ("Image could not be loaded from file", fname);
            Bitmap img = new Bitmap (fname);

            // Image was loaded successfully!
            UserIO.Log (string.Format ("{0} loaded!", fname));

            UserIO.Log ("Hit enter to start...");
            Console.ReadLine ();

            do
            {
                Helper.FlushLog();
                UserIO.Log ("Nominal chunk sizes (fits in with image height):");

                string sizeNotif = "";

                // Determine correct chunks to use...
                for (int i = 2; i < img.Height; i++)
                    // Divisible by i?
                    if (img.Height % i == 0)
                        sizeNotif += i + ", ";

                UserIO.Log(sizeNotif);

                int chunks = Convert.ToInt32 (UserIO.Prompt ("Enter in chunks to use"));
                RunNormalisation (img, chunks);
            } while (true);
        }

        /// <summary>
        /// Runs the normalisation.
        /// </summary>
        /// <param name="img">Bitmap to run normalisation on.</param>
        /// <param name="chunks">Chunks to run normalisation with.</param>
        private static void RunNormalisation(Bitmap img, int chunks)
        {
            if (chunks == 1)
            {
                UserIO.Log ("That will cause a deadlock!");
                return;
            }
            else if ( chunks < 1 )
            {
                UserIO.Log ("I need at least two chunks to work with!");
                return;
            }

            // Reinitialise barrier and brightness ranges based on chunk count
            _reached = new ConcurrencyUtils.Barrier (chunks);
            _rangesForChunk = new BrightnessRange[chunks];

            // Load in pixels (wrap in try/catch block for overchunk size exception)
            Color[][,] pixels;
            try
            {
                pixels = LoadPixels (img, chunks);
            }
            catch (ArgumentOutOfRangeException e)
            {
                // Cannot process with more chunks than the image height!
                UserIO.Log (e.ParamName);
                return;
            }

            // Loading done!
            UserIO.Log (string.Format ("Using {0} threads for image normalisation...", chunks));

            // Start timing threads to finish...
            DateTime start = DateTime.Now;

            // Create bunch of new threads that process their thread number'th chunk
            Thread[] processors = Helper.BunchOfNewThreads(chunks, 
                ()=> NormalizeRange
                (
                    ref pixels[Helper.CurrentThreadInteger], 
                    Helper.CurrentThreadInteger
                ));

            // Start each processor
            foreach (Thread t in processors)
                t.Start ();

            // Join each thread back to main before image reassembly
            foreach (Thread t in processors)
                t.Join ();

            // All threads stopped now...
            DateTime stop = DateTime.Now;

            UserIO.Log ("Reassembling output image...");

            // Reassemble image
            ReassembleImage (img, pixels).Save ("output.png");

            UserIO.Log (string.Format ("Done! Threads took just {0}s!", (stop - start)));
        }

        /// <summary>
        /// Reassembles the original image with its normalised pixels.
        /// </summary>
        /// <returns>The normalised image.</returns>
        /// <param name="orgImg">Orginal image.</param>
        /// <param name="pixels">Pixels after normalisation.</param>
        private static Bitmap ReassembleImage(Bitmap orgImg, Color[][,] pixels)
        {
            // New image based off original image's width and height
            Bitmap retVal = new Bitmap (orgImg.Width, orgImg.Height);

            int numberOfChunks = pixels.Length;
            int chunkHSz = orgImg.Height / numberOfChunks;

            for (int chunk = 0; chunk < numberOfChunks; chunk++)
                for (int x = 0; x < orgImg.Width; x++)
                    for (int y = 0; y < chunkHSz; y++)
                    {
                        // The chunk pixel is the current x and y for this chunk...
                        Color chunkPx = pixels [chunk] [x, y];

                        // Actual y is the y factored in for the relative chunk
                        // for the final image
                        int actY = y + (chunkHSz * chunk);
                        retVal.SetPixel (x, actY, chunkPx);
                    }
            return retVal;
        }


        /// <summary>
        /// Loads in the pixels from the orginal image
        /// </summary>
        /// <returns>The pixels.</returns>
        /// <param name="img">Unnormalised Image.</param>
        /// <param name="numberOfChunks">Number of chunks used to process the image.</param>
        private static Color[][,] LoadPixels(Bitmap img, int numberOfChunks)
        {
            if (img.Height < numberOfChunks)
                throw new ArgumentOutOfRangeException ("Number of chunks exceed the number of vertical pixels for this image!");

            // Work out the number of chunks
            int chunkHSz = img.Height / numberOfChunks;

            // Return value is the number of chunks and the image width:
            // [ ---> ] chunk1
            // [ ---> ] chunk2 etc.
            Color[][,] retVal = new Color[numberOfChunks][,];

            // For each chunk of the image
            for (int chunk = 0; chunk < numberOfChunks; chunk++)
            {
                // Intialise this chunk range
                retVal [chunk] = new Color[img.Width, chunkHSz];
                // Load in the width into the chunk
                for (int x = 0; x < img.Width; x++)
                {
                    // Load in the y going down
                    for (int y = 0; y < chunkHSz; y++)
                    {
                        // Actual y is the y factored in for the relative chunk
                        int actY = y + (chunkHSz * chunk);
                        Color chunkPx = img.GetPixel (x, actY);
                        // Load into this x/chunk'sY the pixel at x, y
                        retVal [chunk] [x, y] = chunkPx;
                    }
                }
            }
            return retVal;
        }

        private static void NormalizeRange(ref Color[,] pixels, int chunkNo)
        {
            // Work out range for this chunk
            _rangesForChunk[chunkNo] = DetermineBrightnessRange (pixels);

            Helper.LogThread ("****");
            // Arrive at barrier
            if ( _reached.Arrive() )
            {
                Helper.LogThread ("CAPT");
                // Last out? Work out range for all ranges
                foreach (BrightnessRange range in _rangesForChunk)
                {
                    // New best max?
                    if (range.max > _normalisedRange.max)
                        _normalisedRange.max = range.max;
                    // New best min?
                    if (range.min < _normalisedRange.min)
                        _normalisedRange.min = range.min;
                }

                // Wait to arrive once more...
                Helper.LogThread ("****");
                _reached.Arrive ();
                Helper.LogThread ("GO");
            }
            else
            {
                Helper.LogThread("****");
                // Not last out? Wait again at barrier...
                _reached.Arrive ();
                Helper.LogThread ("GO");
            }


            // Once both have arrived, then we can perform normalisation on each pixel
            // in this chunk
            for (int x = 0; x < pixels.GetLength (0); x++)
                for (int y = 0; y < pixels.GetLength (1); y++)
                    pixels [x, y] = PerformFunkyEffect (pixels [x, y],chunkNo);
        }

        /// <summary>
        /// Performs normalisation on the given pixel
        /// </summary>
        /// <returns>The normalisation for this image.</returns>
        /// <param name="pixel">Pixel to process.</param>
        /// <param name="oldRange">Old range of this chunk.</param>
        /// <param name="newRange">New range of the chunk.</param>
        private static Color PerformNormalisation(Color pixel, BrightnessRange oldRange, BrightnessRange newRange)
        {
            // Modifier Formula from Wikipedia
            double normalizedModifier = (pixel.GetBrightness () - oldRange.min) * ((newRange.max - newRange.min) / (oldRange.max - oldRange.min)) + newRange.min;

            // Work out new r,g,b based on modifier:
            int newR = (int)Math.Ceiling(pixel.R * normalizedModifier);
            int newG = (int)Math.Ceiling(pixel.G * normalizedModifier);
            int newB = (int)Math.Ceiling(pixel.B * normalizedModifier);

            // Return a new color based on the new rgb
            return Color.FromArgb (newR, newG, newB);
        }

        /// <summary>
        /// Performs the funky effect on an image:
        /// Not sure if the modifier formula is right above... output looks funky
        /// Since this isn't a class on image normalisation, I'm just going to
        /// swap pixels around based on their chunkNo
        /// </summary>
        /// <returns>The funky effect.</returns>
        /// <param name="pixel">Pixel.</param>
        /// <param name="chunkNo">Chunk no.</param>
        private static Color PerformFunkyEffect(Color pixel, int chunkNo)
        {
            int newR, newG, newB;
            // Different rgb based on divisble chunk no
            if (chunkNo % 3 == 0)
            {
                newR = pixel.R;
                newG = pixel.B;
                newB = pixel.G;
            }
            else if (chunkNo % 2 == 0)
            {
                newR = pixel.B;
                newG = pixel.G;
                newB = pixel.R;
            }
            else
            {
                newR = pixel.G;
                newG = pixel.R;
                newB = pixel.B;
            }


            // Return a new color based on the new rgb
            return Color.FromArgb (newR, newG, newB);
        }

        /// <summary>
        /// Determines the brightness range for the given pixels.
        /// </summary>
        /// <returns>The brightness range for these pixels.</returns>
        /// <param name="pixels">Pixels to determine range from.</param>
        private static BrightnessRange DetermineBrightnessRange(Color[,] pixels)
        {
            BrightnessRange retVal;

            // Default max and min based on 0,0'th pixel
            retVal.max = pixels [0, 0].GetBrightness ();
            retVal.min = pixels [0, 0].GetBrightness ();

            // Work out for each pixel
            foreach (Color c in pixels)
            {
                float b = c.GetBrightness ();
                // New max?
                if (b > retVal.max)
                    retVal.max = b;
                // New min?
                else if (b < retVal.min)
                    retVal.min = b;
            }

            return retVal;
        }

    }
}
