using System;
using System.Collections.Generic;
using System.Linq;
using ShaderPlayground.Core.Compilers.Angle;
using ShaderPlayground.Core.Compilers.Clspv;
using ShaderPlayground.Core.Compilers.Dxc;
using ShaderPlayground.Core.Compilers.Fxc;
using ShaderPlayground.Core.Compilers.Glslang;
using ShaderPlayground.Core.Compilers.GlslOptimizer;
using ShaderPlayground.Core.Compilers.Hlsl2Glsl;
using ShaderPlayground.Core.Compilers.HlslCc;
using ShaderPlayground.Core.Compilers.HlslParser;
using ShaderPlayground.Core.Compilers.IntelShaderAnalyzer;
using ShaderPlayground.Core.Compilers.Lzma;
using ShaderPlayground.Core.Compilers.Mali;
using ShaderPlayground.Core.Compilers.Metal;
using ShaderPlayground.Core.Compilers.Miniz;
using ShaderPlayground.Core.Compilers.Naga;
using ShaderPlayground.Core.Compilers.PowerVR;
using ShaderPlayground.Core.Compilers.Rga;
using ShaderPlayground.Core.Compilers.RustGpu;
using ShaderPlayground.Core.Compilers.Slang;
using ShaderPlayground.Core.Compilers.Smolv;
using ShaderPlayground.Core.Compilers.SpirVCross;
using ShaderPlayground.Core.Compilers.SpirVCrossIspc;
using ShaderPlayground.Core.Compilers.SpirvTools;
using ShaderPlayground.Core.Compilers.Tint;
using ShaderPlayground.Core.Compilers.XShaderCompiler;
using ShaderPlayground.Core.Compilers.Yariv;
using ShaderPlayground.Core.Compilers.Zstd;
using ShaderPlayground.Core.Languages;

namespace ShaderPlayground.Core
{
    public static class Compiler
    {
        public static readonly IShaderLanguage[] AllLanguages =
        {
            new HlslLanguage(),
            new GlslLanguage(),
            new MetalLanguage(),
            new OpenCLCLanguage(),
            new RustLanguage(),
            new SlangLanguage(),
            new SpirvLanguage(),
            new WgslLanguage(),
        };

        public static readonly IShaderCompiler[] AllCompilers =
        {
            new ClspvCompiler(),
            new DxcCompiler(),
            new FxcCompiler(),
            new GlslangCompiler(),
            new GlslOptimizerCompiler(),
            new Hlsl2GlslCompiler(),
            new HlslCcCompiler(),
            new HlslParserCompiler(),
            new IntelShaderAnalyzerCompiler(),
            new LzmaCompiler(),
            new MaliCompiler(),
            new MetalCompiler(),
            new MetalLibCompiler(),
            new MetalShaderConverter(),
            new MinizCompiler(),
            new NagaCompiler(),
            new PowerVRCompiler(),
            new RgaCompiler(),
            new RustGpuCompiler(),
            new SlangCompiler(),
            new SmolvToSpirvCompiler(),
            new SpirvAssemblerCompiler(),
            new SpirVCrossCompiler(),
            new SpirVCrossIspcCompiler(),
            new SpirvCfgCompiler(),
            new SpirvOptCompiler(),
            new SpirvRemapCompiler(),
            new SpirvToSmolvCompiler(),
            new SpirvToYarivCompiler(),
            new TintCompiler(),
            new YarivToSpirvCompiler(),
            new XscCompiler(),
            new ZstdCompiler(),

            // Should be first alphabetically, but I want Glslang to be the default,
            // and I haven't implemented not-first defaults.
            new AngleCompiler(),
        };

        public static IReadOnlyList<ShaderCompilerResult> Compile(
            ShaderCode shaderCode,
            params CompilationStep[] compilationSteps)
        {
            if ((shaderCode.Text != null && shaderCode.Text.Length > 1000000) ||
                (shaderCode.Binary != null && shaderCode.Binary.Length > 1000000))
            {
                throw new InvalidOperationException("Code exceeded maximum length.");
            }

            if (compilationSteps.Length == 0 || compilationSteps.Length > 5)
            {
                throw new InvalidOperationException("There must > 0 and <= 5 compilation steps.");
            }

            var eachShaderCode = shaderCode;
            var results = new List<ShaderCompilerResult>();
            var previousArguments = new List<ShaderCompilerArguments>();

            var error = false;

            foreach (var compilationStep in compilationSteps)
            {
                if (error)
                {
                    results.Add(new ShaderCompilerResult(false, null, null, new ShaderCompilerOutput("Output", null, "Error in previous step")));
                    continue;
                }

                var compiler = AllCompilers.First(x => x.Name == compilationStep.CompilerName);

                if (!compiler.InputLanguages.Contains(eachShaderCode.Language))
                {
                    throw new InvalidOperationException($"Invalid input language '{eachShaderCode.Language}' for compiler '{compiler.DisplayName}'.");
                }

                var arguments = new ShaderCompilerArguments(
                    compiler,
                    compilationStep.Arguments);

                var result = compiler.Compile(eachShaderCode, arguments, previousArguments);

                previousArguments.Add(arguments);

                if (!result.Success)
                {
                    error = true;
                }

                results.Add(result);

                eachShaderCode = result.PipeableOutput;
            }

            return results;
        }
    }
}
