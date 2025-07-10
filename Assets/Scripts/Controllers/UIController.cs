using JetBrains.Annotations;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    public GameData gameData;
    public UIDocument uiDocument;
    private Texture2D bgTexture;
    private Texture2D albumCoverTexture;
    public AudioSource musicPlayer;
    public AudioImporter importer;

    public SpriteRenderer backgroundSR;
    public SpriteRenderer albumCoverSR;
    public short totalLoadedAssets = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Awake()
    {
    }

    private void OnAudioLoaded(AudioClip obj)
    {
        this.totalLoadedAssets++;
        this.musicPlayer.clip = obj;
        this.OnFinishLoadingAssets?.Invoke();
    }

    public void Start()
    {
        ReadOnlySpan<byte> bgTextureData;
        ReadOnlySpan<byte> albumCoverTextureData;
        bgTexture = new(Screen.width, Screen.height);
        albumCoverTexture = new(200, 200);
        importer.Loaded += OnAudioLoaded;

        try
        {
            bgTextureData = File.ReadAllBytes(gameData.backgroundImageDir);
            albumCoverTextureData = File.ReadAllBytes(gameData.albumCoverImageDir);
            ImageConversion.LoadImage(bgTexture, bgTextureData);
            totalLoadedAssets++;
            ImageConversion.LoadImage(albumCoverTexture, albumCoverTextureData);
            totalLoadedAssets++;

            TextureScaler.scale(bgTexture, Screen.width, Screen.height);
            TextureScaler.scale(albumCoverTexture, 250, 250);

            var bgSprite = Sprite.Create(bgTexture, new Rect(0, 0, Screen.width, Screen.height), new Vector2(0.5f, 0.5f));
            var albumCoverSrpite = Sprite.Create(albumCoverTexture, new Rect(0, 0, 250, 250), new Vector2(0.5f, 0.5f));


            importer.Import(gameData.musicFileDir);

        }
        catch (Exception e)
        {
            Debug.Log("Couldnt load textures");
            Debug.Log(e);
        }

    }
    public event FinishLoadingAssets OnFinishLoadingAssets;
    public delegate void FinishLoadingAssets();

}
