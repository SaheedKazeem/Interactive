using System.IO;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Selects a video source at runtime. On Windows standalone/editor, if a local path is
/// provided and exists, switches the bound VideoPlayer to use that file via URL.
/// Otherwise, leaves the existing VideoClip assignment intact.
/// Attach this to the same GameObject as the VideoPlayer in each scene/"season".
/// </summary>
public class VideoSourceResolver : MonoBehaviour
{
    [Tooltip("If true, uses the Windows local file path when running on Windows.")]
    public bool useWindowsLocalFile = true;

    [Tooltip("Absolute path to the video file on Windows (e.g. C:/Videos/Season1.mp4). Supports file:/// prefix or raw Windows path.")]
    public string windowsLocalPath = string.Empty;

    [Tooltip("Auto play the video after switching source.")]
    public bool autoPlay = true;

    [Tooltip("Call Prepare() before playing to avoid a frame of black.")]
    public bool prepareBeforePlay = true;

    private VideoPlayer videoPlayer;
    private bool applied;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Start()
    {
        TryApplyPlatformSource();
    }

    private void TryApplyPlatformSource()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!useWindowsLocalFile) return;
        if (videoPlayer == null) return;

        var path = windowsLocalPath?.Trim();
        if (string.IsNullOrEmpty(path)) return;

        // Normalize path: allow both raw Windows path and file:/// URL.
        string url = ToVideoUrl(path);

        // If it's a local file, ensure it exists.
        if (IsLocalFileUrl(url))
        {
            string fileSystemPath = FromFileUrl(url);
            if (!File.Exists(fileSystemPath))
            {
                Debug.LogWarning($"VideoSourceResolver: File not found at '{fileSystemPath}'. Keeping existing clip.");
                return;
            }
        }

        // Switch to URL-based source.
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        applied = true;

        if (prepareBeforePlay)
        {
            videoPlayer.prepareCompleted -= OnPrepared; // avoid double subscription
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.Prepare();
        }
        else if (autoPlay)
        {
            videoPlayer.Play();
        }
#endif
    }

    /// <summary>
    /// Public method to (re)apply the configured source. Useful when another
    /// component sets the path at runtime and wants to force an immediate apply.
    /// </summary>
    public void ApplyConfiguredSource()
    {
        TryApplyPlatformSource();
    }

    private void OnPrepared(VideoPlayer vp)
    {
        videoPlayer.prepareCompleted -= OnPrepared;
        if (autoPlay)
        {
            videoPlayer.Play();
        }
    }

    private static string ToVideoUrl(string path)
    {
        // If already a URL, return as-is
        if (path.StartsWith("http://") || path.StartsWith("https://") || path.StartsWith("file:///"))
            return path;

        // Windows paths: C:\foo\bar.mp4 or C:/foo/bar.mp4
        string normalized = path.Replace('\\', '/');
        // Ensure drive letter format C:
        if (normalized.Length > 1 && normalized[1] == ':')
        {
            return $"file:///{normalized}";
        }

        // Fallback: treat as relative; resolve to absolute based on current directory
        string abs = Path.GetFullPath(path).Replace('\\', '/');
        return $"file:///{abs}";
    }

    private static bool IsLocalFileUrl(string url)
    {
        return url.StartsWith("file:///");
    }

    private static string FromFileUrl(string url)
    {
        if (!url.StartsWith("file:///")) return url;
        // Strip file:/// and convert to OS path
        var withoutScheme = url.Substring("file:///".Length);
        return withoutScheme.Replace('/', Path.DirectorySeparatorChar);
    }
}
