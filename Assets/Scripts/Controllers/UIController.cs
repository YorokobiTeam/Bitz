using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameData gameData;
    private Texture2D bgTexture;
    private Texture2D albumCoverTexture;
    public AudioSource musicPlayer;
    public AudioImporter importer;

    public Image backgroundSR;
    public Image albumCoverSR;
    public TMP_Text songTitle;
    public TMP_Text songAuthor;
    public GameObject gameEndScreen;

    public short totalLoadedAssets = 0;
    public bool isPaused;


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

        bgTextureData = File.ReadAllBytes(gameData.backgroundImageDir);
        albumCoverTextureData = File.ReadAllBytes(gameData.albumCoverImageDir);
        ImageConversion.LoadImage(bgTexture, bgTextureData);
        totalLoadedAssets++;
        ImageConversion.LoadImage(albumCoverTexture, albumCoverTextureData);
        totalLoadedAssets++;

        TextureScaler.scale(albumCoverTexture, 200, 200);
        var bgSprite = Sprite.Create(bgTexture, new Rect(0, 0, 1920, 1080), new Vector2(0.5f, 0.5f));
        var albumCoverSrpite = Sprite.Create(albumCoverTexture, new Rect(0, 0, albumCoverTexture.width, albumCoverTexture.height), new Vector2(0.5f, 0.5f));

        this.backgroundSR.sprite = bgSprite;
        this.albumCoverSR.sprite = albumCoverSrpite;
        this.songAuthor.text = gameData.beatmapData.songAuthor;
        this.songTitle.text = gameData.beatmapData.songTitle;
        importer.Import(gameData.musicFileDir);
        this.OnUpdateGameState += HandleGameEnd;
    }

    private void HandleGameEnd(GameState gameState)
    {
        if (gameState == GameState.Ended)
        {
            this.gameEndScreen.SetActive(true);
        }
    }



    public void Update()
    {
        if (musicPlayer.clip is null) return;
        GameState currentGameState = this.gameData.gameState;

        if (!this.musicPlayer.isPlaying)
        {
            if (this.musicPlayer.time >= this.musicPlayer.clip.length)
            {
                currentGameState = GameState.Ended;
            }
            else if (currentGameState != GameState.Ended)
            {
                currentGameState = GameState.Paused;
            }
        }
        else
        {
            currentGameState = GameState.Ingame;
        }
        if (currentGameState != gameData.gameState)
        {
            this.gameData.gameState = currentGameState;
            this.OnUpdateGameState(currentGameState);
        }
    }

    public void StartPlay()
    {
        this.musicPlayer.Play();
    }

    public event UpdateGameState OnUpdateGameState;
    public event FinishLoadingAssets OnFinishLoadingAssets;

    public delegate void FinishLoadingAssets();
    public delegate void UpdateGameState(GameState gameState);

}
