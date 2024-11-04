using System.Linq;
using Cake.Common.IO;
using Cake.Common.Net;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.DotNet.Test;
using Cake.Common.Tools.MSBuild;
using Cake.Compression;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.Git;
using static Utilities;

public sealed class PrepareBuildDirectory : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.EnsureDirectoryExists(context.ArtifactsPath);
        context.EnsureDirectoryExists("./src/ShaderPlayground.Core/Binaries");
        context.CleanDirectory("./src/ShaderPlayground.Core/Binaries");
    }
}

public sealed class DownloadDxc : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DownloadAndUnzipCompiler(
            "https://ci.appveyor.com/api/projects/dnovillo/directxshadercompiler/artifacts/build%2FRelease%2Fdxc-artifacts.zip?branch=main&pr=false&job=image%3A%20Visual%20Studio%202022",
            "dxc",
            "trunk",
            false,
            "bin/*.*");
    }
}

public sealed class DownloadGlslang : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DownloadAndUnzipCompiler(
            "https://github.com/KhronosGroup/glslang/releases/download/main-tot/glslang-master-windows-Release.zip",
            "glslang",
            "trunk",
            false,
            "bin/*.*");
    }
}

public sealed class DownloadMaliOfflineCompiler : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DownloadAndUnzipCompiler(
            "https://armkeil.blob.core.windows.net/developer/Files/downloads/opengl-es-open-cl-offline-compiler/6.2/Mali_Offline_Compiler_v6.2.0.7d271f_Windows_x64.zip",
            "mali",
            "6.2.0",
            true,
            "Mali_Offline_Compiler_v6.2.0/**/*.*");

        void DownloadMali(string mobileStudioVersion, string maliVersion, string url)
        {
            var armMobileStudioExePath = context.DownloadCompiler(
              url,
              "arm-mobile-studio",
              mobileStudioVersion,
              true);

            var mobileStudioFolder = $"{context.ArtifactsPath}/arm-mobile-studio/{mobileStudioVersion}";
            context.EnsureDirectoryExists(mobileStudioFolder);
            context.CleanDirectory(mobileStudioFolder);

            context.RunAndCheckResult(
              @"C:\Program Files\7-Zip\7z.exe",
              new ProcessSettings
              {
                  Arguments = $@"x -o""{mobileStudioFolder}"" ""{armMobileStudioExePath}"" ""mali_offline_compiler/*"" ""mali_offline_compiler/*/*"""
              });

            var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/mali/{maliVersion}";

            context.EnsureDirectoryExists(binariesFolder);
            context.CleanDirectory(binariesFolder);
            context.CopyFiles($"{mobileStudioFolder}/mali_offline_compiler/**/*.*", binariesFolder, true);
            context.DeleteDirectory(mobileStudioFolder, new DeleteDirectorySettings
            {
                Recursive = true,
                Force = true
            });
        }

        // Can't actually download in this script because the actual URLs require login.
        DownloadMali("2021.0", "7.3.0", "TODO");
        DownloadMali("2022.1", "7.6.0", "TODO");
    }
}

public sealed class DownloadMetalDeveloperTools : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // Doesn't actually work because this URL requires login.
        context.DownloadAndUnzipCompiler(
            "https://developer.apple.com/services-account/download?path=/WWDC_2021/Metal_For_Windows_2.0_beta/Metal_Developer_Tools2.0betaWindows.exe",
            "metal",
            "3.0-beta4",
            true,
            "macos/bin/metal.exe",
            ZipFormat.SevenZip);

        // Doesn't actually work because this URL requires login.
        context.DownloadAndUnzipCompiler(
            "https://developer.apple.com/services-account/download?path=/WWDC_2021/Metal_For_Windows_2.0_beta/Metal_Developer_Tools2.0betaWindows.exe",
            "metal",
            "4.1",
            true,
            "metal/macos/bin/metal.exe",
            ZipFormat.SevenZip);

        // Doesn't actually work because this URL requires login.
        context.DownloadAndUnzipCompiler(
            "https://developer.apple.com/services-account/download?path=/WWDC_2021/Metal_For_Windows_2.0_beta/Metal_Developer_Tools2.0betaWindows.exe",
            "metal-shader-converter",
            "beta",
            true,
            "bin/metal-shaderconverter.exe",
            ZipFormat.SevenZip);

        // Doesn't actually work because this URL requires login.
        context.DownloadAndUnzipCompiler(
            "https://developer.apple.com/services-account/download?path=/WWDC_2021/Metal_For_Windows_2.0_beta/Metal_Developer_Tools2.0betaWindows.exe",
            "metal-shader-converter",
            "1.1",
            true,
            "bin/metal-shaderconverter.exe",
            ZipFormat.SevenZip);
    }
}

public sealed class DownloadSpirvCross : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        void DownloadSpirvCross(string version, string hash)
        {
            context.DownloadAndUnzipCompiler(
              $"https://github.com/KhronosGroup/SPIRV-Cross/releases/download/{version}/spirv-cross-vs2017-64bit-{hash}.tar.gz",
              "spirv-cross",
              version,
              true,
              "bin/spirv-cross.exe",
              ZipFormat.GZip);
        }

        DownloadSpirvCross("2019-06-21", "b4e0163749");
        DownloadSpirvCross("2020-01-16", "f9818f0804");
        DownloadSpirvCross("2020-09-17", "8891bd3512");
        DownloadSpirvCross("2021-01-15", "9acb9ec31f");

        var unzippedFolder = context.DownloadAndUnzipCompiler(
          "https://github.com/KhronosGroup/SPIRV-Cross/archive/master.zip",
          "spirv-cross",
          "trunk",
          false);

        var srcDirectory = $"{unzippedFolder}/SPIRV-Cross-master";
        context.RunAndCheckResult(
            @"cmake.exe",
            new ProcessSettings
            {
                Arguments = ".",
                WorkingDirectory = srcDirectory
            });

        context.MSBuild(srcDirectory + "/SPIRV-Cross.vcxproj", context.CreateCppBuildSettings());

        var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/spirv-cross/trunk";
        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
          $"{srcDirectory}/{context.BuildConfiguration}/SPIRV-Cross.exe",
          binariesFolder,
          true);
    }
}

public sealed class DownloadSpirvCrossIspc : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var unzippedFolder = context.DownloadAndUnzipCompiler(
            "https://github.com/GameTechDev/SPIRV-Cross/archive/master-ispc.zip",
            "spirv-cross-ispc",
            "trunk",
            false);

        context.MSBuild(unzippedFolder + "/SPIRV-Cross-master-ispc/msvc/SPIRV-Cross.vcxproj", context.CreateCppBuildSettings());

        var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/spirv-cross-ispc/trunk";
        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
          $"{unzippedFolder}/SPIRV-Cross-master-ispc/msvc/{context.BuildConfiguration}/SPIRV-Cross.exe",
          binariesFolder,
          true);
    }
}

public sealed class DownloadSpirvTools : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // Hack to get the actual zip URL from the intermediate download page, which contains this HTML:
        // <meta http-equiv="refresh" content="0; url=https://storage.googleapis.com/spirv-tools/artifacts/prod/graphics_shader_compiler/spirv-tools/windows-msvc-2017-release/continuous/1336/20201217-105132/install.zip" />
        var downloadPageFileName = $"{context.ArtifactsPath}/spirv-tools-trunk.html";
        context.DownloadFile(
            "https://storage.googleapis.com/spirv-tools/badges/build_link_windows_vs2017_release.html",
            downloadPageFileName);
        var downloadPageHtml = System.IO.File.ReadAllText(downloadPageFileName);
        var downloadPageUrl = System.Text.RegularExpressions.Regex.Match(downloadPageHtml, "url=([\\s\\S]+)\"").Groups[1].Value;

        context.DownloadAndUnzipCompiler(
            downloadPageUrl,
            "spirv-tools",
            "trunk",
            false,
            "install/bin/*.*");
    }
}

public sealed class DownloadSpirvToolsLegacy : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DownloadAndUnzipCompiler(
            "https://github.com/KhronosGroup/SPIRV-Tools/releases/download/master-tot/SPIRV-Tools-master-windows-x64-Release.zip",
            "spirv-tools-legacy",
            "trunk",
            true,
            "bin/*.*");
    }
}

public sealed class DownloadXShaderCompiler : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DownloadAndUnzipCompiler(
            "https://github.com/LukasBanana/XShaderCompiler/releases/download/v0.10-alpha/Xsc-v0.10-alpha.zip",
            "xshadercompiler",
            "v0.10-alpha",
            true,
            "Xsc-v0.10-alpha/bin/Win32/xsc.exe");
    }
}

public sealed class DownloadSlang : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var cudaExePath = context.DownloadCompiler(
            "http://developer.download.nvidia.com/compute/cuda/11.0.2/local_installers/cuda_11.0.2_451.48_win10.exe",
            "cuda",
            "11.0.2",
            true);

        var cudaFolder = $"{context.ArtifactsPath}/cuda/11.0.2";
        context.EnsureDirectoryExists(cudaFolder);
        context.CleanDirectory(cudaFolder);

        void ExtractFile(string fileName)
        {
            context.RunAndCheckResult(
                @"C:\Program Files\7-Zip\7z.exe",
                new ProcessSettings
                {
                    Arguments = $@"e -o""{cudaFolder}"" ""{cudaExePath}"" cuda_nvrtc\nvrtc\bin\{fileName}"
                });
        }

        var nvrtcFiles = new[]
        {
            "nvrtc64_110_0.dll",
            "nvrtc-builtins64_110.dll"
        };

        foreach (var nvrtcFile in nvrtcFiles)
        {
            ExtractFile(nvrtcFile);
        }

        var nvrtcPaths = nvrtcFiles.Select(x => cudaFolder + "/" + x);

        void DownloadSlang(string version)
        {
            var binariesFolder = context.DownloadAndUnzipCompiler(
                $"https://github.com/shader-slang/slang/releases/download/v{version}/slang-{version}-win64.zip",
                "slang",
                $"v{version}",
                true,
                "bin/windows-x64/release/*.*");

            context.CopyFiles(nvrtcPaths, binariesFolder);

            context.CopyFiles($"./src/ShaderPlayground.Core/Binaries/dxc/trunk/dxcompiler.dll", binariesFolder);
        }

        DownloadSlang("0.10.24");
        DownloadSlang("0.10.25");
        DownloadSlang("0.10.26");
        DownloadSlang("0.11.18");
        DownloadSlang("0.13.10");
        DownloadSlang("0.18.0");
        DownloadSlang("0.18.25");
        DownloadSlang("0.24.20");
        DownloadSlang("2024.0.6");
        DownloadSlang("2024.14.4");
    }
}

public sealed class DownloadHlslParser : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var unzippedFolder = context.DownloadAndUnzipCompiler(
            "https://github.com/Thekla/hlslparser/archive/master.zip",
            "hlslparser",
            "trunk",
            false);

        context.MSBuild(unzippedFolder + "/hlslparser-master/hlslparser.vcxproj", context.CreateCppBuildSettings());

        var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/hlslparser/trunk";
        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
            $"{unzippedFolder}/hlslparser-master/{context.BuildConfiguration}/hlslparser.exe",
            binariesFolder,
            true);
    }
}

public sealed class DownloadZstd : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        void DownloadZstd(string version)
        {
            context.DownloadAndUnzipCompiler(
                $"https://github.com/facebook/zstd/releases/download/v{version}/zstd-v{version}-win64.zip",
                "zstd",
                $"v{version}",
                true,
                "zstd.exe");
        }

        DownloadZstd("1.3.4");
    }
}

public sealed class DownloadLzma : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        void DownloadLzma(string version, string displayVersion)
        {
            context.DownloadAndUnzipCompiler(
                $"https://www.7-zip.org/a/lzma{version}.7z",
                "lzma",
                displayVersion,
                true,
                @"bin\lzma.exe",
                ZipFormat.SevenZip);
        }

        DownloadLzma("1805", "18.05");
    }
}

public sealed class DownloadRga : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var amdDriverExePath = context.DownloadCompiler(
            "https://drivers.amd.com/drivers/beta/win10-radeon-software-adrenalin-2020-edition-20.8.3-sep8.exe",
            "amd-driver",
            "19.9.2",
            true);

        var amdDriverFolder = $"{context.ArtifactsPath}/amd-driver/22.1.2";
        context.EnsureDirectoryExists(amdDriverFolder);
        context.CleanDirectory(amdDriverFolder);

        void ExtractFile(string fileName)
        {
            context.RunAndCheckResult(
                @"C:\Program Files\7-Zip\7z.exe",
                new ProcessSettings
                {
                    Arguments = $@"e -o""{amdDriverFolder}"" ""{amdDriverExePath}"" Packages\Drivers\Display\WT6A_INF\B346681\{fileName}",
                });
        }

        ExtractFile("atidxx64.dll");
        ExtractFile("amdvlk64.dll");

        var driverDllPaths = new[]
        {
            amdDriverFolder + "/atidxx64.dll",
            amdDriverFolder + "/amdvlk64.dll"
        };

        void DownloadRga(string version, string filesToCopy)
        {
            var binariesFolder = context.DownloadAndUnzipCompiler(
              $"https://github.com/GPUOpen-Tools/radeon_gpu_analyzer/releases/download/{version}/rga-windows-x64-{version}.zip",
              "rga",
              version,
              true,
              filesToCopy);

            context.CopyFiles(driverDllPaths, binariesFolder);

            context.CopyFiles("./lib/x64/d3dcompiler_47.dll", binariesFolder);
        }

        DownloadRga("2.0.1", "bin/**/*.*");
        DownloadRga("2.1", "**/*.*");
        DownloadRga("2.2", "**/*.*");
        DownloadRga("2.3", "**/*.*");
        DownloadRga("2.3.1", "**/*.*");
        DownloadRga("2.4", "**/*.*");
        DownloadRga("2.4.1", "**/*.*");
        DownloadRga("2.6", "**/*.*");
        DownloadRga("2.6.1", "**/*.*");
        DownloadRga("2.6.2", "**/*.*");
        DownloadRga("2.7.1", "**/*.*");
    }
}

public sealed class DownloadIntelShaderAnalyzer : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DownloadAndUnzipCompiler(
            "https://github.com/GameTechDev/IntelShaderAnalyzer/releases/download/v1/IntelShaderAnalyzer_v1.zip",
            "intelshaderanalyzer",
            "v1",
            true,
            "IntelShaderAnalyzer/*.*");
    }
}

public sealed class CopyPowerVR : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        void CopyVersion(string version)
        {
            var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/powervr/{version}";
            context.EnsureDirectoryExists(binariesFolder);
            context.CleanDirectory(binariesFolder);

            context.CopyFiles($"./lib/PowerVR/{version}/*.*", binariesFolder);
        }

        CopyVersion("2018 R1");
    }
}

public sealed class BuildAngle : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.RunAndCheckResult(context.MakeAbsolute(context.File("./external/angle/build.bat")), new ProcessSettings
        {
            WorkingDirectory = context.MakeAbsolute(context.Directory("./external/angle"))
        });

        var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/angle/trunk";
        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
            "./external/angle/source/out/Release/angle_shader_translator.exe",
            binariesFolder,
            true);

        context.CopyFiles(
            "./external/angle/source/out/Release/libc++.dll",
            binariesFolder,
            true);
    }
}

public sealed class BuildClspv : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.RunAndCheckResult(context.MakeAbsolute(context.File("./external/clspv/build.bat")), new ProcessSettings
        {
            WorkingDirectory = context.MakeAbsolute(context.Directory("./external/clspv"))
        });

        var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/clspv/trunk";
        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
            "./external/clspv/source/build/bin/Release/clspv.exe",
            binariesFolder,
            true);
    }
}

public sealed class BuildTint : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.RunAndCheckResult(context.MakeAbsolute(context.File("./external/tint/build.bat")), new ProcessSettings
        {
            WorkingDirectory = context.MakeAbsolute(context.Directory("./external/tint"))
        });

        var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/tint/trunk";
        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
            "./external/tint/source/build/Release/tint.exe",
            binariesFolder,
            true);
    }
}

public sealed class BuildRustGpu : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // TODO: Actually build from source.

        var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/rust-gpu/trunk";
        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
          $"{context.ArtifactsPath}/rust-gpu/trunk/**/*.*",
          binariesFolder,
          true);
    }
}

public sealed class BuildNaga : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        void BuildNagaLegacy()
        {
            var repoPath = $"{context.ArtifactsPath}/naga-legacy";
            if (context.DirectoryExists(repoPath))
            {
                context.DeleteDirectory(repoPath, new DeleteDirectorySettings { Recursive = true });
            }
            context.GitClone("https://github.com/gfx-rs/naga.git", repoPath);

            void BuildVersion(string commit, string displayName)
            {
                context.GitCheckout(repoPath, commit);

                context.RunAndCheckResult(
                    "cargo",
                    new ProcessSettings
                    {
                        Arguments = "build --release",
                        WorkingDirectory = repoPath,
                    });

                var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/naga/{displayName}";
                context.EnsureDirectoryExists(binariesFolder);
                context.CleanDirectory(binariesFolder);

                context.CopyFiles(
                    $"{repoPath}/target/release/naga.*",
                    binariesFolder,
                    false);
            }

            BuildVersion("8376bab5622f89ed9689cd0c3aedfd97c333c5bf", "v0.5.0");
            BuildVersion("3a2f7e611e4fe8ec50d9bb365916f22e7c30e46c", "v0.7.0");
        }

        void BuildNaga()
        {
            var repoPath = $"{context.ArtifactsPath}/naga";
            if (context.DirectoryExists(repoPath))
            {
                context.DeleteDirectory(repoPath, new DeleteDirectorySettings { Recursive = true });
            }
            context.GitClone("https://github.com/gfx-rs/wgpu.git", repoPath);

            void BuildVersion(string tag)
            {
                context.GitCheckout(repoPath, tag);

                context.RunAndCheckResult(
                    "cargo",
                    new ProcessSettings
                    {
                        Arguments = "build --release",
                        WorkingDirectory = $"{repoPath}/naga-cli",
                    });

                var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/naga/{tag}";
                context.EnsureDirectoryExists(binariesFolder);
                context.CleanDirectory(binariesFolder);

                context.CopyFiles(
                    $"{repoPath}/target/release/naga.*",
                    binariesFolder,
                    false);
            }

            BuildVersion("v0.19.3");
        }

        BuildNagaLegacy();
        BuildNaga();
    }
}

public sealed class BuildFxcShim : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetPublish("./shims/ShaderPlayground.Shims.Fxc/ShaderPlayground.Shims.Fxc.csproj", new DotNetPublishSettings
        {
            Configuration = context.BuildConfiguration
        });

        var fxcVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo("./lib/x64/d3dcompiler_47.dll").ProductVersion;
        var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/fxc/{fxcVersion}";

        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);
        context.CopyFiles(
            $"./shims/ShaderPlayground.Shims.Fxc/bin/{context.BuildConfiguration}/netcoreapp3.1/publish/**/*.*",
            binariesFolder,
            true);
    }
}

public sealed class BuildHlslCcShim : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.MSBuild("./shims/ShaderPlayground.Shims.HLSLcc/ShaderPlayground.Shims.HLSLcc.vcxproj", context.CreateCppBuildSettings());

        var binariesFolder = "./src/ShaderPlayground.Core/Binaries/hlslcc/trunk";

        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
            $"./shims/ShaderPlayground.Shims.HLSLcc/{context.BuildConfiguration}/ShaderPlayground.Shims.HLSLcc.exe",
            binariesFolder,
            true);
    }
}

public sealed class BuildGlslOptimizerShim : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.MSBuild(
            "./shims/ShaderPlayground.Shims.GlslOptimizer/Source/projects/vs2010/glsl_optimizer_lib.vcxproj", 
            context.CreateCppBuildSettings().SetPlatformTarget(PlatformTarget.Win32));

        context.MSBuild(
            "./shims/ShaderPlayground.Shims.GlslOptimizer/ShaderPlayground.Shims.GlslOptimizer.vcxproj",
            context.CreateCppBuildSettings());

        var binariesFolder = "./src/ShaderPlayground.Core/Binaries/glsl-optimizer/trunk";

        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
            $"./shims/ShaderPlayground.Shims.GlslOptimizer/{context.BuildConfiguration}/ShaderPlayground.Shims.GlslOptimizer.exe",
            binariesFolder,
            true);
    }
}

public sealed class BuildHlsl2GlslShim : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.MSBuild(
            "./shims/ShaderPlayground.Shims.Hlsl2Glsl/Source/hlslang.vcxproj", 
            context.CreateCppBuildSettings().SetPlatformTarget(PlatformTarget.Win32));

        context.MSBuild(
            "./shims/ShaderPlayground.Shims.Hlsl2Glsl/ShaderPlayground.Shims.Hlsl2Glsl.vcxproj",
            context.CreateCppBuildSettings());

        var binariesFolder = "./src/ShaderPlayground.Core/Binaries/hlsl2glsl/trunk";

        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
            $"./shims/ShaderPlayground.Shims.Hlsl2Glsl/{context.BuildConfiguration}/ShaderPlayground.Shims.Hlsl2Glsl.exe",
            binariesFolder,
            true);
    }
}

public sealed class BuildSmolvShim : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.MSBuild("./shims/ShaderPlayground.Shims.Smolv/ShaderPlayground.Shims.Smolv.vcxproj", context.CreateCppBuildSettings());

        var binariesFolder = "./src/ShaderPlayground.Core/Binaries/smol-v/trunk";

        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
          $"./shims/ShaderPlayground.Shims.Smolv/{context.BuildConfiguration}/ShaderPlayground.Shims.Smolv.exe",
          binariesFolder,
          true);
    }
}

public sealed class BuildMinizShim : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.MSBuild("./shims/ShaderPlayground.Shims.Miniz/ShaderPlayground.Shims.Miniz.vcxproj", context.CreateCppBuildSettings());

        var binariesFolder = "./src/ShaderPlayground.Core/Binaries/miniz/2.0.7";

        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
          $"./shims/ShaderPlayground.Shims.Miniz/{context.BuildConfiguration}/ShaderPlayground.Shims.Miniz.exe",
          binariesFolder,
          true);
    }
}

public sealed class BuildYarivShim : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.MSBuild("./shims/ShaderPlayground.Shims.Yariv/ShaderPlayground.Shims.Yariv.vcxproj", context.CreateCppBuildSettings());

        var binariesFolder = "./src/ShaderPlayground.Core/Binaries/yari-v/trunk";

        context.EnsureDirectoryExists(binariesFolder);
        context.CleanDirectory(binariesFolder);

        context.CopyFiles(
          $"./shims/ShaderPlayground.Shims.Yariv/{context.BuildConfiguration}/ShaderPlayground.Shims.Yariv.exe",
          binariesFolder,
          true);
    }
}

[IsDependentOn(typeof(BuildFxcShim))]
[IsDependentOn(typeof(BuildHlslCcShim))]
[IsDependentOn(typeof(BuildGlslOptimizerShim))]
[IsDependentOn(typeof(BuildHlsl2GlslShim))]
[IsDependentOn(typeof(BuildSmolvShim))]
[IsDependentOn(typeof(BuildMinizShim))]
[IsDependentOn(typeof(BuildYarivShim))]
public sealed class BuildShims : FrostingTask
{
    
}

[IsDependentOn(typeof(BuildShims))]
public sealed class Build : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var outputFolder = $"{context.ArtifactsPath}/site";
        context.EnsureDirectoryExists(outputFolder);
        context.CleanDirectory(outputFolder);

        context.DotNetPublish("./src/ShaderPlayground.Web/ShaderPlayground.Web.csproj", new DotNetPublishSettings
        {
            Configuration = context.BuildConfiguration,
            OutputDirectory = outputFolder
        });

        context.ZipCompress(outputFolder, $"{context.ArtifactsPath}/site.zip");
    }
}

[IsDependentOn(typeof(Build))]
public sealed class Test : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetTest("./src/ShaderPlayground.Core.Tests/ShaderPlayground.Core.Tests.csproj", new DotNetTestSettings
        {
            Configuration = context.BuildConfiguration
        });
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(PrepareBuildDirectory))]
[IsDependentOn(typeof(DownloadDxc))]
[IsDependentOn(typeof(DownloadGlslang))]
[IsDependentOn(typeof(DownloadMaliOfflineCompiler))]
[IsDependentOn(typeof(DownloadMetalDeveloperTools))]
[IsDependentOn(typeof(DownloadSpirvCross))]
[IsDependentOn(typeof(DownloadSpirvCrossIspc))]
[IsDependentOn(typeof(DownloadSpirvTools))]
[IsDependentOn(typeof(DownloadSpirvToolsLegacy))]
[IsDependentOn(typeof(DownloadXShaderCompiler))]
[IsDependentOn(typeof(DownloadSlang))]
[IsDependentOn(typeof(DownloadHlslParser))]
[IsDependentOn(typeof(DownloadZstd))]
[IsDependentOn(typeof(DownloadLzma))]
[IsDependentOn(typeof(DownloadRga))]
[IsDependentOn(typeof(DownloadIntelShaderAnalyzer))]
[IsDependentOn(typeof(CopyPowerVR))]
[IsDependentOn(typeof(BuildAngle))]
[IsDependentOn(typeof(BuildClspv))]
[IsDependentOn(typeof(BuildTint))]
[IsDependentOn(typeof(BuildRustGpu))]
[IsDependentOn(typeof(BuildNaga))]
[IsDependentOn(typeof(Build))]
[IsDependentOn(typeof(Test))]
public sealed class DefaultTask : FrostingTask
{

}
