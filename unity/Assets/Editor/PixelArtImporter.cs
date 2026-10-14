using UnityEditor;
using UnityEngine;

namespace Politicmon.Editor
{
    /// <summary>
    /// Import rules applied automatically to every texture and sound of the game:
    /// pixel art (point filter, no compression, no mipmaps, 16 px per unit) and lightweight audio.
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/") && !assetPath.StartsWith("Assets/Art/"))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
                importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Resources/Audio/"))
                return;
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            bool isMusic = assetPath.Contains("/Music/");
            settings.loadType = isMusic ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = isMusic ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = 0.5f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
        }
    }
}
