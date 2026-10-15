using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace SoRCharacters
{
    // RogueLibs.CreateCustomAudio writes the bytes into BepInEx/cache and requests "file:///" + path without escaping,
    // which Unity's curl rejects when the game path has a space ("Streets of Rogue"). This loader avoids the problem:
    // WAV is decoded in managed code, OGG/MP3 go through a properly escaped file URI.
    internal static class ModAudio
    {
        private const int TimeoutMilliseconds = 5000;

        private static readonly List<AudioClip> Known = new List<AudioClip>();

        public static string LastError { get; private set; } = "";

        public static bool TryLoadWav(byte[] data, string name, out AudioClip? clip)
        {
            clip = null;
            try
            {
                if (!ParseWav(data, out var samples, out var channels, out var rate, out var error))
                {
                    LastError = error;
                    return false;
                }

                var frames = samples.Length / channels;
                var created = AudioClip.Create(name, frames, channels, rate, false);
                if (!created.SetData(samples, 0))
                {
                    UnityEngine.Object.Destroy(created);
                    LastError = "Unity rejected the sample data";
                    return false;
                }

                clip = created;
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return false;
            }
        }

        public static bool TryLoadFile(string path, string name, out AudioClip? clip)
        {
            clip = null;
            try
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension == ".wav") return TryLoadWav(File.ReadAllBytes(path), name, out clip);

                AudioType type;
                if (extension == ".ogg") type = AudioType.OGGVORBIS;
                else if (extension == ".mp3") type = AudioType.MPEG;
                else
                {
                    LastError = "unsupported file type " + extension;
                    return false;
                }

                using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, type))
                {
                    request.SendWebRequest();
                    var waited = 0;
                    while (!request.isDone && waited < TimeoutMilliseconds)
                    {
                        Thread.Sleep(1);
                        waited++;
                    }

                    if (!request.isDone)
                    {
                        request.Abort();
                        LastError = "timed out after " + TimeoutMilliseconds / 1000 + " s";
                        return false;
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        LastError = request.error;
                        return false;
                    }

                    var loaded = DownloadHandlerAudioClip.GetContent(request);
                    if (loaded == null)
                    {
                        LastError = "the file decoded to nothing";
                        return false;
                    }

                    loaded.name = name;
                    clip = loaded;
                    return true;
                }
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return false;
            }
        }

        public static void Register(AudioClip clip)
        {
            if (!Known.Contains(clip)) Known.Add(clip);
            EnsureRegistered();
        }

        // The game never clears audioClipDic, but a new AudioHandler would start with an empty one, so this runs from
        // the plugin tick as well and only adds what is missing.
        public static void EnsureRegistered()
        {
            if (Known.Count == 0) return;
            var handler = GameController.gameController?.audioHandler;
            if (handler == null) return;

            foreach (var clip in Known)
            {
                if (clip == null || handler.audioClipDic.ContainsKey(clip.name)) continue;
                handler.audioClipList.Add(clip.name);
                handler.audioClipRealList.Add(clip);
                handler.audioClipDic.Add(clip.name, clip);
            }
        }

        public static bool IsRegistered(string name)
        {
            var handler = GameController.gameController?.audioHandler;
            return handler != null && handler.audioClipDic.ContainsKey(name);
        }

        private static bool ParseWav(byte[] data, out float[] samples, out int channels, out int rate, out string error)
        {
            samples = Array.Empty<float>();
            channels = 0;
            rate = 0;

            if (data.Length < 12 || data[0] != 'R' || data[1] != 'I' || data[2] != 'F' || data[3] != 'F'
                || data[8] != 'W' || data[9] != 'A' || data[10] != 'V' || data[11] != 'E')
            {
                error = "not a RIFF/WAVE file";
                return false;
            }

            var haveFormat = false;
            var position = 12;
            while (position + 8 <= data.Length)
            {
                var id = System.Text.Encoding.ASCII.GetString(data, position, 4);
                var size = BitConverter.ToUInt32(data, position + 4);
                var body = position + 8;
                var available = (long)data.Length - body;

                if (id == "fmt ")
                {
                    if (size < 16 || available < 16)
                    {
                        error = "the fmt chunk is truncated";
                        return false;
                    }

                    var tag = BitConverter.ToUInt16(data, body);
                    if (tag == 0xFFFE && size >= 26 && available >= 26) tag = BitConverter.ToUInt16(data, body + 24);
                    channels = BitConverter.ToUInt16(data, body + 2);
                    rate = (int)BitConverter.ToUInt32(data, body + 4);
                    var bits = BitConverter.ToUInt16(data, body + 14);
                    if (tag != 1 || bits != 16)
                    {
                        error = $"only 16-bit PCM WAV is supported (format {tag}, {bits}-bit)";
                        return false;
                    }

                    if (channels < 1 || channels > 2 || rate <= 0)
                    {
                        error = $"unsupported WAV layout ({channels} channels, {rate} Hz)";
                        return false;
                    }

                    haveFormat = true;
                }
                else if (id == "data")
                {
                    if (!haveFormat)
                    {
                        error = "the data chunk comes before the fmt chunk";
                        return false;
                    }

                    var length = (int)Math.Min(size, (uint)Math.Min(available, int.MaxValue));
                    var count = length / 2 / channels * channels;
                    if (count == 0)
                    {
                        error = "the WAV has no sample data";
                        return false;
                    }

                    samples = new float[count];
                    for (var i = 0; i < count; i++)
                    {
                        samples[i] = BitConverter.ToInt16(data, body + i * 2) / 32768f;
                    }

                    error = "";
                    return true;
                }

                var next = body + (long)size + (size & 1);
                if (next > data.Length) break;
                position = (int)next;
            }

            error = haveFormat ? "no data chunk found" : "no fmt chunk found";
            return false;
        }
    }
}
