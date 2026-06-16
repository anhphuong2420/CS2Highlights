using CS2Highlights.Core.Models;
using CS2Highlights.Renderer;

namespace CS2Highlights.Tests.Renderer;

[TestFixture]
public class CfgScriptBuilderTests
{
    private CfgScriptBuilder _builder = null!;
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _builder = new CfgScriptBuilder();
        _tempDir = Path.Combine(Path.GetTempPath(), $"cs2hl_test_{Guid.NewGuid():N}");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private RenderJob MakeJob(
        int highlightId = 7,
        int tickStart = 10000, int tickEnd = 12000,
        string steamId = "76561198067800921",
        string demoPath = @"C:\demos\test.dem",
        int bufBefore = 5, int bufAfter = 3,
        string encoder = "h264_nvenc", string resolution = "1920x1080", int fps = 60) => new()
    {
        HighlightId   = highlightId,
        DemoPath      = demoPath,
        TickStart     = tickStart,
        TickEnd       = tickEnd,
        PlayerSteamId = steamId,
        Settings = new RenderSettings
        {
            BufferBeforeSeconds = bufBefore,
            BufferAfterSeconds  = bufAfter,
            Encoder             = encoder,
            OutputResolution    = resolution,
            OutputFps           = fps
        }
    };

    private (IReadOnlyList<BatchResult> results, string content) BuildSingle(RenderJob job)
    {
        var results = _builder.Build([job],
            Path.Combine(_tempDir, "cfg"),
            Path.Combine(_tempDir, "clips"));
        return (results, File.ReadAllText(results[0].CfgPath));
    }

    // ---- File creation ----

    [Test]
    public void Build_creates_cfg_file_named_after_demo()
    {
        var (results, _) = BuildSingle(MakeJob());
        Assert.That(File.Exists(results[0].CfgPath), Is.True);
        Assert.That(Path.GetFileName(results[0].CfgPath), Does.StartWith("test_"));
        Assert.That(results[0].CfgPath, Does.EndWith(".cfg"));
    }

    [Test]
    public void Build_returns_clip_path_with_mp4_extension()
    {
        var (results, _) = BuildSingle(MakeJob());
        Assert.That(results[0].Clips[0].ClipPath, Does.EndWith("clip.mp4"));
        Assert.That(results[0].Clips[0].ClipPath, Does.Contain("clip_7"));
    }

    [Test]
    public void Empty_jobs_returns_empty_results()
    {
        var results = _builder.Build([], _tempDir, _tempDir);
        Assert.That(results, Is.Empty);
    }

    // ---- Tick calculations ----

    [Test]
    public void Start_tick_includes_before_buffer()
    {
        // tickStart=10000, bufBefore=5 → 5*64=320 → startTick=9680
        var (_, content) = BuildSingle(MakeJob(tickStart: 10000, bufBefore: 5));
        Assert.That(content, Does.Contain("addAtTick 9680 "));
    }

    [Test]
    public void Stop_tick_includes_after_buffer()
    {
        // tickEnd=12000, bufAfter=3 → 3*64=192 → stopTick=12192
        var (_, content) = BuildSingle(MakeJob(tickEnd: 12000, bufAfter: 3));
        Assert.That(content, Does.Contain("addAtTick 12192 "));
    }

    [Test]
    public void Seek_tick_is_one_second_before_start_tick()
    {
        // bufferStartTick=9680, seekTick=9680-64=9616
        var (_, content) = BuildSingle(MakeJob(tickStart: 10000, bufBefore: 5));
        Assert.That(content, Does.Contain("demo_gototick 9616"));
    }

    [Test]
    public void Buffer_before_does_not_go_below_zero()
    {
        var (_, content) = BuildSingle(MakeJob(tickStart: 100, bufBefore: 5));
        Assert.That(content, Does.Contain("demo_gototick 0"));
        Assert.That(content, Does.Contain("addAtTick 0 "));
    }

    // ---- AccountId ----

    [Test]
    public void AccountId_computed_correctly_from_steamid64()
    {
        // 76561198067800921 - 76561197960265728 = 107535193
        var (_, content) = BuildSingle(MakeJob(steamId: "76561198067800921"));
        Assert.That(content, Does.Contain("spec_lock_to_accountid 107535193"));
    }

    [Test]
    public void Invalid_steamid_produces_accountid_zero()
    {
        var (_, content) = BuildSingle(MakeJob(steamId: "bad"));
        Assert.That(content, Does.Contain("spec_lock_to_accountid 0"));
    }

    // ---- Paths ----

    [Test]
    public void Demo_path_appears_in_playdemo_command()
    {
        var (_, content) = BuildSingle(MakeJob(demoPath: @"C:\demos\match_abc.dem"));
        Assert.That(content, Does.Contain(@"playdemo ""C:\demos\match_abc.dem"""));
    }

    [Test]
    public void Clip_dir_uses_forward_slashes_inside_mirv_cmd()
    {
        var (_, content) = BuildSingle(MakeJob());
        // path inside nested quoted command uses forward slashes
        Assert.That(content, Does.Contain("mirv_streams record name"));
        Assert.That(content, Does.Not.Contain(@"record name ""C:\"));
    }

    // ---- FFmpeg args ----

    [Test]
    public void H264_nvenc_encoder_args_in_cfg()
    {
        var (_, content) = BuildSingle(MakeJob(encoder: "h264_nvenc"));
        Assert.That(content, Does.Contain("h264_nvenc"));
        Assert.That(content, Does.Contain("-preset p4"));
    }

    [Test]
    public void Libx264_encoder_args_in_cfg()
    {
        var (_, content) = BuildSingle(MakeJob(encoder: "libx264"));
        Assert.That(content, Does.Contain("libx264"));
        Assert.That(content, Does.Contain("-crf 18"));
    }

    [Test]
    public void Resolution_converted_to_scale_filter()
    {
        var (_, content) = BuildSingle(MakeJob(resolution: "1920x1080"));
        Assert.That(content, Does.Contain("scale=1920:1080"));
    }

    [Test]
    public void Hlae_template_variables_are_literal_in_cfg()
    {
        var (_, content) = BuildSingle(MakeJob());
        Assert.That(content, Does.Contain("{QUOTE}"));
        Assert.That(content, Does.Contain("{AFX_STREAM_PATH}"));
    }

    [Test]
    public void Output_fps_in_record_fps_command()
    {
        var (_, content) = BuildSingle(MakeJob(fps: 120));
        Assert.That(content, Does.Contain("mirv_streams record fps 120"));
    }

    // ---- Multi-clip batching ----

    [Test]
    public void Two_jobs_same_demo_produce_one_cfg()
    {
        var job1 = MakeJob(highlightId: 1, tickStart: 5000,  tickEnd: 6000);
        var job2 = MakeJob(highlightId: 2, tickStart: 15000, tickEnd: 16000);

        var results = _builder.Build([job1, job2],
            Path.Combine(_tempDir, "cfg"),
            Path.Combine(_tempDir, "clips"));

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Clips, Has.Count.EqualTo(2));
    }

    [Test]
    public void Two_jobs_different_demos_produce_two_cfgs()
    {
        var job1 = MakeJob(highlightId: 1, demoPath: @"C:\demos\match_a.dem");
        var job2 = MakeJob(highlightId: 2, demoPath: @"C:\demos\match_b.dem");

        var results = _builder.Build([job1, job2],
            Path.Combine(_tempDir, "cfg"),
            Path.Combine(_tempDir, "clips"));

        Assert.That(results, Has.Count.EqualTo(2));
    }

    [Test]
    public void Multi_clip_cfg_has_two_addAtTick_start_commands()
    {
        var job1 = MakeJob(highlightId: 1, tickStart: 5000,  tickEnd: 6000);
        var job2 = MakeJob(highlightId: 2, tickStart: 15000, tickEnd: 16000);

        var results = _builder.Build([job1, job2],
            Path.Combine(_tempDir, "cfg"),
            Path.Combine(_tempDir, "clips"));
        var content = File.ReadAllText(results[0].CfgPath);

        var startCount = content.Split("mirv_streams record start").Length - 1;
        Assert.That(startCount, Is.EqualTo(2));
    }

    [Test]
    public void Only_last_clip_stop_command_includes_quit()
    {
        var job1 = MakeJob(highlightId: 1, tickStart: 5000,  tickEnd: 6000);
        var job2 = MakeJob(highlightId: 2, tickStart: 15000, tickEnd: 16000);

        var results = _builder.Build([job1, job2],
            Path.Combine(_tempDir, "cfg"),
            Path.Combine(_tempDir, "clips"));
        var content = File.ReadAllText(results[0].CfgPath);

        Assert.That(content, Does.Contain("record end; quit"));
        // "record end" without quit appears exactly once (for the non-last clip)
        var endOnlyCount = content.Split('\n')
            .Count(line => line.Contains("record end") && !line.Contains("quit"));
        Assert.That(endOnlyCount, Is.EqualTo(1));
    }

    [Test]
    public void Jobs_sorted_by_tick_regardless_of_input_order()
    {
        // Job2 has earlier ticks but is passed second
        var job1 = MakeJob(highlightId: 1, tickStart: 15000, tickEnd: 16000);
        var job2 = MakeJob(highlightId: 2, tickStart: 5000,  tickEnd: 6000);

        var results = _builder.Build([job1, job2],
            Path.Combine(_tempDir, "cfg"),
            Path.Combine(_tempDir, "clips"));
        var content = File.ReadAllText(results[0].CfgPath);

        // Seek tick is based on the earlier clip (job2: bufStart ~4680, seek ~4616)
        Assert.That(content, Does.Contain("demo_gototick 4616"));
    }

    [Test]
    public void Clip_highlightId_and_path_match_each_job()
    {
        var job1 = MakeJob(highlightId: 10, tickStart: 5000,  tickEnd: 6000);
        var job2 = MakeJob(highlightId: 20, tickStart: 15000, tickEnd: 16000);

        var results = _builder.Build([job1, job2],
            Path.Combine(_tempDir, "cfg"),
            Path.Combine(_tempDir, "clips"));
        var clips = results[0].Clips;

        Assert.That(clips.Select(c => c.HighlightId), Is.EquivalentTo(new[] { 10, 20 }));
        Assert.That(clips[0].ClipPath, Does.Contain("clip_10"));
        Assert.That(clips[1].ClipPath, Does.Contain("clip_20"));
    }
}
